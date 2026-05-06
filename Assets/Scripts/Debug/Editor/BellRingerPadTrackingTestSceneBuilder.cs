using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerPadTrackingTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PadTrackingTest.unity";

        [MenuItem("Bell Ringer/Create Pad Tracking Test Scene")]
        public static void CreatePadTrackingTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("BellRingerPadTrackingTest");
            PadTrackingTestController controller = root.AddComponent<PadTrackingTestController>();

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("showRuntimeOverlay").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerPadTrackingTestSceneBuilder] Created {ScenePath}.");
        }
    }
}
