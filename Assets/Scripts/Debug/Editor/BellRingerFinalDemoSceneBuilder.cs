using BellRinger.FinalDemo;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerFinalDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/FinalDemo.unity";
        private const string AssetFolder = "Assets/ScriptableObjects/FinalDemo";
        private const string TuningPath = AssetFolder + "/FinalDemoTuningProfile.asset";
        private const string CueLibraryPath = AssetFolder + "/FinalDemoCueLibrary.asset";

        [MenuItem("Bell Ringer/Create Final Demo Scene")]
        public static void CreateFinalDemoScene()
        {
            EnsureAssetFolders();
            FinalDemoTuningProfile tuningProfile = GetOrCreateAsset<FinalDemoTuningProfile>(TuningPath);
            FinalDemoCueLibrary cueLibrary = GetOrCreateAsset<FinalDemoCueLibrary>(CueLibraryPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("FinalDemoRoot");
            FinalDemoDirector director = root.AddComponent<FinalDemoDirector>();
            FinalDemoInputStatus inputStatus = root.AddComponent<FinalDemoInputStatus>();
            FinalDemoOperatorControls operatorControls = root.AddComponent<FinalDemoOperatorControls>();

            GameObject playerRig = CreatePlayerRig(root.transform);
            GameObject hardwareRoot = CreateHardwareRoot(root.transform);
            GameObject padInputRoot = CreatePadInputRoot(root.transform);
            GameObject audioRoot = CreateAudioRoot(root.transform, cueLibrary, playerRig.transform);
            GameObject lightRoot = CreateLightRoot(root.transform, playerRig.transform, hardwareRoot.GetComponent<HardwareBridge>());
            GameObject hapticRoot = CreateHapticRoot(root.transform);
            CreatePlaceholderWorld(root.transform);

            SerializedObject directorObject = new SerializedObject(director);
            directorObject.FindProperty("tuningProfile").objectReferenceValue = tuningProfile;
            directorObject.FindProperty("cueLibrary").objectReferenceValue = cueLibrary;
            directorObject.FindProperty("playerRig").objectReferenceValue = playerRig.transform;
            directorObject.FindProperty("playerCamera").objectReferenceValue = playerRig.GetComponent<Camera>();
            directorObject.FindProperty("movementController").objectReferenceValue = playerRig.GetComponent<BellRingerSimpleMoveLookController>();
            directorObject.FindProperty("inputStatus").objectReferenceValue = inputStatus;
            directorObject.FindProperty("operatorControls").objectReferenceValue = operatorControls;
            directorObject.FindProperty("audioRouter").objectReferenceValue = audioRoot.GetComponent<FinalDemoAudioRouter>();
            directorObject.FindProperty("lightRouter").objectReferenceValue = lightRoot.GetComponent<FinalDemoLightRouter>();
            directorObject.FindProperty("hapticRouter").objectReferenceValue = hapticRoot.GetComponent<FinalDemoHapticRouter>();
            directorObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject statusObject = new SerializedObject(inputStatus);
            statusObject.FindProperty("hardwareBridge").objectReferenceValue = hardwareRoot.GetComponent<HardwareBridge>();
            statusObject.FindProperty("headImuReceiver").objectReferenceValue = playerRig.GetComponent<HeadImuReceiver>();
            statusObject.FindProperty("headTiltInputProvider").objectReferenceValue = playerRig.GetComponent<HeadTiltInputProvider>();
            statusObject.FindProperty("padTrackingReceiver").objectReferenceValue = padInputRoot.GetComponent<PadTrackingReceiver>();
            statusObject.FindProperty("padImuReceiver").objectReferenceValue = padInputRoot.GetComponent<PadImuReceiver>();
            statusObject.FindProperty("padPoseProvider").objectReferenceValue = padInputRoot.GetComponent<PadPoseProvider>();
            statusObject.FindProperty("audioRouter").objectReferenceValue = audioRoot.GetComponent<FinalDemoAudioRouter>();
            statusObject.FindProperty("lightRouter").objectReferenceValue = lightRoot.GetComponent<FinalDemoLightRouter>();
            statusObject.FindProperty("hapticRouter").objectReferenceValue = hapticRoot.GetComponent<FinalDemoHapticRouter>();
            statusObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controlsObject = new SerializedObject(operatorControls);
            controlsObject.FindProperty("director").objectReferenceValue = director;
            controlsObject.FindProperty("inputStatus").objectReferenceValue = inputStatus;
            controlsObject.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerFinalDemoSceneBuilder] Created {ScenePath}.");
        }

        private static GameObject CreatePlayerRig(Transform root)
        {
            GameObject playerRig = new GameObject("PlayerRig");
            playerRig.transform.SetParent(root);
            playerRig.transform.position = new Vector3(0f, 1.6f, -1.5f);

            Camera camera = playerRig.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.015f, 0.018f);
            camera.fieldOfView = 68f;
            playerRig.AddComponent<AudioListener>();
            playerRig.AddComponent<HeadImuReceiver>();
            playerRig.AddComponent<HeadTiltInputProvider>();
            playerRig.AddComponent<BellRingerSimpleMoveLookController>();
            return playerRig;
        }

        private static GameObject CreateHardwareRoot(Transform root)
        {
            GameObject hardwareRoot = new GameObject("HardwareBridge");
            hardwareRoot.transform.SetParent(root);
            hardwareRoot.AddComponent<HardwareBridge>();
            return hardwareRoot;
        }

        private static GameObject CreatePadInputRoot(Transform root)
        {
            GameObject padInputRoot = new GameObject("PadInput");
            padInputRoot.transform.SetParent(root);
            padInputRoot.AddComponent<PadTrackingReceiver>();
            padInputRoot.AddComponent<PadImuReceiver>();
            padInputRoot.AddComponent<PadPoseProvider>();
            return padInputRoot;
        }

        private static GameObject CreateAudioRoot(Transform root, FinalDemoCueLibrary cueLibrary, Transform listener)
        {
            GameObject audioRoot = new GameObject("WorldAudio");
            audioRoot.transform.SetParent(root);
            FinalDemoAudioRouter router = audioRoot.AddComponent<FinalDemoAudioRouter>();
            router.Initialize(cueLibrary, listener);
            return audioRoot;
        }

        private static GameObject CreateLightRoot(Transform root, Transform listener, HardwareBridge hardwareBridge)
        {
            GameObject lightRoot = new GameObject("WorldLight");
            lightRoot.transform.SetParent(root);
            FinalDemoLightRouter router = lightRoot.AddComponent<FinalDemoLightRouter>();
            router.Initialize(listener, hardwareBridge);
            return lightRoot;
        }

        private static GameObject CreateHapticRoot(Transform root)
        {
            GameObject hapticRoot = new GameObject("WorldHaptics");
            hapticRoot.transform.SetParent(root);
            hapticRoot.AddComponent<FinalDemoHapticRouter>();
            return hapticRoot;
        }

        private static void CreatePlaceholderWorld(Transform root)
        {
            CreatePlaceholder(root, "BellPlaceholder", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 2.4f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.1f, 0.85f, 0.25f));
            CreatePlaceholder(root, "GeneralTinnitusA", PrimitiveType.Sphere, new Vector3(-1.2f, 1.45f, 2.8f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.45f, 0.1f, 0.9f));
            CreatePlaceholder(root, "GeneralTinnitusB", PrimitiveType.Sphere, new Vector3(1.25f, 1.35f, 3.1f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.45f, 0.1f, 0.9f));
            CreatePlaceholder(root, "BossTinnitusPlaceholder", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 4.2f), new Vector3(0.42f, 0.42f, 0.42f), new Color(0.7f, 0.05f, 0.95f));
            CreatePlaceholder(root, "RainFloorPlaceholder", PrimitiveType.Cube, new Vector3(0f, -0.02f, 2f), new Vector3(5f, 0.02f, 5f), new Color(0.02f, 0.07f, 0.22f));
            CreatePlaceholder(root, "WallNoisePlaceholder", PrimitiveType.Cube, new Vector3(0f, 1.25f, 5f), new Vector3(4.5f, 2.5f, 0.05f), new Color(0.18f, 0.36f, 0.38f));
            CreatePlaceholder(root, "ForestEndingPlaceholder", PrimitiveType.Cube, new Vector3(0f, 1.2f, 6.2f), new Vector3(5f, 2.4f, 0.05f), new Color(0.05f, 0.2f, 0.08f));
        }

        private static void CreatePlaceholder(Transform root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject placeholder = GameObject.CreatePrimitive(type);
            placeholder.name = name;
            placeholder.transform.SetParent(root);
            placeholder.transform.position = position;
            placeholder.transform.localScale = scale;

            Renderer renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = color,
                };
            }
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureAssetFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "FinalDemo");
            }
        }
    }
}
