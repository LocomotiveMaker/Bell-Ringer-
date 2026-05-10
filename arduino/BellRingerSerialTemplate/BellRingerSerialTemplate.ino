#include <Adafruit_NeoPixel.h>
#include <Wire.h>

const unsigned long kBaudRate = 115200;
const unsigned long kTelemetryIntervalMs = 10;
const int kPixelsPerMatrix = 64;
const unsigned long kImuSampleIntervalUs = 10000UL;
const uint16_t kGyroCalibrationSamples = 300;
const float kAccelScale = 16384.0f;
const float kGyroScale = 131.0f;
const float kComplementaryTimeConstantSeconds = 0.35f;
const uint8_t kMpu9250AddressLow = 0x68;
const uint8_t kMpu9250AddressHigh = 0x69;
const uint8_t kRegisterWhoAmI = 0x75;
const uint8_t kRegisterPowerManagement1 = 0x6B;
const uint8_t kRegisterPowerManagement2 = 0x6C;
const uint8_t kRegisterSampleRateDivider = 0x19;
const uint8_t kRegisterConfig = 0x1A;
const uint8_t kRegisterGyroConfig = 0x1B;
const uint8_t kRegisterAccelConfig = 0x1C;
const uint8_t kRegisterAccelConfig2 = 0x1D;
const uint8_t kRegisterAccelXoutH = 0x3B;

#if defined(ARDUINO_ARCH_ESP32)
const bool kEnablePadImu = true;
const bool kEnableTelemetry = true;
const int kButtonPin = -1;
const int kLeftMatrixPin = 38;   // Header D6 on the Geekble nano ESP32-S3
const int kRightMatrixPin = 21;  // Header D7 on the Geekble nano ESP32-S3
const int kImuSdaPin = 11;       // Header A4
const int kImuSclPin = 12;       // Header A5 / SCL
#else
const bool kEnablePadImu = false;
const bool kEnableTelemetry = false;
const int kButtonPin = 2;
const int kLeftMatrixPin = 6;
const int kRightMatrixPin = 7;
#endif

Adafruit_NeoPixel leftMatrix(kPixelsPerMatrix, kLeftMatrixPin, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel rightMatrix(kPixelsPerMatrix, kRightMatrixPin, NEO_GRB + NEO_KHZ800);

String incomingLine;
unsigned long lastTelemetryAt = 0;
unsigned long lastImuSampleAtUs = 0;
uint8_t gMpuAddress = 0;
bool gHasPadOrientation = false;
float gPadYawDegrees = 0.0f;
float gPadPitchDegrees = 0.0f;
float gPadRollDegrees = 0.0f;
float gPadYawZeroDegrees = 0.0f;
float gPadPitchZeroDegrees = 0.0f;
float gPadRollZeroDegrees = 0.0f;
float gGyroBiasXDps = 0.0f;
float gGyroBiasYDps = 0.0f;
float gGyroBiasZDps = 0.0f;

struct ImuSample {
  float accelXG;
  float accelYG;
  float accelZG;
  float gyroXDps;
  float gyroYDps;
  float gyroZDps;
};

bool writeRegister(uint8_t deviceAddress, uint8_t registerAddress, uint8_t value) {
  Wire.beginTransmission(deviceAddress);
  Wire.write(registerAddress);
  Wire.write(value);
  return Wire.endTransmission() == 0;
}

bool readRegisters(uint8_t deviceAddress, uint8_t startRegister, uint8_t count, uint8_t* buffer) {
  Wire.beginTransmission(deviceAddress);
  Wire.write(startRegister);
  if (Wire.endTransmission(false) != 0) {
    return false;
  }

  uint8_t received = Wire.requestFrom(deviceAddress, count);
  if (received != count) {
    return false;
  }

  for (uint8_t index = 0; index < count; ++index) {
    buffer[index] = Wire.read();
  }

  return true;
}

int16_t combineInt16(uint8_t highByte, uint8_t lowByte) {
  return static_cast<int16_t>((static_cast<uint16_t>(highByte) << 8) | lowByte);
}

bool probeMpuDevice(uint8_t address, uint8_t& whoAmI) {
  return readRegisters(address, kRegisterWhoAmI, 1, &whoAmI);
}

bool detectPadMpuAddress() {
  uint8_t whoAmI = 0;
  if (probeMpuDevice(kMpu9250AddressLow, whoAmI)) {
    gMpuAddress = kMpu9250AddressLow;
    Serial.print("ACK imu_found_0x68 who=");
    Serial.println(whoAmI, HEX);
    return true;
  }

  if (probeMpuDevice(kMpu9250AddressHigh, whoAmI)) {
    gMpuAddress = kMpu9250AddressHigh;
    Serial.print("ACK imu_found_0x69 who=");
    Serial.println(whoAmI, HEX);
    return true;
  }

  return false;
}

bool initializePadMpu() {
  if (gMpuAddress == 0) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterPowerManagement1, 0x00)) {
    return false;
  }

  delay(100);

  if (!writeRegister(gMpuAddress, kRegisterPowerManagement1, 0x01)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterPowerManagement2, 0x00)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterConfig, 0x03)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterSampleRateDivider, 0x04)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterGyroConfig, 0x00)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterAccelConfig, 0x00)) {
    return false;
  }

  if (!writeRegister(gMpuAddress, kRegisterAccelConfig2, 0x03)) {
    return false;
  }

  return true;
}

