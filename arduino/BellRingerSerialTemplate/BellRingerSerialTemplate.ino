#include <Adafruit_NeoPixel.h>

const unsigned long kBaudRate = 115200;
const bool kEnableTelemetry = false;
const unsigned long kTelemetryIntervalMs = 50;
const int kButtonPin = 2;
const int kLeftMatrixPin = 6;
const int kRightMatrixPin = 7;
const int kPixelsPerMatrix = 64;

Adafruit_NeoPixel leftMatrix(kPixelsPerMatrix, kLeftMatrixPin, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel rightMatrix(kPixelsPerMatrix, kRightMatrixPin, NEO_GRB + NEO_KHZ800);

String incomingLine;
unsigned long lastTelemetryAt = 0;

void setup() {
  pinMode(kButtonPin, INPUT_PULLUP);
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  leftMatrix.begin();
  rightMatrix.begin();
  clearMatrices();
  showMatrices();

  Serial.begin(kBaudRate);
  while (!Serial) {
  }

  Serial.println("ACK boot");
}

void loop() {
  readCommands();
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

  float headYaw = mapFloat(analogRead(A0), 0, 1023, -180.0f, 180.0f);
  float headPitch = mapFloat(analogRead(A1), 0, 1023, -90.0f, 90.0f);
  float headRoll = mapFloat(analogRead(A2), 0, 1023, -45.0f, 45.0f);
  float handYaw = mapFloat(analogRead(A3), 0, 1023, -180.0f, 180.0f);
  float handPitch = mapFloat(analogRead(A4), 0, 1023, -90.0f, 90.0f);
  float handRoll = mapFloat(analogRead(A5), 0, 1023, -45.0f, 45.0f);
  int button = digitalRead(kButtonPin) == LOW ? 1 : 0;

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

float mapFloat(long value, long inMin, long inMax, float outMin, float outMax) {
  return (static_cast<float>(value - inMin) * (outMax - outMin) / static_cast<float>(inMax - inMin)) + outMin;
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
