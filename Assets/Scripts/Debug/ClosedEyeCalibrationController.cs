using System;
using System.Globalization;
using System.IO;
using BellRinger.Audio;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Debug
{
    public enum ClosedEyeCalibrationEffect
    {
        Field,
        SingleCore,
        PairedCores,
        Ripple,
        LowerBand,
        WallNoise,
        TearCore,
        Sweep,
    }

    public sealed class ClosedEyeCalibrationController : MonoBehaviour
    {
        private const int Width = BellRingerAudioLedMapper.DisplayWidth;
        private const int Height = BellRingerAudioLedMapper.DisplayHeight;

        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private bool showRuntimeControls = true;
        [SerializeField] private float serialRefreshRate = 20f;
        [SerializeField] [Range(0f, 0.35f)] private float brightness = 0.08f;
        [SerializeField] [Range(0.2f, 3f)] private float coreSize = 0.75f;
        [SerializeField] [Range(0.1f, 6f)] private float effectSpeed = 1.15f;
        [SerializeField] [Range(0.2f, 1f)] private float averageLightScale = 0.58f;
        [SerializeField] [Range(1f, 3f)] private float peakContrast = 1.85f;
        [SerializeField] [Range(0.25f, 1f)] private float litPixelScale = 0.55f;
        [SerializeField] [Range(1f, 2f)] private float peakIntensityScale = 1.25f;
        [SerializeField] [Range(1f, 10f)] private float previewBrightnessBoost = 5f;
        [SerializeField] private int selectedColorIndex;
        [SerializeField] private ClosedEyeCalibrationEffect selectedEffect = ClosedEyeCalibrationEffect.SingleCore;
        [SerializeField] private bool advanceAfterRating = true;

        private readonly Color[,] _previewPixels = new Color[Width, Height];
        private readonly string[] _colorNames =
        {
            "Red",
            "Deep Red",
            "Rose",
            "Orange",
            "Amber",
            "Yellow",
            "Warm White",
            "Green",
            "Lime",
            "Blue Green",
            "Teal",
            "Cyan",
            "Deep Blue",
            "Violet",
            "Magenta",
            "Cool White",
        };

        private readonly Color[] _colors =
        {
            new Color(1f, 0.02f, 0.01f, 1f),
            new Color(0.55f, 0f, 0.01f, 1f),
            new Color(1f, 0.12f, 0.28f, 1f),
            BellRingerLightStyle.PadOrange,
            new Color(1f, 0.55f, 0.02f, 1f),
            new Color(1f, 0.95f, 0.05f, 1f),
            new Color(1f, 0.78f, 0.45f, 1f),
            BellRingerLightStyle.BellGreen,
            new Color(0.45f, 1f, 0.02f, 1f),
            new Color(0.02f, 0.75f, 0.42f, 1f),
            new Color(0.02f, 0.55f, 0.75f, 1f),
            BellRingerLightStyle.WallCyan,
            BellRingerLightStyle.RainDeepBlue,
            BellRingerLightStyle.TinnitusViolet,
            new Color(0.9f, 0.02f, 1f, 1f),
            new Color(0.62f, 0.82f, 1f, 1f),
        };

        private Camera _camera;
        private float _nextSendTime;
        private float _effectStartTime;
        private int _trialIndex = 1;
        private string _lastRecordedRating = "(none)";
        private string _logPath;

        private Color ActiveColor => _colors[Mathf.Clamp(selectedColorIndex, 0, _colors.Length - 1)];
        private string ActiveColorName => _colorNames[Mathf.Clamp(selectedColorIndex, 0, _colorNames.Length - 1)];

        private void Start()
        {
            DisableHardwareDebugOverlay();
            EnsureScene();
            EnsureLogPath();
            _effectStartTime = Time.unscaledTime;
        }

        private void Update()
        {
            EnsureScene();
            BuildPreview(Time.unscaledTime - _effectStartTime);

            if (Time.unscaledTime < _nextSendTime)
            {
                return;
            }

            float interval = serialRefreshRate <= 0f ? 0.05f : 1f / serialRefreshRate;
            _nextSendTime = Time.unscaledTime + interval;
            SendHardwareFrame(Time.unscaledTime - _effectStartTime);
        }

        private void OnDisable()
        {
            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.ClearLedDisplay();
            }
        }

        private void OnValidate()
        {
            serialRefreshRate = Mathf.Max(1f, serialRefreshRate);
            brightness = Mathf.Clamp(brightness, 0f, 0.35f);
            coreSize = Mathf.Clamp(coreSize, 0.2f, 3f);
            effectSpeed = Mathf.Clamp(effectSpeed, 0.1f, 6f);
            averageLightScale = Mathf.Clamp(averageLightScale, 0.2f, 1f);
            peakContrast = Mathf.Clamp(peakContrast, 1f, 3f);
            litPixelScale = Mathf.Clamp(litPixelScale, 0.25f, 1f);
            peakIntensityScale = Mathf.Clamp(peakIntensityScale, 1f, 2f);
            previewBrightnessBoost = Mathf.Clamp(previewBrightnessBoost, 1f, 10f);
            selectedColorIndex = Mathf.Clamp(selectedColorIndex, 0, Mathf.Max(0, _colors.Length - 1));
        }

        private void OnGUI()
        {
            if (!showRuntimeControls)
            {
                return;
            }

            DrawControlPanel(new Rect(16f, 16f, 520f, 760f));
            DrawBoardPreview(new Rect(552f, 16f, 330f, 226f));
        }

        private void EnsureScene()
        {
            if (_camera == null)
            {
                _camera = Camera.main ?? FindFirstObjectByType<Camera>();
            }

            if (_camera == null)
            {
                GameObject cameraObject = new GameObject("ClosedEyeCalibrationCamera");
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.015f, 0.015f, 0.018f, 1f);
            _camera.transform.position = new Vector3(0f, 1.6f, -5f);
            _camera.transform.rotation = Quaternion.identity;

            if (_camera.GetComponent<AudioListener>() == null)
            {
                _camera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void DrawControlPanel(Rect rect)
        {
            GUILayout.BeginArea(rect, "Closed-Eye Light Calibration", GUI.skin.window);
            GUILayout.Label("Pick a color/effect, close eyes, then rate what you perceived.");
            GUILayout.Label($"Trial: {_trialIndex}  Color: {ActiveColorName}  Effect: {selectedEffect}");
            GUILayout.Label($"Hardware: {HardwareStatus()}  Last rating: {_lastRecordedRating}");
            GUILayout.Label($"Log: {(_logPath == null ? "(pending)" : _logPath)}");

            GUILayout.Space(8f);
            GUILayout.Label("Color");
            int colorColumns = 4;
            int colorRows = Mathf.CeilToInt(_colorNames.Length / (float)colorColumns);
            for (int row = 0; row < colorRows; row++)
            {
                GUILayout.BeginHorizontal();
                int start = row * colorColumns;
                for (int i = start; i < Mathf.Min(start + colorColumns, _colorNames.Length); i++)
                {
                    if (GUILayout.Button((i == selectedColorIndex ? "> " : "") + _colorNames[i], GUILayout.Height(28f)))
                    {
                        selectedColorIndex = i;
                        RestartEffect();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(ColorDescription(selectedColorIndex));

            GUILayout.Space(8f);
            GUILayout.Label("Effect");
            ClosedEyeCalibrationEffect[] effects = (ClosedEyeCalibrationEffect[])Enum.GetValues(typeof(ClosedEyeCalibrationEffect));
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                int start = row * 4;
                for (int i = start; i < Mathf.Min(start + 4, effects.Length); i++)
                {
                    if (GUILayout.Button((effects[i] == selectedEffect ? "> " : "") + effects[i], GUILayout.Height(28f)))
                    {
                        selectedEffect = effects[i];
                        RestartEffect();
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            brightness = Slider("brightness", brightness, 0f, 0.22f);
            coreSize = Slider("coreSize", coreSize, 0.2f, 3f);
            effectSpeed = Slider("effectSpeed", effectSpeed, 0.1f, 4f);
            averageLightScale = Slider("averageLight", averageLightScale, 0.2f, 1f);
            peakIntensityScale = Slider("peakIntensity", peakIntensityScale, 1f, 2f);
            peakContrast = Slider("peakContrast", peakContrast, 1f, 3f);
            litPixelScale = Slider("litPixels", litPixelScale, 0.25f, 1f);
            previewBrightnessBoost = Slider("previewBoost", previewBrightnessBoost, 1f, 10f);

            GUILayout.BeginHorizontal();
            outputToHardware = GUILayout.Toggle(outputToHardware, "Hardware Output", GUILayout.Width(180f));
            advanceAfterRating = GUILayout.Toggle(advanceAfterRating, "Advance After Rating", GUILayout.Width(220f));
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Rate this trial");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Good", GUILayout.Height(34f)))
            {
                RecordRating("Good");
            }
            if (GUILayout.Button("Distinct", GUILayout.Height(34f)))
            {
                RecordRating("Distinct");
            }
            if (GUILayout.Button("Ambiguous", GUILayout.Height(34f)))
            {
                RecordRating("Ambiguous");
            }
            if (GUILayout.Button("Painful", GUILayout.Height(34f)))
            {
                RecordRating("Painful");
                brightness = Mathf.Max(0.01f, brightness * 0.75f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", GUILayout.Height(30f)))
            {
                PreviousTrial();
            }
            if (GUILayout.Button("Restart", GUILayout.Height(30f)))
            {
                RestartEffect();
            }
            if (GUILayout.Button("Next", GUILayout.Height(30f)))
            {
                NextTrial();
            }
            if (GUILayout.Button("Clear LEDs", GUILayout.Height(30f)))
            {
                if (HardwareBridge.Instance != null)
                {
                    HardwareBridge.Instance.ClearLedDisplay();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Effect notes");
            GUILayout.Label(EffectDescription(selectedEffect));
            GUILayout.EndArea();
        }

        private void DrawBoardPreview(Rect rect)
        {
            GUILayout.BeginArea(rect, "16x8 Board Preview", GUI.skin.window);
            GUI.Label(new Rect(16f, 22f, 290f, 20f), $"Actual pattern, UI brightness x{previewBrightnessBoost:0.0}");

            Rect board = new Rect(16f, 48f, 296f, 148f);
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(board, Texture2D.whiteTexture);

            float cellWidth = board.width / Width;
            float cellHeight = board.height / Height;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color pixel = _previewPixels[x, y] * previewBrightnessBoost;
                    GUI.color = new Color(Mathf.Clamp01(pixel.r), Mathf.Clamp01(pixel.g), Mathf.Clamp01(pixel.b), 1f);
                    GUI.DrawTexture(new Rect(board.x + x * cellWidth + 1f, board.y + (7 - y) * cellHeight + 1f, cellWidth - 2f, cellHeight - 2f), Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(16f, 198f, 290f, 20f), "Bottom row here matches the board bottom row.");
            GUILayout.EndArea();
        }

        private void SendHardwareFrame(float elapsed)
        {
            if (!outputToHardware || HardwareBridge.Instance == null)
            {
                return;
            }

            Color color = ActiveColor;
            float level = EffectiveLevel(brightness);
            float effectiveCoreSize = EffectiveCoreSize();
            float density = BellRingerLightStyle.DensityFromLitScale(litPixelScale);
            int seed = Mathf.FloorToInt(elapsed * effectSpeed * 12f);

            switch (selectedEffect)
            {
                case ClosedEyeCalibrationEffect.Field:
                    HardwareBridge.Instance.SendLedField(color, level);
                    break;
                case ClosedEyeCalibrationEffect.PairedCores:
                    HardwareBridge.Instance.SendLedTinnitus(7.5f, 3.5f, effectiveCoreSize, 2.15f * litPixelScale, 1f, 0f, color, level, seed, 1f, 0.2f, peakContrast);
                    break;
                case ClosedEyeCalibrationEffect.Ripple:
                    HardwareBridge.Instance.SendLedPulseCore(7.5f, 3.5f, Mathf.Repeat(elapsed * effectSpeed * 2.2f, 3.6f), effectiveCoreSize, Mathf.Max(0.45f, coreSize * litPixelScale), color, level, peakContrast);
                    break;
                case ClosedEyeCalibrationEffect.LowerBand:
                    HardwareBridge.Instance.SendLedRain(color, level, seed, 7.5f, 0.55f, 16f, Mathf.Lerp(1.1f, 2.1f, litPixelScale), elapsed * effectSpeed, density, peakContrast);
                    break;
                case ClosedEyeCalibrationEffect.WallNoise:
                    HardwareBridge.Instance.SendLedWallNoise(7.5f, 3.5f, 16f, 8f, color, level, seed, density, peakContrast);
                    break;
                case ClosedEyeCalibrationEffect.TearCore:
                    HardwareBridge.Instance.SendLedTinnitus(7.5f, 3.5f, effectiveCoreSize, Mathf.Lerp(0.4f, 3.2f, SafePulse(elapsed)) * litPixelScale, 1f, 0.45f, color, level, seed, 0.85f, 1.35f, peakContrast);
                    break;
                case ClosedEyeCalibrationEffect.Sweep:
                    HardwareBridge.Instance.SendLedPulseCore(SweepX(elapsed), 3.5f, 0f, effectiveCoreSize, Mathf.Max(0.45f, coreSize * litPixelScale), color, level, peakContrast);
                    break;
                default:
                    HardwareBridge.Instance.SendLedPulseCore(7.5f, 3.5f, 0f, effectiveCoreSize, Mathf.Max(0.45f, coreSize * litPixelScale), color, level, peakContrast);
                    break;
            }
        }

        private void BuildPreview(float elapsed)
        {
            ClearPreview();
            Color color = ActiveColor;
            float level = EffectiveLevel(brightness);
            float effectiveCoreSize = EffectiveCoreSize();
            float density = BellRingerLightStyle.DensityFromLitScale(litPixelScale);
            int seed = Mathf.FloorToInt(elapsed * effectSpeed * 12f);

            switch (selectedEffect)
            {
                case ClosedEyeCalibrationEffect.Field:
                    DrawField(color, level);
                    break;
                case ClosedEyeCalibrationEffect.PairedCores:
                    DrawTear(color, new Vector2(7.5f, 3.5f), effectiveCoreSize, 2.15f * litPixelScale, Vector2.right, level, 1f);
                    break;
                case ClosedEyeCalibrationEffect.Ripple:
                    DrawRipple(color, new Vector2(7.5f, 3.5f), Mathf.Repeat(elapsed * effectSpeed * 2.2f, 3.6f), Mathf.Max(0.45f, coreSize * litPixelScale), level);
                    DrawCore(color, new Vector2(7.5f, 3.5f), effectiveCoreSize, level);
                    break;
                case ClosedEyeCalibrationEffect.LowerBand:
                    DrawLowerBand(color, level, seed, elapsed * effectSpeed, density);
                    break;
                case ClosedEyeCalibrationEffect.WallNoise:
                    DrawNoiseField(color, level, seed, density);
                    break;
                case ClosedEyeCalibrationEffect.TearCore:
                    DrawTear(color, new Vector2(7.5f, 3.5f), effectiveCoreSize, Mathf.Lerp(0.4f, 3.2f, SafePulse(elapsed)) * litPixelScale, new Vector2(1f, 0.45f).normalized, level, 0.85f);
                    break;
                case ClosedEyeCalibrationEffect.Sweep:
                    DrawCore(color, new Vector2(SweepX(elapsed), 3.5f), effectiveCoreSize, level);
                    break;
                default:
                    DrawCore(color, new Vector2(7.5f, 3.5f), effectiveCoreSize, level);
                    break;
            }
        }

        private void RecordRating(string rating)
        {
            EnsureLogPath();
            bool writeHeader = !File.Exists(_logPath);
            using (StreamWriter writer = new StreamWriter(_logPath, true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("timestamp\ttrial\tcolor\tr\tg\tb\teffect\tbrightness\tcoreSize\teffectSpeed\taverageLight\tpeakIntensity\tpeakContrast\tlitPixels\trating");
                }

                Color color = ActiveColor;
                writer.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}\t{1}\t{2}\t{3:0.000}\t{4:0.000}\t{5:0.000}\t{6}\t{7:0.000}\t{8:0.000}\t{9:0.000}\t{10:0.000}\t{11:0.000}\t{12:0.000}\t{13:0.000}\t{14}",
                    DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                    _trialIndex,
                    ActiveColorName,
                    color.r,
                    color.g,
                    color.b,
                    selectedEffect,
                    brightness,
                    coreSize,
                    effectSpeed,
                    averageLightScale,
                    peakIntensityScale,
                    peakContrast,
                    litPixelScale,
                    rating));
            }

            _lastRecordedRating = $"{rating} / {ActiveColorName} / {selectedEffect}";
            if (advanceAfterRating)
            {
                NextTrial();
            }
        }

        private void NextTrial()
        {
            ClosedEyeCalibrationEffect[] effects = (ClosedEyeCalibrationEffect[])Enum.GetValues(typeof(ClosedEyeCalibrationEffect));
            int nextEffect = Array.IndexOf(effects, selectedEffect) + 1;
            if (nextEffect >= effects.Length)
            {
                nextEffect = 0;
                selectedColorIndex = (selectedColorIndex + 1) % _colors.Length;
            }

            selectedEffect = effects[nextEffect];
            _trialIndex++;
            RestartEffect();
        }

        private void PreviousTrial()
        {
            ClosedEyeCalibrationEffect[] effects = (ClosedEyeCalibrationEffect[])Enum.GetValues(typeof(ClosedEyeCalibrationEffect));
            int nextEffect = Array.IndexOf(effects, selectedEffect) - 1;
            if (nextEffect < 0)
            {
                nextEffect = effects.Length - 1;
                selectedColorIndex = (selectedColorIndex + _colors.Length - 1) % _colors.Length;
            }

            selectedEffect = effects[nextEffect];
            _trialIndex = Mathf.Max(1, _trialIndex - 1);
            RestartEffect();
        }

        private void RestartEffect()
        {
            _effectStartTime = Time.unscaledTime;
        }

        private void EnsureLogPath()
        {
            if (!string.IsNullOrEmpty(_logPath))
            {
                return;
            }

            string logDirectory = Path.Combine(HardwareBridge.GetProjectRootPath(), "Logs");
            Directory.CreateDirectory(logDirectory);
            _logPath = Path.Combine(logDirectory, "closed-eye-calibration.tsv");
        }

        private void DrawField(Color color, float level)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _previewPixels[x, y] = color * level;
                }
            }
        }

        private void DrawCore(Color color, Vector2 center, float size, float level)
        {
            float sigma = Mathf.Max(0.18f, size);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float distanceSquared = (new Vector2(x, y) - center).sqrMagnitude;
                    float alpha = BellRingerLightStyle.ContrastAlpha(Mathf.Exp(-distanceSquared / (2f * sigma * sigma)), peakContrast) * level;
                    _previewPixels[x, y] = MaxColor(_previewPixels[x, y], color * alpha);
                }
            }
        }

        private void DrawRipple(Color color, Vector2 center, float radius, float width, float level)
        {
            float halfWidth = Mathf.Max(0.1f, width * 0.5f);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = BellRingerLightStyle.ContrastAlpha(Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / halfWidth), peakContrast) * level;
                    _previewPixels[x, y] = MaxColor(_previewPixels[x, y], color * alpha);
                }
            }
        }

        private void DrawLowerBand(Color color, float level, int seed, float phase, float density)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (y > 2)
                    {
                        continue;
                    }

                    float alpha = 0f;
                    int dropCount = Mathf.Clamp(Mathf.RoundToInt(2f + density * 5f), 2, 7);
                    for (int drop = 0; drop < dropCount; drop++)
                    {
                        float dropX = Hash01(seed + drop * 19, 3, 11) * (Width - 1);
                        float dropY = Hash01(seed + drop * 23, 7, 5) * 2f;
                        float radius = Mathf.Repeat((phase * 1.15f) + Hash01(seed + drop * 29, 13, 17) * 2.8f, 2.8f);
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(dropX, dropY));
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 1.15f));
                    }

                    _previewPixels[x, y] = MaxColor(_previewPixels[x, y], color * BellRingerLightStyle.ContrastAlpha(alpha, peakContrast) * level);
                }
            }
        }

        private void DrawNoiseField(Color color, float level, int seed, float density)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Hash01(seed + 71, x, y) > density)
                    {
                        continue;
                    }

                    float alpha = 0.25f + Hash01(seed, x, y) * 0.75f;
                    _previewPixels[x, y] = MaxColor(_previewPixels[x, y], color * BellRingerLightStyle.ContrastAlpha(alpha, peakContrast) * level);
                }
            }
        }

        private void DrawTear(Color color, Vector2 center, float size, float tear, Vector2 axis, float level, float instability)
        {
            DrawCore(color, center, size, level);
            Vector2 normalizedAxis = axis.sqrMagnitude <= 0.001f ? Vector2.right : axis.normalized;
            DrawCore(color, center + normalizedAxis * tear, size * 0.75f, level * instability);
            DrawCore(color, center - normalizedAxis * tear, size * 0.75f, level * instability);
        }

        private void ClearPreview()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _previewPixels[x, y] = Color.clear;
                }
            }
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.000}", GUILayout.Width(170f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(300f));
            GUILayout.EndHorizontal();
            return nextValue;
        }

        private static string EffectDescription(ClosedEyeCalibrationEffect effect)
        {
            switch (effect)
            {
                case ClosedEyeCalibrationEffect.Field:
                    return "Pure color field. Use this only to compare color visibility, not gameplay readability.";
                case ClosedEyeCalibrationEffect.SingleCore:
                    return "Minimal single light mass. Good for testing whether one strong point is easier than many weak points.";
                case ClosedEyeCalibrationEffect.PairedCores:
                    return "Two separated cores. Tests whether count/spacing survives eyelid blur.";
                case ClosedEyeCalibrationEffect.Ripple:
                    return "Slow expanding ring. Tests event readability without fast flicker.";
                case ClosedEyeCalibrationEffect.LowerBand:
                    return "Full-width lower band. Candidate grammar for rain/floor signals.";
                case ClosedEyeCalibrationEffect.WallNoise:
                    return "Low noisy surface. Candidate grammar for walls or broad surfaces.";
                case ClosedEyeCalibrationEffect.TearCore:
                    return "Core splits and returns. Candidate grammar for tinnitus or wrong signal.";
                case ClosedEyeCalibrationEffect.Sweep:
                    return "One core moves horizontally. Tests motion readability.";
                default:
                    return string.Empty;
            }
        }

        private string ColorDescription(int colorIndex)
        {
            switch (Mathf.Clamp(colorIndex, 0, _colorNames.Length - 1))
            {
                case 0:
                case 1:
                case 2:
                    return "Warm red-pass candidates. Likely bright through eyelids, but may collapse into white/red brightness.";
                case 3:
                case 4:
                case 5:
                case 6:
                    return "Long-wavelength warm candidates. Test whether orange/amber/yellow separate from red or just feel brighter.";
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    return "Green-cyan candidates. Lower eyelid transmission, but may separate from warm colors by cool/dim quality.";
                case 12:
                case 13:
                case 14:
                    return "Blue-violet-magenta candidates. Likely dimmer or red-shifted; useful to find any non-warm signature.";
                default:
                    return "White control. Compare against warm/cool color collapse under closed eyelids.";
            }
        }

        private static float SafePulse(float elapsed)
        {
            float phase = Mathf.Repeat(elapsed * 1.25f, 1f);
            if (phase > 0.55f)
            {
                return 0f;
            }

            return Mathf.Sin((phase / 0.55f) * Mathf.PI);
        }

        private float SweepX(float elapsed)
        {
            return Mathf.Lerp(1f, 14f, Mathf.PingPong(elapsed * effectSpeed * 0.45f, 1f));
        }

        private float EffectiveLevel(float level)
        {
            return BellRingerLightStyle.ScaleLevel(level, averageLightScale, peakIntensityScale);
        }

        private float EffectiveCoreSize()
        {
            return coreSize * Mathf.Lerp(0.7f, 1.1f, litPixelScale);
        }

        private static Color MaxColor(Color a, Color b)
        {
            return new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), 1f);
        }

        private static float Hash01(int seed, int x, int y)
        {
            uint value = (uint)seed;
            value ^= (uint)(x + 37) * 1103515245u;
            value ^= (uint)(y + 101) * 12345u;
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            return (value & 0x00FFFFFF) / 16777215f;
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
    }
}