bool readPadImuSample(ImuSample& sample) {
  if (gMpuAddress == 0) {
    return false;
  }

  uint8_t raw[14] = {0};
  if (!readRegisters(gMpuAddress, kRegisterAccelXoutH, sizeof(raw), raw)) {
    return false;
  }

  int16_t ax = combineInt16(raw[0], raw[1]);
  int16_t ay = combineInt16(raw[2], raw[3]);
  int16_t az = combineInt16(raw[4], raw[5]);
  int16_t gx = combineInt16(raw[8], raw[9]);
  int16_t gy = combineInt16(raw[10], raw[11]);
  int16_t gz = combineInt16(raw[12], raw[13]);

  sample.accelXG = ax / kAccelScale;
  sample.accelYG = ay / kAccelScale;
  sample.accelZG = az / kAccelScale;
  sample.gyroXDps = (gx / kGyroScale) - gGyroBiasXDps;
  sample.gyroYDps = (gy / kGyroScale) - gGyroBiasYDps;
  sample.gyroZDps = (gz / kGyroScale) - gGyroBiasZDps;
  return true;
}

void recenterPadImu() {
  gPadYawZeroDegrees = gPadYawDegrees;
  gPadPitchZeroDegrees = gPadPitchDegrees;
  gPadRollZeroDegrees = gPadRollDegrees;
}

void calibratePadGyroBias() {
  Serial.println("ACK imu_calibrating");

  float sumX = 0.0f;
  float sumY = 0.0f;
  float sumZ = 0.0f;
  uint16_t captured = 0;

  while (captured < kGyroCalibrationSamples) {
    ImuSample sample = {};
    if (!readPadImuSample(sample)) {
      delay(10);
      continue;
    }

    sumX += sample.gyroXDps + gGyroBiasXDps;
    sumY += sample.gyroYDps + gGyroBiasYDps;
    sumZ += sample.gyroZDps + gGyroBiasZDps;
    ++captured;
    delay(5);
  }

  gGyroBiasXDps = sumX / kGyroCalibrationSamples;
  gGyroBiasYDps = sumY / kGyroCalibrationSamples;
  gGyroBiasZDps = sumZ / kGyroCalibrationSamples;
  Serial.println("ACK imu_ready");
}

float wrapDegrees(float degrees) {
  while (degrees > 180.0f) {
    degrees -= 360.0f;
  }

  while (degrees < -180.0f) {
    degrees += 360.0f;
  }

  return degrees;
}

