using BellRinger.Audio;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using System.IO;
using UnityEngine;

namespace BellRinger.Debug
{
    public sealed class TinnitusTestController : MonoBehaviour
    {
        [SerializeField] private AudioClip continuousGlitchClip;
        [SerializeField] private AudioClip shortGlitchClip;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private bool showRuntimeControls = true;
        [SerializeField] private float previewBrightnessBoost = 5f;

        private Transform _listenerTransform;
        private Transform _tinnitusTransform;
        private TinnitusAudioController _audioController;
        private TinnitusLightPatternController _lightController;
        private bool _cameraInitialized;
        private bool _staticSceneCreated;
        private bool _sharedSettingsInitialized;
        private string _sharedSettingsStatus = "(pending)";

        private void Start()
        {
            DisableHardwareDebugOverlay();
            EnsureScene();
        }

        private void Update()
        {
            EnsureScene();
        }

        private void OnGUI()
        {
            if (!showRuntimeControls || _audioController == null || _lightController == null)
            {
                return;
            }

            DrawControlPanel(new Rect(16f, 16f, 520f, 860f));
            DrawBoardPreview(new Rect(552f, 16f, 330f, 226f));
        }

        private void EnsureScene()
        {
            Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("TinnitusTestCamera");
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.012f, 0.012f, 1f);
                cameraObject.tag = "MainCamera";
            }

            if (!_cameraInitialized)
            {
                camera.transform.position = new Vector3(0f, 1.6f, -5f);
                camera.transform.rotation = Quaternion.identity;
                _cameraInitialized = true;
            }

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            if (camera.GetComponent<BellRingerSimpleMoveLookController>() == null)
            {
                camera.gameObject.AddComponent<BellRingerSimpleMoveLookController>();
            }

            _listenerTransform = camera.transform;

            if (!_staticSceneCreated)
            {
                CreateStaticSceneObjects();
                _staticSceneCreated = true;
            }

            if (_tinnitusTransform == null)
            {
                CreateTinnitusSource();
            }

