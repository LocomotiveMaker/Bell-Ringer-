#include <EEPROM.h>
#include <Wire.h>

#include "MPU9250.h"

namespace
{
    constexpr unsigned long SerialBaud = 230400UL;
    constexpr unsigned long TelemetryIntervalUs = 10000UL;
    constexpr float StationaryAccelToleranceG = 0.06f;
    constexpr float StationaryGyroToleranceDps = 1.35f;
    constexpr size_t FilterIterations = 12;
    constexpr uint8_t EepromFlagAddress = 0x00;
    constexpr uint8_t EepromAccBiasAddress = 0x01;
    constexpr uint8_t EepromGyroBiasAddress = 0x0D;
    constexpr uint8_t EepromMagBiasAddress = 0x19;
    constexpr uint8_t EepromMagScaleAddress = 0x25;

    MPU9250 gMpu;
    unsigned long gLastTelemetryMicros = 0;
    bool gConnected = false;
    bool gHasPose = false;
    bool gCalibrationStored = false;
    float gOffsetW = 1.0f;
    float gOffsetX = 0.0f;
    float gOffsetY = 0.0f;
    float gOffsetZ = 0.0f;
    float gStillness01 = 0.0f;
    char gCommandBuffer[32] = {0};
    uint8_t gCommandLength = 0;

