using System;
using System.Collections;
using System.IO;
using BellRinger.Core;
using BellRinger.Debug;
using BellRinger.Hardware;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BellRinger.Tests.PlayMode
{
    public sealed class BellRingerRuntimeSmokeTests
    {
        private string _statusPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _statusPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "bellringer-runtime-status-test.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(_statusPath) ?? Application.dataPath);

            if (File.Exists(_statusPath))
            {
                File.Delete(_statusPath);
            }

            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, "1");
            Environment.SetEnvironmentVariable(BellRingerRuntimeStatusWriter.StatusPathEnvName, _statusPath);
            Environment.SetEnvironmentVariable(BellRingerDebugOverlay.DisableOverlayEnvName, "1");

            GameObject existingRoot = GameObject.Find(BellRingerRuntimeBootstrap.RuntimeRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.Destroy(existingRoot);
                yield return null;
            }

            SceneManager.LoadScene("SampleScene");
            yield return null;
            BellRingerRuntimeBootstrap.EnsureRuntimeHost();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Environment.SetEnvironmentVariable(HardwareBridge.SimulateHardwareEnvName, null);
            Environment.SetEnvironmentVariable(BellRingerRuntimeStatusWriter.StatusPathEnvName, null);
            Environment.SetEnvironmentVariable(BellRingerDebugOverlay.DisableOverlayEnvName, null);

            GameObject existingRoot = GameObject.Find(BellRingerRuntimeBootstrap.RuntimeRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.Destroy(existingRoot);
                yield return null;
            }

            if (File.Exists(_statusPath))
            {
                File.Delete(_statusPath);
            }
        }

        [UnityTest]
        public IEnumerator SimulationBridgeWritesStatusFile()
        {
            yield return new WaitUntil(() => HardwareBridge.Instance != null);

            HardwareBridge bridge = HardwareBridge.Instance;
            Assert.That(bridge.IsSimulation, Is.True);

            HardwareTelemetry expectedTelemetry = new HardwareTelemetry
            {
                headYaw = 15f,
                headPitch = -10f,
                headRoll = 3f,
                handYaw = 35f,
                handPitch = 6f,
                handRoll = -4f,
                buttonPressed = true,
            };

            bridge.SetSimulationTelemetry(expectedTelemetry);
            yield return new WaitForSeconds(0.4f);

            Assert.That(File.Exists(_statusPath), Is.True, $"Status file was not written: {_statusPath}");

            string json = File.ReadAllText(_statusPath);
            BellRingerRuntimeStatusEnvelope envelope = JsonUtility.FromJson<BellRingerRuntimeStatusEnvelope>(json);

            Assert.That(envelope, Is.Not.Null);
            Assert.That(envelope.status.isSimulation, Is.True);
            Assert.That(envelope.status.telemetry.headYaw, Is.EqualTo(expectedTelemetry.headYaw).Within(0.01f));
            Assert.That(envelope.status.telemetry.handYaw, Is.EqualTo(expectedTelemetry.handYaw).Within(0.01f));
            Assert.That(envelope.status.telemetry.buttonPressed, Is.True);
        }
    }
}