void updatePadOrientation(const ImuSample& sample, float deltaSeconds) {
  const float accelRollDegrees = atan2(sample.accelYG, sample.accelZG) * 180.0f / PI;
  const float accelPitchDegrees = atan2(-sample.accelXG, sqrt((sample.accelYG * sample.accelYG) + (sample.accelZG * sample.accelZG))) * 180.0f / PI;

  if (!gHasPadOrientation) {
    gPadRollDegrees = accelRollDegrees;
    gPadPitchDegrees = accelPitchDegrees;
    gPadYawDegrees = 0.0f;
    gHasPadOrientation = true;
    recenterPadImu();
    return;
  }

  const float alpha = kComplementaryTimeConstantSeconds / (kComplementaryTimeConstantSeconds + deltaSeconds);
  gPadRollDegrees = (alpha * (gPadRollDegrees + (sample.gyroXDps * deltaSeconds))) + ((1.0f - alpha) * accelRollDegrees);
  gPadPitchDegrees = (alpha * (gPadPitchDegrees + (sample.gyroYDps * deltaSeconds))) + ((1.0f - alpha) * accelPitchDegrees);
  gPadYawDegrees = wrapDegrees(gPadYawDegrees + (sample.gyroZDps * deltaSeconds));
}

void servicePadImu() {
  if (!kEnablePadImu || gMpuAddress == 0) {
    return;
  }

  unsigned long nowUs = micros();
  unsigned long elapsedUs = nowUs - lastImuSampleAtUs;
  if (elapsedUs < kImuSampleIntervalUs) {
    return;
  }

  lastImuSampleAtUs = nowUs;
  ImuSample sample = {};
  if (!readPadImuSample(sample)) {
    return;
  }

  updatePadOrientation(sample, elapsedUs * 0.000001f);
}

void waitForSerialReady() {
  unsigned long startedAt = millis();
  while (!Serial && (millis() - startedAt) < 1500UL) {
    delay(10);
  }
}

void startWireBus() {
#if defined(ARDUINO_ARCH_ESP32)
  Wire.begin(kImuSdaPin, kImuSclPin);
#else
  Wire.begin();
#endif
}

void setup() {
  if (kButtonPin >= 0) {
    pinMode(kButtonPin, INPUT_PULLUP);
  }
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  leftMatrix.begin();
  rightMatrix.begin();
  clearMatrices();
  showMatrices();

  Serial.begin(kBaudRate);
  waitForSerialReady();

  Serial.println("ACK boot");

  if (kEnablePadImu) {
    startWireBus();
    Wire.setClock(400000UL);

    if (detectPadMpuAddress() && initializePadMpu()) {
      calibratePadGyroBias();
      lastImuSampleAtUs = micros();
    } else {
      Serial.println("ACK imu_missing");
    }
  }
}

void loop() {
  readCommands();
  if (kEnablePadImu) {
    servicePadImu();
  }
  sendTelemetry();
}

void readCommands() {
  while (Serial.available() > 0) {
    char incoming = static_cast<char>(Serial.read());

    if (incoming == '\r') {
      continue;
    }

    if (incoming == '\n') {
      handleCommand(incomingLine);
      incomingLine = "";
      continue;
    }

    incomingLine += incoming;
  }
}

void handleCommand(const String& command) {
  if (command.length() == 0) {
    return;
  }

  digitalWrite(LED_BUILTIN, HIGH);

  if (command == "PING") {
    Serial.println("ACK ping");
  } else if (command == "IMU recenter" || command == "r" || command == "recenter") {
    if (kEnablePadImu) {
      recenterPadImu();
      Serial.println("ACK imu_recenter");
    } else {
      Serial.println("ACK imu_disabled");
    }
  } else if (command == "LED clear") {
    clearMatrices();
    showMatrices();
    Serial.println("ACK led_clear");
  } else if (command.startsWith("LED pulse")) {
    handleLedPulseCommand(command);
  } else if (command.startsWith("LED ripple")) {
    handleLedRippleCommand(command);
  } else if (command.startsWith("LED wall")) {
    handleLedWallCommand(command);
  } else if (command.startsWith("LED rain")) {
    handleLedRainCommand(command);
  } else if (command.startsWith("LED tinnitus")) {
    handleLedTinnitusCommand(command);
  } else if (command.startsWith("LED fill")) {
    handleLedFillCommand(command);
  } else if (command.startsWith("LED field")) {
    handleLedFieldCommand(command);
  } else if (command.startsWith("LED ")) {
    handleLedCommand(command);
  } else if (command.startsWith("OUT ")) {
    Serial.println("ACK out");
  } else {
    Serial.print("ACK unknown ");
    Serial.println(command);
  }

  delay(10);
  digitalWrite(LED_BUILTIN, LOW);
}