    void normalizeQuaternion(float& w, float& x, float& y, float& z)
    {
        float magnitude = sqrt((w * w) + (x * x) + (y * y) + (z * z));
        if (magnitude <= 0.00001f)
        {
            w = 1.0f;
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
            return;
        }

        float invMagnitude = 1.0f / magnitude;
        w *= invMagnitude;
        x *= invMagnitude;
        y *= invMagnitude;
        z *= invMagnitude;
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

    void writeFloatToEeprom(int address, float value)
    {
        EEPROM.put(address, value);
    }

    float readFloatFromEeprom(int address)
    {
        float value = 0.0f;
        EEPROM.get(address, value);
        return value;
    }

    bool isCalibrationStored()
    {
        return EEPROM.read(EepromFlagAddress) == 0x01;
    }

    void saveCalibration()
    {
        EEPROM.write(EepromFlagAddress, 0x01);
        writeFloatToEeprom(EepromAccBiasAddress + 0, gMpu.getAccBias(0));
        writeFloatToEeprom(EepromAccBiasAddress + 4, gMpu.getAccBias(1));
        writeFloatToEeprom(EepromAccBiasAddress + 8, gMpu.getAccBias(2));
        writeFloatToEeprom(EepromGyroBiasAddress + 0, gMpu.getGyroBias(0));
        writeFloatToEeprom(EepromGyroBiasAddress + 4, gMpu.getGyroBias(1));
        writeFloatToEeprom(EepromGyroBiasAddress + 8, gMpu.getGyroBias(2));
        writeFloatToEeprom(EepromMagBiasAddress + 0, gMpu.getMagBias(0));
        writeFloatToEeprom(EepromMagBiasAddress + 4, gMpu.getMagBias(1));
        writeFloatToEeprom(EepromMagBiasAddress + 8, gMpu.getMagBias(2));
        writeFloatToEeprom(EepromMagScaleAddress + 0, gMpu.getMagScale(0));
        writeFloatToEeprom(EepromMagScaleAddress + 4, gMpu.getMagScale(1));
        writeFloatToEeprom(EepromMagScaleAddress + 8, gMpu.getMagScale(2));
        gCalibrationStored = true;
        Serial.println(F("[PadMPU-Lib] calibration saved to EEPROM"));
    }

    void loadCalibration()
    {
        gCalibrationStored = isCalibrationStored();
        if (!gCalibrationStored)
        {
            gMpu.setAccBias(0.0f, 0.0f, 0.0f);
            gMpu.setGyroBias(0.0f, 0.0f, 0.0f);
            gMpu.setMagBias(0.0f, 0.0f, 0.0f);
            gMpu.setMagScale(1.0f, 1.0f, 1.0f);
            Serial.println(F("[PadMPU-Lib] EEPROM calibration missing"));
            return;
        }

        gMpu.setAccBias(
            readFloatFromEeprom(EepromAccBiasAddress + 0),
            readFloatFromEeprom(EepromAccBiasAddress + 4),
            readFloatFromEeprom(EepromAccBiasAddress + 8));
        gMpu.setGyroBias(
            readFloatFromEeprom(EepromGyroBiasAddress + 0),
            readFloatFromEeprom(EepromGyroBiasAddress + 4),
            readFloatFromEeprom(EepromGyroBiasAddress + 8));
        gMpu.setMagBias(
            readFloatFromEeprom(EepromMagBiasAddress + 0),
            readFloatFromEeprom(EepromMagBiasAddress + 4),
            readFloatFromEeprom(EepromMagBiasAddress + 8));
        gMpu.setMagScale(
            readFloatFromEeprom(EepromMagScaleAddress + 0),
            readFloatFromEeprom(EepromMagScaleAddress + 4),
            readFloatFromEeprom(EepromMagScaleAddress + 8));
        Serial.println(F("[PadMPU-Lib] EEPROM calibration loaded"));
    }

    void clearCalibration()
    {
        EEPROM.write(EepromFlagAddress, 0x00);
        gCalibrationStored = false;
        gMpu.setAccBias(0.0f, 0.0f, 0.0f);
        gMpu.setGyroBias(0.0f, 0.0f, 0.0f);
        gMpu.setMagBias(0.0f, 0.0f, 0.0f);
        gMpu.setMagScale(1.0f, 1.0f, 1.0f);
        Serial.println(F("[PadMPU-Lib] EEPROM calibration cleared"));
    }

    void recenterNow()
    {
        float rawW = gMpu.getQuaternionW();
        float rawX = gMpu.getQuaternionX();
        float rawY = gMpu.getQuaternionY();
        float rawZ = gMpu.getQuaternionZ();
        normalizeQuaternion(rawW, rawX, rawY, rawZ);
        gOffsetW = rawW;
        gOffsetX = -rawX;
        gOffsetY = -rawY;
        gOffsetZ = -rawZ;
        gHasPose = true;
        Serial.println(F("[PadMPU-Lib] recentered"));
    }

    void printStatus()
    {
        Serial.print(F("[PadMPU-Lib] connected="));
        Serial.print(gConnected ? 1 : 0);
        Serial.print(F(" calibrated="));
        Serial.print(gCalibrationStored ? 1 : 0);
        Serial.print(F(" filter=madgwick iterations="));
        Serial.println(FilterIterations);
    }

    void runAccelGyroCalibration()
    {
        Serial.println(F("[PadMPU-Lib] accel/gyro calibration starting. Keep the pad still."));
        gMpu.verbose(true);
        delay(1000);
        gMpu.calibrateAccelGyro();
        gMpu.verbose(false);
        saveCalibration();
        recenterNow();
    }

    void runMagCalibration()
    {
        Serial.println(F("[PadMPU-Lib] mag calibration starting. Move in a wide figure-8 on all axes for ~15s."));
        gMpu.verbose(true);
        delay(200);
        gMpu.calibrateMag();
        gMpu.verbose(false);
        saveCalibration();
        recenterNow();
    }

    void processCommand(const char* command)
    {
        if (strcmp(command, "r") == 0 ||
            strcmp(command, "R") == 0 ||
            strcmp(command, "recenter") == 0 ||
            strcmp(command, "RECENTER") == 0)
        {
            recenterNow();
            return;
        }

        if (strcmp(command, "status") == 0 || strcmp(command, "STATUS") == 0)
        {
            printStatus();
            return;
        }

        if (strcmp(command, "agcal") == 0 || strcmp(command, "calibrate_accel_gyro") == 0)
        {
            runAccelGyroCalibration();
            return;
        }

        if (strcmp(command, "magcal") == 0 ||
            strcmp(command, "magcal_start") == 0 ||
            strcmp(command, "MAGCAL_START") == 0)
        {
            runMagCalibration();
            return;
        }

        if (strcmp(command, "magcal_stop") == 0 || strcmp(command, "MAGCAL_STOP") == 0)
        {
            Serial.println(F("[PadMPU-Lib] magcal_stop ignored. This sketch runs the full mag calibration on magcal_start."));
            return;
        }

        if (strcmp(command, "savecal") == 0 || strcmp(command, "SAVE_CAL") == 0)
        {
            saveCalibration();
            return;
        }

        if (strcmp(command, "loadcal") == 0 || strcmp(command, "LOAD_CAL") == 0)
        {
            loadCalibration();
            recenterNow();
            return;
        }

        if (strcmp(command, "clearcal") == 0 ||
            strcmp(command, "magcal_reset") == 0 ||
            strcmp(command, "MAGCAL_RESET") == 0)
        {
            clearCalibration();
            recenterNow();
            return;
        }

        if (strcmp(command, "fullcal") == 0)
        {
            runAccelGyroCalibration();
            runMagCalibration();
            return;
        }
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
                    processCommand(gCommandBuffer);
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

    float computeStillness01()
    {
        float accelX = gMpu.getAccX();
        float accelY = gMpu.getAccY();
        float accelZ = gMpu.getAccZ();
        float gyroX = gMpu.getGyroX();
        float gyroY = gMpu.getGyroY();
        float gyroZ = gMpu.getGyroZ();
        float accelMagnitude = sqrt((accelX * accelX) + (accelY * accelY) + (accelZ * accelZ));
        float gyroMagnitude = sqrt((gyroX * gyroX) + (gyroY * gyroY) + (gyroZ * gyroZ));
        bool stationary =
            abs(accelMagnitude - 1.0f) <= StationaryAccelToleranceG &&
            gyroMagnitude <= StationaryGyroToleranceDps;
        float target = stationary ? 1.0f : 0.0f;
        gStillness01 += (target - gStillness01) * 0.15f;
        if (gStillness01 < 0.0f) gStillness01 = 0.0f;
        if (gStillness01 > 1.0f) gStillness01 = 1.0f;
        return gStillness01;
    }

    void printTelemetry()
    {
        float rawW = gMpu.getQuaternionW();
        float rawX = gMpu.getQuaternionX();
        float rawY = gMpu.getQuaternionY();
        float rawZ = gMpu.getQuaternionZ();
        normalizeQuaternion(rawW, rawX, rawY, rawZ);

        float relativeW = 1.0f;
        float relativeX = 0.0f;
        float relativeY = 0.0f;
        float relativeZ = 0.0f;
        multiplyQuaternions(gOffsetW, gOffsetX, gOffsetY, gOffsetZ, rawW, rawX, rawY, rawZ, relativeW, relativeX, relativeY, relativeZ);
        normalizeQuaternion(relativeW, relativeX, relativeY, relativeZ);

        float yaw = 0.0f;
        float pitch = 0.0f;
        float roll = 0.0f;
        quaternionToYawPitchRollDegrees(relativeW, relativeX, relativeY, relativeZ, yaw, pitch, roll);

        Serial.print(F("wy="));
        Serial.print(wrapDegrees(yaw), 1);
        Serial.print(F(",wp="));
        Serial.print(wrapDegrees(pitch), 1);
        Serial.print(F(",wr="));
        Serial.print(wrapDegrees(roll), 1);
        Serial.print(F(",wqw="));
        Serial.print(relativeW, 4);
        Serial.print(F(",wqx="));
        Serial.print(relativeX, 4);
        Serial.print(F(",wqy="));
        Serial.print(relativeY, 4);
        Serial.print(F(",wqz="));
        Serial.print(relativeZ, 4);
        Serial.print(F(",gx="));
        Serial.print(gMpu.getGyroX(), 2);
        Serial.print(F(",gy="));
        Serial.print(gMpu.getGyroY(), 2);
        Serial.print(F(",gz="));
        Serial.print(gMpu.getGyroZ(), 2);
        Serial.print(F(",st="));
        Serial.print(gStillness01, 2);
        Serial.print(F(",mf=9,mc=0,mp="));
        Serial.println(gCalibrationStored ? 1.0f : 0.0f, 2);
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
    Serial.println(F("[PadMPU-Lib] orientation start"));

    Wire.begin();
    delay(2000);

    MPU9250Setting setting;
    setting.accel_fs_sel = ACCEL_FS_SEL::A16G;
    setting.gyro_fs_sel = GYRO_FS_SEL::G2000DPS;
    setting.mag_output_bits = MAG_OUTPUT_BITS::M16BITS;
    setting.fifo_sample_rate = FIFO_SAMPLE_RATE::SMPL_200HZ;
    setting.gyro_fchoice = 0x03;
    setting.gyro_dlpf_cfg = GYRO_DLPF_CFG::DLPF_41HZ;
    setting.accel_fchoice = 0x01;
    setting.accel_dlpf_cfg = ACCEL_DLPF_CFG::DLPF_45HZ;

    gConnected = gMpu.setup(0x68, setting);
    if (!gConnected)
    {
        Serial.println(F("[PadMPU-Lib] ERROR no device at 0x68 or 0x69"));
        return;
    }

    gMpu.selectFilter(QuatFilterSel::MADGWICK);
    gMpu.setFilterIterations(FilterIterations);
    gMpu.setMagneticDeclination(0.0f);
    loadCalibration();
    printStatus();
}

void loop()
{
    processSerialCommands();

    if (!gConnected)
    {
        delay(500);
        return;
    }

    if (!gMpu.update())
    {
        return;
    }

    computeStillness01();

    if (!gHasPose)
    {
        recenterNow();
    }

    unsigned long nowMicros = micros();
    if (nowMicros - gLastTelemetryMicros >= TelemetryIntervalUs)
    {
        gLastTelemetryMicros = nowMicros;
        printTelemetry();
    }
}
