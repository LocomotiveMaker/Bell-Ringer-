using System.IO;
using BellRinger.Hardware;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerCli
    {
        private const string RequiredUnityVersion = "2022.3.9f1";

        public static void RunProjectValidation()
        {
            UnityEngine.Debug.Log($"[BellRingerCli] Unity {Application.unityVersion}");

            if (Application.unityVersion != RequiredUnityVersion)
            {
                throw new BuildFailedException($"Expected Unity {RequiredUnityVersion} but found {Application.unityVersion}.");
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                throw new BuildFailedException("No scenes are present in Build Settings.");
            }

            int enabledSceneCount = 0;
            string firstEnabledScenePath = null;

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                enabledSceneCount++;
                if (firstEnabledScenePath == null)
                {
                    firstEnabledScenePath = scene.path;
                }

                if (!File.Exists(scene.path))
                {
                    throw new BuildFailedException($"Build Settings scene is missing on disk: {scene.path}");
                }

                UnityEngine.Debug.Log($"[BellRingerCli] Build scene: {scene.path}");
            }

            if (enabledSceneCount == 0)
            {
                throw new BuildFailedException("No enabled scenes are present in Build Settings.");
            }

            EditorSceneManager.OpenScene(firstEnabledScenePath, OpenSceneMode.Single);
            UnityEngine.Debug.Log($"[BellRingerCli] Environment: {HardwareBridge.BuildEnvironmentSummary()}");
            UnityEngine.Debug.Log($"[BellRingerCli] Runtime status path: {BellRingerRuntimeStatusWriter.ResolveDefaultStatusPath()}");
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[BellRingerCli] Validation completed successfully.");
        }
    }
}