void sendTelemetry() {
  if (!kEnableTelemetry) {
    return;
  }

  unsigned long now = millis();
  if (now - lastTelemetryAt < kTelemetryIntervalMs) {
    return;
  }

  lastTelemetryAt = now;

  float headYaw = 0.0f;
  float headPitch = gHasPadOrientation ? wrapDegrees(gPadPitchDegrees - gPadPitchZeroDegrees) : 0.0f;
  float headRoll = gHasPadOrientation ? wrapDegrees(gPadRollDegrees - gPadRollZeroDegrees) : 0.0f;
  float handYaw = 0.0f;
  float handPitch = 0.0f;
  float handRoll = 0.0f;
  int button = (kButtonPin >= 0 && digitalRead(kButtonPin) == LOW) ? 1 : 0;

  Serial.print("hy=");
  Serial.print(headYaw, 1);
  Serial.print(",hp=");
  Serial.print(headPitch, 1);
  Serial.print(",hr=");
  Serial.print(headRoll, 1);
  Serial.print(",wy=");
  Serial.print(handYaw, 1);
  Serial.print(",wp=");
  Serial.print(handPitch, 1);
  Serial.print(",wr=");
  Serial.print(handRoll, 1);
  Serial.print(",btn=");
  Serial.println(button);
}

