#include <Wire.h>

namespace
{
#if defined(ARDUINO_ARCH_ESP32)
    constexpr int I2cSdaPin = 11;
    constexpr int I2cSclPin = 12;
#endif
    constexpr uint8_t Mpu9250AddressLow = 0x68;
    constexpr uint8_t Mpu9250AddressHigh = 0x69;

    constexpr uint8_t RegisterWhoAmI = 0x75;
    constexpr uint8_t RegisterPowerManagement1 = 0x6B;
    constexpr uint8_t RegisterPowerManagement2 = 0x6C;
    constexpr uint8_t RegisterSampleRateDivider = 0x19;
    constexpr uint8_t RegisterConfig = 0x1A;
    constexpr uint8_t RegisterGyroConfig = 0x1B;
    constexpr uint8_t RegisterAccelConfig = 0x1C;
    constexpr uint8_t RegisterAccelConfig2 = 0x1D;
    constexpr uint8_t RegisterAccelXoutH = 0x3B;

    constexpr unsigned long SerialBaud = 230400UL;
    constexpr unsigned long SampleIntervalUs = 5000UL;
    constexpr uint16_t GyroCalibrationSamples = 600;
    constexpr float AccelScale = 16384.0f;
    constexpr float GyroScale = 65.5f;
    constexpr float ComplementaryAlpha = 0.985f;
    constexpr float GyroStillnessDeadbandDps = 0.25f;
    constexpr float MadgwickBeta = 0.08f;
    constexpr float StationaryAccelToleranceG = 0.06f;
    constexpr float StationaryGyroToleranceDps = 1.0f;
    constexpr float StationaryEnterSpeed = 5.5f;
    constexpr float StationaryExitSpeed = 8.0f;
    constexpr float GyroBiasAdaptSpeed = 0.75f;

    struct ImuSample
    {
        float accelXG;
        float accelYG;
        float accelZG;
        float rawGyroXDps;
        float rawGyroYDps;
        float rawGyroZDps;
        float gyroXDps;
        float gyroYDps;
        float gyroZDps;
        bool stationaryCandidate;
    };

    uint8_t gMpuAddress = 0;
    bool gHasOrientation = false;
    unsigned long gLastSampleMicros = 0;
    float gGyroBiasXDps = 0.0f;
    float gGyroBiasYDps = 0.0f;
    float gGyroBiasZDps = 0.0f;
    float gQuatW = 1.0f;
    float gQuatX = 0.0f;
    float gQuatY = 0.0f;
    float gQuatZ = 0.0f;
    float gYawDegrees = 0.0f;
    float gPitchDegrees = 0.0f;
    float gRollDegrees = 0.0f;
    float gStillnessBlend = 0.0f;

    bool writeRegister(uint8_t deviceAddress, uint8_t registerAddress, uint8_t value)
    {
        Wire.beginTransmission(deviceAddress);
        Wire.write(registerAddress);
        Wire.write(value);
        return Wire.endTransmission() == 0;
    }

    bool readRegisters(uint8_t deviceAddress, uint8_t startRegister, uint8_t count, uint8_t* buffer)
    {
        Wire.beginTransmission(deviceAddress);
        Wire.write(startRegister);
        if (Wire.endTransmission(false) != 0)
        {
            return false;
        }

        uint8_t received = Wire.requestFrom(deviceAddress, count);
        if (received != count)
        {
            return false;
        }

        for (uint8_t index = 0; index < count; ++index)
        {
            buffer[index] = Wire.read();
        }

        return true;
    }

    int16_t combineInt16(uint8_t highByte, uint8_t lowByte)
    {
        return static_cast<int16_t>((static_cast<uint16_t>(highByte) << 8) | lowByte);
    }

    bool detectMpuAddress()
    {
        uint8_t whoAmI = 0;
        if (readRegisters(Mpu9250AddressLow, RegisterWhoAmI, 1, &whoAmI))
        {
            gMpuAddress = Mpu9250AddressLow;
            return true;
        }

        if (readRegisters(Mpu9250AddressHigh, RegisterWhoAmI, 1, &whoAmI))
        {
            gMpuAddress = Mpu9250AddressHigh;
            return true;
        }

        return false;
    }

