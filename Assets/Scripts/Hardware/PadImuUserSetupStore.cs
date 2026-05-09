using System;
using System.IO;
using UnityEngine;

namespace BellRinger.Hardware
{
    [Serializable]
    public sealed class PadImuUserSetupData
    {
        public int yawAxis;
        public int pitchAxis = 1;
        public int rollAxis = 2;
        public bool invertYaw;
        public bool invertPitch;
        public bool invertRoll;
        public float trimX;
        public float trimY;
        public float trimZ;
    }

    public static class PadImuUserSetupStore
    {
        private const string RelativeSettingsPath = "Settings\\pad-imu-user-setup.json";

        public static string SettingsPath => Path.Combine(HardwareBridge.GetProjectRootPath(), RelativeSettingsPath);

        public static bool TryLoad(out PadImuUserSetupData data)
        {
            data = null;

            if (!File.Exists(SettingsPath))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<PadImuUserSetupData>(File.ReadAllText(SettingsPath));
                return data != null;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[PadImuUserSetupStore] Failed to read {SettingsPath}: {exception.Message}");
                return false;
            }
        }

        public static void Save(PadImuUserSetupData data)
        {
            string directoryPath = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(SettingsPath, JsonUtility.ToJson(data, true));
        }
    }
}
