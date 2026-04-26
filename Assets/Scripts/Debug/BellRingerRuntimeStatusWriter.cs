using System;
using System.IO;
using BellRinger.Hardware;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug
{
    public sealed class BellRingerRuntimeStatusWriter : MonoBehaviour
    {
        public const string StatusPathEnvName = "BELL_RINGER_STATUS_PATH";

        [SerializeField] private float writeIntervalSeconds = 0.25f;

        private bool _initialized;
        private string _statusPath;
        private float _nextWriteTime;

        public static string ResolveDefaultStatusPath()
        {
            return Path.Combine(HardwareBridge.GetProjectRootPath(), "Logs", "runtime-status.json");
        }

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextWriteTime || HardwareBridge.Instance == null)
            {
                return;
            }

            _nextWriteTime = Time.unscaledTime + writeIntervalSeconds;
            WriteSnapshot();
        }

        public void InitializeNow()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _statusPath = ResolveStatusPath();
            Directory.CreateDirectory(Path.GetDirectoryName(_statusPath) ?? HardwareBridge.GetProjectRootPath());
        }

        public void WriteSnapshotNow()
        {
            if (HardwareBridge.Instance == null)
            {
                return;
            }

            InitializeNow();
            WriteSnapshot();
        }

        private string ResolveStatusPath()
        {
            string overriddenPath = Environment.GetEnvironmentVariable(StatusPathEnvName);
            return string.IsNullOrWhiteSpace(overriddenPath) ? ResolveDefaultStatusPath() : overriddenPath;
        }

        private void WriteSnapshot()
        {
            BellRingerRuntimeStatusEnvelope envelope = new BellRingerRuntimeStatusEnvelope
            {
                projectName = Application.productName,
                unityVersion = Application.unityVersion,
                activeScene = SceneManager.GetActiveScene().name,
                writtenUtc = DateTime.UtcNow.ToString("O"),
                status = HardwareBridge.Instance.GetStatusSnapshot(),
            };

            string json = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(_statusPath, json);
        }
    }

    [Serializable]
    public sealed class BellRingerRuntimeStatusEnvelope
    {
        public string projectName;
        public string unityVersion;
        public string activeScene;
        public string writtenUtc;
        public HardwareStatusSnapshot status;
    }
}
