#include <Wire.h>
#include <EEPROM.h>

// MPU9250 is wired for I2C:
// Uno 3.3V -> VCC, GND -> GND, A4 -> SDA, A5 -> SCL,
// GND -> AD0/SDO, 3.3V -> NCS.
//
// This sketch does not depend on an MPU9250 Arduino library. It reads the
// MPU6500 and AK8963 registers directly, calibrates gyro bias and magnetometer
// hard/soft iron error, then runs a gyro/accelerometer quaternion filter with
// magnetometer yaw-only correction. Keeping magnetometer correction yaw-only
// prevents a bad heading sample from tilting roll/pitch or freezing rotation.

const uint8_t MPU_ADDR = 0x68;
const uint8_t AK8963_ADDR = 0x0C;

const uint8_t MPU_WHO_AM_I = 0x75;
const uint8_t MPU_PWR_MGMT_1 = 0x6B;
const uint8_t MPU_PWR_MGMT_2 = 0x6C;
const uint8_t MPU_CONFIG = 0x1A;
const uint8_t MPU_SMPLRT_DIV = 0x19;
const uint8_t MPU_GYRO_CONFIG = 0x1B;
const uint8_t MPU_ACCEL_CONFIG = 0x1C;
const uint8_t MPU_ACCEL_CONFIG2 = 0x1D;
const uint8_t MPU_INT_PIN_CFG = 0x37;
const uint8_t MPU_USER_CTRL = 0x6A;
const uint8_t MPU_ACCEL_XOUT_H = 0x3B;

const uint8_t AK8963_WHO_AM_I = 0x00;
const uint8_t AK8963_ST1 = 0x02;
const uint8_t AK8963_HXL = 0x03;
const uint8_t AK8963_CNTL1 = 0x0A;
const uint8_t AK8963_CNTL2 = 0x0B;
const uint8_t AK8963_ASAX = 0x10;

const float ACCEL_LSB_PER_G = 8192.0f;      // +/-4g
const float GYRO_LSB_PER_DPS = 65.5f;       // +/-500 dps
const float MAG_UT_PER_LSB = 4912.0f / 32760.0f;
const float DEG_TO_RAD_F = 0.0174532925199f;
const float RAD_TO_DEG_F = 57.2957795131f;

// Set this for true-north yaw. Leave at 0 for magnetic north.
const float MAG_DECLINATION_DEG = 0.0f;

const uint16_t GYRO_CAL_SAMPLES = 800;
const uint8_t MAG_CAL_SECONDS = 25;
const uint16_t OUTPUT_INTERVAL_MS = 33;

const float ACCEL_KP = 0.85f;
const float ACCEL_KI = 0.002f;
const float MAG_YAW_BLEND = 0.018f;
const float MAG_YAW_MAX_RATE_DPS = 35.0f;
const float MAG_FIELD_MIN_UT = 12.0f;
const float MAG_FIELD_MAX_UT = 90.0f;
const float GYRO_DEADBAND_DPS = 0.035f;
const float GYRO_STILL_DPS = 0.45f;
const float GYRO_AUTO_ZERO_ALPHA = 0.004f;
const float ACCEL_STILL_TOLERANCE_G = 0.06f;

const uint8_t MAG_AXIS_MAP_COUNT = 8;
const uint32_t CAL_MAGIC = 0x4D503933UL; // "MP93"

struct SensorFrame {
  float ax;
  float ay;
  float az;
  float gxDps;
  float gyDps;
  float gzDps;
  float mx;
  float my;
  float mz;
  float magFieldUt;
  bool stationary;
  bool magValid;
};

struct MagCalibrationStore {
  uint32_t magic;
  uint8_t axisMap;
  uint8_t reserved0;
  uint8_t reserved1;
  uint8_t reserved2;
  float biasX;
  float biasY;
  float biasZ;
  float scaleX;
  float scaleY;
  float scaleZ;
};

float q0 = 1.0f;
float q1 = 0.0f;
float q2 = 0.0f;
float q3 = 0.0f;

float integralX = 0.0f;
float integralY = 0.0f;
float integralZ = 0.0f;