void handleLedCommand(const String& command) {
  int x = 0;
  int y = 0;
  int brightness = 0;

  if (!tryParseInt(command, "x=", x) || !tryParseInt(command, "y=", y) || !tryParseInt(command, "b=", brightness)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  x = constrain(x, 0, 15);
  y = constrain(y, 0, 7);
  brightness = constrain(brightness, 0, 255);

  clearMatrices();
  drawWhiteDot(x, y, brightness);
  showMatrices();
  Serial.println("ACK led_dot");
}

void handleLedFillCommand(const String& command) {
  int brightness = 0;

  if (!tryParseInt(command, "b=", brightness)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  brightness = constrain(brightness, 0, 255);
  fillMatricesWhite(brightness);
  showMatrices();
  Serial.println("ACK led_fill");
}

void handleLedFieldCommand(const String& command) {
  int red = 255;
  int green = 255;
  int blue = 255;
  float level = 0.0f;

  if (!tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);
  level = constrain(level, 0.0f, 1.0f);

  uint32_t color = leftMatrix.Color(
    constrain(static_cast<int>(red * level), 0, 255),
    constrain(static_cast<int>(green * level), 0, 255),
    constrain(static_cast<int>(blue * level), 0, 255));

  for (int i = 0; i < kPixelsPerMatrix; i++) {
    leftMatrix.setPixelColor(i, color);
    rightMatrix.setPixelColor(i, color);
  }

  showMatrices();
  Serial.println("ACK led_field");
}

void handleLedRippleCommand(const String& command) {
  float centerX = 7.5f;
  float centerY = 3.5f;
  float radius = 0.0f;
  float width = 1.0f;
  float level = 0.0f;
  int red = 0;
  int green = 255;
  int blue = 64;

  if (!tryParseFloat(command, "cx=", centerX) ||
      !tryParseFloat(command, "cy=", centerY) ||
      !tryParseFloat(command, "radius=", radius) ||
      !tryParseFloat(command, "width=", width) ||
      !tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  centerX = constrain(centerX, 0.0f, 15.0f);
  centerY = constrain(centerY, 0.0f, 7.0f);
  radius = max(0.0f, radius);
  width = max(0.1f, width);
  level = constrain(level, 0.0f, 1.0f);
  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);

  float halfWidth = max(0.05f, width * 0.5f);

  for (int y = 0; y < 8; y++) {
    for (int x = 0; x < 16; x++) {
      float dx = static_cast<float>(x) - centerX;
      float dy = static_cast<float>(y) - centerY;
      float distance = sqrt((dx * dx) + (dy * dy));
      float alpha = 1.0f - (abs(distance - radius) / halfWidth);
      alpha = constrain(alpha, 0.0f, 1.0f);
      int scaledRed = constrain(static_cast<int>(red * level * alpha), 0, 255);
      int scaledGreen = constrain(static_cast<int>(green * level * alpha), 0, 255);
      int scaledBlue = constrain(static_cast<int>(blue * level * alpha), 0, 255);
      setMappedPixelColor(x, y, leftMatrix.Color(scaledRed, scaledGreen, scaledBlue));
    }
  }

  showMatrices();
  Serial.println("ACK led_ripple");
}

void handleLedPulseCommand(const String& command) {
  float centerX = 7.5f;
  float centerY = 3.5f;
  float radius = 0.0f;
  float core = 0.65f;
  float width = 1.0f;
  float level = 0.0f;
  float contrast = 1.0f;
  int red = 0;
  int green = 255;
  int blue = 64;

  if (!tryParseFloat(command, "cx=", centerX) ||
      !tryParseFloat(command, "cy=", centerY) ||
      !tryParseFloat(command, "radius=", radius) ||
      !tryParseFloat(command, "core=", core) ||
      !tryParseFloat(command, "width=", width) ||
      !tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  tryParseOptionalFloat(command, "contrast=", contrast);

  centerX = constrain(centerX, 0.0f, 15.0f);
  centerY = constrain(centerY, 0.0f, 7.0f);
  radius = constrain(radius, 0.0f, 8.0f);
  core = constrain(core, 0.1f, 4.0f);
  width = constrain(width, 0.1f, 4.0f);
  level = constrain(level, 0.0f, 1.0f);
  contrast = constrain(contrast, 0.1f, 5.0f);
  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);

  float halfWidth = max(0.05f, width * 0.5f);

  for (int y = 0; y < 8; y++) {
    for (int x = 0; x < 16; x++) {
      float dx = static_cast<float>(x) - centerX;
      float dy = static_cast<float>(y) - centerY;
      float distance = sqrt((dx * dx) + (dy * dy));
      float coreAlpha = exp(-(distance * distance) / (2.0f * core * core));
      float ringAlpha = 1.0f - (abs(distance - radius) / halfWidth);
      ringAlpha = constrain(ringAlpha, 0.0f, 1.0f) * 0.75f;
      float alpha = pow(constrain(max(coreAlpha, ringAlpha), 0.0f, 1.0f), contrast) * level;
      int scaledRed = constrain(static_cast<int>(red * alpha), 0, 255);
      int scaledGreen = constrain(static_cast<int>(green * alpha), 0, 255);
      int scaledBlue = constrain(static_cast<int>(blue * alpha), 0, 255);
      setMappedPixelColor(x, y, leftMatrix.Color(scaledRed, scaledGreen, scaledBlue));
    }
  }

  showMatrices();
  Serial.println("ACK led_pulse");
}

void handleLedWallCommand(const String& command) {
  float centerX = 7.5f;
  float centerY = 3.5f;
  float width = 8.0f;
  float height = 4.0f;
  float level = 0.0f;
  float density = 1.0f;
  float contrast = 1.0f;
  int red = 160;
  int green = 160;
  int blue = 160;
  int seed = 0;

  if (!tryParseFloat(command, "cx=", centerX) ||
      !tryParseFloat(command, "cy=", centerY) ||
      !tryParseFloat(command, "w=", width) ||
      !tryParseFloat(command, "h=", height) ||
      !tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level) ||
      !tryParseInt(command, "seed=", seed)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  tryParseOptionalFloat(command, "density=", density);
  tryParseOptionalFloat(command, "contrast=", contrast);

  centerX = constrain(centerX, 0.0f, 15.0f);
  centerY = constrain(centerY, 0.0f, 7.0f);
  width = constrain(width, 0.1f, 16.0f);
  height = constrain(height, 0.1f, 8.0f);
  level = constrain(level, 0.0f, 1.0f);
  density = constrain(density, 0.0f, 1.0f);
  contrast = constrain(contrast, 0.1f, 5.0f);
  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);

  float halfWidth = width * 0.5f;
  float halfHeight = height * 0.5f;

  for (int y = 0; y < 8; y++) {
    for (int x = 0; x < 16; x++) {
      float dx = abs(static_cast<float>(x) - centerX);
      float dy = abs(static_cast<float>(y) - centerY);
      float inside = (dx <= halfWidth && dy <= halfHeight) ? 1.0f : 0.0f;
      float edgeX = halfWidth <= 0.0f ? 0.0f : constrain((halfWidth - dx) / 1.5f, 0.0f, 1.0f);
      float edgeY = halfHeight <= 0.0f ? 0.0f : constrain(((centerY + halfHeight) - static_cast<float>(y)) / 1.5f, 0.0f, 1.0f);
      float glitch = 0.25f + (static_cast<float>(hashByte(seed, x, y, 17)) / 255.0f) * 0.75f;
      float sparse = static_cast<float>(hashByte(seed, x, y, 71)) / 255.0f;
      float active = sparse <= density ? 1.0f : 0.0f;
      float alpha = inside * active * edgeX * edgeY * pow(glitch, contrast) * level;
      int scaledRed = constrain(static_cast<int>(red * alpha), 0, 255);
      int scaledGreen = constrain(static_cast<int>(green * alpha), 0, 255);
      int scaledBlue = constrain(static_cast<int>(blue * alpha), 0, 255);
      setMappedPixelColor(x, y, leftMatrix.Color(scaledRed, scaledGreen, scaledBlue));
    }
  }

  showMatrices();
  Serial.println("ACK led_wall");
}

void handleLedRainCommand(const String& command) {
  float centerX = 7.5f;
  float centerY = 1.5f;
  float width = 16.0f;
  float height = 3.0f;
  float level = 0.0f;
  float density = 1.0f;
  float contrast = 1.0f;
  int red = 5;
  int green = 31;
  int blue = 255;
  int seed = 0;
  float phase = 0.0f;

  if (!tryParseFloat(command, "cx=", centerX) ||
      !tryParseFloat(command, "cy=", centerY) ||
      !tryParseFloat(command, "w=", width) ||
      !tryParseFloat(command, "h=", height) ||
      !tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level) ||
      !tryParseInt(command, "seed=", seed) ||
      !tryParseFloat(command, "phase=", phase)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  tryParseOptionalFloat(command, "density=", density);
  tryParseOptionalFloat(command, "contrast=", contrast);

  centerX = constrain(centerX, 0.0f, 15.0f);
  centerY = constrain(centerY, 0.0f, 7.0f);
  width = constrain(width, 0.1f, 16.0f);
  height = constrain(height, 0.1f, 8.0f);
  level = constrain(level, 0.0f, 1.0f);
  density = constrain(density, 0.0f, 1.0f);
  contrast = constrain(contrast, 0.1f, 5.0f);
  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);

  float halfWidth = width * 0.5f;
  float halfHeight = height * 0.5f;

  for (int y = 0; y < 8; y++) {
    for (int x = 0; x < 16; x++) {
      float areaDx = abs(static_cast<float>(x) - centerX);
      float areaDy = abs(static_cast<float>(y) - centerY);
      float inside = (areaDx <= halfWidth && areaDy <= halfHeight) ? 1.0f : 0.0f;
      if (inside <= 0.0f) {
        setMappedPixelColor(x, y, leftMatrix.Color(0, 0, 0));
        continue;
      }

      float alpha = 0.0f;

      int dropCount = constrain(static_cast<int>(2 + density * 5.0f), 2, 7);
      for (int drop = 0; drop < dropCount; drop++) {
        float dropX = (centerX - halfWidth) + (static_cast<float>(hashByte(seed, drop, 3, 11)) / 255.0f) * width;
        float dropY = (centerY - halfHeight) + (static_cast<float>(hashByte(seed, drop, 7, 19)) / 255.0f) * height;
        float offset = static_cast<float>(hashByte(seed, drop, 13, 23)) / 255.0f;
        float radius = fmod((phase * 1.15f) + (offset * 2.8f), 2.8f);
        float dx = static_cast<float>(x) - dropX;
        float dy = static_cast<float>(y) - dropY;
        float distance = sqrt((dx * dx) + (dy * dy));
        float ring = 1.0f - (abs(distance - radius) / 1.15f);
        alpha = max(alpha, constrain(ring, 0.0f, 1.0f));
      }

      float edgeX = width >= 15.9f ? 1.0f : (halfWidth <= 0.0f ? 0.0f : constrain((halfWidth - areaDx) / 1.5f, 0.0f, 1.0f));
      float edgeY = halfHeight <= 0.0f ? 0.0f : constrain(((centerY + halfHeight) - static_cast<float>(y)) / 1.25f, 0.0f, 1.0f);
      alpha = pow(constrain(alpha, 0.0f, 1.0f), contrast) * edgeX * edgeY * level;
      int scaledRed = constrain(static_cast<int>(red * alpha), 0, 255);
      int scaledGreen = constrain(static_cast<int>(green * alpha), 0, 255);
      int scaledBlue = constrain(static_cast<int>(blue * alpha), 0, 255);
      setMappedPixelColor(x, y, leftMatrix.Color(scaledRed, scaledGreen, scaledBlue));
    }
  }

  showMatrices();
  Serial.println("ACK led_rain");
}