    bool initializeMpu9250()
    {
        if (gMpuAddress == 0)
        {
            return false;
        }

        if (!writeRegister(gMpuAddress, RegisterPowerManagement1, 0x00))
        {
            return false;
        }

        delay(100);

        return
            writeRegister(gMpuAddress, RegisterPowerManagement1, 0x01) &&
            writeRegister(gMpuAddress, RegisterPowerManagement2, 0x00) &&
            writeRegister(gMpuAddress, RegisterConfig, 0x03) &&
            writeRegister(gMpuAddress, RegisterSampleRateDivider, 0x04) &&
            writeRegister(gMpuAddress, RegisterGyroConfig, 0x08) &&
            writeRegister(gMpuAddress, RegisterAccelConfig, 0x00) &&
            writeRegister(gMpuAddress, RegisterAccelConfig2, 0x03);
    }

    bool readSample(ImuSample& sample)
    {
        uint8_t raw[14] = {0};
        if (!readRegisters(gMpuAddress, RegisterAccelXoutH, sizeof(raw), raw))
        {
            return false;
        }

        int16_t rawAx = combineInt16(raw[0], raw[1]);
        int16_t rawAy = combineInt16(raw[2], raw[3]);
        int16_t rawAz = combineInt16(raw[4], raw[5]);
        int16_t rawGx = combineInt16(raw[8], raw[9]);
        int16_t rawGy = combineInt16(raw[10], raw[11]);
        int16_t rawGz = combineInt16(raw[12], raw[13]);

        sample.accelXG = rawAx / AccelScale;
        sample.accelYG = rawAy / AccelScale;
        sample.accelZG = rawAz / AccelScale;
        sample.rawGyroXDps = rawGx / GyroScale;
        sample.rawGyroYDps = rawGy / GyroScale;
        sample.rawGyroZDps = rawGz / GyroScale;
        sample.gyroXDps = sample.rawGyroXDps - gGyroBiasXDps;
        sample.gyroYDps = sample.rawGyroYDps - gGyroBiasYDps;
        sample.gyroZDps = sample.rawGyroZDps - gGyroBiasZDps;

        float accelMagnitude = sqrt(
            (sample.accelXG * sample.accelXG) +
            (sample.accelYG * sample.accelYG) +
            (sample.accelZG * sample.accelZG));
        float gyroMagnitude = sqrt(
            (sample.gyroXDps * sample.gyroXDps) +
            (sample.gyroYDps * sample.gyroYDps) +
            (sample.gyroZDps * sample.gyroZDps));
        sample.stationaryCandidate =
            abs(accelMagnitude - 1.0f) <= StationaryAccelToleranceG &&
            gyroMagnitude <= StationaryGyroToleranceDps;
        return true;
    }

    bool calibrateGyro()
    {
        float sumX = 0.0f;
        float sumY = 0.0f;
        float sumZ = 0.0f;

        for (uint16_t index = 0; index < GyroCalibrationSamples; ++index)
        {
            uint8_t raw[14] = {0};
            if (!readRegisters(gMpuAddress, RegisterAccelXoutH, sizeof(raw), raw))
            {
                return false;
            }

            sumX += combineInt16(raw[8], raw[9]) / GyroScale;
            sumY += combineInt16(raw[10], raw[11]) / GyroScale;
            sumZ += combineInt16(raw[12], raw[13]) / GyroScale;
            delay(3);
        }

        gGyroBiasXDps = sumX / GyroCalibrationSamples;
        gGyroBiasYDps = sumY / GyroCalibrationSamples;
        gGyroBiasZDps = sumZ / GyroCalibrationSamples;
        return true;
    }

    float accelPitchDegrees(const ImuSample& sample)
    {
        return atan2(-sample.accelXG, sqrt((sample.accelYG * sample.accelYG) + (sample.accelZG * sample.accelZG))) * RAD_TO_DEG;
    }

    float accelRollDegrees(const ImuSample& sample)
    {
        return atan2(sample.accelYG, sample.accelZG) * RAD_TO_DEG;
    }

    float wrapDegrees(float degrees)
    {
        while (degrees > 180.0f)
        {
            degrees -= 360.0f;
        }

        while (degrees < -180.0f)
        {
            degrees += 360.0f;
        }

        return degrees;
    }

    float invSqrt(float value)
    {
        return 1.0f / sqrt(value);
    }