float gyroBiasDps[3] = {0.0f, 0.0f, 0.0f};
float magBias[3] = {0.0f, 0.0f, 0.0f};
float magScale[3] = {1.0f, 1.0f, 1.0f};
float magFactoryAdj[3] = {1.0f, 1.0f, 1.0f};
uint8_t magAxisMap = 0;

bool magCalibrated = false;
bool orientationInitialized = false;
uint32_t lastUpdateMicros = 0;
uint32_t lastOutputMillis = 0;

bool writeByte(uint8_t address, uint8_t reg, uint8_t value) {
  Wire.beginTransmission(address);
  Wire.write(reg);
  Wire.write(value);
  return Wire.endTransmission() == 0;
}

bool readBytes(uint8_t address, uint8_t reg, uint8_t count, uint8_t *dest) {
  Wire.beginTransmission(address);
  Wire.write(reg);
  if (Wire.endTransmission(false) != 0) {
    return false;
  }

  uint8_t received = Wire.requestFrom((int)address, (int)count);
  if (received != count) {
    while (Wire.available()) {
      Wire.read();
    }
    return false;
  }

  for (uint8_t i = 0; i < count; i++) {
    dest[i] = Wire.read();
  }

  return true;
}

bool readByte(uint8_t address, uint8_t reg, uint8_t *value) {
  return readBytes(address, reg, 1, value);
}

int16_t makeInt16(uint8_t highByte, uint8_t lowByte) {
  return (int16_t)(((uint16_t)highByte << 8) | lowByte);
}

int16_t makeInt16Le(uint8_t lowByte, uint8_t highByte) {
  return (int16_t)(((uint16_t)highByte << 8) | lowByte);
}

bool validNumber(float value, float minValue, float maxValue) {
  return value == value && value >= minValue && value <= maxValue;
}

float wrapPi(float angle) {
  while (angle > PI) {
    angle -= 2.0f * PI;
  }
  while (angle < -PI) {
    angle += 2.0f * PI;
  }
  return angle;
}

float clampFloat(float value, float minValue, float maxValue) {
  if (value < minValue) {
    return minValue;
  }
  if (value > maxValue) {
    return maxValue;
  }
  return value;
}

void printMagAxisMap() {
  Serial.print(F("INFO,mag_axis_map,"));
  Serial.println(magAxisMap);
}

void clearMagCalibrationValues() {
  magBias[0] = 0.0f;
  magBias[1] = 0.0f;
  magBias[2] = 0.0f;
  magScale[0] = 1.0f;
  magScale[1] = 1.0f;
  magScale[2] = 1.0f;
  magCalibrated = false;
}

bool initMpu9250() {
  writeByte(MPU_ADDR, MPU_PWR_MGMT_1, 0x80);
  delay(100);
  writeByte(MPU_ADDR, MPU_PWR_MGMT_1, 0x01);
  delay(50);
  writeByte(MPU_ADDR, MPU_PWR_MGMT_2, 0x00);
  delay(10);

  uint8_t who = 0;
  if (!readByte(MPU_ADDR, MPU_WHO_AM_I, &who)) {
    Serial.println(F("ERR,mpu_whoami_read_failed"));
    return false;
  }

  Serial.print(F("INFO,mpu_whoami,0x"));
  Serial.println(who, HEX);
  if (who != 0x71 && who != 0x73) {
    Serial.println(F("ERR,unexpected_mpu_whoami"));
    return false;
  }

  writeByte(MPU_ADDR, MPU_CONFIG, 0x03);        // gyro DLPF about 41 Hz
  writeByte(MPU_ADDR, MPU_SMPLRT_DIV, 0x04);   // 200 Hz internal sample
  writeByte(MPU_ADDR, MPU_GYRO_CONFIG, 0x08);  // +/-500 dps
  writeByte(MPU_ADDR, MPU_ACCEL_CONFIG, 0x08); // +/-4g
  writeByte(MPU_ADDR, MPU_ACCEL_CONFIG2, 0x03);
  writeByte(MPU_ADDR, MPU_USER_CTRL, 0x00);    // disable MPU I2C master
  writeByte(MPU_ADDR, MPU_INT_PIN_CFG, 0x02);  // bypass AK8963 to host I2C
  delay(10);
  return true;
}

