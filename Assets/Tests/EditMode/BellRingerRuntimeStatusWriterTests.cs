using System;
using System.IO;
using BellRinger.Debug;
using BellRinger.Hardware;
using NUnit.Framework;
using UnityEngine;

namespace BellRinger.Tests.EditMode
{
    public sealed class BellRingerRuntimeStatusWriterTests
    {
        private string _statusPath;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _statusPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "bellringer-editmode-status-test.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(_statusPath) ?? Application.dataPath);

            if (File.Exists(_statusPath))
            {
                File.Delete(_statusPath);
            }

            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, "1");
            Environment.SetEnvironmentVariable(BellRingerRuntimeStatusWriter.StatusPathEnvName, _statusPath);
            Environment.SetEnvironmentVariable(BellRingerDebugOverlay.DisableOverlayEnvName, "1");

            _root = new GameObject("BellRingerEditModeTestRoot");

            HardwareBridge bridge = _root.AddComponent<HardwareBridge>();
            bridge.InitializeNow();
            bridge.SetSimulationTelemetry(new HardwareTelemetry
            {
                headYaw = 12f,
                headPitch = -4f,
                headRoll = 1.5f,
                handYaw = 28f,
                handPitch = 9f,
                handRoll = -2f,
                buttonPressed = true,
            });

            BellRingerRuntimeStatusWriter writer = _root.AddComponent<BellRingerRuntimeStatusWriter>();
            writer.InitializeNow();
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, null);
            Environment.SetEnvironmentVariable(BellRingerRuntimeStatusWriter.StatusPathEnvName, null);
            Environment.SetEnvironmentVariable(BellRingerDebugOverlay.DisableOverlayEnvName, null);

            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void WritesStatusSnapshot()
        {
            BellRingerRuntimeStatusWriter writer = _root.GetComponent<BellRingerRuntimeStatusWriter>();
            writer.WriteSnapshotNow();

            Assert.That(File.Exists(_statusPath), Is.True, $"Status file was not written: {_statusPath}");

            string json = File.ReadAllText(_statusPath);
            BellRingerRuntimeStatusEnvelope envelope = JsonUtility.FromJson<BellRingerRuntimeStatusEnvelope>(json);

            Assert.That(envelope, Is.Not.Null);
            Assert.That(envelope.status.isSimulation, Is.True);
            Assert.That(envelope.status.telemetry.headYaw, Is.EqualTo(12f).Within(0.01f));
            Assert.That(envelope.status.telemetry.handYaw, Is.EqualTo(28f).Within(0.01f));
            Assert.That(envelope.status.telemetry.buttonPressed, Is.True);
        }
    }
}