void handleLedTinnitusCommand(const String& command) {
  float centerX = 7.5f;
  float centerY = 3.5f;
  float core = 0.65f;
  float tear = 0.0f;
  float axisX = 1.0f;
  float axisY = 0.0f;
  float level = 0.0f;
  float contrast = 1.0f;
  int red = 255;
  int green = 0;
  int blue = 0;
  int seed = 0;
  float instability = 0.0f;
  float smear = 1.0f;

  if (!tryParseFloat(command, "cx=", centerX) ||
      !tryParseFloat(command, "cy=", centerY) ||
      !tryParseFloat(command, "core=", core) ||
      !tryParseFloat(command, "tear=", tear) ||
      !tryParseFloat(command, "axisX=", axisX) ||
      !tryParseFloat(command, "axisY=", axisY) ||
      !tryParseInt(command, "red=", red) ||
      !tryParseInt(command, "green=", green) ||
      !tryParseInt(command, "blue=", blue) ||
      !tryParseFloat(command, "level=", level) ||
      !tryParseInt(command, "seed=", seed) ||
      !tryParseFloat(command, "instability=", instability) ||
      !tryParseFloat(command, "smear=", smear)) {
    Serial.println("ACK led_parse_error");
    return;
  }

  tryParseOptionalFloat(command, "contrast=", contrast);

  centerX = constrain(centerX, 0.0f, 15.0f);
  centerY = constrain(centerY, 0.0f, 7.0f);
  core = constrain(core, 0.1f, 4.0f);
  tear = constrain(tear, 0.0f, 8.0f);
  level = constrain(level, 0.0f, 1.0f);
  red = constrain(red, 0, 255);
  green = constrain(green, 0, 255);
  blue = constrain(blue, 0, 255);
  instability = constrain(instability, 0.0f, 1.0f);
  smear = constrain(smear, 0.1f, 8.0f);
  contrast = constrain(contrast, 0.1f, 5.0f);

  float axisLength = sqrt((axisX * axisX) + (axisY * axisY));
  if (axisLength <= 0.001f) {
    axisX = 1.0f;
    axisY = 0.0f;
  } else {
    axisX /= axisLength;
    axisY /= axisLength;
  }

  float perpendicularX = -axisY;
  float perpendicularY = axisX;
  float coreSigma = max(0.18f, core);
  float lobeSigma = 0.35f + instability * 0.35f;

  for (int y = 0; y < 8; y++) {
    for (int x = 0; x < 16; x++) {
      float dx = static_cast<float>(x) - centerX;
      float dy = static_cast<float>(y) - centerY;
      float distanceSquared = (dx * dx) + (dy * dy);
      float coreAlpha = exp(-distanceSquared / (2.0f * coreSigma * coreSigma));
      float along = (dx * axisX) + (dy * axisY);
      float across = abs((dx * perpendicularX) + (dy * perpendicularY));
      float splitDistance = abs(abs(along) - tear);
      float tearAlpha = exp(-(splitDistance * splitDistance) / (2.0f * lobeSigma * lobeSigma)) *
                        exp(-(across * across) / (2.0f * 0.32f * 0.32f)) *
                        instability;
      float smearAlpha = exp(-abs(along) / max(0.1f, tear + smear)) *
                         exp(-(across * across) / (2.0f * 0.75f * 0.75f)) *
                         instability * 0.22f;
      float alpha = pow(constrain(max(coreAlpha, max(tearAlpha, smearAlpha)), 0.0f, 1.0f), contrast) * level;
      int scaledRed = constrain(static_cast<int>(red * alpha), 0, 255);
      int scaledGreen = constrain(static_cast<int>(green * alpha), 0, 255);
      int scaledBlue = constrain(static_cast<int>(blue * alpha), 0, 255);
      setMappedPixelColor(x, y, leftMatrix.Color(scaledRed, scaledGreen, scaledBlue));
    }
  }

  showMatrices();
  Serial.println("ACK led_tinnitus");
}