bool initAk8963() {
  writeByte(AK8963_ADDR, AK8963_CNTL2, 0x01);
  delay(100);

  uint8_t who = 0;
  if (!readByte(AK8963_ADDR, AK8963_WHO_AM_I, &who)) {
    Serial.println(F("ERR,ak8963_whoami_read_failed"));
    return false;
  }

  Serial.print(F("INFO,ak8963_whoami,0x"));
  Serial.println(who, HEX);
  if (who != 0x48) {
    Serial.println(F("ERR,unexpected_ak8963_whoami"));
    return false;
  }

  writeByte(AK8963_ADDR, AK8963_CNTL1, 0x00);
  delay(20);
  writeByte(AK8963_ADDR, AK8963_CNTL1, 0x0F); // fuse ROM access
  delay(20);

  uint8_t asa[3] = {0, 0, 0};
  if (!readBytes(AK8963_ADDR, AK8963_ASAX, 3, asa)) {
    Serial.println(F("ERR,ak8963_asa_read_failed"));
    return false;
  }

  for (uint8_t i = 0; i < 3; i++) {
    magFactoryAdj[i] = (((float)asa[i] - 128.0f) / 256.0f) + 1.0f;
  }

  writeByte(AK8963_ADDR, AK8963_CNTL1, 0x00);
  delay(20);
  writeByte(AK8963_ADDR, AK8963_CNTL1, 0x16); // 16-bit, continuous 100 Hz
  delay(20);
  return true;
}

bool readRawMpu(int16_t *accel, int16_t *gyro) {
  uint8_t raw[14];
  if (!readBytes(MPU_ADDR, MPU_ACCEL_XOUT_H, 14, raw)) {
    return false;
  }

  accel[0] = makeInt16(raw[0], raw[1]);
  accel[1] = makeInt16(raw[2], raw[3]);
  accel[2] = makeInt16(raw[4], raw[5]);
  gyro[0] = makeInt16(raw[8], raw[9]);
  gyro[1] = makeInt16(raw[10], raw[11]);
  gyro[2] = makeInt16(raw[12], raw[13]);
  return true;
}

void applyMagAxisMap(float akX, float akY, float akZ, float *magUt) {
  switch (magAxisMap) {
    case 0:
      magUt[0] = akY;
      magUt[1] = akX;
      magUt[2] = -akZ;
      break;
    case 1:
      magUt[0] = akX;
      magUt[1] = akY;
      magUt[2] = akZ;
      break;
    case 2:
      magUt[0] = -akY;
      magUt[1] = akX;
      magUt[2] = akZ;
      break;
    case 3:
      magUt[0] = akY;
      magUt[1] = -akX;
      magUt[2] = akZ;
      break;
    case 4:
      magUt[0] = -akX;
      magUt[1] = akY;
      magUt[2] = -akZ;
      break;
    case 5:
      magUt[0] = akX;
      magUt[1] = -akY;
      magUt[2] = -akZ;
      break;
    case 6:
      magUt[0] = -akY;
      magUt[1] = -akX;
      magUt[2] = -akZ;
      break;
    default:
      magUt[0] = -akX;
      magUt[1] = -akY;
      magUt[2] = akZ;
      break;
  }
}

bool readRawMagAligned(float *magUt) {
  uint8_t st1 = 0;
  if (!readByte(AK8963_ADDR, AK8963_ST1, &st1)) {
    return false;
  }
  if ((st1 & 0x01) == 0) {
    return false;
  }

  uint8_t raw[7];
  if (!readBytes(AK8963_ADDR, AK8963_HXL, 7, raw)) {
    return false;
  }
  if ((raw[6] & 0x08) != 0) {
    return false;
  }

  float akX = (float)makeInt16Le(raw[0], raw[1]) * MAG_UT_PER_LSB * magFactoryAdj[0];
  float akY = (float)makeInt16Le(raw[2], raw[3]) * MAG_UT_PER_LSB * magFactoryAdj[1];
  float akZ = (float)makeInt16Le(raw[4], raw[5]) * MAG_UT_PER_LSB * magFactoryAdj[2];

  applyMagAxisMap(akX, akY, akZ, magUt);
  return true;
}

