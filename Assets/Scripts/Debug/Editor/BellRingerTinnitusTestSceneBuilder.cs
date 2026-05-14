using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerTinnitusTestSceneBuilder
    {
        private const string NormalScenePath = "Assets/Scenes/TinnitusTest.unity";
        private const string GiantScenePath = "Assets/Scenes/GiantTinnitusTest.unity";
        private const string BellGazeScenePath = "Assets/Scenes/BellGazeTutorialTest.unity";
        private const string LongGlitchClipPath = "Assets/Audios/glitch/long glitch.mp3";
        private const string ShortGlitchClipPath = "Assets/Audios/glitch/short glitch.mp3";
        private const string ResolveClipPath = "Assets/Audio/Curated/Tinnitus/Resolve/01_mixkit_sci_fi_confirmation_clean.wav";
        private const string BellClipPath = "Assets/Audios/Bell/freesound_community-bicycle-bell-66855.mp3";

        [MenuItem("Bell Ringer/Create Tinnitus Test Scene")]
        public static void CreateTinnitusTestScene()
        {
            CreateScene(NormalScenePath, "BellRingerTinnitusTest", testMode: 0, enablePad: true, enableGiant: false, enableBell: false);
        }

        [MenuItem("Bell Ringer/Create Giant Tinnitus Test Scene")]
        public static void CreateGiantTinnitusTestScene()
        {
            CreateScene(GiantScenePath, "BellRingerGiantTinnitusTest", testMode: 1, enablePad: true, enableGiant: true, enableBell: false);
        }

        [MenuItem("Bell Ringer/Create Bell Gaze Tutorial Test Scene")]
        public static void CreateBellGazeTutorialTestScene()
        {
            CreateScene(BellGazeScenePath, "BellRingerBellGazeTutorialTest", testMode: 2, enablePad: false, enableGiant: false, enableBell: true);
        }

        private static void CreateScene(string scenePath, string rootName, int testMode, bool enablePad, bool enableGiant, bool enableBell)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject(rootName);
            TinnitusTestController controller = root.AddComponent<TinnitusTestController>();

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("testMode").enumValueIndex = testMode;
            serializedObject.FindProperty("continuousGlitchClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(LongGlitchClipPath);
            serializedObject.FindProperty("shortGlitchClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ShortGlitchClipPath);
            serializedObject.FindProperty("resolveClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ResolveClipPath);
            serializedObject.FindProperty("bellClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BellClipPath);
            serializedObject.FindProperty("outputToHardware").boolValue = true;
            serializedObject.FindProperty("showRuntimeControls").boolValue = true;
            serializedObject.FindProperty("enablePadTreatmentPrototype").boolValue = enablePad;
            serializedObject.FindProperty("enableTreatmentHaptics").boolValue = true;
            serializedObject.FindProperty("enableGiantTinnitusPrototype").boolValue = enableGiant;
            serializedObject.FindProperty("enableBellGazeTutorialPrototype").boolValue = enableBell;
            serializedObject.FindProperty("headImuPortName").stringValue = "COM40";
            serializedObject.FindProperty("previewBrightnessBoost").floatValue = 5f;
            serializedObject.FindProperty("treatmentSeconds").floatValue = 4f;
            serializedObject.FindProperty("treatmentLockPulseSeconds").floatValue = 0.16f;
            serializedObject.FindProperty("treatmentHealingPulseHz").floatValue = 2.1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerTinnitusTestSceneBuilder] Created {scenePath}.");
        }
    }
}
