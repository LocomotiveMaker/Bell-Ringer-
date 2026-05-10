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

    float applyStillnessDeadband(float valueDps)
    {
        return abs(valueDps) < GyroStillnessDeadbandDps ? 0.0f : valueDps;
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

    void updateOrientation(const ImuSample& sample, float deltaSeconds)
    {
        float integratedPitch = gPitchDegrees + (applyStillnessDeadband(sample.gyroYDps) * deltaSeconds);
        float integratedRoll = gRollDegrees + (applyStillnessDeadband(sample.gyroXDps) * deltaSeconds);

        float accelPitch = accelPitchDegrees(sample);
        float accelRoll = accelRollDegrees(sample);

        if (!gHasOrientation)
        {
            gPitchDegrees = accelPitch;
            gRollDegrees = accelRoll;
            gHasOrientation = true;
            return;
        }

        gPitchDegrees = (ComplementaryAlpha * integratedPitch) + ((1.0f - ComplementaryAlpha) * accelPitch);
        gRollDegrees = (ComplementaryAlpha * integratedRoll) + ((1.0f - ComplementaryAlpha) * accelRoll);
        gPitchDegrees = wrapDegrees(gPitchDegrees);
        gRollDegrees = wrapDegrees(gRollDegrees);
    }

    void printTelemetry()
    {
        Serial.print(F("hy=0,hp="));
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
    Serial.println(F("[HeadMPU] streaming 6-axis pitch/roll"));
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