bool readSensorFrame(SensorFrame *frame) {
  int16_t accelRaw[3];
  int16_t gyroRaw[3];
  if (!readRawMpu(accelRaw, gyroRaw)) {
    return false;
  }

  frame->ax = (float)accelRaw[0] / ACCEL_LSB_PER_G;
  frame->ay = (float)accelRaw[1] / ACCEL_LSB_PER_G;
  frame->az = (float)accelRaw[2] / ACCEL_LSB_PER_G;
  frame->gxDps = ((float)gyroRaw[0] / GYRO_LSB_PER_DPS) - gyroBiasDps[0];
  frame->gyDps = ((float)gyroRaw[1] / GYRO_LSB_PER_DPS) - gyroBiasDps[1];
  frame->gzDps = ((float)gyroRaw[2] / GYRO_LSB_PER_DPS) - gyroBiasDps[2];
  frame->stationary = false;

  float rawMag[3];
  frame->magValid = readRawMagAligned(rawMag);
  if (frame->magValid) {
    frame->mx = (rawMag[0] - magBias[0]) * magScale[0];
    frame->my = (rawMag[1] - magBias[1]) * magScale[1];
    frame->mz = (rawMag[2] - magBias[2]) * magScale[2];
    frame->magFieldUt = sqrt(frame->mx * frame->mx + frame->my * frame->my + frame->mz * frame->mz);
  } else {
    frame->mx = 0.0f;
    frame->my = 0.0f;
    frame->mz = 0.0f;
    frame->magFieldUt = 0.0f;
  }

  return true;
}

void reduceStationaryGyroDrift(SensorFrame *frame) {
  float accelNorm = sqrt(frame->ax * frame->ax + frame->ay * frame->ay + frame->az * frame->az);
  float gyroNorm = sqrt(frame->gxDps * frame->gxDps +
                        frame->gyDps * frame->gyDps +
                        frame->gzDps * frame->gzDps);

  frame->stationary = fabs(accelNorm - 1.0f) < ACCEL_STILL_TOLERANCE_G &&
                      gyroNorm < GYRO_STILL_DPS;
  if (frame->stationary) {
    gyroBiasDps[0] += frame->gxDps * GYRO_AUTO_ZERO_ALPHA;
    gyroBiasDps[1] += frame->gyDps * GYRO_AUTO_ZERO_ALPHA;
    gyroBiasDps[2] += frame->gzDps * GYRO_AUTO_ZERO_ALPHA;
  }

  if (fabs(frame->gxDps) < GYRO_DEADBAND_DPS) {
    frame->gxDps = 0.0f;
  }
  if (fabs(frame->gyDps) < GYRO_DEADBAND_DPS) {
    frame->gyDps = 0.0f;
  }
  if (fabs(frame->gzDps) < GYRO_DEADBAND_DPS) {
    frame->gzDps = 0.0f;
  }

  if (frame->stationary) {
    frame->gxDps = 0.0f;
    frame->gyDps = 0.0f;
    frame->gzDps = 0.0f;
  }
}

bool loadMagCalibration() {
  MagCalibrationStore stored;
  EEPROM.get(0, stored);

  if (stored.magic != CAL_MAGIC) {
    return false;
  }

  if (stored.axisMap >= MAG_AXIS_MAP_COUNT ||
      !validNumber(stored.biasX, -1000.0f, 1000.0f) ||
      !validNumber(stored.biasY, -1000.0f, 1000.0f) ||
      !validNumber(stored.biasZ, -1000.0f, 1000.0f) ||
      !validNumber(stored.scaleX, 0.05f, 20.0f) ||
      !validNumber(stored.scaleY, 0.05f, 20.0f) ||
      !validNumber(stored.scaleZ, 0.05f, 20.0f)) {
    return false;
  }

  magAxisMap = stored.axisMap;
  magBias[0] = stored.biasX;
  magBias[1] = stored.biasY;
  magBias[2] = stored.biasZ;
  magScale[0] = stored.scaleX;
  magScale[1] = stored.scaleY;
  magScale[2] = stored.scaleZ;
  magCalibrated = true;
  Serial.println(F("INFO,mag_calibration_loaded"));
  printMagAxisMap();
  return true;
}

