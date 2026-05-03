using BellRinger.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerAudioLabSceneBuilder
    {
        private const string AudioFolderPath = "Assets/Settings/Audio";
        private const string ScenePath = "Assets/Scenes/AudioLabTest.unity";
        private const string BellClipPath = "Assets/Audios/freesound_community-bicycle-bell-66855.mp3";

        [MenuItem("Bell Ringer/Create Audio Lab Test Scene")]
        public static void CreateAudioLabTestScene()
        {
            EnsureAudioFolder();

            BellRingerAudioSourcePreset bell3d = CreatePreset("Bell3D", "Bell 3D", BellRingerAudioBus.Bell, 0.85f, 1f, 0.75f, 8f, false, 22000f, false);
            BellRingerAudioSourcePreset muffled = CreatePreset("MuffledFarBell", "Muffled Far Bell", BellRingerAudioBus.Bell, 0.8f, 1f, 1.5f, 12f, true, 1400f, true);
            BellRingerAudioSourcePreset tone = CreatePreset("GeneratedTone3D", "Generated Tone 3D", BellRingerAudioBus.GeneratedTone, 0.28f, 1f, 0.75f, 10f, false, 22000f, false);
            BellRingerAudioSourcePreset flat2d = CreatePreset("Flat2DReference", "Flat 2D Reference", BellRingerAudioBus.Bell, 0.75f, 0f, 1f, 8f, false, 22000f, false);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("BellRingerAudioLab");
            BellRingerAudioLabController controller = root.AddComponent<BellRingerAudioLabController>();

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("bellClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BellClipPath);
            serializedObject.FindProperty("bell3dPreset").objectReferenceValue = bell3d;
            serializedObject.FindProperty("muffledPreset").objectReferenceValue = muffled;
            serializedObject.FindProperty("tonePreset").objectReferenceValue = tone;
            serializedObject.FindProperty("flat2dPreset").objectReferenceValue = flat2d;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerAudioLabSceneBuilder] Created {ScenePath}.");
        }

        private static void EnsureAudioFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(AudioFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "Audio");
            }
        }

        private static BellRingerAudioSourcePreset CreatePreset(
            string fileName,
            string displayName,
            BellRingerAudioBus bus,
            float volume,
            float spatialBlend,
            float minDistance,
            float maxDistance,
            bool enableLowPass,
            float lowPassCutoffHz,
            bool enableReverbFilter)
        {
            string path = $"{AudioFolderPath}/{fileName}.asset";
            BellRingerAudioSourcePreset preset = AssetDatabase.LoadAssetAtPath<BellRingerAudioSourcePreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<BellRingerAudioSourcePreset>();
                AssetDatabase.CreateAsset(preset, path);
            }

            SerializedObject serializedObject = new SerializedObject(preset);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("bus").enumValueIndex = (int)bus;
            serializedObject.FindProperty("volume").floatValue = volume;
            serializedObject.FindProperty("pitch").floatValue = 1f;
            serializedObject.FindProperty("spatialBlend").floatValue = spatialBlend;
            serializedObject.FindProperty("spatialize").boolValue = false;
            serializedObject.FindProperty("spatializePostEffects").boolValue = false;
            serializedObject.FindProperty("rolloffMode").enumValueIndex = (int)AudioRolloffMode.Linear;
            serializedObject.FindProperty("minDistance").floatValue = minDistance;
            serializedObject.FindProperty("maxDistance").floatValue = maxDistance;
            serializedObject.FindProperty("dopplerLevel").floatValue = 0f;
            serializedObject.FindProperty("spread").floatValue = 0f;
            serializedObject.FindProperty("enableLowPass").boolValue = enableLowPass;
            serializedObject.FindProperty("lowPassCutoffHz").floatValue = lowPassCutoffHz;
            serializedObject.FindProperty("lowPassResonanceQ").floatValue = 1f;
            serializedObject.FindProperty("enableReverbFilter").boolValue = enableReverbFilter;
            serializedObject.FindProperty("reverbPreset").enumValueIndex = (int)AudioReverbPreset.Generic;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(preset);
            return preset;
        }
    }
}
