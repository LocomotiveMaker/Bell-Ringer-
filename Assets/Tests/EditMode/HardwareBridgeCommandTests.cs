using System;
using BellRinger.Hardware;
using NUnit.Framework;
using UnityEngine;

namespace BellRinger.Tests.EditMode
{
    public sealed class HardwareBridgeCommandTests
    {
        private GameObject _root;
        private HardwareBridge _bridge;

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, "1");

            _root = new GameObject("HardwareBridgeCommandTests");
            _bridge = _root.AddComponent<HardwareBridge>();
            _bridge.InitializeNow();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, null);

            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void SendLedFillFormatsFullMatrixCommand()
        {
            _bridge.SendLedFill(0.12f);

            HardwareStatusSnapshot snapshot = _bridge.GetStatusSnapshot();
            Assert.That(snapshot.lastCommand, Is.EqualTo("LED fill b=31"));
        }

        [Test]
        public void SendLedFillClampsBrightnessToByteRange()
        {
            _bridge.SendLedFill(2f);

            HardwareStatusSnapshot snapshot = _bridge.GetStatusSnapshot();
            Assert.That(snapshot.lastCommand, Is.EqualTo("LED fill b=255"));
        }

        [Test]
        public void SendLedRippleFormatsRippleCommand()
        {
            _bridge.SendLedRipple(7.5f, 3.5f, 2f, 1.3f, new Color(0.125f, 1f, 0.375f), 0.16f);

            HardwareStatusSnapshot snapshot = _bridge.GetStatusSnapshot();
            Assert.That(snapshot.lastCommand, Is.EqualTo("LED ripple cx=7.50 cy=3.50 radius=2.00 width=1.30 red=32 green=255 blue=96 level=0.16"));
        }

        [Test]
        public void SendLedWallNoiseFormatsWallCommand()
        {
            _bridge.SendLedWallNoise(7.5f, 3.5f, 9f, 4f, new Color(0.5f, 0.5f, 0.5f), 0.12f, 123);

            HardwareStatusSnapshot snapshot = _bridge.GetStatusSnapshot();
            Assert.That(snapshot.lastCommand, Is.EqualTo("LED wall cx=7.50 cy=3.50 w=9.00 h=4.00 red=128 green=128 blue=128 level=0.12 seed=123"));
        }

        [Test]
        public void SendLedRainFormatsRainCommand()
        {
            _bridge.SendLedRain(new Color(0.47f, 0.67f, 1f), 0.14f, 42, 7.5f, 1.5f, 15f, 3f, 2.25f);

            HardwareStatusSnapshot snapshot = _bridge.GetStatusSnapshot();
            Assert.That(snapshot.lastCommand, Is.EqualTo("LED rain cx=7.50 cy=1.50 w=15.00 h=3.00 red=120 green=171 blue=255 level=0.14 seed=42 phase=2.25"));
        }
    }
}