void saveMagCalibration() {
  MagCalibrationStore stored;
  stored.magic = CAL_MAGIC;
  stored.axisMap = magAxisMap;
  stored.reserved0 = 0;
  stored.reserved1 = 0;
  stored.reserved2 = 0;
  stored.biasX = magBias[0];
  stored.biasY = magBias[1];
  stored.biasZ = magBias[2];
  stored.scaleX = magScale[0];
  stored.scaleY = magScale[1];
  stored.scaleZ = magScale[2];
  EEPROM.put(0, stored);
  Serial.println(F("INFO,mag_calibration_saved"));
}

void eraseMagCalibration() {
  MagCalibrationStore stored;
  stored.magic = 0;
  stored.axisMap = magAxisMap;
  stored.reserved0 = 0;
  stored.reserved1 = 0;
  stored.reserved2 = 0;
  stored.biasX = 0.0f;
  stored.biasY = 0.0f;
  stored.biasZ = 0.0f;
  stored.scaleX = 1.0f;
  stored.scaleY = 1.0f;
  stored.scaleZ = 1.0f;
  EEPROM.put(0, stored);

  clearMagCalibrationValues();
  Serial.println(F("INFO,mag_calibration_erased"));
}

void selectNextMagAxisMap() {
  magAxisMap = (uint8_t)((magAxisMap + 1) % MAG_AXIS_MAP_COUNT);
  eraseMagCalibration();
  printMagAxisMap();
  Serial.println(F("INFO,run_mag_cal_after_axis_map_change"));
}

void calibrateGyro() {
  Serial.println(F("CAL,gyro,hold_still"));

  float sum[3] = {0.0f, 0.0f, 0.0f};
  uint16_t used = 0;
  for (uint16_t i = 0; i < GYRO_CAL_SAMPLES; i++) {
    int16_t accelRaw[3];
    int16_t gyroRaw[3];
    if (readRawMpu(accelRaw, gyroRaw)) {
      sum[0] += (float)gyroRaw[0] / GYRO_LSB_PER_DPS;
      sum[1] += (float)gyroRaw[1] / GYRO_LSB_PER_DPS;
      sum[2] += (float)gyroRaw[2] / GYRO_LSB_PER_DPS;
      used++;
    }
    delay(3);
  }

  if (used > 0) {
    gyroBiasDps[0] = sum[0] / (float)used;
    gyroBiasDps[1] = sum[1] / (float)used;
    gyroBiasDps[2] = sum[2] / (float)used;
  }

  Serial.print(F("CAL,gyro_done,"));
  Serial.print(gyroBiasDps[0], 4);
  Serial.print(',');
  Serial.print(gyroBiasDps[1], 4);
  Serial.print(',');
  Serial.println(gyroBiasDps[2], 4);
}