            _lightController.Configure(_listenerTransform, _tinnitusTransform, _audioController, outputToHardware);
            if (!_sharedSettingsInitialized)
            {
                ApplySavedOrDefaultSettings();
                _sharedSettingsInitialized = true;
            }
        }

        private void CreateTinnitusSource()
        {
            GameObject source = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            source.name = "Tinnitus Test Source";
            source.transform.position = new Vector3(0.8f, 1.45f, 4.2f);
            source.transform.localScale = Vector3.one * 0.35f;
            SetMaterialColor(source, BellRingerLightStyle.TinnitusViolet);

            _tinnitusTransform = source.transform;
            _audioController = source.AddComponent<TinnitusAudioController>();
            _audioController.SetGlitchClips(continuousGlitchClip, shortGlitchClip);
            _lightController = source.AddComponent<TinnitusLightPatternController>();
            _lightController.Configure(_listenerTransform, _tinnitusTransform, _audioController, outputToHardware);
        }

        private void CreateStaticSceneObjects()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TinnitusTestFloor";
            floor.transform.localScale = new Vector3(1.7f, 1f, 1.7f);
            SetMaterialColor(floor, new Color(0.04f, 0.032f, 0.032f, 1f));

            GameObject lightObject = new GameObject("TinnitusTestKeyLight");
            lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        private void DrawControlPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, "Tinnitus Test", GUI.skin.window);
            GUILayout.Label("WASD move, right mouse drag look. Look at the violet source to see/hear tinnitus.");
            GUILayout.Label($"Visible: {_lightController.IsVisible}  LED: {(outputToHardware ? "Hardware" : "Preview only")}  COM: {HardwareStatus()}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Burst", GUILayout.Height(28f)))
            {
                _audioController.TriggerBurst(1f);
            }

            outputToHardware = GUILayout.Toggle(outputToHardware, "Hardware Output", GUILayout.Width(160f));
            _lightController.OutputToHardware = outputToHardware;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Saved", GUILayout.Height(28f)))
            {
                ApplySavedOrDefaultSettings();
            }

            if (GUILayout.Button("Save Current", GUILayout.Height(28f)))
            {
                SaveCurrentSettings();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"Shared settings: {_sharedSettingsStatus}");

            GUILayout.Space(8f);
            GUILayout.Label("Audio");
            _audioController.Volume = Slider("volume", _audioController.Volume, 0f, 0.12f);
            _audioController.BaseFrequency = Slider("baseFrequency", _audioController.BaseFrequency, 3000f, 12000f);
            _audioController.BeatFrequencyOffset = Slider("beatFrequencyOffset", _audioController.BeatFrequencyOffset, 0.5f, 40f);
            _audioController.PitchWobble = Slider("pitchWobble", _audioController.PitchWobble, 0f, 0.04f);
            _audioController.GlitchDensity = Slider("glitchDensity", _audioController.GlitchDensity, 0f, 1.5f);
            _audioController.Roughness = Slider("roughness", _audioController.Roughness, 0f, 1f);
            _audioController.BurstIntensity = Slider("burstIntensity", _audioController.BurstIntensity, 0f, 1f);

            GUILayout.Space(8f);
            GUILayout.Label("Light");
            _lightController.CoreIntensity = Slider("coreIntensity", _lightController.CoreIntensity, 0f, 0.25f);
            _lightController.CoreSize = Slider("coreSize", _lightController.CoreSize, 0.2f, 2.2f);
            _lightController.TearAmount = Slider("tearAmount", _lightController.TearAmount, 0f, 5f);
            _lightController.Jitter = Slider("jitter", _lightController.Jitter, 0f, 1.5f);
            _lightController.SmearDecay = Slider("smearDecay", _lightController.SmearDecay, 0.1f, 4f);
            _lightController.PulseRate = Slider("pulseRate <= 3Hz", _lightController.PulseRate, 0.1f, 3f);
            _lightController.Instability = Slider("instability", _lightController.Instability, 0f, 1f);
            _lightController.AverageLightScale = Slider("averageLight", _lightController.AverageLightScale, 0.2f, 1f);
            _lightController.PeakIntensityScale = Slider("peakIntensity", _lightController.PeakIntensityScale, 1f, 2f);
            _lightController.PeakContrast = Slider("peakContrast", _lightController.PeakContrast, 1f, 3f);
            _lightController.LitPixelScale = Slider("litPixels", _lightController.LitPixelScale, 0.25f, 1f);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"tearAxis: {_lightController.TearAxis.x:0.00}, {_lightController.TearAxis.y:0.00}", GUILayout.Width(190f));
            if (GUILayout.Button("Horizontal"))
            {
                _lightController.TearAxis = Vector2.right;
            }

            if (GUILayout.Button("Diagonal"))
            {
                _lightController.TearAxis = new Vector2(1f, 0.55f);
            }

            if (GUILayout.Button("AntiDiag"))
            {
                _lightController.TearAxis = new Vector2(1f, -0.55f);
            }

            GUILayout.EndHorizontal();

            float stability = Slider("cleanseStability", _audioController.CleanseStability, 0f, 1f);
            _audioController.CleanseStability = stability;
            _lightController.CleanseStability = stability;

            GUILayout.Space(8f);
            GUILayout.Label($"Effective burst: {_audioController.EffectiveBurstIntensity:0.00}  audio instability: {_audioController.EffectiveInstability:0.00}");
            GUILayout.Label($"Light center: {_lightController.LastCorePosition.x:0.00}, {_lightController.LastCorePosition.y:0.00}  tear: {_lightController.LastTearStrength:0.00}  instability: {_lightController.LastEffectiveInstability:0.00}");
            GUILayout.EndArea();
        }

        private void ApplySavedOrDefaultSettings()
        {
            if (_audioController == null || _lightController == null)
            {
                return;
            }

            TinnitusSharedSettingsData settings = TinnitusSharedSettingsStore.Load();
            _audioController.ApplySharedSettings(settings);
            _lightController.ApplySharedSettings(settings);
            _sharedSettingsStatus = TinnitusSharedSettingsStore.HasSavedSettings
                ? $"loaded {Path.GetFileName(TinnitusSharedSettingsStore.SettingsPath)}"
                : "applied built-in defaults";
        }

        private void SaveCurrentSettings()
        {
            if (_audioController == null || _lightController == null)
            {
                return;
            }

            TinnitusSharedSettingsData settings = new TinnitusSharedSettingsData();
            _audioController.FillSharedSettings(settings);
            _lightController.FillSharedSettings(settings);
            TinnitusSharedSettingsStore.Save(settings);
            _sharedSettingsStatus = $"saved {Path.GetFileName(TinnitusSharedSettingsStore.SettingsPath)}";
        }

        private void DrawBoardPreview(Rect rect)
        {
            GUILayout.BeginArea(rect, "16x8 LED Board Preview", GUI.skin.window);
            GUI.Label(new Rect(16f, 22f, 290f, 20f), $"Preview brightness x{Mathf.Max(1f, previewBrightnessBoost):0.0}");

            Rect board = new Rect(16f, 48f, 296f, 148f);
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(board, Texture2D.whiteTexture);

            float cellWidth = board.width / BellRingerAudioLedMapper.DisplayWidth;
            float cellHeight = board.height / BellRingerAudioLedMapper.DisplayHeight;
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    Color pixel = _lightController.GetPreviewPixel(x, y) * Mathf.Max(1f, previewBrightnessBoost);
                    GUI.color = new Color(Mathf.Clamp01(pixel.r), Mathf.Clamp01(pixel.g), Mathf.Clamp01(pixel.b), 1f);
                    GUI.DrawTexture(new Rect(board.x + x * cellWidth + 1f, board.y + (7 - y) * cellHeight + 1f, cellWidth - 2f, cellHeight - 2f), Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(16f, 198f, 290f, 20f), "Bottom row here matches the board bottom row.");
            GUILayout.EndArea();
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.000}", GUILayout.Width(190f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(280f));
            GUILayout.EndHorizontal();
            return nextValue;
        }

        private static void DisableHardwareDebugOverlay()
        {
            BellRingerDebugOverlay[] overlays = FindObjectsByType<BellRingerDebugOverlay>(FindObjectsSortMode.None);
            for (int i = 0; i < overlays.Length; i++)
            {
                overlays[i].enabled = false;
            }
        }

        private static string HardwareStatus()
        {
            if (HardwareBridge.Instance == null)
            {
                return "(no bridge)";
            }

            HardwareStatusSnapshot snapshot = HardwareBridge.Instance.GetStatusSnapshot();
            return snapshot.isConnected ? $"Serial {snapshot.portName}" : snapshot.isSimulation ? "Simulation" : "Disconnected";
        }

        private static void SetMaterialColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
            }
        }
    }
}
