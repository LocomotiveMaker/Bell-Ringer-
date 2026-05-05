using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.Hardware
{
    public sealed class HardwareBridge : MonoBehaviour
    {
        public const string SerialPortEnvName = "BELL_RINGER_SERIAL_PORT";
        public const string SerialBaudEnvName = "BELL_RINGER_SERIAL_BAUD";
        public const string SimulateHardwareEnvName = "BELL_RINGER_SIMULATE_HARDWARE";

        [SerializeField] private string preferredPortName = "COM9";
        [SerializeField] private int baudRate = 115200;
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private bool allowSimulationFallback = true;
        [SerializeField] private bool enableKeyboardSimulation = true;
        [SerializeField] private float reconnectIntervalSeconds = 3f;
        [SerializeField] private float simulationYawSpeedDegreesPerSecond = 90f;
        [SerializeField] private float simulationPitchSpeedDegreesPerSecond = 60f;

        private object _serialPort;
        private readonly StringBuilder _serialBuffer = new StringBuilder(1024);
        private HardwareTelemetry _telemetry;
        private string[] _availablePorts = Array.Empty<string>();
        private string _lastTelemetryRaw = string.Empty;
        private string _lastCommand = string.Empty;
        private string _lastError = string.Empty;
        private DateTime _lastUpdateUtc = DateTime.MinValue;
        private bool _simulateHardware;
        private bool _simulationButtonHeld;
        private bool _initialized;
        private float _nextReconnectTime;

        public static HardwareBridge Instance { get; private set; }

        public bool IsConnected => _serialPort != null && GetBooleanProperty(_serialPort, "IsOpen");
        public bool IsSimulation => _simulateHardware;
        public string ActivePortName => IsConnected ? GetStringProperty(_serialPort, "PortName") : preferredPortName;
        public int ActiveBaudRate => baudRate;
        public HardwareTelemetry CurrentTelemetry => _telemetry;

        public void SetSimulationTelemetry(HardwareTelemetry telemetry)
        {
            InitializeNow();
            _telemetry = telemetry;
            _simulateHardware = true;
            _lastTelemetryRaw = telemetry.ToString();
            _lastUpdateUtc = DateTime.UtcNow;
        }

        public HardwareStatusSnapshot GetStatusSnapshot()
        {
            return new HardwareStatusSnapshot
            {
                isConnected = IsConnected,
                isSimulation = IsSimulation,
                portName = string.IsNullOrEmpty(ActivePortName) ? "(auto)" : ActivePortName,
                baudRate = baudRate,
                lastError = _lastError,
                availablePorts = _availablePorts,
                lastTelemetryRaw = _lastTelemetryRaw,
                lastCommand = _lastCommand,
                lastUpdateUtc = _lastUpdateUtc == DateTime.MinValue ? string.Empty : _lastUpdateUtc.ToString("O", CultureInfo.InvariantCulture),
                telemetry = _telemetry,
            };
        }

        public void SendPing()
        {
            SendCommand("PING");
        }

        public void SendDebugFeedback(float vibrationNormalized, Color lightColor, float pulseNormalized)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(lightColor.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(lightColor.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(lightColor.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "OUT vib={0:0.00} lr={1} lg={2} lb={3} pulse={4:0.00}",
                Mathf.Clamp01(vibrationNormalized),
                red,
                green,
                blue,
                Mathf.Clamp01(pulseNormalized));

            SendCommand(command);
        }

        public void SendLedDot(int x, int y, float brightnessNormalized)
        {
            int clampedX = Mathf.Clamp(x, 0, 15);
            int clampedY = Mathf.Clamp(y, 0, 7);
            int brightness = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(brightnessNormalized) * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED x={0} y={1} b={2}",
                clampedX,
                clampedY,
                brightness);

            SendCommand(command);
        }

        public void SendLedFill(float brightnessNormalized)
        {
            int brightness = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(brightnessNormalized) * 255f), 0, 255);
            string command = string.Format(CultureInfo.InvariantCulture, "LED fill b={0}", brightness);
            SendCommand(command);
        }

        public void SendLedField(Color color, float brightnessNormalized)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED field red={0} green={1} blue={2} level={3:0.00}",
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized));

            SendCommand(command);
        }

        public void SendLedRipple(float centerX, float centerY, float radiusPixels, float widthPixels, Color color, float brightnessNormalized)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED ripple cx={0:0.00} cy={1:0.00} radius={2:0.00} width={3:0.00} red={4} green={5} blue={6} level={7:0.00}",
                Mathf.Clamp(centerX, 0f, 15f),
                Mathf.Clamp(centerY, 0f, 7f),
                Mathf.Max(0f, radiusPixels),
                Mathf.Max(0.1f, widthPixels),
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized));

            SendCommand(command);
        }

        public void SendLedPulseCore(float centerX, float centerY, float radiusPixels, float coreSizePixels, float widthPixels, Color color, float brightnessNormalized, float contrast)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED pulse cx={0:0.00} cy={1:0.00} radius={2:0.00} core={3:0.00} width={4:0.00} red={5} green={6} blue={7} level={8:0.00} contrast={9:0.00}",
                Mathf.Clamp(centerX, 0f, 15f),
                Mathf.Clamp(centerY, 0f, 7f),
                Mathf.Clamp(radiusPixels, 0f, 8f),
                Mathf.Clamp(coreSizePixels, 0.1f, 4f),
                Mathf.Clamp(widthPixels, 0.1f, 4f),
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized),
                Mathf.Clamp(contrast, 0.1f, 5f));

            SendCommand(command);
        }

        public void SendLedWallNoise(float centerX, float centerY, float widthPixels, float heightPixels, Color color, float brightnessNormalized, int seed, float density = 1f, float contrast = 1f)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED wall cx={0:0.00} cy={1:0.00} w={2:0.00} h={3:0.00} red={4} green={5} blue={6} level={7:0.00} seed={8} density={9:0.00} contrast={10:0.00}",
                Mathf.Clamp(centerX, 0f, 15f),
                Mathf.Clamp(centerY, 0f, 7f),
                Mathf.Clamp(widthPixels, 0.1f, 16f),
                Mathf.Clamp(heightPixels, 0.1f, 8f),
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized),
                seed,
                Mathf.Clamp01(density),
                Mathf.Clamp(contrast, 0.1f, 5f));

            SendCommand(command);
        }

        public void SendLedRain(
            Color color,
            float brightnessNormalized,
            int seed,
            float centerX = 7.5f,
            float centerY = 1.5f,
            float widthPixels = 16f,
            float heightPixels = 3f,
            float phase = 0f,
            float density = 1f,
            float contrast = 1f)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED rain cx={0:0.00} cy={1:0.00} w={2:0.00} h={3:0.00} red={4} green={5} blue={6} level={7:0.00} seed={8} phase={9:0.00} density={10:0.00} contrast={11:0.00}",
                Mathf.Clamp(centerX, 0f, 15f),
                Mathf.Clamp(centerY, 0f, 7f),
                Mathf.Clamp(widthPixels, 0.1f, 16f),
                Mathf.Clamp(heightPixels, 0.1f, 8f),
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized),
                seed,
                Mathf.Max(0f, phase),
                Mathf.Clamp01(density),
                Mathf.Clamp(contrast, 0.1f, 5f));

            SendCommand(command);
        }

        public void SendLedTinnitus(
            float centerX,
            float centerY,
            float coreSize,
            float tearAmount,
            float axisX,
            float axisY,
            Color color,
            float brightnessNormalized,
            int seed,
            float instability,
            float smearDecay,
            float contrast = 1f)
        {
            int red = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int green = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int blue = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);

            string command = string.Format(
                CultureInfo.InvariantCulture,
                "LED tinnitus cx={0:0.00} cy={1:0.00} core={2:0.00} tear={3:0.00} axisX={4:0.00} axisY={5:0.00} red={6} green={7} blue={8} level={9:0.00} seed={10} instability={11:0.00} smear={12:0.00} contrast={13:0.00}",
                Mathf.Clamp(centerX, 0f, 15f),
                Mathf.Clamp(centerY, 0f, 7f),
                Mathf.Clamp(coreSize, 0.1f, 4f),
                Mathf.Clamp(tearAmount, 0f, 8f),
                Mathf.Clamp(axisX, -1f, 1f),
                Mathf.Clamp(axisY, -1f, 1f),
                red,
                green,
                blue,
                Mathf.Clamp01(brightnessNormalized),
                seed,
                Mathf.Clamp01(instability),
                Mathf.Clamp(smearDecay, 0.1f, 8f),
                Mathf.Clamp(contrast, 0.1f, 5f));

            SendCommand(command);
        }

        public void ClearLedDisplay()
        {
            SendCommand("LED clear");
        }

        public static string BuildEnvironmentSummary()
        {
            string portOverride = Environment.GetEnvironmentVariable(SerialPortEnvName);
            string baudOverride = Environment.GetEnvironmentVariable(SerialBaudEnvName);
            string simulationOverride = Environment.GetEnvironmentVariable(SimulateHardwareEnvName);
            string[] ports;

            try
            {
                ports = GetPortNames();
            }
            catch (Exception exception)
            {
                return $"env port={ValueOrUnset(portOverride)}, baud={ValueOrUnset(baudOverride)}, simulate={ValueOrUnset(simulationOverride)}, ports=<error: {exception.Message}>";
            }

            string availablePorts = ports.Length == 0 ? "<none>" : string.Join(", ", ports);
            return $"env port={ValueOrUnset(portOverride)}, baud={ValueOrUnset(baudOverride)}, simulate={ValueOrUnset(simulationOverride)}, ports={availablePorts}";
        }

        public static string GetProjectRootPath()
        {
            string assetPath = Application.dataPath;
            string rootPath = Directory.GetParent(assetPath)?.FullName;
            return string.IsNullOrEmpty(rootPath) ? assetPath : rootPath;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
                Application.runInBackground = true;
            }
        }

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            if (IsConnected)
            {
                PollSerialInput();
                return;
            }

            if (IsSimulation)
            {
                UpdateSimulation();
                return;
            }

            if (Time.unscaledTime >= _nextReconnectTime)
            {
                RefreshAvailablePorts();
                ConnectOrFallback();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        public void InitializeNow()
        {
            if (_initialized)
            {
                return;
            }

            if (Instance == null)
            {
                Instance = this;
            }

            _initialized = true;
            ApplyEnvironmentOverrides();
            RefreshAvailablePorts();

            if (autoConnectOnStart)
            {
                ConnectOrFallback();
            }
        }

        private void ApplyEnvironmentOverrides()
        {
            string portOverride = Environment.GetEnvironmentVariable(SerialPortEnvName);
            if (!string.IsNullOrWhiteSpace(portOverride))
            {
                preferredPortName = portOverride.Trim();
            }

            string baudOverride = Environment.GetEnvironmentVariable(SerialBaudEnvName);
            if (int.TryParse(baudOverride, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedBaud) && parsedBaud > 0)
            {
                baudRate = parsedBaud;
            }

            _simulateHardware = IsTruthy(Environment.GetEnvironmentVariable(SimulateHardwareEnvName));
        }

        private void RefreshAvailablePorts()
        {
            try
            {
                _availablePorts = GetPortNames();
            }
            catch (Exception exception)
            {
                _availablePorts = Array.Empty<string>();
                _lastError = $"Serial port enumeration failed: {exception.Message}";
            }
        }

        private void ConnectOrFallback()
        {
            if (_simulateHardware)
            {
                _lastError = "Hardware simulation forced by environment.";
                return;
            }

            string selectedPort = ResolvePortName();
            if (string.IsNullOrEmpty(selectedPort))
            {
                if (allowSimulationFallback)
                {
                    _simulateHardware = true;
                    _lastError = "No serial ports found. Falling back to simulation.";
                }

                return;
            }

            TryOpenPort(selectedPort);
        }

        private string ResolvePortName()
        {
            if (_availablePorts.Length == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(preferredPortName))
            {
                foreach (string candidate in _availablePorts)
                {
                    if (string.Equals(candidate, preferredPortName, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
            }

            return _availablePorts[0];
        }

        private void TryOpenPort(string portName)
        {
            Disconnect();

            try
            {
                Type serialPortType = ResolveSerialPortType();
                if (serialPortType == null)
                {
                    throw new InvalidOperationException("System.IO.Ports.SerialPort is unavailable in the current Unity runtime.");
                }

                _serialPort = Activator.CreateInstance(serialPortType, portName, baudRate);
                SetProperty(_serialPort, "NewLine", "\n");
                SetProperty(_serialPort, "DtrEnable", true);
                SetProperty(_serialPort, "ReadTimeout", 1);
                SetProperty(_serialPort, "WriteTimeout", 100);
                InvokeMethod(_serialPort, "Open");
                _simulateHardware = false;
                preferredPortName = portName;
                _lastError = string.Empty;
                _lastUpdateUtc = DateTime.UtcNow;
                UnityEngine.Debug.Log($"[HardwareBridge] Connected to {portName} @ {baudRate}.");
            }
            catch (Exception exception)
            {
                _lastError = $"Failed to open {portName}: {DescribeException(exception)}";
                Disconnect();

                if (allowSimulationFallback)
                {
                    _simulateHardware = true;
                    UnityEngine.Debug.LogWarning($"[HardwareBridge] {_lastError} Falling back to simulation.");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[HardwareBridge] {_lastError}");
                }
            }
        }

        private void Disconnect()
        {
            if (_serialPort == null)
            {
                return;
            }

            try
            {
                if (IsConnected)
                {
                    InvokeMethod(_serialPort, "Close");
                }
            }
            catch (Exception exception)
            {
                _lastError = $"Failed to close serial port: {exception.Message}";
            }
            finally
            {
                if (_serialPort is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _serialPort = null;
            }
        }

        private void PollSerialInput()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                string incoming = InvokeMethod(_serialPort, "ReadExisting") as string;
                if (string.IsNullOrEmpty(incoming))
                {
                    return;
                }

                _serialBuffer.Append(incoming);
                ProcessBufferedSerialLines();
            }
            catch (TimeoutException)
            {
            }
            catch (Exception exception)
            {
                _lastError = $"Serial read failed: {exception.Message}";
                UnityEngine.Debug.LogWarning($"[HardwareBridge] {_lastError}");
                Disconnect();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }
        }

        private void ProcessBufferedSerialLines()
        {
            string bufferedText = _serialBuffer.ToString();
            int newlineIndex = bufferedText.IndexOf('\n');

            while (newlineIndex >= 0)
            {
                string line = bufferedText.Substring(0, newlineIndex).Trim();
                bufferedText = bufferedText.Substring(newlineIndex + 1);

                if (!string.IsNullOrEmpty(line) && TryParseTelemetry(line, out HardwareTelemetry parsedTelemetry))
                {
                    _telemetry = parsedTelemetry;
                    _lastTelemetryRaw = line;
                    _lastUpdateUtc = DateTime.UtcNow;
                }

                newlineIndex = bufferedText.IndexOf('\n');
            }

            _serialBuffer.Clear();
            _serialBuffer.Append(bufferedText);
        }

        private bool TryParseTelemetry(string line, out HardwareTelemetry telemetry)
        {
            telemetry = _telemetry;
            string trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            if (!trimmed.Contains("=") && !trimmed.Contains(":"))
            {
                return TryParseOrderedCsv(trimmed, out telemetry);
            }

            bool parsedAny = false;
            string[] parts = trimmed.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string[] keyValue = part.Split(new[] { '=', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                if (TryApplyTelemetryField(ref telemetry, keyValue[0], keyValue[1]))
                {
                    parsedAny = true;
                }
            }

            return parsedAny;
        }

        private static bool TryParseOrderedCsv(string line, out HardwareTelemetry telemetry)
        {
            telemetry = default;
            string[] values = line.Split(',');
            if (values.Length != 7)
            {
                return false;
            }

            return
                TryParseFloat(values[0], out telemetry.headYaw) &&
                TryParseFloat(values[1], out telemetry.headPitch) &&
                TryParseFloat(values[2], out telemetry.headRoll) &&
                TryParseFloat(values[3], out telemetry.handYaw) &&
                TryParseFloat(values[4], out telemetry.handPitch) &&
                TryParseFloat(values[5], out telemetry.handRoll) &&
                TryParseBool(values[6], out telemetry.buttonPressed);
        }

        private static bool TryApplyTelemetryField(ref HardwareTelemetry telemetry, string key, string rawValue)
        {
            string normalizedKey = key.Trim().ToLowerInvariant();

            switch (normalizedKey)
            {
                case "hy":
                case "headyaw":
                    return TryParseFloat(rawValue, out telemetry.headYaw);
                case "hp":
                case "headpitch":
                    return TryParseFloat(rawValue, out telemetry.headPitch);
                case "hr":
                case "headroll":
                    return TryParseFloat(rawValue, out telemetry.headRoll);
                case "wy":
                case "handyaw":
                    return TryParseFloat(rawValue, out telemetry.handYaw);
                case "wp":
                case "handpitch":
                    return TryParseFloat(rawValue, out telemetry.handPitch);
                case "wr":
                case "handroll":
                    return TryParseFloat(rawValue, out telemetry.handRoll);
                case "btn":
                case "button":
                    return TryParseBool(rawValue, out telemetry.buttonPressed);
                default:
                    return false;
            }
        }

        private void UpdateSimulation()
        {
            if (!enableKeyboardSimulation || Keyboard.current == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            float deltaTime = Time.unscaledDeltaTime;

            float headYawDelta = AxisValue(keyboard.leftArrowKey.isPressed, keyboard.rightArrowKey.isPressed) * simulationYawSpeedDegreesPerSecond * deltaTime;
            float headPitchDelta = AxisValue(keyboard.downArrowKey.isPressed, keyboard.upArrowKey.isPressed) * simulationPitchSpeedDegreesPerSecond * deltaTime;
            float handYawDelta = AxisValue(keyboard.jKey.isPressed, keyboard.lKey.isPressed) * simulationYawSpeedDegreesPerSecond * deltaTime;
            float handPitchDelta = AxisValue(keyboard.kKey.isPressed, keyboard.iKey.isPressed) * simulationPitchSpeedDegreesPerSecond * deltaTime;

            _telemetry.headYaw += headYawDelta;
            _telemetry.headPitch += headPitchDelta;
            _telemetry.handYaw += handYawDelta;
            _telemetry.handPitch += handPitchDelta;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _simulationButtonHeld = !_simulationButtonHeld;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                _telemetry = default;
                _simulationButtonHeld = false;
            }

            _telemetry.buttonPressed = _simulationButtonHeld;
            _lastTelemetryRaw = _telemetry.ToString();
            _lastUpdateUtc = DateTime.UtcNow;
        }

        private void SendCommand(string command)
        {
            _lastCommand = command;

            if (!IsConnected)
            {
                return;
            }

            try
            {
                InvokeMethod(_serialPort, "WriteLine", command);
            }
            catch (Exception exception)
            {
                _lastError = $"Serial write failed: {DescribeException(exception)}";
                UnityEngine.Debug.LogWarning($"[HardwareBridge] {_lastError}");
                Disconnect();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }
        }

        private static float AxisValue(bool negative, bool positive)
        {
            if (negative == positive)
            {
                return 0f;
            }

            return positive ? 1f : -1f;
        }

        private static bool TryParseFloat(string rawValue, out float parsedValue)
        {
            return float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue);
        }

        private static bool TryParseBool(string rawValue, out bool parsedValue)
        {
            string normalizedValue = rawValue.Trim().ToLowerInvariant();

            if (normalizedValue == "1" || normalizedValue == "true" || normalizedValue == "pressed" || normalizedValue == "down")
            {
                parsedValue = true;
                return true;
            }

            if (normalizedValue == "0" || normalizedValue == "false" || normalizedValue == "released" || normalizedValue == "up")
            {
                parsedValue = false;
                return true;
            }

            parsedValue = false;
            return false;
        }

        private static bool IsTruthy(string rawValue)
        {
            return !string.IsNullOrWhiteSpace(rawValue) &&
                   (rawValue == "1" || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase));
        }

        private static string[] GetPortNames()
        {
            Type serialPortType = ResolveSerialPortType();
            if (serialPortType == null)
            {
                return Array.Empty<string>();
            }

            MethodInfo getPortNamesMethod = serialPortType.GetMethod("GetPortNames", BindingFlags.Public | BindingFlags.Static);
            object result = getPortNamesMethod?.Invoke(null, null);
            return result as string[] ?? Array.Empty<string>();
        }

        private static Type ResolveSerialPortType()
        {
            return Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports") ??
                   Type.GetType("System.IO.Ports.SerialPort, System");
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            propertyInfo?.SetValue(target, value);
        }

        private static bool GetBooleanProperty(object target, string propertyName)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return propertyInfo != null && propertyInfo.GetValue(target) is bool value && value;
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return propertyInfo?.GetValue(target) as string ?? string.Empty;
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return methodInfo?.Invoke(target, arguments);
        }

        private static string DescribeException(Exception exception)
        {
            Exception current = exception;

            while (current is TargetInvocationException && current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private static string ValueOrUnset(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<unset>" : value;
        }
    }
}