bool calibrateMagnetometer(uint8_t seconds) {
  Serial.println(F("CAL,mag,rotate_all_axes"));
  printMagAxisMap();

  float minMag[3] = {100000.0f, 100000.0f, 100000.0f};
  float maxMag[3] = {-100000.0f, -100000.0f, -100000.0f};
  uint32_t start = millis();
  uint32_t nextPrint = 0;
  uint16_t used = 0;

  while ((uint32_t)(millis() - start) < (uint32_t)seconds * 1000UL) {
    float rawMag[3];
    if (readRawMagAligned(rawMag)) {
      for (uint8_t axis = 0; axis < 3; axis++) {
        if (rawMag[axis] < minMag[axis]) {
          minMag[axis] = rawMag[axis];
        }
        if (rawMag[axis] > maxMag[axis]) {
          maxMag[axis] = rawMag[axis];
        }
      }
      used++;
    }

    if ((int32_t)(millis() - nextPrint) >= 0) {
      uint8_t elapsed = (uint8_t)((millis() - start) / 1000UL);
      uint8_t remaining = elapsed >= seconds ? 0 : seconds - elapsed;
      Serial.print(F("CAL,mag_remaining,"));
      Serial.println(remaining);
      nextPrint = millis() + 1000UL;
    }

    delay(10);
  }

  if (used < 80) {
    Serial.println(F("ERR,mag_calibration_no_samples"));
    return false;
  }

  float halfRange[3];
  float averageRange = 0.0f;
  for (uint8_t axis = 0; axis < 3; axis++) {
    halfRange[axis] = (maxMag[axis] - minMag[axis]) * 0.5f;
    averageRange += halfRange[axis];
  }
  averageRange /= 3.0f;

  if (averageRange < 5.0f ||
      halfRange[0] < 5.0f ||
      halfRange[1] < 5.0f ||
      halfRange[2] < 5.0f) {
    Serial.println(F("ERR,mag_calibration_needs_more_rotation"));
    return false;
  }

  for (uint8_t axis = 0; axis < 3; axis++) {
    magBias[axis] = (maxMag[axis] + minMag[axis]) * 0.5f;
    magScale[axis] = averageRange / halfRange[axis];
  }
  magCalibrated = true;
  saveMagCalibration();

  Serial.print(F("CAL,mag_done,bias,"));
  Serial.print(magBias[0], 3);
  Serial.print(',');
  Serial.print(magBias[1], 3);
  Serial.print(',');
  Serial.print(magBias[2], 3);
  Serial.print(F(",scale,"));
  Serial.print(magScale[0], 4);
  Serial.print(',');
  Serial.print(magScale[1], 4);
  Serial.print(',');
  Serial.println(magScale[2], 4);
  return true;
}

float clampUnit(float value) {
  if (value > 1.0f) {
    return 1.0f;
  }
  if (value < -1.0f) {
    return -1.0f;
  }
  return value;
}

void normalizeQuaternion() {
  float norm = sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
  if (norm <= 0.0f) {
    q0 = 1.0f;
    q1 = 0.0f;
    q2 = 0.0f;
    q3 = 0.0f;
    return;
  }

  float invNorm = 1.0f / norm;
  q0 *= invNorm;
  q1 *= invNorm;
  q2 *= invNorm;
  q3 *= invNorm;
}

void setQuaternionFromEuler(float roll, float pitch, float yaw) {
  float cr = cos(roll * 0.5f);
  float sr = sin(roll * 0.5f);
  float cp = cos(pitch * 0.5f);
  float sp = sin(pitch * 0.5f);
  float cy = cos(yaw * 0.5f);
  float sy = sin(yaw * 0.5f);

  q0 = cr * cp * cy + sr * sp * sy;
  q1 = sr * cp * cy - cr * sp * sy;
  q2 = cr * sp * cy + sr * cp * sy;
  q3 = cr * cp * sy - sr * sp * cy;
  normalizeQuaternion();
}

bool isMagUsable(const SensorFrame *frame) {
  return frame->magValid &&
         magCalibrated &&
         frame->magFieldUt >= MAG_FIELD_MIN_UT &&
         frame->magFieldUt <= MAG_FIELD_MAX_UT;
}

float magYawFromTilt(float roll, float pitch, float mx, float my, float mz) {
  float sr = sin(roll);
  float cr = cos(roll);
  float sp = sin(pitch);
  float cp = cos(pitch);
  float mxh = mx * cp + mz * sp;
  float myh = mx * sr * sp + my * cr - mz * sr * cp;
  return wrapPi(atan2(-myh, mxh) + MAG_DECLINATION_DEG * DEG_TO_RAD_F);
}

void quaternionToEulerRad(float *yaw, float *pitch, float *roll) {
  *roll = atan2(2.0f * (q0 * q1 + q2 * q3),
                1.0f - 2.0f * (q1 * q1 + q2 * q2));
  *pitch = asin(clampUnit(2.0f * (q0 * q2 - q3 * q1)));
  *yaw = atan2(2.0f * (q0 * q3 + q1 * q2),
               1.0f - 2.0f * (q2 * q2 + q3 * q3));
}

