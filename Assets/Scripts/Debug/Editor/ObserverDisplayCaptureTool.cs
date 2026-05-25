using System.Collections.Generic;
using System.IO;
using BellRinger.FinalDemo;
using BellRinger.ObserverDisplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug.Editor
{
    public static class ObserverDisplayCaptureTool
    {
        private const int CaptureWidth = 1440;
        private const int CaptureHeight = 760;

        [MenuItem("Bell Ringer/Capture Observer Visual Checks")]
        public static void CaptureAll()
        {
            string outputDirectory = System.Environment.GetEnvironmentVariable("BELL_RINGER_OBSERVER_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "ObserverVisualCaptures");
            }

            Directory.CreateDirectory(outputDirectory);

            CaptureStage(outputDirectory, "01_pad_bell", FinalDemoStage.BellGaze);
            CaptureStage(outputDirectory, "02_rain", FinalDemoStage.BellFollowRain);
            CaptureStage(outputDirectory, "03_tinnitus", FinalDemoStage.GeneralTinnitusOne);
            CaptureStage(outputDirectory, "04_boss", FinalDemoStage.BossPatternOne);
            CaptureStage(outputDirectory, "05_forest", FinalDemoStage.ForestEnding);

            UnityEngine.Debug.Log($"Observer visual captures written to {outputDirectory}");
        }

        private static void CaptureStage(string outputDirectory, string fileStem, FinalDemoStage stage)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.54f);

            GameObject root = new GameObject("ObserverVisualCaptureRoot");
            CreateEnvironment(root.transform, stage);

            Camera camera = CreateCamera();
            CreateLight(root.transform);

            ObserverDisplaySnapshot snapshot = BuildSnapshot(stage);
            List<FinalDemoAudioCueSnapshot> cues = BuildCueSnapshots(stage, snapshot);

            ObserverPadView padView = root.AddComponent<ObserverPadView>();
            ObserverBellView bellView = root.AddComponent<ObserverBellView>();
            ObserverRainView rainView = root.AddComponent<ObserverRainView>();
            ObserverTinnitusView tinnitusView = root.AddComponent<ObserverTinnitusView>();
            ObserverBossTinnitusView bossView = root.AddComponent<ObserverBossTinnitusView>();
            ObserverForestView forestView = root.AddComponent<ObserverForestView>();

            padView.ApplySnapshot(snapshot, null, snapshot.hasBell, snapshot.bellWorldPosition);
            bellView.ApplySnapshot(snapshot, null, cues, 0.82f);
            rainView.ApplySnapshot(snapshot, null);
            tinnitusView.ApplySnapshot(snapshot, null);
            bossView.ApplySnapshot(snapshot, null);
            forestView.ApplySnapshot(snapshot, null);

            SimulateParticles();

            string outputPath = Path.Combine(outputDirectory, $"{fileStem}.png");
            RenderCameraToPng(camera, outputPath);
            UnityEngine.Debug.Log($"Captured {stage}: {outputPath}");

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(camera.gameObject);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static ObserverDisplaySnapshot BuildSnapshot(FinalDemoStage stage)
        {
            ObserverDisplaySnapshot snapshot = new ObserverDisplaySnapshot
            {
                stage = stage,
                objectiveLabel = stage.ToString(),
                objectiveProgress01 = stage == FinalDemoStage.ForestEnding ? 1f : 0.45f,
                activeSoundFocusLabel = ResolveFocusLabel(stage),
                rainIntensity01 = stage == FinalDemoStage.BellFollowRain ? 1f : 0f,
                bellGazeProgress01 = stage == FinalDemoStage.BellGaze ? 0.72f : 0f,
                tinnitusProgress01 = stage == FinalDemoStage.GeneralTinnitusOne ? 0.38f : 0f,
                tinnitusMatch01 = stage == FinalDemoStage.GeneralTinnitusOne ? 0.82f : 0f,
                bossPatternProgress01 = stage == FinalDemoStage.BossPatternOne ? 0.52f : 0f,
                bossPatternMatch01 = stage == FinalDemoStage.BossPatternOne ? 0.72f : 0f,
                playerWorldPosition = Vector3.zero,
                hasPadPose = true,
                padWorldPosition = new Vector3(0f, 0.26f, 0.55f),
                padWorldRotation = Quaternion.Euler(18f, 0f, -5f),
                hasBell = stage == FinalDemoStage.BellGaze || stage == FinalDemoStage.BellFollowRain || stage == FinalDemoStage.ForestEnding,
                bellWorldPosition = stage == FinalDemoStage.ForestEnding ? new Vector3(0f, 1.35f, 4.1f) : new Vector3(0f, 1.55f, 2.45f),
                hasTinnitus = stage == FinalDemoStage.GeneralTinnitusOne,
                tinnitusWorldPosition = new Vector3(-0.65f, 1.35f, 2.75f),
                hasBoss = stage == FinalDemoStage.BossPatternOne,
                bossWorldPosition = new Vector3(0f, 1.5f, 3.2f),
                hasForestBell = stage == FinalDemoStage.ForestEnding,
                forestBellWorldPosition = new Vector3(0f, 1.35f, 4.1f),
            };

            return snapshot;
        }

        private static string ResolveFocusLabel(FinalDemoStage stage)
        {
            return stage switch
            {
                FinalDemoStage.BellFollowRain => "RAIN / WIND",
                FinalDemoStage.GeneralTinnitusOne => "TINNITUS",
                FinalDemoStage.BossPatternOne => "BOSS TINNITUS",
                FinalDemoStage.ForestEnding => "FOREST BELL",
                _ => "LIVING BELL",
            };
        }

        private static List<FinalDemoAudioCueSnapshot> BuildCueSnapshots(FinalDemoStage stage, ObserverDisplaySnapshot snapshot)
        {
            List<FinalDemoAudioCueSnapshot> cues = new List<FinalDemoAudioCueSnapshot>();
            if (stage == FinalDemoStage.BellGaze || stage == FinalDemoStage.BellFollowRain || stage == FinalDemoStage.ForestEnding)
            {
                cues.Add(new FinalDemoAudioCueSnapshot(FinalDemoCueId.BellDistantCall, FinalDemoAudioBus.Bell, snapshot.bellWorldPosition, 1f, false, 0.02f));
            }

            return cues;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("ObserverCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.020f, 0.024f);
            camera.fieldOfView = 47f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(0f, 3.6f, -5.4f);
            camera.transform.LookAt(new Vector3(0f, 0.85f, 2.65f));
            return camera;
        }

        private static void CreateLight(Transform parent)
        {
            GameObject lightObject = new GameObject("ObserverCaptureKeyLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.96f, 0.98f, 1f);
            light.intensity = 1.65f;
            light.shadows = LightShadows.None;
        }

        private static void CreateEnvironment(Transform parent, FinalDemoStage stage)
        {
            Color floorColor = stage == FinalDemoStage.ForestEnding
                ? new Color(0.18f, 0.22f, 0.18f)
                : stage == FinalDemoStage.BellFollowRain
                    ? new Color(0.14f, 0.16f, 0.18f)
                    : new Color(0.30f, 0.31f, 0.33f);
            Color wallColor = stage == FinalDemoStage.ForestEnding
                ? new Color(0.24f, 0.30f, 0.25f)
                : new Color(0.88f, 0.90f, 0.92f);

            CreatePart(parent, "CaptureFloor", PrimitiveType.Cube, new Vector3(0f, -0.03f, 3.0f), new Vector3(18f, 0.04f, 16f), floorColor);
            CreatePart(parent, "CaptureWall", PrimitiveType.Cube, new Vector3(0f, 2.1f, 8f), new Vector3(18f, 4.4f, 0.08f), wallColor);
        }

        private static void CreatePart(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = CreateMaterial(color);
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

        private static void SimulateParticles()
        {
            ParticleSystem[] systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            for (int index = 0; index < systems.Length; index++)
            {
                systems[index].Simulate(1.5f, true, true, true);
            }
        }

        private static void RenderCameraToPng(Camera camera, string outputPath)
        {
            RenderTexture renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(renderTexture);
        }
    }
}