bool tryParseInt(const String& source, const char* token, int& value) {
  int startIndex = source.indexOf(token);
  if (startIndex < 0) {
    return false;
  }

  startIndex += static_cast<int>(strlen(token));
  int endIndex = source.indexOf(' ', startIndex);
  String rawValue = endIndex < 0 ? source.substring(startIndex) : source.substring(startIndex, endIndex);
  rawValue.trim();

  if (rawValue.length() == 0) {
    return false;
  }

  value = rawValue.toInt();
  return true;
}

bool tryParseFloat(const String& source, const char* token, float& value) {
  int startIndex = source.indexOf(token);
  if (startIndex < 0) {
    return false;
  }

  startIndex += static_cast<int>(strlen(token));
  int endIndex = source.indexOf(' ', startIndex);
  String rawValue = endIndex < 0 ? source.substring(startIndex) : source.substring(startIndex, endIndex);
  rawValue.trim();

  if (rawValue.length() == 0) {
    return false;
  }

  value = rawValue.toFloat();
  return true;
}

void tryParseOptionalFloat(const String& source, const char* token, float& value) {
  float parsedValue = value;
  if (tryParseFloat(source, token, parsedValue)) {
    value = parsedValue;
  }
}

void drawWhiteDot(int globalX, int globalY, int brightness) {
  uint32_t color = leftMatrix.Color(brightness, brightness, brightness);
  setMappedPixelColor(globalX, globalY, color);
}

