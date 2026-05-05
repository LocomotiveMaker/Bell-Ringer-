using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerTinnitusTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TinnitusTest.unity";
        private const string LongGlitchClipPath = "Assets/Audios/glitch/long glitch.mp3";
        private const string ShortGlitchClipPath = "Assets/Audios/glitch/short glitch.mp3";

        [MenuItem("Bell Ringer/Create Tinnitus Test Scene")]
        public static void CreateTinnitusTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("BellRingerTinnitusTest");
            TinnitusTestController controller = root.AddComponent<TinnitusTestController>();

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("continuousGlitchClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(LongGlitchClipPath);
            serializedObject.FindProperty("shortGlitchClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ShortGlitchClipPath);
            serializedObject.FindProperty("outputToHardware").boolValue = true;
            serializedObject.FindProperty("showRuntimeControls").boolValue = true;
            serializedObject.FindProperty("previewBrightnessBoost").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerTinnitusTestSceneBuilder] Created {ScenePath}.");
        }
    }
}
