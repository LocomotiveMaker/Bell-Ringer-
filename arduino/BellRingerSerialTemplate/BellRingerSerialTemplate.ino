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
  } else if (command.startsWith("LED ripple")) {
    handleLedRippleCommand(command);
  } else if (command.startsWith("LED fill")) {
    handleLedFillCommand(command);
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

void showMatrices() {
  leftMatrix.show();
  rightMatrix.show();
}
