using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerClosedEyeCalibrationSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ClosedEyeCalibration.unity";

        [MenuItem("Bell Ringer/Create Closed Eye Calibration Scene")]
        public static void CreateClosedEyeCalibrationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("BellRingerClosedEyeCalibration");
            ClosedEyeCalibrationController controller = root.AddComponent<ClosedEyeCalibrationController>();

            SerializedObject serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("outputToHardware").boolValue = true;
            serializedObject.FindProperty("showRuntimeControls").boolValue = true;
            serializedObject.FindProperty("serialRefreshRate").floatValue = 20f;
            serializedObject.FindProperty("brightness").floatValue = 0.08f;
            serializedObject.FindProperty("coreSize").floatValue = 0.75f;
            serializedObject.FindProperty("effectSpeed").floatValue = 1.15f;
            serializedObject.FindProperty("averageLightScale").floatValue = 0.58f;
            serializedObject.FindProperty("peakContrast").floatValue = 1.85f;
            serializedObject.FindProperty("litPixelScale").floatValue = 0.55f;
            serializedObject.FindProperty("peakIntensityScale").floatValue = 1.25f;
            serializedObject.FindProperty("previewBrightnessBoost").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerClosedEyeCalibrationSceneBuilder] Created {ScenePath}.");
        }
    }
}
