using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BellRinger.Hardware
{
    [DisallowMultipleComponent]
    public sealed class HeadImuReceiver : MonoBehaviour
    {
        public const string SerialPortEnvName = "BELL_RINGER_HEAD_IMU_PORT";
        public const string SerialBaudEnvName = "BELL_RINGER_HEAD_IMU_BAUD";

        [SerializeField] private bool useSharedHardwareBridgeTelemetry = true;
        [SerializeField] private string preferredPortName = string.Empty;
        [SerializeField] private int baudRate = 230400;
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private float reconnectIntervalSeconds = 2f;
        [SerializeField] private float staleAfterSeconds = 0.25f;

        private readonly StringBuilder _serialBuffer = new StringBuilder(512);
        private object _serialPort;
        private string[] _availablePorts = Array.Empty<string>();
        private string _lastError = string.Empty;
        private string _lastRawLine = string.Empty;
        private DateTime _lastUpdateUtc = DateTime.MinValue;
        private float _lastSampleRealtime;
        private bool _initialized;
        private float _nextReconnectTime;
        private bool _usingSharedHardwareBridgeTelemetry;
        private float _yawDegrees;
        private float _pitchDegrees;
        private float _rollDegrees;
        private float _stillness01;

        public bool IsConnected => _usingSharedHardwareBridgeTelemetry
            ? HardwareBridge.Instance != null && HardwareBridge.Instance.IsConnected
            : _serialPort != null && GetBooleanProperty(_serialPort, "IsOpen");
        public bool HasFreshSample => _lastSampleRealtime > 0f && Time.realtimeSinceStartup - _lastSampleRealtime <= staleAfterSeconds;
        public string ActivePortName => _usingSharedHardwareBridgeTelemetry
            ? (HardwareBridge.Instance != null ? HardwareBridge.Instance.ActivePortName : preferredPortName)
            : (IsConnected ? GetStringProperty(_serialPort, "PortName") : preferredPortName);
        public int ActiveBaudRate => baudRate;
        public string[] AvailablePorts => _availablePorts;
        public string LastError => _lastError;
        public string LastRawLine => _lastRawLine;
        public float LastSampleAgeSeconds => _lastSampleRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastSampleRealtime;
        public float YawDegrees => _yawDegrees;
        public float PitchDegrees => _pitchDegrees;
        public float RollDegrees => _rollDegrees;
        public float Stillness01 => _stillness01;
        public bool UsingSharedHardwareBridgeTelemetry => _usingSharedHardwareBridgeTelemetry;

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            InitializeNow();

            if (TryUpdateFromSharedHardwareBridge())
            {
                return;
            }

            if (IsConnected)
            {
                PollSerialInput();
                return;
            }

            if (Time.unscaledTime >= _nextReconnectTime)
            {
                RefreshAvailablePorts();
                TryConnect();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void InitializeNow()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ApplyEnvironmentOverrides();
            RefreshAvailablePorts();

            if (autoConnectOnStart)
            {
                TryConnect();
            }
        }

        public void RefreshAndReconnect()
        {
            Disconnect();
            RefreshAvailablePorts();
            TryConnect();
            _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
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

        private bool TryUpdateFromSharedHardwareBridge()
        {
            _usingSharedHardwareBridgeTelemetry = false;

            if (!useSharedHardwareBridgeTelemetry || HardwareBridge.Instance == null || !HardwareBridge.Instance.IsConnected)
            {
                return false;
            }

            if (_serialPort != null)
            {
                Disconnect();
            }

            HardwareBridge hardwareBridge = HardwareBridge.Instance;
            HardwareStatusSnapshot snapshot = hardwareBridge.GetStatusSnapshot();
            _usingSharedHardwareBridgeTelemetry = true;
            _lastError = string.Empty;
            _lastRawLine = snapshot.lastTelemetryRaw ?? string.Empty;
            _yawDegrees = snapshot.telemetry.headYaw;
            _pitchDegrees = snapshot.telemetry.headPitch;
            _rollDegrees = snapshot.telemetry.headRoll;
            _stillness01 = 0f;

            if (hardwareBridge.LastUpdateAgeSeconds < float.PositiveInfinity)
            {
                _lastSampleRealtime = Time.realtimeSinceStartup - hardwareBridge.LastUpdateAgeSeconds;
            }

            if (DateTime.TryParse(snapshot.lastUpdateUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedUtc))
            {
                _lastUpdateUtc = parsedUtc;
            }

            return true;
        }

        private void TryConnect()
        {
            if (_usingSharedHardwareBridgeTelemetry)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(preferredPortName))
            {
                if (!ContainsAvailablePort(preferredPortName))
                {
                    _lastError = $"Preferred head IMU port {preferredPortName} is not currently available.";
                    return;
                }

                if (TryOpenPort(preferredPortName, out string preferredError))
                {
                    return;
                }

                _lastError = preferredError;
                return;
            }

            foreach (string candidatePort in _availablePorts)
            {
                if (HardwareBridge.Instance != null && string.Equals(candidatePort, HardwareBridge.Instance.ActivePortName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryOpenPort(candidatePort, out _))
                {
                    return;
                }
            }
        }

        private bool TryOpenPort(string portName, out string error)
        {
            error = string.Empty;
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
                InvokeMethod(_serialPort, "Open");
                preferredPortName = portName;
                _lastError = string.Empty;
                _lastUpdateUtc = DateTime.UtcNow;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to open {portName}: {DescribeException(exception)}";
                Disconnect();
                return false;
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
                if (GetBooleanProperty(_serialPort, "IsOpen"))
                {
                    InvokeMethod(_serialPort, "Close");
                }
            }
            catch
            {
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
                _lastError = $"Head IMU read failed: {exception.Message}";
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

                if (!string.IsNullOrEmpty(line) && TryParseTelemetry(line))
                {
                    _lastRawLine = line;
                    _lastUpdateUtc = DateTime.UtcNow;
                    _lastSampleRealtime = Time.realtimeSinceStartup;
                }

                newlineIndex = bufferedText.IndexOf('\n');
            }

            _serialBuffer.Clear();
            _serialBuffer.Append(bufferedText);
        }

        private bool TryParseTelemetry(string line)
        {
            bool parsedAny = false;
            string[] parts = line.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string[] keyValue = part.Split(new[] { '=', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                string key = keyValue[0].Trim().ToLowerInvariant();
                string rawValue = keyValue[1].Trim();
                switch (key)
                {
                    case "hy":
                    case "headyaw":
                        parsedAny |= float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _yawDegrees);
                        break;
                    case "hp":
                    case "headpitch":
                        parsedAny |= float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _pitchDegrees);
                        break;
                    case "hr":
                    case "headroll":
                        parsedAny |= float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _rollDegrees);
                        break;
                    case "st":
                    case "hs":
                    case "headstill":
                        if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedStillness))
                        {
                            _stillness01 = Mathf.Clamp01(parsedStillness);
                        }
                        break;
                }
            }

            return parsedAny;
        }

        private static bool ContainsAvailablePort(string portName)
        {
            string[] ports = GetPortNames();
            foreach (string candidate in ports)
            {
                if (string.Equals(candidate, portName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] GetPortNames()
        {
            Type serialPortType = ResolveSerialPortType();
            if (serialPortType == null)
            {
                return Array.Empty<string>();
            }

            object result = InvokeStaticMethod(serialPortType, "GetPortNames");
            return result as string[] ?? Array.Empty<string>();
        }

        private static Type ResolveSerialPortType()
        {
            return Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports", false)
                   ?? Type.GetType("System.IO.Ports.SerialPort, System", false);
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            return target.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, target, arguments);
        }

        private static object InvokeStaticMethod(Type type, string methodName, params object[] arguments)
        {
            return type.InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, arguments);
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            object value = target.GetType().InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, target, null);
            return value as string ?? string.Empty;
        }

        private static bool GetBooleanProperty(object target, string propertyName)
        {
            object value = target.GetType().InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, target, null);
            return value is bool boolValue && boolValue;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            target.GetType().InvokeMember(propertyName, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance, null, target, new[] { value });
        }

        private static string DescribeException(Exception exception)
        {
            Exception current = exception;
            while (current is TargetInvocationException invocationException && invocationException.InnerException != null)
            {
                current = invocationException.InnerException;
            }

            return current.Message;
        }
    }
}
