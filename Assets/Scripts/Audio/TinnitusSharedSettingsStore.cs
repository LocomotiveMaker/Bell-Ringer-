using System;
using System.IO;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Audio
{
    [Serializable]
    public sealed class TinnitusSharedSettingsData
    {
        public float volume = 0.052f;
        public float baseFrequency = 6627.497f;
        public float beatFrequencyOffset = 10.013f;
        public float pitchWobble = 0.016f;
        public float glitchDensity = 0.455f;
        public float roughness = 0.528f;
        public float burstIntensity = 0.353f;
        public float cleanseStability;
        public float coreIntensity = 0.25f;
        public float coreSize = 1.12f;
        public float tearAmount = 5f;
        public float jitter;
        public float smearDecay = 1.92f;
        public float pulseRate = 0.947f;
        public float instability = 0.425f;
        public float averageLightScale = 0.898f;
        public float peakIntensityScale = 1.638f;
        public float peakContrast = 1.309f;
        public float litPixelScale = 0.25f;
        public float tearAxisX = 1f;
        public float tearAxisY;
    }

    public static class TinnitusSharedSettingsStore
    {
        private const string RelativeSettingsPath = "Settings\\tinnitus-shared-settings.json";

        public static string SettingsPath => Path.Combine(HardwareBridge.GetProjectRootPath(), RelativeSettingsPath);
        public static bool HasSavedSettings => File.Exists(SettingsPath);

        public static TinnitusSharedSettingsData Load()
        {
            if (!HasSavedSettings)
            {
                return new TinnitusSharedSettingsData();
            }

            try
            {
                string json = File.ReadAllText(SettingsPath);
                TinnitusSharedSettingsData data = JsonUtility.FromJson<TinnitusSharedSettingsData>(json);
                return data ?? new TinnitusSharedSettingsData();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[TinnitusSharedSettingsStore] Failed to read {SettingsPath}: {exception.Message}");
                return new TinnitusSharedSettingsData();
            }
        }

        public static void Save(TinnitusSharedSettingsData data)
        {
            string directoryPath = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(SettingsPath, JsonUtility.ToJson(data ?? new TinnitusSharedSettingsData(), true));
        }
    }
}
