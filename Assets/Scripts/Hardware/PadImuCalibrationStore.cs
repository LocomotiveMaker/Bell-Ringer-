using System;
using System.IO;
using UnityEngine;

namespace BellRinger.Hardware
{
    [Serializable]
    public sealed class PadImuCalibrationData
    {
        public bool hasBasisCalibration;
        public float basisW = 1f;
        public float basisX;
        public float basisY;
        public float basisZ;
    }

    public static class PadImuCalibrationStore
    {
        private const string RelativeSettingsPath = "Settings\\pad-imu-calibration.json";

        public static string SettingsPath => Path.Combine(HardwareBridge.GetProjectRootPath(), RelativeSettingsPath);

        public static bool TryLoadBasisQuaternion(out Quaternion basisQuaternion)
        {
            basisQuaternion = Quaternion.identity;

            if (!File.Exists(SettingsPath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(SettingsPath);
                PadImuCalibrationData data = JsonUtility.FromJson<PadImuCalibrationData>(json);
                if (data == null || !data.hasBasisCalibration)
                {
                    return false;
                }

                basisQuaternion = NormalizeQuaternion(new Quaternion(data.basisX, data.basisY, data.basisZ, data.basisW));
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[PadImuCalibrationStore] Failed to read {SettingsPath}: {exception.Message}");
                return false;
            }
        }

        public static void SaveBasisQuaternion(Quaternion basisQuaternion)
        {
            string directoryPath = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            Quaternion normalizedBasisQuaternion = NormalizeQuaternion(basisQuaternion);
            PadImuCalibrationData data = new PadImuCalibrationData
            {
                hasBasisCalibration = true,
                basisW = normalizedBasisQuaternion.w,
                basisX = normalizedBasisQuaternion.x,
                basisY = normalizedBasisQuaternion.y,
                basisZ = normalizedBasisQuaternion.z,
            };

            File.WriteAllText(SettingsPath, JsonUtility.ToJson(data, true));
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

            float inverseMagnitude = 1f / magnitude;
            return new Quaternion(
                quaternion.x * inverseMagnitude,
                quaternion.y * inverseMagnitude,
                quaternion.z * inverseMagnitude,
                quaternion.w * inverseMagnitude);
        }
    }
}
