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
    }
}
