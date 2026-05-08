#include <Wire.h>
#include <EEPROM.h>

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

    constexpr unsigned long SerialBaud = 230400UL;
    constexpr unsigned long SampleIntervalUs = 5000UL;
    constexpr uint16_t GyroCalibrationSamples = 600;
    constexpr float AccelScale = 16384.0f;
    constexpr float GyroScale = 32.8f;
    constexpr float MagScaleUt = 0.15f;
    constexpr bool PreferMagnetometerFusion = true;
    constexpr float GyroDeadbandDps = 0.35f;
    constexpr float MadgwickBeta = 0.045f;
    constexpr float StationaryAccelToleranceG = 0.06f;
    constexpr float StationaryGyroToleranceDps = 1.35f;
    constexpr float StationaryEnterSpeed = 5.5f;
    constexpr float StationaryExitSpeed = 8.0f;
    constexpr float GyroBiasAdaptSpeed = 0.75f;
    constexpr float MagCalibrationSpanGoalUt = 45.0f;
    constexpr uint32_t MagCalibrationMagic = 0x4D43414CUL;
    constexpr uint16_t MagCalibrationVersion = 1;
    constexpr unsigned long TelemetryIntervalUs = 10000UL;
    constexpr float MagYawCorrectionGain = 2.2f;
    constexpr float MagYawCorrectionMovingGain = 0.8f;
    constexpr float MaxYawCorrectionPerStepDegrees = 4.0f;

    struct ImuSample
    {
        float accelXG;
        float accelYG;
        float accelZG;
        float rawGyroXDps;
        float rawGyroYDps;
        float rawGyroZDps;
        float unbiasedGyroXDps;
        float unbiasedGyroYDps;
        float unbiasedGyroZDps;
        float gyroXDps;
        float gyroYDps;
        float gyroZDps;
        float magXUt;
        float magYUt;
        float magZUt;
        bool hasMagnetometer;
        bool stationaryCandidate;
    };

    struct StoredMagCalibration
    {
        uint32_t magic;
        uint16_t version;
        float biasXUt;
        float biasYUt;
        float biasZUt;
        float scaleX;
        float scaleY;
        float scaleZ;
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
    float gMagBiasXUt = 0.0f;
    float gMagBiasYUt = 0.0f;
    float gMagBiasZUt = 0.0f;
    float gMagScaleX = 1.0f;
    float gMagScaleY = 1.0f;
    float gMagScaleZ = 1.0f;
    float gMagMinXUt = 0.0f;
    float gMagMinYUt = 0.0f;
    float gMagMinZUt = 0.0f;
    float gMagMaxXUt = 0.0f;
    float gMagMaxYUt = 0.0f;
    float gMagMaxZUt = 0.0f;
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
    float gStationaryBlend = 0.0f;
    float gLastGyroXDps = 0.0f;
    float gLastGyroYDps = 0.0f;
    float gLastGyroZDps = 0.0f;
    float gMagCalibrationProgress01 = 0.0f;
    bool gHasSavedMagCalibration = false;
    bool gMagCalibrationActive = false;
    uint32_t gMagCalibrationSampleCount = 0;
    unsigned long gLastTelemetryMicros = 0;
    float gMagReferenceHeadingDegrees = 0.0f;
    bool gHasMagReferenceHeading = false;

    void quaternionToYawPitchRollDegrees(float w, float x, float y, float z, float& yaw, float& pitch, float& roll);

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

    void quaternionFromAxisAngle(float axisX, float axisY, float axisZ, float angleDegrees, float& outW, float& outX, float& outY, float& outZ)
    {
        float halfRadians = angleDegrees * DEG_TO_RAD * 0.5f;
        float sinHalf = sin(halfRadians);
        outW = cos(halfRadians);
        outX = axisX * sinHalf;
        outY = axisY * sinHalf;
        outZ = axisZ * sinHalf;
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

    float applyGyroDeadband(float valueDps)
    {
        return abs(valueDps) < GyroDeadbandDps ? 0.0f : valueDps;
    }

    float angleDifferenceDegrees(float targetDegrees, float currentDegrees)
    {
        return wrapDegrees(targetDegrees - currentDegrees);
    }

    float sanitizePositiveScale(float value)
    {
        return value > 0.0001f ? value : 1.0f;
    }

    bool shouldUseMagnetometerFusion()
    {
        return PreferMagnetometerFusion && gHasMagnetometer && gHasSavedMagCalibration;
    }

    void resetMagCalibrationAccumulator()
    {
        gMagMinXUt = 100000.0f;
        gMagMinYUt = 100000.0f;
        gMagMinZUt = 100000.0f;
        gMagMaxXUt = -100000.0f;
        gMagMaxYUt = -100000.0f;
        gMagMaxZUt = -100000.0f;
        gMagCalibrationProgress01 = 0.0f;
        gMagCalibrationSampleCount = 0;
    }

    void updateMagCalibrationProgressFromRanges()
    {
        float spanX = max(0.0f, gMagMaxXUt - gMagMinXUt);
        float spanY = max(0.0f, gMagMaxYUt - gMagMinYUt);
        float spanZ = max(0.0f, gMagMaxZUt - gMagMinZUt);
        float axisCoverage =
            (min(1.0f, spanX / MagCalibrationSpanGoalUt) +
             min(1.0f, spanY / MagCalibrationSpanGoalUt) +
             min(1.0f, spanZ / MagCalibrationSpanGoalUt)) / 3.0f;
        float sampleCoverage = min(1.0f, gMagCalibrationSampleCount / 800.0f);
        gMagCalibrationProgress01 = min(1.0f, axisCoverage * 0.75f + sampleCoverage * 0.25f);
    }

    void updateMagCalibrationAccumulator(float magXUt, float magYUt, float magZUt)
    {
        if (!gMagCalibrationActive)
        {
            return;
        }

        gMagMinXUt = min(gMagMinXUt, magXUt);
        gMagMinYUt = min(gMagMinYUt, magYUt);
        gMagMinZUt = min(gMagMinZUt, magZUt);
        gMagMaxXUt = max(gMagMaxXUt, magXUt);
        gMagMaxYUt = max(gMagMaxYUt, magYUt);
        gMagMaxZUt = max(gMagMaxZUt, magZUt);
        ++gMagCalibrationSampleCount;
        updateMagCalibrationProgressFromRanges();
    }

    void applyStoredMagCalibration(float& magXUt, float& magYUt, float& magZUt)
    {
        if (!gHasSavedMagCalibration)
        {
            return;
        }

        magXUt = (magXUt - gMagBiasXUt) * gMagScaleX;
        magYUt = (magYUt - gMagBiasYUt) * gMagScaleY;
        magZUt = (magZUt - gMagBiasZUt) * gMagScaleZ;
    }

    void saveMagCalibrationToEeprom()
    {
        StoredMagCalibration data = {};
        data.magic = MagCalibrationMagic;
        data.version = MagCalibrationVersion;
        data.biasXUt = gMagBiasXUt;
        data.biasYUt = gMagBiasYUt;
        data.biasZUt = gMagBiasZUt;
        data.scaleX = gMagScaleX;
        data.scaleY = gMagScaleY;
        data.scaleZ = gMagScaleZ;
        EEPROM.put(0, data);
    }

    bool loadMagCalibrationFromEeprom()
    {
        StoredMagCalibration data = {};
        EEPROM.get(0, data);
        if (data.magic != MagCalibrationMagic || data.version != MagCalibrationVersion)
        {
            return false;
        }

        gMagBiasXUt = data.biasXUt;
        gMagBiasYUt = data.biasYUt;
        gMagBiasZUt = data.biasZUt;
        gMagScaleX = sanitizePositiveScale(data.scaleX);
        gMagScaleY = sanitizePositiveScale(data.scaleY);
        gMagScaleZ = sanitizePositiveScale(data.scaleZ);
        return true;
    }

    void clearMagCalibration()
    {
        gHasSavedMagCalibration = false;
        gMagCalibrationActive = false;
        gMagCalibrationProgress01 = 0.0f;
        gMagBiasXUt = 0.0f;
        gMagBiasYUt = 0.0f;
        gMagBiasZUt = 0.0f;
        gMagScaleX = 1.0f;
        gMagScaleY = 1.0f;
        gMagScaleZ = 1.0f;
        gMagReferenceHeadingDegrees = 0.0f;
        gHasMagReferenceHeading = false;

        StoredMagCalibration cleared = {};
        EEPROM.put(0, cleared);
        resetMagCalibrationAccumulator();
    }

    bool probeDevice(uint8_t address, uint8_t& whoAmI)
    {
        return readRegisters(address, RegisterWhoAmI, 1, &whoAmI);
    }

    bool detectMpuAddress()
    {
        uint8_t whoAmI = 0;
        for (uint8_t attempt = 0; attempt < 5; ++attempt)
        {
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

            delay(80);
        }

        return false;
    }

    void printI2cScanResults()
    {
        Serial.print(F("[PadMPU] I2C scan:"));
        bool foundAny = false;
        for (uint8_t address = 1; address < 127; ++address)
        {
            Wire.beginTransmission(address);
            if (Wire.endTransmission() == 0)
            {
                foundAny = true;
                Serial.print(' ');
                if (address < 16)
                {
                    Serial.print('0');
                }

                Serial.print(address, HEX);
            }
        }

        if (!foundAny)
        {
            Serial.print(F(" <none>"));
        }

        Serial.println();
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
            !writeRegister(gMpuAddress, RegisterConfig, 0x01) ||
            !writeRegister(gMpuAddress, RegisterSampleRateDivider, 0x00) ||
            !writeRegister(gMpuAddress, RegisterGyroConfig, 0x10) ||
            !writeRegister(gMpuAddress, RegisterAccelConfig, 0x00) ||
            !writeRegister(gMpuAddress, RegisterAccelConfig2, 0x01))
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
        sample.rawGyroXDps = gx / GyroScale;
        sample.rawGyroYDps = gy / GyroScale;
        sample.rawGyroZDps = gz / GyroScale;
        sample.unbiasedGyroXDps = sample.rawGyroXDps - gGyroBiasXDps;
        sample.unbiasedGyroYDps = sample.rawGyroYDps - gGyroBiasYDps;
        sample.unbiasedGyroZDps = sample.rawGyroZDps - gGyroBiasZDps;
        sample.gyroXDps = applyGyroDeadband(sample.unbiasedGyroXDps);
        sample.gyroYDps = applyGyroDeadband(sample.unbiasedGyroYDps);
        sample.gyroZDps = applyGyroDeadband(sample.unbiasedGyroZDps);

        float accelMagnitude = sqrt(
            (sample.accelXG * sample.accelXG) +
            (sample.accelYG * sample.accelYG) +
            (sample.accelZG * sample.accelZG));
        float gyroMagnitude = sqrt(
            (sample.unbiasedGyroXDps * sample.unbiasedGyroXDps) +
            (sample.unbiasedGyroYDps * sample.unbiasedGyroYDps) +
            (sample.unbiasedGyroZDps * sample.unbiasedGyroZDps));
        sample.stationaryCandidate =
            abs(accelMagnitude - 1.0f) <= StationaryAccelToleranceG &&
            gyroMagnitude <= StationaryGyroToleranceDps;
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

                gLastMagXUt = rawX * MagScaleUt * gMagAdjustX;
                gLastMagYUt = rawY * MagScaleUt * gMagAdjustY;
                gLastMagZUt = rawZ * MagScaleUt * gMagAdjustZ;
                gHasLastMagSample = true;
            }
        }

        if (!gHasLastMagSample)
        {
            return;
        }

        updateMagCalibrationAccumulator(gLastMagXUt, gLastMagYUt, gLastMagZUt);

        sample.hasMagnetometer = true;
        sample.magXUt = gLastMagXUt;
        sample.magYUt = gLastMagYUt;
        sample.magZUt = gLastMagZUt;
        applyStoredMagCalibration(sample.magXUt, sample.magYUt, sample.magZUt);
    }

    void startMagCalibration()
    {
        if (!gHasMagnetometer)
        {
            Serial.println(F("[PadMPU] magcal failed: magnetometer missing"));
            return;
        }

        gMagCalibrationActive = true;
        resetMagCalibrationAccumulator();
        Serial.println(F("[PadMPU] magcal start: rotate through wide figure-8 and all axes"));
    }

    void finishMagCalibrationAndSave()
    {
        if (!gMagCalibrationActive)
        {
            Serial.println(F("[PadMPU] magcal stop ignored: not active"));
            return;
        }

        gMagCalibrationActive = false;
        float spanX = max(0.0f, gMagMaxXUt - gMagMinXUt);
        float spanY = max(0.0f, gMagMaxYUt - gMagMinYUt);
        float spanZ = max(0.0f, gMagMaxZUt - gMagMinZUt);
        if (spanX < 12.0f || spanY < 12.0f || spanZ < 12.0f)
        {
            Serial.println(F("[PadMPU] magcal failed: not enough motion coverage"));
            updateMagCalibrationProgressFromRanges();
            return;
        }

        float halfRangeX = spanX * 0.5f;
        float halfRangeY = spanY * 0.5f;
        float halfRangeZ = spanZ * 0.5f;
        float averageRadius = (halfRangeX + halfRangeY + halfRangeZ) / 3.0f;

        gMagBiasXUt = (gMagMaxXUt + gMagMinXUt) * 0.5f;
        gMagBiasYUt = (gMagMaxYUt + gMagMinYUt) * 0.5f;
        gMagBiasZUt = (gMagMaxZUt + gMagMinZUt) * 0.5f;
        gMagScaleX = sanitizePositiveScale(averageRadius / max(0.0001f, halfRangeX));
        gMagScaleY = sanitizePositiveScale(averageRadius / max(0.0001f, halfRangeY));
        gMagScaleZ = sanitizePositiveScale(averageRadius / max(0.0001f, halfRangeZ));
        gHasSavedMagCalibration = true;
        gMagCalibrationProgress01 = 1.0f;
        saveMagCalibrationToEeprom();

        Serial.print(F("[PadMPU] magcal saved bias="));
        Serial.print(gMagBiasXUt, 2);
        Serial.print(',');
        Serial.print(gMagBiasYUt, 2);
        Serial.print(',');
        Serial.print(gMagBiasZUt, 2);
        Serial.print(F(" scale="));
        Serial.print(gMagScaleX, 3);
        Serial.print(',');
        Serial.print(gMagScaleY, 3);
        Serial.print(',');
        Serial.println(gMagScaleZ, 3);
        gHasMagReferenceHeading = false;
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

        if (shouldUseMagnetometerFusion() && gHasLastMagSample)
        {
            float yaw = 0.0f;
            float pitch = 0.0f;
            float roll = 0.0f;
            quaternionToYawPitchRollDegrees(gQuatW, gQuatX, gQuatY, gQuatZ, yaw, pitch, roll);
            float rollRadians = roll * DEG_TO_RAD;
            float pitchRadians = pitch * DEG_TO_RAD;

            float magXUt = gLastMagXUt;
            float magYUt = gLastMagYUt;
            float magZUt = gLastMagZUt;
            applyStoredMagCalibration(magXUt, magYUt, magZUt);

            float horizontalX = magXUt * cos(pitchRadians) + magZUt * sin(pitchRadians);
            float horizontalY =
                magXUt * sin(rollRadians) * sin(pitchRadians) +
                magYUt * cos(rollRadians) -
                magZUt * sin(rollRadians) * cos(pitchRadians);

            if ((horizontalX * horizontalX + horizontalY * horizontalY) > 0.0001f)
            {
                gMagReferenceHeadingDegrees = atan2(horizontalY, horizontalX) * 180.0f / PI;
                gHasMagReferenceHeading = true;
            }
        }

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
                if (gCommandLength > 0)
                {
                    if (strcmp(gCommandBuffer, "r") == 0 ||
                        strcmp(gCommandBuffer, "R") == 0 ||
                        strcmp(gCommandBuffer, "recenter") == 0 ||
                        strcmp(gCommandBuffer, "RECENTER") == 0)
                    {
                        recenterNow();
                    }
                    else if (strcmp(gCommandBuffer, "magcal_start") == 0 ||
                             strcmp(gCommandBuffer, "MAGCAL_START") == 0 ||
                             strcmp(gCommandBuffer, "mag_start") == 0)
                    {
                        startMagCalibration();
                    }
                    else if (strcmp(gCommandBuffer, "magcal_stop") == 0 ||
                             strcmp(gCommandBuffer, "MAGCAL_STOP") == 0 ||
                             strcmp(gCommandBuffer, "mag_stop") == 0)
                    {
                        finishMagCalibrationAndSave();
                    }
                    else if (strcmp(gCommandBuffer, "magcal_reset") == 0 ||
                             strcmp(gCommandBuffer, "MAGCAL_RESET") == 0)
                    {
                        clearMagCalibration();
                        Serial.println(F("[PadMPU] magcal reset"));
                    }
                    else if (strcmp(gCommandBuffer, "magcal_status") == 0 ||
                             strcmp(gCommandBuffer, "MAGCAL_STATUS") == 0)
                    {
                        Serial.print(F("[PadMPU] magcal active="));
                        Serial.print(gMagCalibrationActive ? 1 : 0);
                        Serial.print(F(" saved="));
                        Serial.print(gHasSavedMagCalibration ? 1 : 0);
                        Serial.print(F(" progress="));
                        Serial.print(gMagCalibrationProgress01, 2);
                        Serial.print(F(" fusion="));
                        Serial.println(shouldUseMagnetometerFusion() ? 9 : 6);
                    }
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

            sumX += sample.rawGyroXDps;
            sumY += sample.rawGyroYDps;
            sumZ += sample.rawGyroZDps;
            ++captured;
            delay(5);
        }

        gGyroBiasXDps = sumX / GyroCalibrationSamples;
        gGyroBiasYDps = sumY / GyroCalibrationSamples;
        gGyroBiasZDps = sumZ / GyroCalibrationSamples;
    }

    void updateStillnessAndGyroBias(const ImuSample& sample, float deltaSeconds)
    {
        float targetStillness = sample.stationaryCandidate ? 1.0f : 0.0f;
        float blendSpeed = sample.stationaryCandidate ? StationaryEnterSpeed : StationaryExitSpeed;
        float blendFactor = min(1.0f, blendSpeed * deltaSeconds);
        gStationaryBlend += (targetStillness - gStationaryBlend) * blendFactor;

        if (gStationaryBlend < 0.82f)
        {
            return;
        }

        float biasBlend = min(1.0f, GyroBiasAdaptSpeed * deltaSeconds * gStationaryBlend);
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

    void correctYawFromMagnetometer(const ImuSample& sample, float deltaSeconds)
    {
        if (!shouldUseMagnetometerFusion() || !sample.hasMagnetometer || gMagCalibrationActive)
        {
            return;
        }

        float currentYaw = 0.0f;
        float currentPitch = 0.0f;
        float currentRoll = 0.0f;
        quaternionToYawPitchRollDegrees(gQuatW, gQuatX, gQuatY, gQuatZ, currentYaw, currentPitch, currentRoll);

        float rollRadians = currentRoll * DEG_TO_RAD;
        float pitchRadians = currentPitch * DEG_TO_RAD;
        float horizontalX = sample.magXUt * cos(pitchRadians) + sample.magZUt * sin(pitchRadians);
        float horizontalY =
            sample.magXUt * sin(rollRadians) * sin(pitchRadians) +
            sample.magYUt * cos(rollRadians) -
            sample.magZUt * sin(rollRadians) * cos(pitchRadians);

        if ((horizontalX * horizontalX + horizontalY * horizontalY) <= 0.0001f)
        {
            return;
        }

        float measuredHeadingDegrees = atan2(horizontalY, horizontalX) * 180.0f / PI;
        if (!gHasMagReferenceHeading)
        {
            gMagReferenceHeadingDegrees = measuredHeadingDegrees;
            gHasMagReferenceHeading = true;
            return;
        }

        float desiredYawDegrees = angleDifferenceDegrees(measuredHeadingDegrees, gMagReferenceHeadingDegrees);
        float yawErrorDegrees = angleDifferenceDegrees(desiredYawDegrees, currentYaw);
        float correctionGain = gStationaryBlend >= 0.75f ? MagYawCorrectionGain : MagYawCorrectionMovingGain;
        float correctionDegrees = yawErrorDegrees * min(1.0f, correctionGain * deltaSeconds);
        correctionDegrees = constrain(correctionDegrees, -MaxYawCorrectionPerStepDegrees, MaxYawCorrectionPerStepDegrees);
        if (abs(correctionDegrees) <= 0.001f)
        {
            return;
        }

        float correctionW = 1.0f;
        float correctionX = 0.0f;
        float correctionY = 0.0f;
        float correctionZ = 0.0f;
        quaternionFromAxisAngle(0.0f, 0.0f, 1.0f, correctionDegrees, correctionW, correctionX, correctionY, correctionZ);

        float newW = 1.0f;
        float newX = 0.0f;
        float newY = 0.0f;
        float newZ = 0.0f;
        multiplyQuaternions(correctionW, correctionX, correctionY, correctionZ, gQuatW, gQuatX, gQuatY, gQuatZ, newW, newX, newY, newZ);
        normalizeQuaternion(newW, newX, newY, newZ);
        gQuatW = newW;
        gQuatX = newX;
        gQuatY = newY;
        gQuatZ = newZ;
    }

    void updateOrientation(const ImuSample& sample, float deltaSeconds)
    {
        madgwickUpdateImu(sample, deltaSeconds);
        correctYawFromMagnetometer(sample, deltaSeconds);

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
        Serial.print(gYawDegrees, 1);
        Serial.print(F(",wp="));
        Serial.print(gPitchDegrees, 1);
        Serial.print(F(",wr="));
        Serial.print(gRollDegrees, 1);
        Serial.print(F(",wqw="));
        Serial.print(relativeW, 4);
        Serial.print(F(",wqx="));
        Serial.print(relativeX, 4);
        Serial.print(F(",wqy="));
        Serial.print(relativeY, 4);
        Serial.print(F(",wqz="));
        Serial.print(relativeZ, 4);
        Serial.print(F(",gx="));
        Serial.print(gLastGyroXDps, 2);
        Serial.print(F(",gy="));
        Serial.print(gLastGyroYDps, 2);
        Serial.print(F(",gz="));
        Serial.print(gLastGyroZDps, 2);
        Serial.print(F(",st="));
        Serial.print(gStationaryBlend, 2);
        Serial.print(F(",mf="));
        Serial.print(shouldUseMagnetometerFusion() ? 9 : 6);
        Serial.print(F(",mc="));
        Serial.print(gMagCalibrationActive ? 1 : 0);
        Serial.print(F(",mp="));
        Serial.println(gMagCalibrationProgress01, 2);
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
    delay(120);

    if (!detectMpuAddress())
    {
        Serial.println(F("[PadMPU] ERROR no device at 0x68 or 0x69"));
        printI2cScanResults();
        return;
    }

    if (!initializeMpu9250())
    {
        Serial.println(F("[PadMPU] ERROR initialization failed"));
        return;
    }

    gHasMagnetometer = initializeAk8963();
    resetMagCalibrationAccumulator();
    gHasSavedMagCalibration = loadMagCalibrationFromEeprom();
    calibrateGyroBias();
    gLastSampleMicros = micros();
    gLastTelemetryMicros = gLastSampleMicros;
    Serial.println(gHasMagnetometer ? F("[PadMPU] magnetometer ready") : F("[PadMPU] magnetometer missing"));
    Serial.println(gHasSavedMagCalibration ? F("[PadMPU] mag calibration loaded from EEPROM") : F("[PadMPU] mag calibration missing"));
    Serial.println(shouldUseMagnetometerFusion() ? F("[PadMPU] fusion mode: 9-axis calibrated heading") : F("[PadMPU] fusion mode: 6-axis controller yaw"));
    Serial.println(F("[PadMPU] fusion updates at 200 Hz, telemetry at 100 Hz"));
    Serial.println(F("[PadMPU] Send 'r' or 'recenter' to zero the current pose"));
    Serial.println(F("[PadMPU] Send 'magcal_start' then rotate on all axes, then 'magcal_stop'"));
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
    float deltaSeconds = elapsedMicros * 0.000001f;
    updateStillnessAndGyroBias(sample, deltaSeconds);
    updateOrientation(sample, deltaSeconds);
    gLastGyroXDps = sample.gyroXDps;
    gLastGyroYDps = sample.gyroYDps;
    gLastGyroZDps = sample.gyroZDps;

    if (!gHasOrientation)
    {
        gHasOrientation = true;
        recenterNow();
    }

    if (nowMicros - gLastTelemetryMicros >= TelemetryIntervalUs)
    {
        gLastTelemetryMicros = nowMicros;
        printTelemetry();
    }
}