void setMappedPixelColor(int globalX, int globalY, uint32_t color) {
  if (globalX < 8) {
    int pixelIndex = mapLeftMatrixIndex(globalX, globalY);
    leftMatrix.setPixelColor(pixelIndex, color);
    return;
  }

  int pixelIndex = mapRightMatrixIndex(globalX - 8, globalY);
  rightMatrix.setPixelColor(pixelIndex, color);
}

int mapLeftMatrixIndex(int localX, int localY) {
  return (localX * 8) + localY;
}

int mapRightMatrixIndex(int localX, int localY) {
  int physicalColumn = 7 - localX;
  return (physicalColumn * 8) + (7 - localY);
}

void clearMatrices() {
  for (int i = 0; i < kPixelsPerMatrix; i++) {
    leftMatrix.setPixelColor(i, 0);
    rightMatrix.setPixelColor(i, 0);
  }
}

void fillMatricesWhite(int brightness) {
  uint32_t color = leftMatrix.Color(brightness, brightness, brightness);

  for (int i = 0; i < kPixelsPerMatrix; i++) {
    leftMatrix.setPixelColor(i, color);
    rightMatrix.setPixelColor(i, color);
  }
}

byte hashByte(int seed, int x, int y, int salt) {
  unsigned long value = static_cast<unsigned long>(seed);
  value ^= static_cast<unsigned long>(x + 37) * 1103515245UL;
  value ^= static_cast<unsigned long>(y + 101) * 12345UL;
  value ^= static_cast<unsigned long>(salt + 17) * 2654435761UL;
  value ^= value >> 16;
  value *= 2246822519UL;
  value ^= value >> 13;
  return static_cast<byte>(value & 0xFF);
}

void showMatrices() {
  leftMatrix.show();
  rightMatrix.show();
}