    void normalizeQuaternion(float& w, float& x, float& y, float& z)
    {
        float norm = invSqrt((w * w) + (x * x) + (y * y) + (z * z));
        w *= norm;
        x *= norm;
        y *= norm;
        z *= norm;
    }

    float applyStillnessDeadband(float valueDps)
    {
        return abs(valueDps) < GyroStillnessDeadbandDps ? 0.0f : valueDps;
    }

    void quaternionToYawPitchRollDegrees(float w, float x, float y, float z, float& yaw, float& pitch, float& roll)
    {
        float sinrCosp = 2.0f * ((w * x) + (y * z));
        float cosrCosp = 1.0f - (2.0f * ((x * x) + (y * y)));
        roll = atan2(sinrCosp, cosrCosp) * RAD_TO_DEG;

        float sinp = 2.0f * ((w * y) - (z * x));
        pitch = abs(sinp) >= 1.0f
            ? (sinp < 0.0f ? -90.0f : 90.0f)
            : asin(sinp) * RAD_TO_DEG;

        float sinyCosp = 2.0f * ((w * z) + (x * y));
        float cosyCosp = 1.0f - (2.0f * ((y * y) + (z * z)));
        yaw = atan2(sinyCosp, cosyCosp) * RAD_TO_DEG;
    }

    void updateStillnessAndGyroBias(const ImuSample& sample, float deltaSeconds)
    {
        float targetStillness = sample.stationaryCandidate ? 1.0f : 0.0f;
        float blendSpeed = sample.stationaryCandidate ? StationaryEnterSpeed : StationaryExitSpeed;
        float blendFactor = min(1.0f, blendSpeed * deltaSeconds);
        gStillnessBlend += (targetStillness - gStillnessBlend) * blendFactor;

        if (gStillnessBlend < 0.82f)
        {
            return;
        }

        float biasBlend = min(1.0f, GyroBiasAdaptSpeed * deltaSeconds * gStillnessBlend);
        gGyroBiasXDps += (sample.rawGyroXDps - gGyroBiasXDps) * biasBlend;
        gGyroBiasYDps += (sample.rawGyroYDps - gGyroBiasYDps) * biasBlend;
        gGyroBiasZDps += (sample.rawGyroZDps - gGyroBiasZDps) * biasBlend;
    }

    void madgwickUpdateImu(const ImuSample& sample, float deltaSeconds)
    {
        float q0 = gQuatW;
        float q1 = gQuatX;
        float q2 = gQuatY;
        float q3 = gQuatZ;
        float gyroHold = gStillnessBlend >= 0.82f ? 0.0f : 1.0f;
        float gx = applyStillnessDeadband(sample.gyroXDps) * gyroHold * DEG_TO_RAD;
        float gy = applyStillnessDeadband(sample.gyroYDps) * gyroHold * DEG_TO_RAD;
        float gz = applyStillnessDeadband(sample.gyroZDps) * gyroHold * DEG_TO_RAD;
        float ax = sample.accelXG;
        float ay = sample.accelYG;
        float az = sample.accelZG;

        float qDot0 = 0.5f * ((-q1 * gx) - (q2 * gy) - (q3 * gz));
        float qDot1 = 0.5f * ((q0 * gx) + (q2 * gz) - (q3 * gy));
        float qDot2 = 0.5f * ((q0 * gy) - (q1 * gz) + (q3 * gx));
        float qDot3 = 0.5f * ((q0 * gz) + (q1 * gy) - (q2 * gx));

        if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
        {
            float recipNorm = invSqrt((ax * ax) + (ay * ay) + (az * az));
            ax *= recipNorm;
            ay *= recipNorm;
            az *= recipNorm;

            float twoQ0 = 2.0f * q0;
            float twoQ1 = 2.0f * q1;
            float twoQ2 = 2.0f * q2;
            float twoQ3 = 2.0f * q3;
            float fourQ0 = 4.0f * q0;
            float fourQ1 = 4.0f * q1;
            float fourQ2 = 4.0f * q2;
            float eightQ1 = 8.0f * q1;
            float eightQ2 = 8.0f * q2;
            float q0q0 = q0 * q0;
            float q1q1 = q1 * q1;
            float q2q2 = q2 * q2;
            float q3q3 = q3 * q3;

            float s0 = (fourQ0 * q2q2) + (twoQ2 * ax) + (fourQ0 * q1q1) - (twoQ1 * ay);
            float s1 = (fourQ1 * q3q3) - (twoQ3 * ax) + (4.0f * q0q0 * q1) - (twoQ0 * ay) - fourQ1 + (eightQ1 * q1q1) + (eightQ1 * q2q2) + (fourQ1 * az);
            float s2 = (4.0f * q0q0 * q2) + (twoQ0 * ax) + (fourQ2 * q3q3) - (twoQ3 * ay) - fourQ2 + (eightQ2 * q1q1) + (eightQ2 * q2q2) + (fourQ2 * az);
            float s3 = (4.0f * q1q1 * q3) - (twoQ1 * ax) + (4.0f * q2q2 * q3) - (twoQ2 * ay);
            float gradientMagnitude = (s0 * s0) + (s1 * s1) + (s2 * s2) + (s3 * s3);
            if (gradientMagnitude > 0.000001f)
            {
                recipNorm = invSqrt(gradientMagnitude);
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                qDot0 -= MadgwickBeta * s0;
                qDot1 -= MadgwickBeta * s1;
                qDot2 -= MadgwickBeta * s2;
                qDot3 -= MadgwickBeta * s3;
            }
        }

        q0 += qDot0 * deltaSeconds;
        q1 += qDot1 * deltaSeconds;
        q2 += qDot2 * deltaSeconds;
        q3 += qDot3 * deltaSeconds;
        normalizeQuaternion(q0, q1, q2, q3);
        gQuatW = q0;
        gQuatX = q1;
        gQuatY = q2;
        gQuatZ = q3;
    }

