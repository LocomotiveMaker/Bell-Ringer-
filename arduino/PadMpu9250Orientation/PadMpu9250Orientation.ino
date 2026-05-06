#include <Wire.h>

namespace
{
    constexpr uint8_t Mpu9250AddressLow = 0x68;
    constexpr uint8_t Mpu9250AddressHigh = 0x69;
    constexpr uint8_t Ak8963Address = 0x0C;

    constexpr uint8_t RegisterWhoAmI = 0x75;
    constexpr uint8_t RegisterPowerManagement1 = 0x6B;
    constexpr uint8_t RegisterPowerManagement2 = 0x6C;
    constexpr uint8_t RegisterSampleRateDivider = 0x19;
    constexpr uint8_t RegisterConfig = 0x1A;
    constexpr uint8_t RegisterGyroConfig = 0x1B;
    constexpr uint8_t RegisterAccelConfig = 0x1C;
    constexpr uint8_t RegisterAccelConfig2 = 0x1D;
    constexpr uint8_t RegisterIntPinCfg = 0x37;
    constexpr uint8_t RegisterUserCtrl = 0x6A;
    constexpr uint8_t RegisterAccelXoutH = 0x3B;

    constexpr uint8_t AkWhoAmI = 0x00;
    constexpr uint8_t AkSt1 = 0x02;
    constexpr uint8_t AkXoutL = 0x03;
    constexpr uint8_t AkCntl1 = 0x0A;
    constexpr uint8_t AkCntl2 = 0x0B;
    constexpr uint8_t AkAsax = 0x10;

    constexpr unsigned long SerialBaud = 115200UL;
    constexpr unsigned long SampleIntervalUs = 10000UL;
    constexpr uint16_t GyroCalibrationSamples = 300;
    constexpr float AccelScale = 16384.0f;
    constexpr float GyroScale = 131.0f;
    constexpr float MagScaleUt = 0.15f;
    constexpr float MadgwickBeta = 0.12f;

    struct ImuSample
    {
        float accelXG;
        float accelYG;
        float accelZG;
        float gyroXDps;
        float gyroYDps;
        float gyroZDps;
        float magXUt;
        float magYUt;
        float magZUt;
        bool hasMagnetometer;
    };

    uint8_t gMpuAddress = 0;
    bool gHasOrientation = false;
    bool gHasMagnetometer = false;
    bool gHasLastMagSample = false;
    unsigned long gLastSampleMicros = 0;
    float gGyroBiasXDps = 0.0f;
    float gGyroBiasYDps = 0.0f;
    float gGyroBiasZDps = 0.0f;
    float gMagAdjustX = 1.0f;
    float gMagAdjustY = 1.0f;
    float gMagAdjustZ = 1.0f;
    float gLastMagXUt = 0.0f;
    float gLastMagYUt = 0.0f;
    float gLastMagZUt = 0.0f;
    float gQuatW = 1.0f;
    float gQuatX = 0.0f;
    float gQuatY = 0.0f;
    float gQuatZ = 0.0f;
    float gQuatOffsetW = 1.0f;
    float gQuatOffsetX = 0.0f;
    float gQuatOffsetY = 0.0f;
    float gQuatOffsetZ = 0.0f;
    float gYawDegrees = 0.0f;
    float gPitchDegrees = 0.0f;
    float gRollDegrees = 0.0f;
    char gCommandBuffer[32] = {0};
    uint8_t gCommandLength = 0;

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

    bool writeAkRegister(uint8_t registerAddress, uint8_t value)
    {
        return writeRegister(Ak8963Address, registerAddress, value);
    }

    bool readAkRegisters(uint8_t startRegister, uint8_t count, uint8_t* buffer)
    {
        return readRegisters(Ak8963Address, startRegister, count, buffer);
    }

