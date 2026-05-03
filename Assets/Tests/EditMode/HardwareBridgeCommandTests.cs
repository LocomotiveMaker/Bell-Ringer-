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
    }
}
