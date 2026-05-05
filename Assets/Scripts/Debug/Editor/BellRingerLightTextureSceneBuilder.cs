using BellRinger.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class BellRingerLightTextureSceneBuilder
    {
        private const string PresetFolderPath = "Assets/Settings/LightTextures";
        private const string ScenePath = "Assets/Scenes/LightTextureTest.unity";
        private const string BellClipPath = "Assets/Audios/freesound_community-bicycle-bell-66855.mp3";
        private const string RainClipPath = "Assets/Audios/rain/boons_freak-rain-sound-188158.mp3";

        [MenuItem("Bell Ringer/Create Light Texture Test Scene")]
        public static void CreateLightTextureTestScene()
        {
            EnsurePresetFolder();

            BellRingerLightTexturePreset[] presets =
            {
                CreatePreset("GreenBellRipple", "Green Bell Ripple", BellRingerLightStyle.BellGreen, 0.096f, 0.05f, 0.2f, 5.0f, 1.35f),
                CreatePreset("SoftWideRipple", "Soft Wide Ripple", BellRingerLightStyle.BellGreen, 0.072f, 0.12f, 0.32f, 3.5f, 2.2f),
                CreatePreset("ThinFastRipple", "Thin Fast Ripple", BellRingerLightStyle.BellGreen, 0.108f, 0.03f, 0.12f, 7.25f, 0.95f),
            };

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("BellRingerLightTextureTest");

            Camera camera = CreateCamera();
            CreatePlayerMarker();
            GameObject bell = CreateBell();
            CreateFloor();
            CreateLight();

            BellRingerLightTexturePlayer texturePlayer = root.AddComponent<BellRingerLightTexturePlayer>();
            ConfigureTexturePlayer(texturePlayer, camera.transform, presets);

            BellRingerLightTextureTestDriver testDriver = root.AddComponent<BellRingerLightTextureTestDriver>();
            ConfigureTestDriver(testDriver, texturePlayer, camera.transform, bell.transform, bell.GetComponent<AudioSource>());
            ConfigureSpatialSampleController(root.AddComponent<BellRingerSpatialLightTextureSampleController>(), presets);

            Selection.activeObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[BellRingerLightTextureSceneBuilder] Created {ScenePath} with {presets.Length} presets.");
        }

        private static void EnsurePresetFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(PresetFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "LightTextures");
            }
        }

        private static BellRingerLightTexturePreset CreatePreset(
            string fileName,
            string displayName,
            Color color,
            float maximumBrightness,
            float fadeInSeconds,
            float fadeOutSeconds,
            float rippleSpeed,
            float rippleWidth)
        {
            string path = $"{PresetFolderPath}/{fileName}.asset";
            BellRingerLightTexturePreset preset = AssetDatabase.LoadAssetAtPath<BellRingerLightTexturePreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<BellRingerLightTexturePreset>();
                AssetDatabase.CreateAsset(preset, path);
            }

            SerializedObject serializedObject = new SerializedObject(preset);
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("color").colorValue = color;
            serializedObject.FindProperty("maximumBrightness").floatValue = maximumBrightness;
            serializedObject.FindProperty("fadeInSeconds").floatValue = fadeInSeconds;
            serializedObject.FindProperty("fadeOutSeconds").floatValue = fadeOutSeconds;
            serializedObject.FindProperty("rippleSpeedPixelsPerSecond").floatValue = rippleSpeed;
            serializedObject.FindProperty("rippleWidthPixels").floatValue = rippleWidth;
            serializedObject.FindProperty("startRadiusPixels").floatValue = 0f;
            serializedObject.FindProperty("maxRadiusPixels").floatValue = 11f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("LightTextureTestCamera");
            cameraObject.transform.position = new Vector3(0f, 1.6f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.02f, 1f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static GameObject CreatePlayerMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = "PlayerMarker";
            marker.transform.position = new Vector3(0f, 0.85f, 0f);
            marker.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            marker.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.18f, 0.22f, 0.18f, 1f));
            return marker;
        }

        private static GameObject CreateBell()
        {
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "OrbitingBell";
            bell.transform.position = new Vector3(0f, 1.35f, 4f);
            bell.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            bell.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.1f, 0.9f, 0.28f, 1f));

            AudioSource source = bell.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BellClipPath);
            source.volume = 0.85f;
            source.spatialBlend = 1f;
            source.minDistance = 0.5f;
            source.maxDistance = 8f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return bell;
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "LightTextureTestFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            floor.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(new Color(0.045f, 0.055f, 0.045f, 1f));
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("LightTextureTestKeyLight");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }

        private static void ConfigureTexturePlayer(BellRingerLightTexturePlayer texturePlayer, Transform listenerTransform, BellRingerLightTexturePreset[] presets)
        {
            SerializedObject serializedObject = new SerializedObject(texturePlayer);
            serializedObject.FindProperty("listenerTransform").objectReferenceValue = listenerTransform;
            SerializedProperty presetsProperty = serializedObject.FindProperty("presets");
            presetsProperty.arraySize = presets.Length;

            for (int i = 0; i < presets.Length; i++)
            {
                presetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];
            }

            serializedObject.FindProperty("selectedPresetIndex").intValue = 0;
            serializedObject.FindProperty("outputToHardware").boolValue = true;
            serializedObject.FindProperty("showRuntimeControls").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTestDriver(
            BellRingerLightTextureTestDriver testDriver,
            BellRingerLightTexturePlayer texturePlayer,
            Transform listenerTransform,
            Transform bellTransform,
            AudioSource bellAudioSource)
        {
            SerializedObject serializedObject = new SerializedObject(testDriver);
            serializedObject.FindProperty("texturePlayer").objectReferenceValue = texturePlayer;
            serializedObject.FindProperty("listenerTransform").objectReferenceValue = listenerTransform;
            serializedObject.FindProperty("bellTransform").objectReferenceValue = bellTransform;
            serializedObject.FindProperty("bellClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BellClipPath);
            serializedObject.FindProperty("bellAudioSource").objectReferenceValue = bellAudioSource;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSpatialSampleController(BellRingerSpatialLightTextureSampleController controller, BellRingerLightTexturePreset[] presets)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty presetsProperty = serializedObject.FindProperty("presets");
            presetsProperty.arraySize = presets.Length;

            for (int i = 0; i < presets.Length; i++)
            {
                presetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];
            }

            serializedObject.FindProperty("bellClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BellClipPath);
            serializedObject.FindProperty("rainClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RainClipPath);
            serializedObject.FindProperty("showBoardPreview").boolValue = true;
            serializedObject.FindProperty("bellColor").colorValue = BellRingerLightStyle.BellGreen;
            serializedObject.FindProperty("wallNoiseColor").colorValue = BellRingerLightStyle.WallCyan;
            serializedObject.FindProperty("wallNoiseBrightness").floatValue = 0.075f;
            serializedObject.FindProperty("rainColor").colorValue = BellRingerLightStyle.RainDeepBlue;
            serializedObject.FindProperty("rainBrightness").floatValue = 0.07f;
            serializedObject.FindProperty("rainNeutralRows").floatValue = 2f;
            serializedObject.FindProperty("rainLookDownRows").floatValue = 5f;
            serializedObject.FindProperty("averageLightScale").floatValue = 0.58f;
            serializedObject.FindProperty("peakContrast").floatValue = 1.85f;
            serializedObject.FindProperty("litPixelScale").floatValue = 0.55f;
            serializedObject.FindProperty("peakIntensityScale").floatValue = 1.25f;
            serializedObject.FindProperty("bellMaxRadiusPixels").floatValue = 3.4f;
            serializedObject.FindProperty("bellCoreSizePixels").floatValue = 0.58f;
            serializedObject.FindProperty("previewBrightnessBoost").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
