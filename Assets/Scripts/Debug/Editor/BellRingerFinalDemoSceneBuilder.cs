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
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.33f, 0.36f, 0.38f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.32f);

            GameObject root = new GameObject("FinalDemoRoot");
            FinalDemoDirector director = root.AddComponent<FinalDemoDirector>();
            FinalDemoInputStatus inputStatus = root.AddComponent<FinalDemoInputStatus>();
            FinalDemoOperatorControls operatorControls = root.AddComponent<FinalDemoOperatorControls>();
            FinalDemoSceneReferences sceneReferences = root.AddComponent<FinalDemoSceneReferences>();
            FinalDemoStageVisibility stageVisibility = root.AddComponent<FinalDemoStageVisibility>();

            GameObject playerRoot = CreateRoot(root.transform, "Player");
            GameObject hardwareRoot = CreateRoot(root.transform, "Hardware");
            GameObject systemsRoot = CreateRoot(root.transform, "Systems");
            GameObject worldRoot = CreateRoot(root.transform, "World");
            GameObject padRoot = CreateRoot(root.transform, "Pad");
            GameObject bellRoot = CreateRoot(root.transform, "Bell");
            GameObject rainRoot = CreateRoot(root.transform, "Rain");
            GameObject tinnitusRoot = CreateRoot(root.transform, "Tinnitus");
            GameObject bossRoot = CreateRoot(root.transform, "BossTinnitus");
            GameObject forestRoot = CreateRoot(root.transform, "Forest");
            GameObject uiRoot = CreateRoot(root.transform, "UI");

            GameObject playerRig = CreatePlayerRig(playerRoot.transform);
            GameObject hardwareBridge = CreateHardwareRoot(hardwareRoot.transform);
            GameObject padInput = CreatePadInputRoot(systemsRoot.transform);
            GameObject audioRoot = CreateAudioRoot(systemsRoot.transform, cueLibrary, playerRig.transform);
            GameObject lightRoot = CreateLightRoot(systemsRoot.transform, playerRig.transform, hardwareBridge.GetComponent<HardwareBridge>());
            GameObject hapticRoot = CreateHapticRoot(systemsRoot.transform);
            GameObject authoringCapture = CreateAuthoringCaptureRoot(systemsRoot.transform);
            CreateWorldEnvironment(worldRoot.transform, out GameObject darkSky, out GameObject clearSky);
            CreatePadVisual(padRoot.transform, playerRig.transform, padInput.GetComponent<PadPoseProvider>());
            CreateBellAuthoring(bellRoot.transform);
            CreateRainAuthoring(rainRoot.transform);
            CreateTinnitusAuthoring(tinnitusRoot.transform);
            CreateBossAuthoring(bossRoot.transform);
            CreateForestAuthoring(forestRoot.transform);
            CreateUiAuthoring(uiRoot.transform);

            AssignDirector(director, tuningProfile, cueLibrary, sceneReferences, playerRig, inputStatus, operatorControls, audioRoot, lightRoot, hapticRoot);
            AssignInputStatus(inputStatus, hardwareBridge, playerRig, padInput, audioRoot, lightRoot, hapticRoot);
            AssignOperator(operatorControls, director, inputStatus);
            AssignSceneReferences(sceneReferences, playerRoot, playerRig, hardwareRoot, systemsRoot, worldRoot, padRoot, bellRoot, rainRoot, tinnitusRoot, bossRoot, forestRoot, uiRoot, padInput);
            AssignAuthoringCapture(authoringCapture.GetComponent<FinalDemoAuthoringCapture>(), sceneReferences, padInput, playerRig);
            AssignStageVisibility(stageVisibility, director, padRoot, bellRoot, rainRoot, tinnitusRoot, bossRoot, forestRoot, darkSky, clearSky);

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerFinalDemoSceneBuilder] Created authoring-centered {ScenePath}.");
        }

        private static GameObject CreateRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject CreatePlayerRig(Transform root)
        {
            GameObject playerRig = new GameObject("PlayerRig");
            playerRig.transform.SetParent(root);
            playerRig.transform.position = new Vector3(0f, 1.6f, -1.5f);

            Camera camera = playerRig.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.045f, 0.05f);
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 80f;
            playerRig.AddComponent<AudioListener>();
            playerRig.AddComponent<HeadImuReceiver>();
            playerRig.AddComponent<HeadTiltInputProvider>();
            playerRig.AddComponent<BellRingerSimpleMoveLookController>();
            FinalDemoPlayerViewViewportLayout viewportLayout = playerRig.AddComponent<FinalDemoPlayerViewViewportLayout>();
            SerializedObject viewportObject = new SerializedObject(viewportLayout);
            viewportObject.FindProperty("playerViewCamera").objectReferenceValue = camera;
            viewportObject.FindProperty("followOperatorHudBand").boolValue = true;
            viewportObject.FindProperty("fillRemainingTopArea").boolValue = true;
            viewportObject.FindProperty("viewportWidth01").floatValue = 1f;
            viewportObject.FindProperty("viewportHeight01").floatValue = 0.5f;
            viewportObject.FindProperty("bottomHudBandPixels").floatValue = 316f;
            viewportObject.FindProperty("bottomGapPixels").floatValue = 18f;
            viewportObject.FindProperty("verticalOffsetPixels").floatValue = 0f;
            viewportObject.ApplyModifiedPropertiesWithoutUndo();
            return playerRig;
        }

        private static GameObject CreateHardwareRoot(Transform root)
        {
            GameObject hardwareRoot = new GameObject("HardwareBridge_COM40_LED_HEAD");
            hardwareRoot.transform.SetParent(root);
            hardwareRoot.AddComponent<HardwareBridge>();
            return hardwareRoot;
        }

        private static GameObject CreatePadInputRoot(Transform root)
        {
            GameObject padInputRoot = new GameObject("PadInput_COM30_ARUCO_IMU");
            padInputRoot.transform.SetParent(root);
            padInputRoot.AddComponent<PadTrackingReceiver>();
            padInputRoot.AddComponent<PadImuReceiver>();
            padInputRoot.AddComponent<PadPoseProvider>();
            padInputRoot.AddComponent<PadPoseMatchEvaluator>();
            return padInputRoot;
        }

        private static GameObject CreateAudioRoot(Transform root, FinalDemoCueLibrary cueLibrary, Transform listener)
        {
            GameObject audioRoot = new GameObject("AudioRouter");
            audioRoot.transform.SetParent(root);
            FinalDemoAudioRouter router = audioRoot.AddComponent<FinalDemoAudioRouter>();
            router.Initialize(cueLibrary, listener);
            return audioRoot;
        }

        private static GameObject CreateLightRoot(Transform root, Transform listener, HardwareBridge hardwareBridge)
        {
            GameObject lightRoot = new GameObject("LightRouter_LED");
            lightRoot.transform.SetParent(root);
            FinalDemoLightRouter router = lightRoot.AddComponent<FinalDemoLightRouter>();
            router.Initialize(listener, hardwareBridge);
            return lightRoot;
        }

        private static GameObject CreateHapticRoot(Transform root)
        {
            GameObject hapticRoot = new GameObject("HapticRouter_Gamepad");
            hapticRoot.transform.SetParent(root);
            hapticRoot.AddComponent<FinalDemoHapticRouter>();
            return hapticRoot;
        }

        private static GameObject CreateAuthoringCaptureRoot(Transform root)
        {
            GameObject captureRoot = new GameObject("AuthoringCapture_RuntimePoseAndPath");
            captureRoot.transform.SetParent(root);
            captureRoot.AddComponent<FinalDemoAuthoringCapture>();
            return captureRoot;
        }

        private static void CreateWorldEnvironment(Transform root, out GameObject darkSky, out GameObject clearSky)
        {
            GameObject environment = CreateRoot(root, "Environment");
            CreatePrimitive(environment.transform, "Ground_Grey_Authoring", PrimitiveType.Cube, new Vector3(0f, -0.04f, 4f), new Vector3(24f, 0.08f, 24f), new Color(0.34f, 0.36f, 0.38f));
            CreatePrimitive(environment.transform, "BackWall_White_Authoring", PrimitiveType.Cube, new Vector3(0f, 2.15f, 9f), new Vector3(18f, 4.3f, 0.08f), new Color(0.86f, 0.90f, 0.93f));
            CreatePrimitive(environment.transform, "LeftSoftWall_Authoring", PrimitiveType.Cube, new Vector3(-9f, 1.8f, 3.8f), new Vector3(0.08f, 3.6f, 10f), new Color(0.43f, 0.46f, 0.48f));
            CreatePrimitive(environment.transform, "RightSoftWall_Authoring", PrimitiveType.Cube, new Vector3(9f, 1.8f, 3.8f), new Vector3(0.08f, 3.6f, 10f), new Color(0.43f, 0.46f, 0.48f));

            darkSky = CreateRoot(environment.transform, "DarkSkyAndFog_Authoring");
            CreatePrimitive(darkSky.transform, "DarkSkyPlane", PrimitiveType.Cube, new Vector3(0f, 5.2f, 4.5f), new Vector3(22f, 0.04f, 20f), new Color(0.05f, 0.06f, 0.07f));
            CreatePrimitive(darkSky.transform, "DistantFogBand", PrimitiveType.Cube, new Vector3(0f, 1.35f, 6.2f), new Vector3(14f, 1.4f, 0.08f), new Color(0.25f, 0.28f, 0.30f));

            clearSky = CreateRoot(environment.transform, "ClearBlueSky_Forest_Authoring");
            CreatePrimitive(clearSky.transform, "BlueSkyPlane", PrimitiveType.Cube, new Vector3(0f, 5.3f, 4.5f), new Vector3(22f, 0.04f, 20f), new Color(0.33f, 0.62f, 0.86f));
            CreatePrimitive(clearSky.transform, "SoftHorizon", PrimitiveType.Cube, new Vector3(0f, 2.4f, 8.6f), new Vector3(16f, 1.5f, 0.08f), new Color(0.64f, 0.78f, 0.84f));
        }

        private static GameObject CreatePadVisual(Transform root, Transform viewReference, PadPoseProvider padPoseProvider)
        {
            GameObject pad = CreateRoot(root, "PadVisual_Authoring_FollowsPose");
            pad.transform.position = new Vector3(0f, 1.08f, -0.7f);
            pad.transform.localScale = Vector3.one * 1.55f;
            FinalDemoPadSceneVisual visual = pad.AddComponent<FinalDemoPadSceneVisual>();

            Material shell = CreateMaterial(new Color(0.03f, 0.035f, 0.04f));
            Material dark = CreateMaterial(new Color(0.13f, 0.14f, 0.15f));
            Material amber = CreateMaterial(new Color(1f, 0.70f, 0.12f));
            Material white = CreateMaterial(new Color(0.92f, 0.94f, 0.88f));
            Material blue = CreateMaterial(new Color(0.1f, 0.56f, 0.95f));
            Material yellow = CreateMaterial(new Color(0.95f, 0.82f, 0.1f));
            Material green = CreateMaterial(new Color(0.22f, 0.82f, 0.28f));
            Material red = CreateMaterial(new Color(0.9f, 0.16f, 0.12f));

            CreatePrimitive(pad.transform, "Pad_Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.56f, 0.06f, 0.24f), shell);
            CreatePrimitive(pad.transform, "Pad_LeftGrip", PrimitiveType.Sphere, new Vector3(-0.25f, -0.015f, -0.025f), new Vector3(0.27f, 0.07f, 0.22f), shell);
            CreatePrimitive(pad.transform, "Pad_RightGrip", PrimitiveType.Sphere, new Vector3(0.25f, -0.015f, -0.025f), new Vector3(0.27f, 0.07f, 0.22f), shell);
            CreatePrimitive(pad.transform, "Pad_AmberGlow", PrimitiveType.Cylinder, new Vector3(0f, -0.04f, 0f), new Vector3(0.46f, 0.006f, 0.20f), amber);
            CreatePrimitive(pad.transform, "Pad_ArucoPlate_Yellow", PrimitiveType.Cube, new Vector3(0f, 0.037f, 0.12f), new Vector3(0.15f, 0.008f, 0.11f), amber);
            CreatePrimitive(pad.transform, "Pad_ArucoWhite", PrimitiveType.Cube, new Vector3(0f, 0.045f, 0.12f), new Vector3(0.11f, 0.006f, 0.075f), white);
            CreatePrimitive(pad.transform, "Pad_LeftStick", PrimitiveType.Cylinder, new Vector3(-0.18f, 0.05f, 0.035f), new Vector3(0.07f, 0.018f, 0.07f), dark);
            CreatePrimitive(pad.transform, "Pad_RightStick", PrimitiveType.Cylinder, new Vector3(0.16f, 0.05f, -0.045f), new Vector3(0.07f, 0.018f, 0.07f), dark);
            CreatePrimitive(pad.transform, "Pad_DPadH", PrimitiveType.Cube, new Vector3(-0.19f, 0.052f, -0.055f), new Vector3(0.13f, 0.018f, 0.033f), dark);
            CreatePrimitive(pad.transform, "Pad_DPadV", PrimitiveType.Cube, new Vector3(-0.19f, 0.054f, -0.055f), new Vector3(0.033f, 0.018f, 0.13f), dark);
            CreatePrimitive(pad.transform, "Button_X_Blue", PrimitiveType.Sphere, new Vector3(0.25f, 0.055f, 0.04f), new Vector3(0.035f, 0.018f, 0.035f), blue);
            CreatePrimitive(pad.transform, "Button_Y_Yellow", PrimitiveType.Sphere, new Vector3(0.285f, 0.055f, 0.075f), new Vector3(0.035f, 0.018f, 0.035f), yellow);
            CreatePrimitive(pad.transform, "Button_A_Green", PrimitiveType.Sphere, new Vector3(0.285f, 0.055f, 0.005f), new Vector3(0.035f, 0.018f, 0.035f), green);
            CreatePrimitive(pad.transform, "Button_B_Red", PrimitiveType.Sphere, new Vector3(0.32f, 0.055f, 0.04f), new Vector3(0.035f, 0.018f, 0.035f), red);

            SerializedObject visualObject = new SerializedObject(visual);
            visualObject.FindProperty("viewReference").objectReferenceValue = viewReference;
            visualObject.FindProperty("padPoseProvider").objectReferenceValue = padPoseProvider;
            visualObject.ApplyModifiedPropertiesWithoutUndo();
            return pad;
        }

        private static GameObject CreateBellAuthoring(Transform root)
        {
            GameObject bell = CreateRoot(root, "FinalDemo_BellPlaceholder");
            bell.transform.position = new Vector3(0f, 1.6f, 2.4f);
            bell.transform.localScale = Vector3.one;

            Material bronze = CreateMaterial(new Color(0.24f, 0.17f, 0.10f));
            Material patina = CreateMaterial(new Color(0.08f, 0.36f, 0.24f));
            Material glow = CreateMaterial(new Color(0.16f, 0.95f, 0.38f));
            CreatePrimitive(bell.transform, "Bell_Body_Bronze", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.42f, 0.34f, 0.42f), bronze);
            CreatePrimitive(bell.transform, "Bell_Mouth_Patina_Ring", PrimitiveType.Cylinder, new Vector3(0f, -0.24f, 0f), new Vector3(0.5f, 0.07f, 0.5f), patina);
            CreatePrimitive(bell.transform, "Bell_Handle", PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0.09f, 0.35f, 0.09f), patina);
            CreatePrimitive(bell.transform, "Bell_GreenSoundRing", PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0f), new Vector3(0.66f, 0.035f, 0.66f), glow);
            CreatePrimitive(bell.transform, "Bell_Clapper", PrimitiveType.Sphere, new Vector3(0f, -0.36f, 0f), new Vector3(0.14f, 0.14f, 0.14f), bronze);

            GameObject orbitPath = CreatePath(root, "BellOrbitPath_Authoring", new[]
            {
                new Vector3(-1.4f, 1.65f, 1.5f),
                new Vector3(0f, 2.05f, 2.1f),
                new Vector3(1.4f, 1.62f, 1.7f),
                new Vector3(0.35f, 1.3f, 1.0f),
            }, true);
            GameObject followPath = CreatePath(root, "BellFollowPath_Authoring", new[]
            {
                new Vector3(-1.4f, 1.45f, 3.2f),
                new Vector3(1.55f, 1.45f, 4.1f),
            });
            GameObject gazePath = CreatePath(root, "BellGazePath_Authoring", new[]
            {
                new Vector3(-0.65f, 1.48f, 1.85f),
                new Vector3(0.74f, 1.57f, 1.95f),
                new Vector3(0.1f, 1.72f, 2.35f),
            });
            orbitPath.transform.SetSiblingIndex(1);
            followPath.transform.SetSiblingIndex(2);
            gazePath.transform.SetSiblingIndex(3);
            return bell;
        }

        private static void CreateRainAuthoring(Transform root)
        {
            GameObject volume = CreateRoot(root, "RainVolume_Authoring");
            volume.transform.position = new Vector3(0f, 0f, 3.2f);
            CreatePrimitive(volume.transform, "FinalDemo_RainFloor", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(12f, 0.02f, 8f), new Color(0.12f, 0.15f, 0.18f));
            CreatePrimitive(volume.transform, "RainGroundRippleGuide", PrimitiveType.Cylinder, new Vector3(-1.3f, 0.035f, -0.8f), new Vector3(0.7f, 0.006f, 0.7f), new Color(0.34f, 0.62f, 0.82f));
            CreatePrimitive(volume.transform, "RainGroundRippleGuide_B", PrimitiveType.Cylinder, new Vector3(1.6f, 0.035f, 0.5f), new Vector3(0.95f, 0.006f, 0.95f), new Color(0.34f, 0.62f, 0.82f));
            CreatePrimitive(volume.transform, "RainSkySheet", PrimitiveType.Cube, new Vector3(0f, 3.2f, 0f), new Vector3(10f, 0.04f, 7f), new Color(0.18f, 0.28f, 0.36f));
            CreatePrimitive(volume.transform, "RainFogVolume", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.8f), new Vector3(10f, 1.5f, 5f), new Color(0.24f, 0.28f, 0.31f));
            CreateRainParticleSystem(volume.transform, "RainSkyParticles", new Vector3(0f, 3.4f, 0f), 550f, 1.15f, new Vector3(9.5f, 0.1f, 6.5f));
            CreateRippleParticleSystem(volume.transform, "RainGroundRippleParticles", new Vector3(0f, 0.06f, 0f));
        }

        private static void CreateTinnitusAuthoring(Transform root)
        {
            GameObject one = CreateTinnitusObject(root, "FinalDemo_TinnitusA", new Vector3(-1.2f, 1.45f, 2.8f));
            GameObject onePose = CreatePoseMarker(root, "TinnitusA_HealPose_Authoring", new Vector3(-0.16f, -0.03f, 0.68f), new Vector3(0f, 0f, 0f), new Color(0.72f, 0.26f, 1f));
            onePose.transform.SetParent(one.transform.parent);

            GameObject two = CreateTinnitusObject(root, "FinalDemo_TinnitusB", new Vector3(1.25f, 1.35f, 3.1f));
            GameObject twoPose = CreatePoseMarker(root, "TinnitusB_HealPose_Authoring", new Vector3(0.18f, 0.02f, 0.74f), new Vector3(12f, -18f, 8f), new Color(0.72f, 0.26f, 1f));
            twoPose.transform.SetParent(two.transform.parent);
        }

        private static GameObject CreateTinnitusObject(Transform root, string name, Vector3 position)
        {
            GameObject tinnitus = CreateRoot(root, name);
            tinnitus.transform.position = position;
            Material core = CreateMaterial(new Color(0.35f, 0.08f, 0.78f));
            Material tear = CreateMaterial(new Color(0.80f, 0.24f, 1f));
            CreatePrimitive(tinnitus.transform, "Tinnitus_Core", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), core);
            CreatePrimitive(tinnitus.transform, "Tinnitus_Tear_Left", PrimitiveType.Cube, new Vector3(-0.32f, 0f, 0f), new Vector3(0.32f, 0.06f, 0.06f), tear);
            CreatePrimitive(tinnitus.transform, "Tinnitus_Tear_Right", PrimitiveType.Cube, new Vector3(0.32f, 0f, 0f), new Vector3(0.32f, 0.06f, 0.06f), tear);
            return tinnitus;
        }

        private static void CreateBossAuthoring(Transform root)
        {
            GameObject boss = CreateRoot(root, "FinalDemo_BossTinnitus");
            boss.transform.position = new Vector3(0f, 1.6f, 4.2f);
            Material mass = CreateMaterial(new Color(0.20f, 0.04f, 0.42f));
            Material weak = CreateMaterial(new Color(0.93f, 0.28f, 1f));
            CreatePrimitive(boss.transform, "BossMass_A", PrimitiveType.Sphere, new Vector3(-0.3f, 0.04f, 0f), new Vector3(0.88f, 0.88f, 0.88f), mass);
            CreatePrimitive(boss.transform, "BossMass_B", PrimitiveType.Sphere, new Vector3(0.34f, -0.02f, 0.02f), new Vector3(0.78f, 0.72f, 0.74f), mass);
            CreatePrimitive(boss.transform, "BossWeakpoint_Current", PrimitiveType.Sphere, new Vector3(0.2f, 0.18f, -0.45f), new Vector3(0.16f, 0.16f, 0.16f), weak);
            CreatePoseMarker(root, "BossPose_01_Authoring", new Vector3(-0.12f, 0f, 0.68f), Vector3.zero, new Color(0.9f, 0.2f, 1f));
            CreatePoseMarker(root, "BossPose_02_Authoring", new Vector3(0.05f, -0.02f, 0.74f), new Vector3(10f, 16f, -8f), new Color(0.9f, 0.2f, 1f));
            CreatePoseMarker(root, "BossPose_03_Authoring", new Vector3(0.16f, 0.04f, 0.80f), new Vector3(-12f, -20f, 12f), new Color(0.9f, 0.2f, 1f));
            CreatePath(root, "BossWeakpointPath_01_Authoring", new[] { new Vector3(-0.22f, 1.56f, 3.9f), new Vector3(0.45f, 1.62f, 4.1f) });
            CreatePath(root, "BossWeakpointPath_02_Authoring", new[] { new Vector3(0.18f, 1.45f, 3.82f), new Vector3(-0.35f, 1.78f, 4.18f) });
            CreatePath(root, "BossWeakpointPath_03_Authoring", new[] { new Vector3(-0.1f, 1.72f, 3.75f), new Vector3(0.1f, 1.34f, 4.25f), new Vector3(0.48f, 1.52f, 4.05f) });
        }

        private static void CreateForestAuthoring(Transform root)
        {
            GameObject set = CreateRoot(root, "ForestSet_Authoring");
            set.transform.position = new Vector3(0f, 0f, 5.2f);
            CreatePrimitive(set.transform, "ForestGround_DarkGreen", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.4f), new Vector3(14f, 0.04f, 8f), new Color(0.10f, 0.18f, 0.12f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/Pine_1.fbx", "Pine_Left_Authoring", new Vector3(-2.9f, 0f, 0.7f), 0.38f, new Color(0.08f, 0.30f, 0.13f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/Pine_2.fbx", "Pine_Back_Authoring", new Vector3(2.7f, 0f, 1.8f), 0.34f, new Color(0.09f, 0.34f, 0.16f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/CommonTree_1.fbx", "CommonTree_Right_Authoring", new Vector3(3.7f, 0f, -0.2f), 0.32f, new Color(0.18f, 0.42f, 0.22f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/DeadTree_1.fbx", "DeadTree_Back_Authoring", new Vector3(-3.6f, 0f, 2.2f), 0.30f, new Color(0.28f, 0.22f, 0.16f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/Rock_Medium_1.fbx", "Rock_Left_Authoring", new Vector3(-1.2f, 0f, 0.2f), 0.85f, new Color(0.34f, 0.36f, 0.34f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/Rock_Medium_2.fbx", "Rock_Right_Authoring", new Vector3(1.45f, 0f, 0.45f), 0.95f, new Color(0.32f, 0.34f, 0.32f));
            InstantiateForestModel(set.transform, "Assets/Resources/ObserverAssets/Forest/Models/Grass_Common_Tall.fbx", "Grass_Center_Authoring", new Vector3(0f, 0f, -0.45f), 1.1f, new Color(0.18f, 0.42f, 0.18f));
        }

        private static void CreateUiAuthoring(Transform root)
        {
            GameObject marker = CreateRoot(root, "RuntimeOnGUI_OperatorAndDebug");
            marker.transform.localPosition = Vector3.zero;
        }

        private static GameObject CreatePath(Transform parent, string name, Vector3[] positions, bool loop = false)
        {
            GameObject path = CreateRoot(parent, name);
            for (int index = 0; index < positions.Length; index++)
            {
                CreatePrimitive(path.transform, $"Waypoint_{index + 1:00}", PrimitiveType.Sphere, positions[index], new Vector3(0.12f, 0.12f, 0.12f), new Color(0.12f, 0.8f, 1f));
            }

            FinalDemoAuthoringPath authoringPath = path.AddComponent<FinalDemoAuthoringPath>();
            SerializedObject serialized = new SerializedObject(authoringPath);
            serialized.FindProperty("loop").boolValue = loop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            authoringPath.CollectChildWaypoints();
            return path;
        }

        private static GameObject CreatePoseMarker(Transform parent, string name, Vector3 cameraSpacePosition, Vector3 yawPitchRoll, Color color)
        {
            GameObject pose = CreateRoot(parent, name);
            pose.transform.localPosition = cameraSpacePosition;
            pose.transform.localRotation = Quaternion.Euler(-yawPitchRoll.y, yawPitchRoll.x, -yawPitchRoll.z);
            FinalDemoPoseAuthoringMarker marker = pose.AddComponent<FinalDemoPoseAuthoringMarker>();
            SerializedObject serialized = new SerializedObject(marker);
            serialized.FindProperty("targetCameraSpacePosition").vector3Value = cameraSpacePosition;
            serialized.FindProperty("targetYawPitchRollDegrees").vector3Value = yawPitchRoll;
            serialized.FindProperty("useTransformLocalPose").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreatePrimitive(pose.transform, "PosePosition", PrimitiveType.Cube, Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), color);
            CreatePrimitive(pose.transform, "PoseForward", PrimitiveType.Cube, new Vector3(0f, 0f, 0.18f), new Vector3(0.055f, 0.055f, 0.36f), color);
            return pose;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            return CreatePrimitive(parent, name, type, position, scale, CreateMaterial(color));
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return primitive;
        }

        private static void CreateRainParticleSystem(Transform parent, string name, Vector3 localPosition, float rate, float speed, Vector3 box)
        {
            GameObject particleObject = CreateRoot(parent, name);
            particleObject.transform.localPosition = localPosition;
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed, speed * 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.82f, 1f, 0.42f));
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(-2.2f, -3.4f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.35f;
            renderer.sharedMaterial = CreateMaterial(new Color(0.60f, 0.80f, 1f));
        }

        private static void CreateRippleParticleSystem(Transform parent, string name, Vector3 localPosition)
        {
            GameObject particleObject = CreateRoot(parent, name);
            particleObject.transform.localPosition = localPosition;
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.85f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.78f, 1f, 0.26f));
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 28f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8.5f, 0.02f, 4.7f);
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.sharedMaterial = CreateMaterial(new Color(0.45f, 0.74f, 1f));
        }

        private static void InstantiateForestModel(Transform parent, string assetPath, string name, Vector3 localPosition, float scale, Color tint)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.SetParent(parent);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = Quaternion.Euler(0f, RandomSeededAngle(name), 0f);
                instance.transform.localScale = Vector3.one * scale;
            }
            else
            {
                instance = CreatePrimitive(parent, name + "_Fallback", PrimitiveType.Capsule, localPosition, Vector3.one * scale, tint);
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Material material = CreateMaterial(tint);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static float RandomSeededAngle(string seed)
        {
            int hash = seed.GetHashCode();
            return Mathf.Abs(hash % 360);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Standard");
            return new Material(shader)
            {
                color = color,
            };
        }

        private static void AssignDirector(FinalDemoDirector director, FinalDemoTuningProfile tuningProfile, FinalDemoCueLibrary cueLibrary, FinalDemoSceneReferences sceneReferences, GameObject playerRig, FinalDemoInputStatus inputStatus, FinalDemoOperatorControls operatorControls, GameObject audioRoot, GameObject lightRoot, GameObject hapticRoot)
        {
            SerializedObject serialized = new SerializedObject(director);
            serialized.FindProperty("tuningProfile").objectReferenceValue = tuningProfile;
            serialized.FindProperty("cueLibrary").objectReferenceValue = cueLibrary;
            serialized.FindProperty("sceneReferences").objectReferenceValue = sceneReferences;
            serialized.FindProperty("playerRig").objectReferenceValue = playerRig.transform;
            serialized.FindProperty("playerCamera").objectReferenceValue = playerRig.GetComponent<Camera>();
            serialized.FindProperty("movementController").objectReferenceValue = playerRig.GetComponent<BellRingerSimpleMoveLookController>();
            serialized.FindProperty("inputStatus").objectReferenceValue = inputStatus;
            serialized.FindProperty("operatorControls").objectReferenceValue = operatorControls;
            serialized.FindProperty("audioRouter").objectReferenceValue = audioRoot.GetComponent<FinalDemoAudioRouter>();
            serialized.FindProperty("lightRouter").objectReferenceValue = lightRoot.GetComponent<FinalDemoLightRouter>();
            serialized.FindProperty("hapticRouter").objectReferenceValue = hapticRoot.GetComponent<FinalDemoHapticRouter>();
            serialized.FindProperty("createMissingRuntimeObjects").boolValue = false;
            serialized.FindProperty("showRuntimeGizmos").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignInputStatus(FinalDemoInputStatus inputStatus, GameObject hardwareBridge, GameObject playerRig, GameObject padInput, GameObject audioRoot, GameObject lightRoot, GameObject hapticRoot)
        {
            SerializedObject serialized = new SerializedObject(inputStatus);
            serialized.FindProperty("hardwareBridge").objectReferenceValue = hardwareBridge.GetComponent<HardwareBridge>();
            serialized.FindProperty("headImuReceiver").objectReferenceValue = playerRig.GetComponent<HeadImuReceiver>();
            serialized.FindProperty("headTiltInputProvider").objectReferenceValue = playerRig.GetComponent<HeadTiltInputProvider>();
            serialized.FindProperty("padTrackingReceiver").objectReferenceValue = padInput.GetComponent<PadTrackingReceiver>();
            serialized.FindProperty("padImuReceiver").objectReferenceValue = padInput.GetComponent<PadImuReceiver>();
            serialized.FindProperty("padPoseProvider").objectReferenceValue = padInput.GetComponent<PadPoseProvider>();
            serialized.FindProperty("audioRouter").objectReferenceValue = audioRoot.GetComponent<FinalDemoAudioRouter>();
            serialized.FindProperty("lightRouter").objectReferenceValue = lightRoot.GetComponent<FinalDemoLightRouter>();
            serialized.FindProperty("hapticRouter").objectReferenceValue = hapticRoot.GetComponent<FinalDemoHapticRouter>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignOperator(FinalDemoOperatorControls operatorControls, FinalDemoDirector director, FinalDemoInputStatus inputStatus)
        {
            SerializedObject serialized = new SerializedObject(operatorControls);
            serialized.FindProperty("director").objectReferenceValue = director;
            serialized.FindProperty("inputStatus").objectReferenceValue = inputStatus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignAuthoringCapture(FinalDemoAuthoringCapture capture, FinalDemoSceneReferences sceneReferences, GameObject padInput, GameObject playerRig)
        {
            SerializedObject serialized = new SerializedObject(capture);
            serialized.FindProperty("sceneReferences").objectReferenceValue = sceneReferences;
            serialized.FindProperty("padPoseProvider").objectReferenceValue = padInput.GetComponent<PadPoseProvider>();
            serialized.FindProperty("playerCamera").objectReferenceValue = playerRig.GetComponent<Camera>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSceneReferences(FinalDemoSceneReferences sceneReferences, GameObject playerRoot, GameObject playerRig, GameObject hardwareRoot, GameObject systemsRoot, GameObject worldRoot, GameObject padRoot, GameObject bellRoot, GameObject rainRoot, GameObject tinnitusRoot, GameObject bossRoot, GameObject forestRoot, GameObject uiRoot, GameObject padInput)
        {
            SerializedObject serialized = new SerializedObject(sceneReferences);
            serialized.FindProperty("playerRoot").objectReferenceValue = playerRoot.transform;
            serialized.FindProperty("playerCamera").objectReferenceValue = playerRig.GetComponent<Camera>();
            serialized.FindProperty("hardwareRoot").objectReferenceValue = hardwareRoot.transform;
            serialized.FindProperty("systemsRoot").objectReferenceValue = systemsRoot.transform;
            serialized.FindProperty("worldRoot").objectReferenceValue = worldRoot.transform;
            serialized.FindProperty("padRoot").objectReferenceValue = padRoot.transform;
            serialized.FindProperty("bellRoot").objectReferenceValue = bellRoot.transform;
            serialized.FindProperty("rainRoot").objectReferenceValue = rainRoot.transform;
            serialized.FindProperty("tinnitusRoot").objectReferenceValue = tinnitusRoot.transform;
            serialized.FindProperty("bossRoot").objectReferenceValue = bossRoot.transform;
            serialized.FindProperty("forestRoot").objectReferenceValue = forestRoot.transform;
            serialized.FindProperty("uiRoot").objectReferenceValue = uiRoot.transform;
            serialized.FindProperty("padVisual").objectReferenceValue = FindChild(padRoot.transform, "PadVisual_Authoring_FollowsPose");
            serialized.FindProperty("bellVisual").objectReferenceValue = FindChild(bellRoot.transform, "FinalDemo_BellPlaceholder");
            serialized.FindProperty("tinnitusOneVisual").objectReferenceValue = FindChild(tinnitusRoot.transform, "FinalDemo_TinnitusA");
            serialized.FindProperty("tinnitusTwoVisual").objectReferenceValue = FindChild(tinnitusRoot.transform, "FinalDemo_TinnitusB");
            serialized.FindProperty("bossVisual").objectReferenceValue = FindChild(bossRoot.transform, "FinalDemo_BossTinnitus");
            serialized.FindProperty("rainVolume").objectReferenceValue = FindChild(rainRoot.transform, "RainVolume_Authoring");
            serialized.FindProperty("forestSet").objectReferenceValue = FindChild(forestRoot.transform, "ForestSet_Authoring");
            serialized.FindProperty("bellOrbitPath").objectReferenceValue = FindChild(bellRoot.transform, "BellOrbitPath_Authoring");
            serialized.FindProperty("bellFollowPath").objectReferenceValue = FindChild(bellRoot.transform, "BellFollowPath_Authoring");
            serialized.FindProperty("bellGazePath").objectReferenceValue = FindChild(bellRoot.transform, "BellGazePath_Authoring");
            serialized.FindProperty("bossWeakpointPathOne").objectReferenceValue = FindChild(bossRoot.transform, "BossWeakpointPath_01_Authoring");
            serialized.FindProperty("bossWeakpointPathTwo").objectReferenceValue = FindChild(bossRoot.transform, "BossWeakpointPath_02_Authoring");
            serialized.FindProperty("bossWeakpointPathThree").objectReferenceValue = FindChild(bossRoot.transform, "BossWeakpointPath_03_Authoring");
            serialized.FindProperty("tinnitusOneHealPose").objectReferenceValue = FindChild(tinnitusRoot.transform, "TinnitusA_HealPose_Authoring");
            serialized.FindProperty("tinnitusTwoHealPose").objectReferenceValue = FindChild(tinnitusRoot.transform, "TinnitusB_HealPose_Authoring");
            serialized.FindProperty("bossPoseOne").objectReferenceValue = FindChild(bossRoot.transform, "BossPose_01_Authoring");
            serialized.FindProperty("bossPoseTwo").objectReferenceValue = FindChild(bossRoot.transform, "BossPose_02_Authoring");
            serialized.FindProperty("bossPoseThree").objectReferenceValue = FindChild(bossRoot.transform, "BossPose_03_Authoring");
            serialized.FindProperty("padPoseProvider").objectReferenceValue = padInput.GetComponent<PadPoseProvider>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignStageVisibility(FinalDemoStageVisibility visibility, FinalDemoDirector director, GameObject padRoot, GameObject bellRoot, GameObject rainRoot, GameObject tinnitusRoot, GameObject bossRoot, GameObject forestRoot, GameObject darkSky, GameObject clearSky)
        {
            SerializedObject serialized = new SerializedObject(visibility);
            serialized.FindProperty("director").objectReferenceValue = director;
            serialized.FindProperty("padRoot").objectReferenceValue = padRoot;
            serialized.FindProperty("bellRoot").objectReferenceValue = bellRoot;
            serialized.FindProperty("rainRoot").objectReferenceValue = rainRoot;
            serialized.FindProperty("tinnitusRoot").objectReferenceValue = tinnitusRoot;
            serialized.FindProperty("bossRoot").objectReferenceValue = bossRoot;
            serialized.FindProperty("forestRoot").objectReferenceValue = forestRoot;
            serialized.FindProperty("darkSkyRoot").objectReferenceValue = darkSky;
            serialized.FindProperty("clearSkyRoot").objectReferenceValue = clearSky;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
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