    void updateOrientation(const ImuSample& sample, float deltaSeconds)
    {
        madgwickUpdateImu(sample, deltaSeconds);
        quaternionToYawPitchRollDegrees(gQuatW, gQuatX, gQuatY, gQuatZ, gYawDegrees, gPitchDegrees, gRollDegrees);
        gYawDegrees = wrapDegrees(gYawDegrees);
        gPitchDegrees = wrapDegrees(gPitchDegrees);
        gRollDegrees = wrapDegrees(gRollDegrees);
        gHasOrientation = true;
    }

    void printTelemetry()
    {
        Serial.print(F("hy="));
        Serial.print(gYawDegrees, 2);
        Serial.print(F(",hp="));
        Serial.print(gPitchDegrees, 2);
        Serial.print(F(",hr="));
        Serial.print(gRollDegrees, 2);
        Serial.print(F(",st="));
        Serial.print(gStillnessBlend, 2);
        Serial.print(F(",wy=0,wp=0,wr=0,btn=0"));
        Serial.println();
    }
}

void setup()
{
    Serial.begin(SerialBaud);
    unsigned long serialWaitStartedAt = millis();
    while (!Serial && (millis() - serialWaitStartedAt) < 1500UL)
    {
        delay(10);
    }

    delay(200);
#if defined(ARDUINO_ARCH_ESP32)
    Wire.begin(I2cSdaPin, I2cSclPin);
#else
    Wire.begin();
#endif
    Wire.setClock(400000UL);

    if (!detectMpuAddress())
    {
        Serial.println(F("[HeadMPU] ERROR no device at 0x68 or 0x69"));
        return;
    }

    if (!initializeMpu9250())
    {
        Serial.println(F("[HeadMPU] ERROR initialization failed"));
        return;
    }

    Serial.println(F("[HeadMPU] hold still for gyro calibration"));
    if (!calibrateGyro())
    {
        Serial.println(F("[HeadMPU] ERROR gyro calibration failed"));
        return;
    }

    gLastSampleMicros = micros();
    Serial.println(F("[HeadMPU] streaming 6-axis yaw/pitch/roll"));
}

void loop()
{
    if (gMpuAddress == 0)
    {
        delay(50);
        return;
    }

    unsigned long nowMicros = micros();
    unsigned long elapsedMicros = nowMicros - gLastSampleMicros;
    if (elapsedMicros < SampleIntervalUs)
    {
        return;
    }

    gLastSampleMicros = nowMicros;

    ImuSample sample = {};
    if (!readSample(sample))
    {
        Serial.println(F("[HeadMPU] ERROR sample read failed"));
        delay(5);
        return;
    }

    float deltaSeconds = elapsedMicros * 0.000001f;
    updateStillnessAndGyroBias(sample, deltaSeconds);
    updateOrientation(sample, deltaSeconds);
    printTelemetry();
}