void initializeOrientation(const SensorFrame *frame) {
  float roll = atan2(frame->ay, frame->az);
  float pitch = atan2(-frame->ax, sqrt(frame->ay * frame->ay + frame->az * frame->az));
  float yaw = 0.0f;

  if (isMagUsable(frame)) {
    yaw = magYawFromTilt(roll, pitch, frame->mx, frame->my, frame->mz);
  }

  setQuaternionFromEuler(roll, pitch, yaw);
  integralX = 0.0f;
  integralY = 0.0f;
  integralZ = 0.0f;
  orientationInitialized = true;
  Serial.println(F("INFO,orientation_initialized"));
}

void updateImuFilter(float gx, float gy, float gz, float ax, float ay, float az, float dt) {
  if (dt <= 0.0f) {
    return;
  }

  float norm = sqrt(ax * ax + ay * ay + az * az);
  if (norm > 0.25f) {
    float accelTrust = 1.0f - clampFloat(fabs(norm - 1.0f) * 4.0f, 0.0f, 1.0f);
    if (accelTrust > 0.0f) {
      ax /= norm;
      ay /= norm;
      az /= norm;

      float vx = 2.0f * (q1 * q3 - q0 * q2);
      float vy = 2.0f * (q0 * q1 + q2 * q3);
      float vz = q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3;

      float ex = (ay * vz - az * vy) * accelTrust;
      float ey = (az * vx - ax * vz) * accelTrust;
      float ez = (ax * vy - ay * vx) * accelTrust;

      if (ACCEL_KI > 0.0f) {
        integralX += ACCEL_KI * ex * dt;
        integralY += ACCEL_KI * ey * dt;
        integralZ += ACCEL_KI * ez * dt;
        gx += integralX;
        gy += integralY;
        gz += integralZ;
      }

      gx += ACCEL_KP * ex;
      gy += ACCEL_KP * ey;
      gz += ACCEL_KP * ez;
    }
  } else {
    integralX *= 0.995f;
    integralY *= 0.995f;
    integralZ *= 0.995f;
  }

  float qa = q0;
  float qb = q1;
  float qc = q2;
  float qd = q3;
  float halfDt = 0.5f * dt;

  q0 += (-qb * gx - qc * gy - qd * gz) * halfDt;
  q1 += (qa * gx + qc * gz - qd * gy) * halfDt;
  q2 += (qa * gy - qb * gz + qd * gx) * halfDt;
  q3 += (qa * gz + qb * gy - qc * gx) * halfDt;
  normalizeQuaternion();
}

void applyMagYawCorrection(const SensorFrame *frame, float dt) {
  if (!isMagUsable(frame) || dt <= 0.0f) {
    return;
  }

  float yaw;
  float pitch;
  float roll;
  quaternionToEulerRad(&yaw, &pitch, &roll);
  float targetYaw = magYawFromTilt(roll, pitch, frame->mx, frame->my, frame->mz);
  float yawError = wrapPi(targetYaw - yaw);
  float maxStep = MAG_YAW_MAX_RATE_DPS * DEG_TO_RAD_F * dt;
  float correction = clampFloat(yawError * MAG_YAW_BLEND, -maxStep, maxStep);

  if (fabs(correction) > 0.000001f) {
    setQuaternionFromEuler(roll, pitch, wrapPi(yaw + correction));
  }
}

void quaternionToEuler(float *yawDeg, float *pitchDeg, float *rollDeg) {
  float yaw;
  float pitch;
  float roll;
  quaternionToEulerRad(&yaw, &pitch, &roll);

  *yawDeg = yaw * RAD_TO_DEG_F;
  *pitchDeg = pitch * RAD_TO_DEG_F;
  *rollDeg = roll * RAD_TO_DEG_F;
}

