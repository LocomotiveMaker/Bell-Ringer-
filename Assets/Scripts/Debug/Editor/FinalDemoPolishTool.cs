using BellRinger.FinalDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BellRinger.Debug.Editor
{
    public static class FinalDemoPolishTool
    {
        private const string ScenePath = "Assets/Scenes/FinalDemo.unity";
        private const string TuningProfilePath = "Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset";
        private const string BellModelPath = "Assets/Resources/FinalDemoModels/Bell/BellModel.obj";
        private const string PadModelPath = "Assets/Resources/FinalDemoModels/Pad/PadGamepad.fbx";
        private const string PadTexturePath = "Assets/Art/Models/Pad/gamepads/textures/5.png";

        [MenuItem("Bell Ringer/Final Demo/Apply Polish")]
        public static void ApplyFinalDemoPolish()
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(BellModelPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PadModelPath, ImportAssetOptions.ForceUpdate);

            EditorSceneManager.OpenScene(ScenePath);
            GameObject root = GameObject.Find("FinalDemoRoot");
            if (root == null)
            {
                throw new MissingReferenceException("FinalDemoRoot was not found.");
            }

            ApplyTuningProfileDefaults();
            ApplyRootGuide(root);
            ApplyModelPresenter(root);

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[FinalDemoPolishTool] Applied FinalDemo polish.");
        }

        private static void ApplyTuningProfileDefaults()
        {
            FinalDemoTuningProfile profile = AssetDatabase.LoadAssetAtPath<FinalDemoTuningProfile>(TuningProfilePath);
            if (profile == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(profile);
            SetFloat(serialized, "bellOrbitSeconds", 19.1f);
            SetFloat(serialized, "bellOrbitCallIntervalSeconds", 0.39f);
            SetBool(serialized, "bellFollowProgressBlockerEnabled", true);
            SetFloat(serialized, "bellFollowBlockerMarginMeters", 0.35f);
            SetFloat(serialized, "rainIntensityRampSeconds", 3f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        private static void ApplyRootGuide(GameObject root)
        {
            FinalDemoKoreanGuide guide = root.GetComponent<FinalDemoKoreanGuide>();
            if (guide == null)
            {
                guide = root.AddComponent<FinalDemoKoreanGuide>();
            }

            EditorUtility.SetDirty(guide);
        }

        private static void ApplyModelPresenter(GameObject root)
        {
            FinalDemoSceneReferences sceneReferences = root.GetComponent<FinalDemoSceneReferences>();
            FinalDemoModelPresenter presenter = root.GetComponent<FinalDemoModelPresenter>();
            if (presenter == null)
            {
                presenter = root.AddComponent<FinalDemoModelPresenter>();
            }

            SerializedObject serialized = new SerializedObject(presenter);
            SetObject(serialized, "bellVisual", sceneReferences != null ? sceneReferences.BellVisual : null);
            SetObject(serialized, "padVisual", sceneReferences != null ? sceneReferences.PadVisual : null);
            SetObject(serialized, "bellModelPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BellModelPath));
            SetObject(serialized, "padModelPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PadModelPath));
            SetVector3(serialized, "bellModelLocalPosition", new Vector3(0f, -0.04f, 0f));
            SetVector3(serialized, "bellModelLocalEuler", new Vector3(-180f, 0f, 0f));
            SetFloat(serialized, "bellTargetMaxSize", 0.56f);
            SetVector3(serialized, "padModelLocalPosition", Vector3.zero);
            SetVector3(serialized, "padModelLocalEuler", new Vector3(0f, 270f, 0f));
            SetFloat(serialized, "padTargetMaxSize", 0.72f);
            SetObject(serialized, "padAlbedoTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(PadTexturePath));
            SetBool(serialized, "applyPadTextureMaterial", true);
            SetBool(serialized, "applyWhitePadMaterial", false);
            SetBool(serialized, "showSinglePadModel", true);
            SetInt(serialized, "padVisibleModelIndex", 0);
            SetBool(serialized, "centerModelBoundsOnLocalPosition", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            presenter.ApplyModelVisuals();
            EditorUtility.SetDirty(presenter);
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
