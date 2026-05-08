using System;
using System.Text;
using BellRinger.Hardware;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.Debug
{
    public sealed class BellRingerDebugOverlay : MonoBehaviour
    {
        public const string DisableOverlayEnvName = "BELL_RINGER_DISABLE_OVERLAY";

        [SerializeField] private bool visibleByDefault = true;

        private readonly StringBuilder _builder = new StringBuilder(512);
        private bool _isVisible;
        private PadImuReceiver _padImuReceiver;

        private void Start()
        {
            _isVisible = visibleByDefault && !OverlayDisabledByEnvironment();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _isVisible = !_isVisible;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || HardwareBridge.Instance == null)
            {
                return;
            }

            HardwareStatusSnapshot snapshot = HardwareBridge.Instance.GetStatusSnapshot();
            if (_padImuReceiver == null)
            {
                _padImuReceiver = FindFirstObjectByType<PadImuReceiver>();
            }

            bool usingDedicatedPadImu = _padImuReceiver != null && !_padImuReceiver.UsingSharedHardwareBridgeTelemetry;

            _builder.Clear();
            _builder.AppendLine("Bell Ringer Hardware Debug");
            _builder.Append("Mode: ").AppendLine(snapshot.isSimulation ? "Simulation" : "Serial");
            _builder.Append("Connected: ").AppendLine(snapshot.isConnected ? "Yes" : "No");
            _builder.Append("Port: ").AppendLine(snapshot.portName);
            _builder.Append("Baud: ").AppendLine(snapshot.baudRate.ToString());
            _builder.Append("Last update: ").AppendLine(string.IsNullOrEmpty(snapshot.lastUpdateUtc) ? "(none)" : snapshot.lastUpdateUtc);
            _builder.Append("Head: ")
                .Append(snapshot.telemetry.headYaw.ToString("0.0")).Append(", ")
                .Append(snapshot.telemetry.headPitch.ToString("0.0")).Append(", ")
                .Append(snapshot.telemetry.headRoll.ToString("0.0")).AppendLine();
            _builder.Append(usingDedicatedPadImu ? "Bridge Hand: " : "Hand: ")
                .Append(snapshot.telemetry.handYaw.ToString("0.0")).Append(", ")
                .Append(snapshot.telemetry.handPitch.ToString("0.0")).Append(", ")
                .Append(snapshot.telemetry.handRoll.ToString("0.0")).AppendLine();
            _builder.Append("Button: ").AppendLine(snapshot.telemetry.buttonPressed ? "Pressed" : "Released");
            if (_padImuReceiver != null)
            {
                _builder.Append("Pad IMU: ")
                    .AppendLine(_padImuReceiver.UsingSharedHardwareBridgeTelemetry ? "Shared HardwareBridge" : "Dedicated Serial");
                _builder.Append("Pad IMU Port: ").AppendLine(string.IsNullOrWhiteSpace(_padImuReceiver.ActivePortName) ? "(none)" : _padImuReceiver.ActivePortName);
                _builder.Append("Pad IMU Fresh: ").AppendLine(_padImuReceiver.HasFreshSample ? "Yes" : "No");
                _builder.Append("Pad IMU YPR: ")
                    .Append(_padImuReceiver.MappedYawDegrees.ToString("0.0")).Append(", ")
                    .Append(_padImuReceiver.MappedPitchDegrees.ToString("0.0")).Append(", ")
                    .Append(_padImuReceiver.MappedRollDegrees.ToString("0.0")).AppendLine();
            }
            _builder.Append("Ports: ").AppendLine(snapshot.availablePorts == null || snapshot.availablePorts.Length == 0 ? "(none)" : string.Join(", ", snapshot.availablePorts));

            if (!string.IsNullOrEmpty(snapshot.lastError))
            {
                _builder.Append("Last error: ").AppendLine(snapshot.lastError);
            }

            if (!string.IsNullOrEmpty(snapshot.lastCommand))
            {
                _builder.Append("Last command: ").AppendLine(snapshot.lastCommand);
            }

            float width = 360f;
            float height = _padImuReceiver == null ? 300f : 360f;
            float x = Mathf.Max(16f, Screen.width - width - 16f);
            GUI.Box(new Rect(x, 16f, width, height), _builder.ToString());
        }

        private static bool OverlayDisabledByEnvironment()
        {
            string rawValue = Environment.GetEnvironmentVariable(DisableOverlayEnvName);
            return !string.IsNullOrWhiteSpace(rawValue) &&
                   (rawValue == "1" || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