void printTelemetry(const SensorFrame *frame) {
  uint32_t now = millis();
  if ((uint32_t)(now - lastOutputMillis) < OUTPUT_INTERVAL_MS) {
    return;
  }
  lastOutputMillis = now;

  float yawDeg;
  float pitchDeg;
  float rollDeg;
  quaternionToEuler(&yawDeg, &pitchDeg, &rollDeg);

  Serial.print(F("Q,"));
  Serial.print(q0, 6);
  Serial.print(',');
  Serial.print(q1, 6);
  Serial.print(',');
  Serial.print(q2, 6);
  Serial.print(',');
  Serial.print(q3, 6);
  Serial.print(F(",YPR,"));
  Serial.print(yawDeg, 2);
  Serial.print(',');
  Serial.print(pitchDeg, 2);
  Serial.print(',');
  Serial.print(rollDeg, 2);
  Serial.print(F(",M,"));
  Serial.print(frame->mx, 2);
  Serial.print(',');
  Serial.print(frame->my, 2);
  Serial.print(',');
  Serial.print(frame->mz, 2);
  Serial.print(F(",G,"));
  Serial.print(frame->gxDps, 3);
  Serial.print(',');
  Serial.print(frame->gyDps, 3);
  Serial.print(',');
  Serial.print(frame->gzDps, 3);
  Serial.print(F(",F,"));
  Serial.print(frame->magFieldUt, 2);
  Serial.print(F(",MAP,"));
  Serial.print(magAxisMap);
  Serial.print(F(",CAL,"));
  Serial.print(magCalibrated ? 1 : 0);
  Serial.print(F(",MAG,"));
  Serial.print(isMagUsable(frame) ? 1 : 0);
  Serial.print(F(",STILL,"));
  Serial.println(frame->stationary ? 1 : 0);
}

void handleSerialCommands() {
  while (Serial.available() > 0) {
    char command = (char)Serial.read();
    if (command == 'c' || command == 'C') {
      calibrateMagnetometer(MAG_CAL_SECONDS);
      orientationInitialized = false;
    } else if (command == 'g' || command == 'G') {
      calibrateGyro();
      orientationInitialized = false;
    } else if (command == 'x' || command == 'X') {
      eraseMagCalibration();
      orientationInitialized = false;
    } else if (command == 'm' || command == 'M') {
      selectNextMagAxisMap();
      orientationInitialized = false;
    } else if (command == 'r' || command == 'R') {
      orientationInitialized = false;
      Serial.println(F("INFO,orientation_reset"));
    }
  }
}

void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println(F("INFO,mpu9250_nine_axis_visual_test"));

  Wire.begin();
  Wire.setClock(400000UL);

  if (!initMpu9250()) {
    Serial.println(F("ERR,mpu_init_failed"));
    while (true) {
      delay(1000);
    }
  }

  if (!initAk8963()) {
    Serial.println(F("ERR,mag_init_failed"));
    while (true) {
      delay(1000);
    }
  }

  calibrateGyro();

  if (!loadMagCalibration()) {
    Serial.println(F("INFO,no_saved_mag_calibration"));
    printMagAxisMap();
    calibrateMagnetometer(MAG_CAL_SECONDS);
  }

  lastUpdateMicros = micros();
  lastOutputMillis = millis();
}

void loop() {
  handleSerialCommands();

  SensorFrame frame;
  if (!readSensorFrame(&frame)) {
    Serial.println(F("ERR,sensor_read_failed"));
    delay(20);
    return;
  }
  reduceStationaryGyroDrift(&frame);

  if (!orientationInitialized) {
    initializeOrientation(&frame);
    lastUpdateMicros = micros();
    return;
  }

  uint32_t nowMicros = micros();
  float dt = (float)(nowMicros - lastUpdateMicros) * 0.000001f;
  lastUpdateMicros = nowMicros;
  if (dt <= 0.0f || dt > 0.1f) {
    dt = 0.01f;
  }

  updateImuFilter(frame.gxDps * DEG_TO_RAD_F,
                  frame.gyDps * DEG_TO_RAD_F,
                  frame.gzDps * DEG_TO_RAD_F,
                  frame.ax,
                  frame.ay,
                  frame.az,
                  dt);
  applyMagYawCorrection(&frame, dt);

  printTelemetry(&frame);
}
