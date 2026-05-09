using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BellRinger.Hardware
{
    [DisallowMultipleComponent]
    public sealed class PadImuReceiver : MonoBehaviour
    {
        private enum ImuAxis
        {
            Yaw,
            Pitch,
            Roll,
        }

        public const string SerialPortEnvName = "BELL_RINGER_PAD_IMU_PORT";
        public const string SerialBaudEnvName = "BELL_RINGER_PAD_IMU_BAUD";

        [SerializeField] private bool useSharedHardwareBridgeTelemetry = false;
        [SerializeField] private string preferredPortName = "COM10";
        [SerializeField] private int baudRate = 230400;
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private float reconnectIntervalSeconds = 2f;
        [SerializeField] private float staleAfterSeconds = 0.25f;
        [SerializeField] private bool useQuaternionWhenAvailable = true;
        [SerializeField] private bool enableGyroPrediction = true;
        [SerializeField] private float maxPredictionSeconds = 0.024f;
        [SerializeField] private float stillnessSuppressPredictionThreshold = 0.82f;
        [SerializeField] private ImuAxis yawAxis = ImuAxis.Yaw;
        [SerializeField] private ImuAxis pitchAxis = ImuAxis.Pitch;
        [SerializeField] private ImuAxis rollAxis = ImuAxis.Roll;
        [SerializeField] private bool invertYaw;
        [SerializeField] private bool invertPitch;
        [SerializeField] private bool invertRoll;
        [SerializeField] private Vector3 localRotationTrimEuler = Vector3.zero;
        [SerializeField] private float motionSmoothingStrength = 10f;

        private readonly StringBuilder _serialBuffer = new StringBuilder(512);
        private object _serialPort;
        private string[] _availablePorts = Array.Empty<string>();
        private string _lastError = string.Empty;
        private string _lastRawLine = string.Empty;
        private string _lastCommand = string.Empty;
        private DateTime _lastUpdateUtc = DateTime.MinValue;
        private bool _initialized;
        private float _nextReconnectTime;
        private float _lastSampleRealtime;
        private float _yawDegrees;
        private float _pitchDegrees;
        private float _rollDegrees;
        private bool _usingSharedHardwareBridgeTelemetry;
        private float _mappedYawDegrees;
        private float _mappedPitchDegrees;
        private float _mappedRollDegrees;
        private Quaternion _relativeRotation = Quaternion.identity;
        private float _motionIntensity01;
        private bool _hasDerivedPose;
        private bool _hasQuaternionTelemetry;
        private Quaternion _rawHandQuaternion = Quaternion.identity;
        private bool _hasRuntimeQuaternionCalibration;
        private Quaternion _calibrationReferenceQuaternion = Quaternion.identity;
        private Quaternion _calibrationBasisQuaternion = Quaternion.identity;
        private float _gyroXDegreesPerSecond;
        private float _gyroYDegreesPerSecond;
        private float _gyroZDegreesPerSecond;
        private float _stillness01;
        private int _fusionMode;
        private bool _magCalibrationActive;
        private float _magCalibrationProgress01;
        private string _lastSetupStatus = string.Empty;

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
        public string LastCommand => _lastCommand;
        public string LastUpdateUtc => _lastUpdateUtc == DateTime.MinValue ? string.Empty : _lastUpdateUtc.ToString("O", CultureInfo.InvariantCulture);
        public float LastSampleAgeSeconds => _lastSampleRealtime <= 0f ? float.PositiveInfinity : Time.realtimeSinceStartup - _lastSampleRealtime;
        public float YawDegrees => _yawDegrees;
        public float PitchDegrees => _pitchDegrees;
        public float RollDegrees => _rollDegrees;
        public float MappedYawDegrees => _mappedYawDegrees;
        public float MappedPitchDegrees => _mappedPitchDegrees;
        public float MappedRollDegrees => _mappedRollDegrees;
        public Quaternion RelativeRotation => _relativeRotation;
        public float MotionIntensity01 => _motionIntensity01;
        public bool UsingSharedHardwareBridgeTelemetry => _usingSharedHardwareBridgeTelemetry;
        public bool HasQuaternionTelemetry => _hasQuaternionTelemetry;
        public bool HasRuntimeQuaternionCalibration => _hasRuntimeQuaternionCalibration;
        public Vector3 GyroDegreesPerSecond => new Vector3(_gyroXDegreesPerSecond, _gyroYDegreesPerSecond, _gyroZDegreesPerSecond);
        public float Stillness01 => _stillness01;
        public int FusionMode => _fusionMode;
        public bool UsingMagnetometerFusion => _fusionMode >= 9;
        public bool MagCalibrationActive => _magCalibrationActive;
        public float MagCalibrationProgress01 => _magCalibrationProgress01;
        public string YawAxisLabel => BuildAxisLabel(yawAxis, invertYaw);
        public string PitchAxisLabel => BuildAxisLabel(pitchAxis, invertPitch);
        public string RollAxisLabel => BuildAxisLabel(rollAxis, invertRoll);
        public Vector3 LocalRotationTrimEuler => localRotationTrimEuler;
        public string LastSetupStatus => _lastSetupStatus;

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            InitializeNow();

            if (TryUpdateFromSharedHardwareBridge())
            {
                UpdateDerivedPose();
                return;
            }

            if (IsConnected)
            {
                PollSerialInput();
                UpdateDerivedPose();
                return;
            }

            if (Time.unscaledTime >= _nextReconnectTime)
            {
                RefreshAvailablePorts();
                TryConnect();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }

            UpdateDerivedPose();
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
            LoadSavedCalibration();
            LoadSavedUserSetup();
            RefreshAvailablePorts();

            if (autoConnectOnStart)
            {
                TryConnect();
            }
        }

        public void Recenter()
        {
            if (_usingSharedHardwareBridgeTelemetry && HardwareBridge.Instance != null)
            {
                _lastCommand = "IMU recenter";
                HardwareBridge.Instance.SendPadImuRecenter();
                return;
            }

            SendCommand("r");
        }

        public void StartMagCalibration()
        {
            SendImuCommand("magcal_start");
        }

        public void FinishMagCalibrationAndSave()
        {
            SendImuCommand("magcal_stop");
        }

        public void ResetMagCalibration()
        {
            SendImuCommand("magcal_reset");
        }

        public void RequestMagCalibrationStatus()
        {
            SendImuCommand("magcal_status");
        }

        public void RefreshAndReconnect()
        {
            Disconnect();
            RefreshAvailablePorts();
            TryConnect();
            _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
        }

        public void CycleYawAxis()
        {
            yawAxis = NextAxis(yawAxis);
            UpdateDerivedPose();
        }

        public void CyclePitchAxis()
        {
            pitchAxis = NextAxis(pitchAxis);
            UpdateDerivedPose();
        }

        public void CycleRollAxis()
        {
            rollAxis = NextAxis(rollAxis);
            UpdateDerivedPose();
        }

        public void ToggleYawInvert()
        {
            invertYaw = !invertYaw;
            UpdateDerivedPose();
        }

        public void TogglePitchInvert()
        {
            invertPitch = !invertPitch;
            UpdateDerivedPose();
        }

        public void ToggleRollInvert()
        {
            invertRoll = !invertRoll;
            UpdateDerivedPose();
        }

        public void ResetAxisMapping()
        {
            yawAxis = ImuAxis.Yaw;
            pitchAxis = ImuAxis.Pitch;
            rollAxis = ImuAxis.Roll;
            invertYaw = false;
            invertPitch = false;
            invertRoll = false;
            localRotationTrimEuler = Vector3.zero;
            UpdateDerivedPose();
        }

        public void RotateTrimXPositive()
        {
            RotateTrim(new Vector3(90f, 0f, 0f));
        }

        public void RotateTrimYPositive()
        {
            RotateTrim(new Vector3(0f, 90f, 0f));
        }

        public void RotateTrimZPositive()
        {
            RotateTrim(new Vector3(0f, 0f, 90f));
        }

        public void RotateTrimXNegative()
        {
            RotateTrim(new Vector3(-90f, 0f, 0f));
        }

        public void RotateTrimYNegative()
        {
            RotateTrim(new Vector3(0f, -90f, 0f));
        }

        public void RotateTrimZNegative()
        {
            RotateTrim(new Vector3(0f, 0f, -90f));
        }

        public void ResetMountTrim()
        {
            localRotationTrimEuler = Vector3.zero;
            UpdateDerivedPose();
        }

        public void SaveUserSetup()
        {
            PadImuUserSetupStore.Save(new PadImuUserSetupData
            {
                yawAxis = (int)yawAxis,
                pitchAxis = (int)pitchAxis,
                rollAxis = (int)rollAxis,
                invertYaw = invertYaw,
                invertPitch = invertPitch,
                invertRoll = invertRoll,
                trimX = localRotationTrimEuler.x,
                trimY = localRotationTrimEuler.y,
                trimZ = localRotationTrimEuler.z,
            });

            _lastSetupStatus = "Pad IMU setup saved.";
        }

        public void ReloadSavedUserSetup()
        {
            if (!LoadSavedUserSetup())
            {
                _lastSetupStatus = "No saved Pad IMU setup was found.";
            }
        }

        public bool TryGetRawQuaternion(out Quaternion rawQuaternion)
        {
            rawQuaternion = _rawHandQuaternion;
            return _hasQuaternionTelemetry && HasFreshSample;
        }

        public void ApplyRuntimeQuaternionCalibration(Quaternion referenceQuaternion, Quaternion basisQuaternion)
        {
            _calibrationReferenceQuaternion = NormalizeQuaternion(referenceQuaternion);
            _calibrationBasisQuaternion = NormalizeQuaternion(basisQuaternion);
            _hasRuntimeQuaternionCalibration = true;
            UpdateDerivedPose();
        }

        public void ApplySavedMountCalibration(Quaternion basisQuaternion)
        {
            _calibrationReferenceQuaternion = Quaternion.identity;
            _calibrationBasisQuaternion = NormalizeQuaternion(basisQuaternion);
            _hasRuntimeQuaternionCalibration = true;
            UpdateDerivedPose();
        }

        public void ClearRuntimeQuaternionCalibration()
        {
            _calibrationReferenceQuaternion = Quaternion.identity;
            _calibrationBasisQuaternion = Quaternion.identity;
            _hasRuntimeQuaternionCalibration = false;
            UpdateDerivedPose();
        }

        private void SendImuCommand(string command)
        {
            if (_usingSharedHardwareBridgeTelemetry)
            {
                _lastError = "Mag calibration commands are only supported on the dedicated IMU serial receiver.";
                UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
                return;
            }

            SendCommand(command);
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
                return;
            }

            if (baudRate == 115200)
            {
                baudRate = 230400;
            }
        }

        private void LoadSavedCalibration()
        {
            if (!PadImuCalibrationStore.TryLoadBasisQuaternion(out Quaternion basisQuaternion))
            {
                return;
            }

            ApplySavedMountCalibration(basisQuaternion);
        }

        private bool LoadSavedUserSetup()
        {
            if (!PadImuUserSetupStore.TryLoad(out PadImuUserSetupData data))
            {
                return false;
            }

            yawAxis = ClampAxisIndex(data.yawAxis);
            pitchAxis = ClampAxisIndex(data.pitchAxis);
            rollAxis = ClampAxisIndex(data.rollAxis);
            invertYaw = data.invertYaw;
            invertPitch = data.invertPitch;
            invertRoll = data.invertRoll;
            localRotationTrimEuler = new Vector3(data.trimX, data.trimY, data.trimZ);
            UpdateDerivedPose();
            _lastSetupStatus = "Saved Pad IMU setup loaded.";
            return true;
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
                    _lastError = $"Preferred IMU port {preferredPortName} is not currently available.";
                    UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
                    return;
                }

                if (TryOpenPort(preferredPortName, out string preferredError))
                {
                    return;
                }

                _lastError = preferredError;
                UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
                return;
            }

            string[] candidatePorts = ResolveCandidatePorts();
            if (candidatePorts.Length == 0)
            {
                _lastError = "No candidate COM port found for pad IMU.";
                return;
            }

            string firstError = string.Empty;
            foreach (string candidatePort in candidatePorts)
            {
                if (TryOpenPort(candidatePort, out string attemptError))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(firstError))
                {
                    firstError = attemptError;
                }
            }

            _lastError = string.IsNullOrWhiteSpace(firstError)
                ? $"Failed to open any IMU port. Tried: {string.Join(", ", candidatePorts)}"
                : $"Failed to open any IMU port. Tried: {string.Join(", ", candidatePorts)}. First error: {firstError}";
            UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
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
            _lastCommand = snapshot.lastCommand ?? _lastCommand;
            _yawDegrees = snapshot.telemetry.handYaw;
            _pitchDegrees = snapshot.telemetry.handPitch;
            _rollDegrees = snapshot.telemetry.handRoll;
            _gyroXDegreesPerSecond = 0f;
            _gyroYDegreesPerSecond = 0f;
            _gyroZDegreesPerSecond = 0f;
            _stillness01 = 0f;
            _fusionMode = 0;
            _magCalibrationActive = false;
            _magCalibrationProgress01 = 0f;
            _hasQuaternionTelemetry = snapshot.telemetry.handQuaternionValid;
            if (_hasQuaternionTelemetry)
            {
                _rawHandQuaternion = NormalizeQuaternion(new Quaternion(
                    snapshot.telemetry.handQuatX,
                    snapshot.telemetry.handQuatY,
                    snapshot.telemetry.handQuatZ,
                    snapshot.telemetry.handQuatW));
            }

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

        private void UpdateDerivedPose()
        {
            Quaternion targetRotation;

            if (useQuaternionWhenAvailable && _hasQuaternionTelemetry)
            {
                Quaternion correctedQuaternion = ApplyQuaternionCalibration(_rawHandQuaternion);
                Quaternion predictedQuaternion = PredictQuaternionForward(correctedQuaternion);
                Quaternion trimmedQuaternion = Quaternion.Normalize(predictedQuaternion * Quaternion.Euler(localRotationTrimEuler));
                Vector3 signedEuler = ToSignedEulerDegrees(trimmedQuaternion.eulerAngles);
                _mappedYawDegrees = signedEuler.y;
                _mappedPitchDegrees = -signedEuler.x;
                _mappedRollDegrees = -signedEuler.z;
                targetRotation = trimmedQuaternion;
            }
            else
            {
                _mappedYawDegrees = ReadMappedAxis(yawAxis, invertYaw);
                _mappedPitchDegrees = ReadMappedAxis(pitchAxis, invertPitch);
                _mappedRollDegrees = ReadMappedAxis(rollAxis, invertRoll);
                targetRotation =
                    Quaternion.Euler(-_mappedPitchDegrees, _mappedYawDegrees, -_mappedRollDegrees) *
                    Quaternion.Euler(localRotationTrimEuler);
            }

            if (!_hasDerivedPose)
            {
                _relativeRotation = targetRotation;
                _motionIntensity01 = 0f;
                _hasDerivedPose = true;
                return;
            }

            float deltaAngle = Quaternion.Angle(_relativeRotation, targetRotation);
            _relativeRotation = targetRotation;

            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float angularSpeed = deltaAngle / deltaTime;
            float targetMotion = Mathf.InverseLerp(15f, 260f, angularSpeed);
            float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0f, motionSmoothingStrength) * deltaTime);
            _motionIntensity01 = Mathf.Lerp(_motionIntensity01, targetMotion, lerpFactor);

            if (!HasFreshSample)
            {
                _motionIntensity01 = Mathf.MoveTowards(_motionIntensity01, 0f, deltaTime * 2f);
            }
        }

        private void RotateTrim(Vector3 eulerDelta)
        {
            localRotationTrimEuler = new Vector3(
                NormalizeSignedAngle(localRotationTrimEuler.x + eulerDelta.x),
                NormalizeSignedAngle(localRotationTrimEuler.y + eulerDelta.y),
                NormalizeSignedAngle(localRotationTrimEuler.z + eulerDelta.z));
            UpdateDerivedPose();
        }

        private Quaternion ApplyQuaternionCalibration(Quaternion rawQuaternion)
        {
            Quaternion normalizedRawQuaternion = NormalizeQuaternion(rawQuaternion);
            if (!_hasRuntimeQuaternionCalibration)
            {
                return normalizedRawQuaternion;
            }

            Quaternion relativeQuaternion = Quaternion.Normalize(Quaternion.Inverse(_calibrationReferenceQuaternion) * normalizedRawQuaternion);
            return Quaternion.Normalize(Quaternion.Inverse(_calibrationBasisQuaternion) * relativeQuaternion * _calibrationBasisQuaternion);
        }

        private Quaternion PredictQuaternionForward(Quaternion correctedQuaternion)
        {
            if (!enableGyroPrediction || !HasFreshSample || _stillness01 >= stillnessSuppressPredictionThreshold)
            {
                return correctedQuaternion;
            }

            float predictionSeconds = Mathf.Min(maxPredictionSeconds, LastSampleAgeSeconds);
            if (predictionSeconds <= 0.0001f)
            {
                return correctedQuaternion;
            }

            Vector3 deltaDegrees = new Vector3(_gyroXDegreesPerSecond, _gyroYDegreesPerSecond, _gyroZDegreesPerSecond) * predictionSeconds;
            if (_hasRuntimeQuaternionCalibration)
            {
                deltaDegrees = Quaternion.Inverse(_calibrationBasisQuaternion) * deltaDegrees;
            }

            float deltaAngle = deltaDegrees.magnitude;
            if (deltaAngle <= 0.0001f)
            {
                return correctedQuaternion;
            }

            Quaternion deltaRotation = Quaternion.AngleAxis(deltaAngle, deltaDegrees / deltaAngle);
            return Quaternion.Normalize(correctedQuaternion * deltaRotation);
        }

        private float ReadMappedAxis(ImuAxis axis, bool inverted)
        {
            float value = axis switch
            {
                ImuAxis.Yaw => _yawDegrees,
                ImuAxis.Pitch => _pitchDegrees,
                _ => _rollDegrees,
            };

            return inverted ? -value : value;
        }

        private string[] ResolveCandidatePorts()
        {
            if (_availablePorts.Length == 0)
            {
                return Array.Empty<string>();
            }

            string hardwarePort = HardwareBridge.Instance != null && HardwareBridge.Instance.IsConnected
                ? HardwareBridge.Instance.ActivePortName
                : string.Empty;
            string[] orderedPorts = new string[_availablePorts.Length];
            int count = 0;

            foreach (string candidate in _availablePorts)
            {
                if (string.Equals(candidate, hardwarePort, StringComparison.OrdinalIgnoreCase) || ContainsPort(orderedPorts, count, candidate))
                {
                    continue;
                }

                orderedPorts[count++] = candidate;
            }

            if (count == 0 && !string.IsNullOrWhiteSpace(hardwarePort))
            {
                foreach (string candidate in _availablePorts)
                {
                    if (!ContainsPort(orderedPorts, count, candidate))
                    {
                        orderedPorts[count++] = candidate;
                    }
                }
            }

            string[] result = new string[count];
            Array.Copy(orderedPorts, result, count);
            return result;
        }

        private bool TryOpenPort(string portName, out string error)
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
                preferredPortName = portName;
                _lastError = string.Empty;
                UnityEngine.Debug.Log($"[PadImuReceiver] Connected to {portName} @ {baudRate}.");
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to open {portName}: {NormalizePortOpenError(DescribeException(exception))}";
                _lastError = error;
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
                _lastError = $"Serial read failed: {DescribeException(exception)}";
                UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
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

                if (!string.IsNullOrEmpty(line))
                {
                    _lastRawLine = line;
                    if (TryParseOrientationLine(line))
                    {
                        _lastSampleRealtime = Time.realtimeSinceStartup;
                        _lastUpdateUtc = DateTime.UtcNow;
                    }
                }

                newlineIndex = bufferedText.IndexOf('\n');
            }

            _serialBuffer.Clear();
            _serialBuffer.Append(bufferedText);
        }

        private bool TryParseOrientationLine(string line)
        {
            bool sawAny = false;
            bool sawQuaternion = false;
            float yaw = _yawDegrees;
            float pitch = _pitchDegrees;
            float roll = _rollDegrees;
            float quatW = _rawHandQuaternion.w;
            float quatX = _rawHandQuaternion.x;
            float quatY = _rawHandQuaternion.y;
            float quatZ = _rawHandQuaternion.z;
            float gyroX = _gyroXDegreesPerSecond;
            float gyroY = _gyroYDegreesPerSecond;
            float gyroZ = _gyroZDegreesPerSecond;
            float stillness = _stillness01;
            int fusionMode = _fusionMode;
            bool magCalibrationActive = _magCalibrationActive;
            float magCalibrationProgress = _magCalibrationProgress01;

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
                if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
                {
                    continue;
                }

                switch (key)
                {
                    case "wy":
                    case "handyaw":
                        yaw = parsedValue;
                        sawAny = true;
                        break;
                    case "wp":
                    case "handpitch":
                        pitch = parsedValue;
                        sawAny = true;
                        break;
                    case "wr":
                    case "handroll":
                        roll = parsedValue;
                        sawAny = true;
                        break;
                    case "wqw":
                    case "handquatw":
                        quatW = parsedValue;
                        sawQuaternion = true;
                        break;
                    case "wqx":
                    case "handquatx":
                        quatX = parsedValue;
                        sawQuaternion = true;
                        break;
                    case "wqy":
                    case "handquaty":
                        quatY = parsedValue;
                        sawQuaternion = true;
                        break;
                    case "wqz":
                    case "handquatz":
                        quatZ = parsedValue;
                        sawQuaternion = true;
                        break;
                    case "gx":
                        gyroX = parsedValue;
                        break;
                    case "gy":
                        gyroY = parsedValue;
                        break;
                    case "gz":
                        gyroZ = parsedValue;
                        break;
                    case "st":
                        stillness = parsedValue;
                        break;
                    case "mf":
                        fusionMode = Mathf.RoundToInt(parsedValue);
                        break;
                    case "mc":
                        magCalibrationActive = parsedValue >= 0.5f;
                        break;
                    case "mp":
                        magCalibrationProgress = parsedValue;
                        break;
                }
            }

            if (!sawAny && !sawQuaternion)
            {
                return false;
            }

            if (sawAny)
            {
                _yawDegrees = yaw;
                _pitchDegrees = pitch;
                _rollDegrees = roll;
            }

            _hasQuaternionTelemetry = sawQuaternion;
            if (sawQuaternion)
            {
                _rawHandQuaternion = NormalizeQuaternion(new Quaternion(quatX, quatY, quatZ, quatW));
            }

            _gyroXDegreesPerSecond = gyroX;
            _gyroYDegreesPerSecond = gyroY;
            _gyroZDegreesPerSecond = gyroZ;
            _stillness01 = Mathf.Clamp01(stillness);
            _fusionMode = fusionMode;
            _magCalibrationActive = magCalibrationActive;
            _magCalibrationProgress01 = Mathf.Clamp01(magCalibrationProgress);

            return true;
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
                UnityEngine.Debug.LogWarning($"[PadImuReceiver] {_lastError}");
                Disconnect();
                _nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            }
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

        private static string NormalizePortOpenError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Unknown serial port open error.";
            }

            if (message.IndexOf("액세스가 거부", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("access is denied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"{message} 다른 프로그램(Arduino Serial Monitor, 다른 Unity 인스턴스 등)이 이 포트를 이미 사용 중일 수 있습니다.";
            }

            return message;
        }

        private static ImuAxis NextAxis(ImuAxis axis)
        {
            return axis switch
            {
                ImuAxis.Yaw => ImuAxis.Pitch,
                ImuAxis.Pitch => ImuAxis.Roll,
                _ => ImuAxis.Yaw,
            };
        }

        private static string BuildAxisLabel(ImuAxis axis, bool inverted)
        {
            string label = axis switch
            {
                ImuAxis.Yaw => "Yaw",
                ImuAxis.Pitch => "Pitch",
                _ => "Roll",
            };

            return inverted ? $"-{label}" : $"+{label}";
        }

        private static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            float magnitude = Mathf.Sqrt(
                (quaternion.x * quaternion.x) +
                (quaternion.y * quaternion.y) +
                (quaternion.z * quaternion.z) +
                (quaternion.w * quaternion.w));

            if (magnitude <= 0.00001f)
            {
                return Quaternion.identity;
            }

            float invMagnitude = 1f / magnitude;
            return new Quaternion(
                quaternion.x * invMagnitude,
                quaternion.y * invMagnitude,
                quaternion.z * invMagnitude,
                quaternion.w * invMagnitude);
        }

        private static Vector3 ToSignedEulerDegrees(Vector3 rawEulerDegrees)
        {
            return new Vector3(
                NormalizeSignedAngle(rawEulerDegrees.x),
                NormalizeSignedAngle(rawEulerDegrees.y),
                NormalizeSignedAngle(rawEulerDegrees.z));
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            return Mathf.Approximately(wrapped, -180f) ? 180f : wrapped;
        }

        private static bool ContainsPort(string[] ports, int count, string candidate)
        {
            for (int index = 0; index < count; ++index)
            {
                if (string.Equals(ports[index], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsAvailablePort(string candidate)
        {
            foreach (string portName in _availablePorts)
            {
                if (string.Equals(portName, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ImuAxis ClampAxisIndex(int rawValue)
        {
            return rawValue switch
            {
                0 => ImuAxis.Yaw,
                1 => ImuAxis.Pitch,
                2 => ImuAxis.Roll,
                _ => ImuAxis.Yaw,
            };
        }
    }
}
