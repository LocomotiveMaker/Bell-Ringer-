#include <Wire.h>

namespace
{
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

    constexpr unsigned long SerialBaud = 115200UL;
    constexpr unsigned long SampleIntervalMs = 20UL;

    uint8_t g_mpuAddress = 0;
    unsigned long g_lastSampleMs = 0;

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

    bool probeDevice(uint8_t address, uint8_t& whoAmI)
    {
        return readRegisters(address, RegisterWhoAmI, 1, &whoAmI);
    }

    void scanI2cBus()
    {
        Serial.println(F("[MPU9250] I2C scan start"));

        for (uint8_t address = 1; address < 127; ++address)
        {
            Wire.beginTransmission(address);
            uint8_t result = Wire.endTransmission();
            if (result == 0)
            {
                Serial.print(F("[MPU9250] I2C device found at 0x"));
                if (address < 16)
                {
                    Serial.print('0');
                }

                Serial.println(address, HEX);
            }
        }

        Serial.println(F("[MPU9250] I2C scan end"));
    }

    bool detectMpuAddress()
    {
        uint8_t whoAmI = 0;
        if (probeDevice(Mpu9250AddressLow, whoAmI))
        {
            g_mpuAddress = Mpu9250AddressLow;
            Serial.print(F("[MPU9250] Found candidate at 0x68 WHO_AM_I=0x"));
            if (whoAmI < 16)
            {
                Serial.print('0');
            }

            Serial.println(whoAmI, HEX);
            return true;
        }

        if (probeDevice(Mpu9250AddressHigh, whoAmI))
        {
            g_mpuAddress = Mpu9250AddressHigh;
            Serial.print(F("[MPU9250] Found candidate at 0x69 WHO_AM_I=0x"));
            if (whoAmI < 16)
            {
                Serial.print('0');
            }

            Serial.println(whoAmI, HEX);
            return true;
        }

        return false;
    }

    bool initializeMpu9250()
    {
        if (g_mpuAddress == 0)
        {
            return false;
        }

        // Wake the chip and choose the gyro PLL as the clock source.
        if (!writeRegister(g_mpuAddress, RegisterPowerManagement1, 0x00))
        {
            return false;
        }

        delay(100);

        if (!writeRegister(g_mpuAddress, RegisterPowerManagement1, 0x01))
        {
            return false;
        }

        if (!writeRegister(g_mpuAddress, RegisterPowerManagement2, 0x00))
        {
            return false;
        }

        // Conservative bring-up settings:
        // accel +/-2g, gyro +/-250dps, light low-pass filtering.
        if (!writeRegister(g_mpuAddress, RegisterConfig, 0x03))
        {
            return false;
        }

        if (!writeRegister(g_mpuAddress, RegisterSampleRateDivider, 0x04))
        {
            return false;
        }

        if (!writeRegister(g_mpuAddress, RegisterGyroConfig, 0x00))
        {
            return false;
        }

        if (!writeRegister(g_mpuAddress, RegisterAccelConfig, 0x00))
        {
            return false;
        }

        if (!writeRegister(g_mpuAddress, RegisterAccelConfig2, 0x03))
        {
            return false;
        }

        return true;
    }

    void printScaledValues(int16_t ax, int16_t ay, int16_t az, int16_t gx, int16_t gy, int16_t gz)
    {
        constexpr float AccelScale = 16384.0f; // +/-2g
        constexpr float GyroScale = 131.0f;    // +/-250dps

        Serial.print(F(" scaled "));
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
    }

    void sampleAndPrint()
    {
        uint8_t raw[14] = {0};
        if (!readRegisters(g_mpuAddress, RegisterAccelXoutH, sizeof(raw), raw))
        {
            Serial.println(F("[MPU9250] ERROR readRegisters failed"));
            delay(250);
            return;
        }

        int16_t ax = combineInt16(raw[0], raw[1]);
        int16_t ay = combineInt16(raw[2], raw[3]);
        int16_t az = combineInt16(raw[4], raw[5]);
        int16_t temperatureRaw = combineInt16(raw[6], raw[7]);
        int16_t gx = combineInt16(raw[8], raw[9]);
        int16_t gy = combineInt16(raw[10], raw[11]);
        int16_t gz = combineInt16(raw[12], raw[13]);

        float temperatureC = (temperatureRaw / 333.87f) + 21.0f;

        Serial.print(F("raw "));
        Serial.print(F("ax="));
        Serial.print(ax);
        Serial.print(F(" ay="));
        Serial.print(ay);
        Serial.print(F(" az="));
        Serial.print(az);
        Serial.print(F(" gx="));
        Serial.print(gx);
        Serial.print(F(" gy="));
        Serial.print(gy);
        Serial.print(F(" gz="));
        Serial.print(gz);
        Serial.print(F(" tempC="));
        Serial.print(temperatureC, 2);
        printScaledValues(ax, ay, az, gx, gy, gz);
        Serial.println();
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
    Serial.println(F("[MPU9250] bring-up start"));

    Wire.begin();
    Wire.setClock(400000UL);

    scanI2cBus();

    if (!detectMpuAddress())
    {
        Serial.println(F("[MPU9250] ERROR no device at 0x68 or 0x69"));
        Serial.println(F("[MPU9250] Check VCC/GND/SDA/SCL/AD0/CS wiring"));
        return;
    }

    if (!initializeMpu9250())
    {
        Serial.println(F("[MPU9250] ERROR initialization failed"));
        return;
    }

    Serial.print(F("[MPU9250] initialized at 0x"));
    if (g_mpuAddress < 16)
    {
        Serial.print('0');
    }

    Serial.println(g_mpuAddress, HEX);
    Serial.println(F("[MPU9250] printing raw accel/gyro every 20 ms"));
}

void loop()
{
    if (g_mpuAddress == 0)
    {
        delay(500);
        return;
    }

    unsigned long now = millis();
    if (now - g_lastSampleMs < SampleIntervalMs)
    {
        return;
    }

    g_lastSampleMs = now;
    sampleAndPrint();
}