    int16_t combineInt16(uint8_t highByte, uint8_t lowByte)
    {
        return static_cast<int16_t>((static_cast<uint16_t>(highByte) << 8) | lowByte);
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

    void multiplyQuaternions(
        float aw, float ax, float ay, float az,
        float bw, float bx, float by, float bz,
        float& outW, float& outX, float& outY, float& outZ)
    {
        outW = (aw * bw) - (ax * bx) - (ay * by) - (az * bz);
        outX = (aw * bx) + (ax * bw) + (ay * bz) - (az * by);
        outY = (aw * by) - (ax * bz) + (ay * bw) + (az * bx);
        outZ = (aw * bz) + (ax * by) - (ay * bx) + (az * bw);
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

    bool probeDevice(uint8_t address, uint8_t& whoAmI)
    {
        return readRegisters(address, RegisterWhoAmI, 1, &whoAmI);
    }

    bool detectMpuAddress()
    {
        uint8_t whoAmI = 0;
        if (probeDevice(Mpu9250AddressLow, whoAmI))
        {
            gMpuAddress = Mpu9250AddressLow;
            Serial.print(F("[PadMPU] Found candidate at 0x68 WHO_AM_I=0x"));
            Serial.println(whoAmI, HEX);
            return true;
        }

        if (probeDevice(Mpu9250AddressHigh, whoAmI))
        {
            gMpuAddress = Mpu9250AddressHigh;
            Serial.print(F("[PadMPU] Found candidate at 0x69 WHO_AM_I=0x"));
            Serial.println(whoAmI, HEX);
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

        if (!writeRegister(gMpuAddress, RegisterPowerManagement1, 0x01) ||
            !writeRegister(gMpuAddress, RegisterPowerManagement2, 0x00) ||
            !writeRegister(gMpuAddress, RegisterConfig, 0x03) ||
            !writeRegister(gMpuAddress, RegisterSampleRateDivider, 0x04) ||
            !writeRegister(gMpuAddress, RegisterGyroConfig, 0x00) ||
            !writeRegister(gMpuAddress, RegisterAccelConfig, 0x00) ||
            !writeRegister(gMpuAddress, RegisterAccelConfig2, 0x03))
        {
            return false;
        }

        return true;
    }

    bool initializeAk8963()
    {
        if (!writeRegister(gMpuAddress, RegisterUserCtrl, 0x00) ||
            !writeRegister(gMpuAddress, RegisterIntPinCfg, 0x02))
        {
            return false;
        }

        uint8_t whoAmI = 0;
        if (!readAkRegisters(AkWhoAmI, 1, &whoAmI) || whoAmI != 0x48)
        {
            return false;
        }

        writeAkRegister(AkCntl1, 0x00);
        delay(10);
        writeAkRegister(AkCntl2, 0x01);
        delay(10);
        writeAkRegister(AkCntl1, 0x0F);
        delay(10);

        uint8_t asa[3] = {0};
        if (!readAkRegisters(AkAsax, 3, asa))
        {
            return false;
        }

        gMagAdjustX = ((static_cast<float>(asa[0]) - 128.0f) / 256.0f) + 1.0f;
        gMagAdjustY = ((static_cast<float>(asa[1]) - 128.0f) / 256.0f) + 1.0f;
        gMagAdjustZ = ((static_cast<float>(asa[2]) - 128.0f) / 256.0f) + 1.0f;

        writeAkRegister(AkCntl1, 0x00);
        delay(10);
        if (!writeAkRegister(AkCntl1, 0x16))
        {
            return false;
        }

        delay(10);
        return true;
    }

    bool readImuSample(ImuSample& sample)
    {
        sample.hasMagnetometer = false;
        sample.magXUt = 0.0f;
        sample.magYUt = 0.0f;
        sample.magZUt = 0.0f;

        uint8_t raw[14] = {0};
        if (!readRegisters(gMpuAddress, RegisterAccelXoutH, sizeof(raw), raw))
        {
            return false;
        }

        int16_t ax = combineInt16(raw[0], raw[1]);
        int16_t ay = combineInt16(raw[2], raw[3]);
        int16_t az = combineInt16(raw[4], raw[5]);
        int16_t gx = combineInt16(raw[8], raw[9]);
        int16_t gy = combineInt16(raw[10], raw[11]);
        int16_t gz = combineInt16(raw[12], raw[13]);

        sample.accelXG = ax / AccelScale;
        sample.accelYG = ay / AccelScale;
        sample.accelZG = az / AccelScale;
        sample.gyroXDps = (gx / GyroScale) - gGyroBiasXDps;
        sample.gyroYDps = (gy / GyroScale) - gGyroBiasYDps;
        sample.gyroZDps = (gz / GyroScale) - gGyroBiasZDps;
        return true;
    }

    void readMagnetometer(ImuSample& sample)
    {
        if (!gHasMagnetometer)
        {
            return;
        }

        uint8_t st1 = 0;
        if (readAkRegisters(AkSt1, 1, &st1) && (st1 & 0x01) != 0)
        {
            uint8_t raw[7] = {0};
            if (readAkRegisters(AkXoutL, 7, raw) && (raw[6] & 0x08) == 0)
            {
                int16_t rawX = combineInt16(raw[1], raw[0]);
                int16_t rawY = combineInt16(raw[3], raw[2]);
                int16_t rawZ = combineInt16(raw[5], raw[4]);

                // Common MPU9250 breakout alignment.
                gLastMagXUt = rawY * MagScaleUt * gMagAdjustY;
                gLastMagYUt = rawX * MagScaleUt * gMagAdjustX;
                gLastMagZUt = -rawZ * MagScaleUt * gMagAdjustZ;
                gHasLastMagSample = true;
            }
        }

        if (!gHasLastMagSample)
        {
            return;
        }

        sample.hasMagnetometer = true;
        sample.magXUt = gLastMagXUt;
        sample.magYUt = gLastMagYUt;
        sample.magZUt = gLastMagZUt;
    }

    void recenterNow()
    {
        normalizeQuaternion(gQuatW, gQuatX, gQuatY, gQuatZ);
        gQuatOffsetW = gQuatW;
        gQuatOffsetX = -gQuatX;
        gQuatOffsetY = -gQuatY;
        gQuatOffsetZ = -gQuatZ;
        gYawDegrees = 0.0f;
        gPitchDegrees = 0.0f;
        gRollDegrees = 0.0f;
        Serial.println(F("[PadMPU] recentered"));
    }

    void processSerialCommands()
    {
        while (Serial.available() > 0)
        {
            char c = static_cast<char>(Serial.read());
            if (c == '\r')
            {
                continue;
            }

            if (c == '\n')
            {
                gCommandBuffer[gCommandLength] = '\0';
                if (gCommandLength > 0 &&
                    (strcmp(gCommandBuffer, "r") == 0 ||
                     strcmp(gCommandBuffer, "R") == 0 ||
                     strcmp(gCommandBuffer, "recenter") == 0 ||
                     strcmp(gCommandBuffer, "RECENTER") == 0))
                {
                    recenterNow();
                }

                gCommandLength = 0;
                continue;
            }

            if (gCommandLength + 1 < sizeof(gCommandBuffer))
            {
                gCommandBuffer[gCommandLength++] = c;
            }
        }
    }

    void calibrateGyroBias()
    {
        Serial.println(F("[PadMPU] Keep the pad still. Calibrating gyro bias..."));

        float sumX = 0.0f;
        float sumY = 0.0f;
        float sumZ = 0.0f;
        uint16_t captured = 0;

        while (captured < GyroCalibrationSamples)
        {
            ImuSample sample = {};
            if (!readImuSample(sample))
            {
                delay(10);
                continue;
            }

            sumX += sample.gyroXDps + gGyroBiasXDps;
            sumY += sample.gyroYDps + gGyroBiasYDps;
            sumZ += sample.gyroZDps + gGyroBiasZDps;
            ++captured;
            delay(5);
        }

        gGyroBiasXDps = sumX / GyroCalibrationSamples;
        gGyroBiasYDps = sumY / GyroCalibrationSamples;
        gGyroBiasZDps = sumZ / GyroCalibrationSamples;
    }

    void madgwickUpdateImu(const ImuSample& sample, float deltaSeconds)
    {
        float q0 = gQuatW;
        float q1 = gQuatX;
        float q2 = gQuatY;
        float q3 = gQuatZ;

        float gx = sample.gyroXDps * DEG_TO_RAD;
        float gy = sample.gyroYDps * DEG_TO_RAD;
        float gz = sample.gyroZDps * DEG_TO_RAD;
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
            recipNorm = invSqrt((s0 * s0) + (s1 * s1) + (s2 * s2) + (s3 * s3));
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            qDot0 -= MadgwickBeta * s0;
            qDot1 -= MadgwickBeta * s1;
            qDot2 -= MadgwickBeta * s2;
            qDot3 -= MadgwickBeta * s3;
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

    void madgwickUpdateMarg(const ImuSample& sample, float deltaSeconds)
    {
        if (!sample.hasMagnetometer)
        {
            madgwickUpdateImu(sample, deltaSeconds);
            return;
        }

        float q0 = gQuatW;
        float q1 = gQuatX;
        float q2 = gQuatY;
        float q3 = gQuatZ;

        float gx = sample.gyroXDps * DEG_TO_RAD;
        float gy = sample.gyroYDps * DEG_TO_RAD;
        float gz = sample.gyroZDps * DEG_TO_RAD;
        float ax = sample.accelXG;
        float ay = sample.accelYG;
        float az = sample.accelZG;
        float mx = sample.magXUt;
        float my = sample.magYUt;
        float mz = sample.magZUt;

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

            recipNorm = invSqrt((mx * mx) + (my * my) + (mz * mz));
            mx *= recipNorm;
            my *= recipNorm;
            mz *= recipNorm;

            float twoQ0mx = 2.0f * q0 * mx;
            float twoQ0my = 2.0f * q0 * my;
            float twoQ0mz = 2.0f * q0 * mz;
            float twoQ1mx = 2.0f * q1 * mx;
            float twoQ0 = 2.0f * q0;
            float twoQ1 = 2.0f * q1;
            float twoQ2 = 2.0f * q2;
            float twoQ3 = 2.0f * q3;
            float q0q0 = q0 * q0;
            float q0q1 = q0 * q1;
            float q0q2 = q0 * q2;
            float q0q3 = q0 * q3;
            float q1q1 = q1 * q1;
            float q1q2 = q1 * q2;
            float q1q3 = q1 * q3;
            float q2q2 = q2 * q2;
            float q2q3 = q2 * q3;
            float q3q3 = q3 * q3;

            float hx = mx * q0q0 - twoQ0my * q3 + twoQ0mz * q2 + mx * q1q1 + twoQ1 * my * q2 + twoQ1 * mz * q3 - mx * q2q2 - mx * q3q3;
            float hy = twoQ0mx * q3 + my * q0q0 - twoQ0mz * q1 + twoQ1mx * q2 - my * q1q1 + my * q2q2 + twoQ2 * mz * q3 - my * q3q3;
            float twoBx = sqrt((hx * hx) + (hy * hy));
            float twoBz = -twoQ0mx * q2 + twoQ0my * q1 + mz * q0q0 + twoQ1mx * q3 - mz * q1q1 + twoQ2 * my * q3 - mz * q2q2 + mz * q3q3;
            float fourBx = 2.0f * twoBx;
            float fourBz = 2.0f * twoBz;

            float s0 =
                -twoQ2 * (2.0f * (q1q3 - q0q2) - ax) +
                twoQ1 * (2.0f * (q0q1 + q2q3) - ay) -
                twoBz * q2 * (twoBx * (0.5f - q2q2 - q3q3) + twoBz * (q1q3 - q0q2) - mx) +
                (-twoBx * q3 + twoBz * q1) * (twoBx * (q1q2 - q0q3) + twoBz * (q0q1 + q2q3) - my) +
                twoBx * q2 * (twoBx * (q0q2 + q1q3) + twoBz * (0.5f - q1q1 - q2q2) - mz);
            float s1 =
                twoQ3 * (2.0f * (q1q3 - q0q2) - ax) +
                twoQ0 * (2.0f * (q0q1 + q2q3) - ay) -
                4.0f * q1 * (1.0f - 2.0f * (q1q1 + q2q2) - az) +
                twoBz * q3 * (twoBx * (0.5f - q2q2 - q3q3) + twoBz * (q1q3 - q0q2) - mx) +
                (twoBx * q2 + twoBz * q0) * (twoBx * (q1q2 - q0q3) + twoBz * (q0q1 + q2q3) - my) +
                (twoBx * q3 - fourBz * q1) * (twoBx * (q0q2 + q1q3) + twoBz * (0.5f - q1q1 - q2q2) - mz);
            float s2 =
                -twoQ0 * (2.0f * (q1q3 - q0q2) - ax) +
                twoQ3 * (2.0f * (q0q1 + q2q3) - ay) -
                4.0f * q2 * (1.0f - 2.0f * (q1q1 + q2q2) - az) +
                (-fourBx * q2 - twoBz * q0) * (twoBx * (0.5f - q2q2 - q3q3) + twoBz * (q1q3 - q0q2) - mx) +
                (twoBx * q1 + twoBz * q3) * (twoBx * (q1q2 - q0q3) + twoBz * (q0q1 + q2q3) - my) +
                (twoBx * q0 - fourBz * q2) * (twoBx * (q0q2 + q1q3) + twoBz * (0.5f - q1q1 - q2q2) - mz);
            float s3 =
                twoQ1 * (2.0f * (q1q3 - q0q2) - ax) +
                twoQ2 * (2.0f * (q0q1 + q2q3) - ay) +
                (-fourBx * q3 + twoBz * q1) * (twoBx * (0.5f - q2q2 - q3q3) + twoBz * (q1q3 - q0q2) - mx) +
                (-twoBx * q0 + twoBz * q2) * (twoBx * (q1q2 - q0q3) + twoBz * (q0q1 + q2q3) - my) +
                twoBx * q1 * (twoBx * (q0q2 + q1q3) + twoBz * (0.5f - q1q1 - q2q2) - mz);

            recipNorm = invSqrt((s0 * s0) + (s1 * s1) + (s2 * s2) + (s3 * s3));
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            qDot0 -= MadgwickBeta * s0;
            qDot1 -= MadgwickBeta * s1;
            qDot2 -= MadgwickBeta * s2;
            qDot3 -= MadgwickBeta * s3;
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

    void getRelativeQuaternion(float& outW, float& outX, float& outY, float& outZ)
    {
        multiplyQuaternions(
            gQuatOffsetW, gQuatOffsetX, gQuatOffsetY, gQuatOffsetZ,
            gQuatW, gQuatX, gQuatY, gQuatZ,
            outW, outX, outY, outZ);
        normalizeQuaternion(outW, outX, outY, outZ);
    }

    void quaternionToYawPitchRollDegrees(float w, float x, float y, float z, float& yaw, float& pitch, float& roll)
    {
        float sinrCosp = 2.0f * ((w * x) + (y * z));
        float cosrCosp = 1.0f - (2.0f * ((x * x) + (y * y)));
        roll = atan2(sinrCosp, cosrCosp) * 180.0f / PI;

        float sinp = 2.0f * ((w * y) - (z * x));
        pitch = abs(sinp) >= 1.0f
            ? (sinp < 0.0f ? -90.0f : 90.0f)
            : asin(sinp) * 180.0f / PI;

        float sinyCosp = 2.0f * ((w * z) + (x * y));
        float cosyCosp = 1.0f - (2.0f * ((y * y) + (z * z)));
        yaw = atan2(sinyCosp, cosyCosp) * 180.0f / PI;
    }

    void updateOrientation(const ImuSample& sample, float deltaSeconds)
    {
        madgwickUpdateMarg(sample, deltaSeconds);

        float relativeW = 1.0f;
        float relativeX = 0.0f;
        float relativeY = 0.0f;
        float relativeZ = 0.0f;
        getRelativeQuaternion(relativeW, relativeX, relativeY, relativeZ);
        quaternionToYawPitchRollDegrees(relativeW, relativeX, relativeY, relativeZ, gYawDegrees, gPitchDegrees, gRollDegrees);
        gYawDegrees = wrapDegrees(gYawDegrees);
        gPitchDegrees = wrapDegrees(gPitchDegrees);
        gRollDegrees = wrapDegrees(gRollDegrees);
    }

    void printTelemetry()
    {
        float relativeW = 1.0f;
        float relativeX = 0.0f;
        float relativeY = 0.0f;
        float relativeZ = 0.0f;
        getRelativeQuaternion(relativeW, relativeX, relativeY, relativeZ);

        Serial.print(F("wy="));
        Serial.print(gYawDegrees, 2);
        Serial.print(F(",wp="));
        Serial.print(gPitchDegrees, 2);
        Serial.print(F(",wr="));
        Serial.print(gRollDegrees, 2);
        Serial.print(F(",wqw="));
        Serial.print(relativeW, 4);
        Serial.print(F(",wqx="));
        Serial.print(relativeX, 4);
        Serial.print(F(",wqy="));
        Serial.print(relativeY, 4);
        Serial.print(F(",wqz="));
        Serial.print(relativeZ, 4);
        Serial.println(F(",btn=0"));
    }
}

void setup()
{
    Serial.begin(SerialBaud);
    while (!Serial)
    {
    }

    delay(250);
    Serial.println();
    Serial.println(F("[PadMPU] orientation start"));

    Wire.begin();
    Wire.setClock(400000UL);

    if (!detectMpuAddress())
    {
        Serial.println(F("[PadMPU] ERROR no device at 0x68 or 0x69"));
        return;
    }

    if (!initializeMpu9250())
    {
        Serial.println(F("[PadMPU] ERROR initialization failed"));
        return;
    }

    gHasMagnetometer = initializeAk8963();
    calibrateGyroBias();
    gLastSampleMicros = micros();
    Serial.println(gHasMagnetometer ? F("[PadMPU] magnetometer ready") : F("[PadMPU] magnetometer missing"));
    Serial.println(F("[PadMPU] streaming wy/wp/wr + quaternion at 100 Hz"));
    Serial.println(F("[PadMPU] Send 'r' or 'recenter' to zero the current pose"));
}

void loop()
{
    processSerialCommands();

    if (gMpuAddress == 0)
    {
        delay(500);
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
    if (!readImuSample(sample))
    {
        return;
    }

    readMagnetometer(sample);
    updateOrientation(sample, elapsedMicros * 0.000001f);

    if (!gHasOrientation)
    {
        gHasOrientation = true;
        recenterNow();
    }

    printTelemetry();
}
