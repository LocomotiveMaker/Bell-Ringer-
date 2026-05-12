#include <Wire.h>

namespace
{
    constexpr unsigned long SerialBaud = 115200UL;
    constexpr unsigned long PrintIntervalMs = 100UL;

#if defined(ARDUINO_ARCH_ESP32)
    constexpr int I2cSdaPin = 11; // Geekble nano ESP32-S3 header A4
    constexpr int I2cSclPin = 12; // Geekble nano ESP32-S3 header A5 / SCL
#endif

    constexpr uint8_t MpuAddressLow = 0x68;
    constexpr uint8_t MpuAddressHigh = 0x69;
    constexpr uint8_t RegisterWhoAmI = 0x75;
    constexpr uint8_t RegisterPowerManagement1 = 0x6B;
    constexpr uint8_t RegisterPowerManagement2 = 0x6C;
    constexpr uint8_t RegisterConfig = 0x1A;
    constexpr uint8_t RegisterGyroConfig = 0x1B;
    constexpr uint8_t RegisterAccelConfig = 0x1C;
    constexpr uint8_t RegisterAccelConfig2 = 0x1D;
    constexpr uint8_t RegisterAccelXoutH = 0x3B;

    uint8_t gMpuAddress = 0;
    unsigned long gLastPrintMs = 0;

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

    void printHexByte(uint8_t value)
    {
        if (value < 16)
        {
            Serial.print('0');
        }

        Serial.print(value, HEX);
    }

    void scanI2c()
    {
        bool foundAny = false;
        Serial.println(F("[S3-MPU] I2C scan start"));

        for (uint8_t address = 1; address < 127; ++address)
        {
            Wire.beginTransmission(address);
            uint8_t result = Wire.endTransmission();
            if (result == 0)
            {
                foundAny = true;
                Serial.print(F("[S3-MPU] device 0x"));
                printHexByte(address);
                Serial.println();
            }
        }

        if (!foundAny)
        {
            Serial.println(F("[S3-MPU] no I2C devices found"));
        }

        Serial.println(F("[S3-MPU] I2C scan end"));
    }

    bool probeMpu(uint8_t address)
    {
        uint8_t whoAmI = 0;
        if (!readRegisters(address, RegisterWhoAmI, 1, &whoAmI))
        {
            return false;
        }

        Serial.print(F("[S3-MPU] candidate 0x"));
        printHexByte(address);
        Serial.print(F(" WHO_AM_I=0x"));
        printHexByte(whoAmI);
        Serial.println();
        gMpuAddress = address;
        return true;
    }

    bool initializeMpu()
    {
        if (!probeMpu(MpuAddressLow) && !probeMpu(MpuAddressHigh))
        {
            return false;
        }

        if (!writeRegister(gMpuAddress, RegisterPowerManagement1, 0x00))
        {
            return false;
        }

        delay(100);

        return writeRegister(gMpuAddress, RegisterPowerManagement1, 0x01) &&
               writeRegister(gMpuAddress, RegisterPowerManagement2, 0x00) &&
               writeRegister(gMpuAddress, RegisterConfig, 0x03) &&
               writeRegister(gMpuAddress, RegisterGyroConfig, 0x00) &&
               writeRegister(gMpuAddress, RegisterAccelConfig, 0x00) &&
               writeRegister(gMpuAddress, RegisterAccelConfig2, 0x03);
    }

    void printSample()
    {
        uint8_t raw[14] = {0};
        if (!readRegisters(gMpuAddress, RegisterAccelXoutH, sizeof(raw), raw))
        {
            Serial.println(F("[S3-MPU] ERROR read failed"));
            return;
        }

        int16_t ax = combineInt16(raw[0], raw[1]);
        int16_t ay = combineInt16(raw[2], raw[3]);
        int16_t az = combineInt16(raw[4], raw[5]);
        int16_t temperatureRaw = combineInt16(raw[6], raw[7]);
        int16_t gx = combineInt16(raw[8], raw[9]);
        int16_t gy = combineInt16(raw[10], raw[11]);
        int16_t gz = combineInt16(raw[12], raw[13]);

        constexpr float AccelScale = 16384.0f; // +/-2g
        constexpr float GyroScale = 131.0f;    // +/-250dps
        float temperatureC = (temperatureRaw / 333.87f) + 21.0f;

        Serial.print(F("ax_g="));
        Serial.print(ax / AccelScale, 3);
        Serial.print(F(" ay_g="));
        Serial.print(ay / AccelScale, 3);
        Serial.print(F(" az_g="));
        Serial.print(az / AccelScale, 3);
        Serial.print(F(" gx_dps="));
        Serial.print(gx / GyroScale, 2);
        Serial.print(F(" gy_dps="));
        Serial.print(gy / GyroScale, 2);
        Serial.print(F(" gz_dps="));
        Serial.print(gz / GyroScale, 2);
        Serial.print(F(" tempC="));
        Serial.println(temperatureC, 2);
    }
}

void setup()
{
    Serial.begin(SerialBaud);
    delay(800);

    Serial.println();
    Serial.println(F("[S3-MPU] serial check start"));
    Serial.println(F("[S3-MPU] Expected wiring: VCC->3V3, GND->GND, SDA->A4/GPIO11, SCL->A5/GPIO12"));

#if defined(ARDUINO_ARCH_ESP32)
    Wire.begin(I2cSdaPin, I2cSclPin);
#else
    Wire.begin();
#endif
    Wire.setClock(400000UL);

    scanI2c();

    if (!initializeMpu())
    {
        Serial.println(F("[S3-MPU] ERROR no MPU9250 at 0x68 or 0x69, or init failed"));
        Serial.println(F("[S3-MPU] If scan is empty, check 3V3/GND/SDA/SCL and breadboard row continuity."));
        return;
    }

    Serial.print(F("[S3-MPU] initialized at 0x"));
    printHexByte(gMpuAddress);
    Serial.println();
    Serial.println(F("[S3-MPU] Move the board. Accel/gyro values should change."));
}

void loop()
{
    if (gMpuAddress == 0)
    {
        delay(500);
        return;
    }

    unsigned long now = millis();
    if (now - gLastPrintMs < PrintIntervalMs)
    {
        return;
    }

    gLastPrintMs = now;
    printSample();
}
