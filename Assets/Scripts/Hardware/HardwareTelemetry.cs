using System;
using System.Globalization;

namespace BellRinger.Hardware
{
    [Serializable]
    public struct HardwareTelemetry
    {
        public float headYaw;
        public float headPitch;
        public float headRoll;
        public float handYaw;
        public float handPitch;
        public float handRoll;
        public bool handQuaternionValid;
        public float handQuatW;
        public float handQuatX;
        public float handQuatY;
        public float handQuatZ;
        public bool buttonPressed;

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "head({0:0.0}, {1:0.0}, {2:0.0}) hand({3:0.0}, {4:0.0}, {5:0.0}) button({6})",
                headYaw,
                headPitch,
                headRoll,
                handYaw,
                handPitch,
                handRoll,
                buttonPressed ? "down" : "up");
        }
    }

    [Serializable]
    public struct HardwareStatusSnapshot
    {
        public bool isConnected;
        public bool isSimulation;
        public string portName;
        public int baudRate;
        public string lastError;
        public string[] availablePorts;
        public string lastTelemetryRaw;
        public string lastCommand;
        public string lastUpdateUtc;
        public HardwareTelemetry telemetry;
    }
}
