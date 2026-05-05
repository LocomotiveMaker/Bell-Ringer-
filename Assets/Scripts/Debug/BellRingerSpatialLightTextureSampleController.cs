using BellRinger.Audio;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Debug
{
    public enum BellRingerSpatialLightSampleMode
    {
        BellPoint,
        WallNoise,
        RainFloor,
    }

    public sealed class BellRingerSpatialLightTextureSampleController : MonoBehaviour
    {
        [SerializeField] private BellRingerLightTexturePreset[] presets = new BellRingerLightTexturePreset[0];
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private AudioClip rainClip;
        [SerializeField] private int selectedPresetIndex;
        [SerializeField] private BellRingerSpatialLightSampleMode mode = BellRingerSpatialLightSampleMode.BellPoint;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private bool showRuntimeControls = true;
        [SerializeField] private bool showBoardPreview = true;
        [SerializeField] private float serialRefreshRate = 18f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private Color bellColor = BellRingerLightStyle.BellGreen;
        [SerializeField] private Color wallNoiseColor = BellRingerLightStyle.WallCyan;
        [SerializeField] [Range(0f, 0.35f)] private float wallNoiseBrightness = 0.075f;
        [SerializeField] private Color rainColor = BellRingerLightStyle.RainDeepBlue;
        [SerializeField] [Range(0f, 0.35f)] private float rainBrightness = 0.07f;
        [SerializeField] private float rainNeutralRows = 2f;
        [SerializeField] private float rainLookDownRows = 5f;
        [SerializeField] [Range(0.2f, 1f)] private float averageLightScale = 0.58f;
        [SerializeField] [Range(1f, 3f)] private float peakContrast = 1.85f;
        [SerializeField] [Range(0.25f, 1f)] private float litPixelScale = 0.55f;
        [SerializeField] [Range(1f, 2f)] private float peakIntensityScale = 1.25f;
        [SerializeField] [Range(2f, 4f)] private float bellMaxRadiusPixels = 3.4f;
        [SerializeField] [Range(0.2f, 1.4f)] private float bellCoreSizePixels = 0.58f;
        [SerializeField] private float previewBrightnessBoost = 5f;

        private readonly float[,] _previewAlpha = new float[BellRingerAudioLedMapper.DisplayWidth, BellRingerAudioLedMapper.DisplayHeight];
        private Transform _listenerTransform;
        private Transform _bellTransform;
        private Transform _wallTransform;
        private Transform _floorTransform;
        private AudioSource _bellSource;
        private AudioSource _wallNoiseSource;
        private AudioSource _rainSource;
        private uint _wallNoiseState = 1;
        private uint _rainNoiseState = 101;
        private float _wallNoiseFilter;
        private float _rainNoiseFilter;
        private float _nextSendTime;
        private bool _cameraInitialized;

        private BellRingerLightTexturePreset ActivePreset
        {
            get
            {
                if (presets == null || presets.Length == 0)
                {
                    return null;
                }

                selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Length - 1);
                return presets[selectedPresetIndex];
            }
        }

        private void Start()
        {
            EnsureScene();
            ApplyModeAudio();
        }

        private void Update()
        {
            EnsureScene();
            AnimateSources();
            ApplyModeAudio();

            if (Time.unscaledTime < _nextSendTime)
            {
                return;
            }

            float interval = serialRefreshRate <= 0f ? 0.05f : 1f / serialRefreshRate;
            _nextSendTime = Time.unscaledTime + interval;
            SendAndPreviewActiveMode();
        }

        private void OnGUI()
        {
            if (!showRuntimeControls)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(312f, 388f, 360f, 372f), "Spatial Light Texture", GUI.skin.window);
            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"Preset: {(ActivePreset == null ? "(none)" : ActivePreset.DisplayName)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell Point"))
            {
                mode = BellRingerSpatialLightSampleMode.BellPoint;
            }

            if (GUILayout.Button("Wall Noise"))
            {
                mode = BellRingerSpatialLightSampleMode.WallNoise;
            }

            if (GUILayout.Button("Rain Floor"))
            {
                mode = BellRingerSpatialLightSampleMode.RainFloor;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev Preset"))
            {
                SelectPreset(selectedPresetIndex - 1);
            }

            if (GUILayout.Button("Next Preset"))
            {
                SelectPreset(selectedPresetIndex + 1);
            }

            GUILayout.EndHorizontal();

            outputToHardware = GUILayout.Toggle(outputToHardware, "Hardware Output");
            showBoardPreview = GUILayout.Toggle(showBoardPreview, "Board Preview");
            averageLightScale = Slider("Average", averageLightScale, 0.2f, 1f);
            peakIntensityScale = Slider("Peak", peakIntensityScale, 1f, 2f);
            peakContrast = Slider("Contrast", peakContrast, 1f, 3f);
            litPixelScale = Slider("Lit Pixels", litPixelScale, 0.25f, 1f);
            GUILayout.Label("WASD move, right mouse drag look. Rain is a full-width lower screen band.");
            GUILayout.EndArea();

            if (showBoardPreview)
            {
                DrawBoardPreviewWindow(new Rect(16f, 388f, 292f, 190f));
            }
        }

        private void SelectPreset(int nextIndex)
        {
            if (presets == null || presets.Length == 0)
            {
                selectedPresetIndex = 0;
                return;
            }

            selectedPresetIndex = (nextIndex + presets.Length) % presets.Length;
        }

        private void EnsureScene()
        {
            Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            _listenerTransform = camera.transform;
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

            if (_bellTransform == null)
            {
                CreateBellPoint();
            }

            if (_wallTransform == null)
            {
                CreateWallPlane();
            }

            if (_floorTransform == null)
            {
                CreateFloor();
            }
        }

        private void CreateBellPoint()
        {
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "Sample Light Bell Point";
            bell.transform.position = new Vector3(0f, 1.45f, 4f);
            bell.transform.localScale = Vector3.one * 0.4f;
            SetMaterialColor(bell, bellColor);

            _bellTransform = bell.transform;
            _bellSource = bell.AddComponent<AudioSource>();
            _bellSource.clip = bellClip;
            _bellSource.volume = 0.7f;
            _bellSource.spatialBlend = 1f;
            _bellSource.minDistance = 0.7f;
            _bellSource.maxDistance = maxDistance;
            _bellSource.rolloffMode = AudioRolloffMode.Linear;
            _bellSource.loop = true;
            _bellSource.playOnAwake = false;
        }

        private void CreateWallPlane()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Sample Glitch Wall Plane";
            wall.transform.position = new Vector3(-2.5f, 1.7f, 4.2f);
            wall.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            wall.transform.localScale = new Vector3(5.8f, 3.4f, 0.08f);
            SetMaterialColor(wall, new Color(0.22f, 0.22f, 0.22f, 1f));

            _wallTransform = wall.transform;
            _wallNoiseSource = wall.AddComponent<AudioSource>();
            _wallNoiseSource.clip = AudioClip.Create("Sample Wall Glitch Noise", AudioSettings.outputSampleRate, 1, AudioSettings.outputSampleRate, true, OnWallNoiseRead);
            _wallNoiseSource.volume = 0.18f;
            _wallNoiseSource.spatialBlend = 1f;
            _wallNoiseSource.minDistance = 1f;
            _wallNoiseSource.maxDistance = maxDistance;
            _wallNoiseSource.rolloffMode = AudioRolloffMode.Linear;
            _wallNoiseSource.loop = true;
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Sample Rain Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
            SetMaterialColor(floor, new Color(0.04f, 0.055f, 0.05f, 1f));

            _floorTransform = floor.transform;
            _rainSource = floor.AddComponent<AudioSource>();
            _rainSource.clip = rainClip != null
                ? rainClip
                : AudioClip.Create("Sample Rain Noise", AudioSettings.outputSampleRate, 1, AudioSettings.outputSampleRate, true, OnRainNoiseRead);
            _rainSource.volume = 0.2f;
            _rainSource.spatialBlend = 0.15f;
            _rainSource.loop = true;
        }

        private void AnimateSources()
        {
            if (_bellTransform != null)
            {
                float angle = Time.unscaledTime * 0.55f;
                _bellTransform.position = new Vector3(Mathf.Sin(angle) * 2.4f, 1.45f + Mathf.Sin(angle * 1.7f) * 0.25f, 4f + Mathf.Cos(angle) * 1.2f);
            }

            if (_wallTransform != null)
            {
                float noise = Mathf.PerlinNoise(Time.unscaledTime * 7f, 0.37f);
                Renderer renderer = _wallTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float value = 0.12f + noise * 0.2f;
                    renderer.sharedMaterial.color = new Color(value, value, value, 1f);
                }
            }
        }

        private void ApplyModeAudio()
        {
            SetLoopState(_bellSource, mode == BellRingerSpatialLightSampleMode.BellPoint);
            SetLoopState(_wallNoiseSource, mode == BellRingerSpatialLightSampleMode.WallNoise);
            SetLoopState(_rainSource, mode == BellRingerSpatialLightSampleMode.RainFloor);
        }

        private static void SetLoopState(AudioSource source, bool shouldPlay)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (shouldPlay && !source.isPlaying)
            {
                source.Play();
            }
            else if (!shouldPlay && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void SendAndPreviewActiveMode()
        {
            ClearPreview();
            BellRingerLightTexturePreset preset = ActivePreset;
            if (preset == null || _listenerTransform == null)
            {
                return;
            }

            switch (mode)
            {
                case BellRingerSpatialLightSampleMode.WallNoise:
                    SendWallNoise(preset);
                    break;
                case BellRingerSpatialLightSampleMode.RainFloor:
                    SendRainFloor();
                    break;
                default:
                    SendBellPoint(preset);
                    break;
            }
        }

        private void SendBellPoint(BellRingerLightTexturePreset preset)
        {
            if (_bellTransform == null ||
                !BellRingerAudioLedMapper.TryMap(
                    _listenerTransform,
                    _bellTransform.position,
                    0.2f,
                    maxDistance,
                    1f,
                    1f,
                    preset.MaximumBrightness,
                    0.01f,
                    110f,
                    55f,
                    out BellRingerLedDotFrame dotFrame))
            {
                ClearHardware();
                return;
            }

            float radius = Mathf.Repeat(Time.unscaledTime * preset.RippleSpeedPixelsPerSecond, Mathf.Max(0.1f, bellMaxRadiusPixels));
            float level = EffectiveLevel(dotFrame.brightnessNormalized);
            float coreSize = bellCoreSizePixels * Mathf.Lerp(0.75f, 1.25f, litPixelScale);
            float width = Mathf.Max(0.45f, preset.RippleWidthPixels * litPixelScale);
            DrawPreviewBellPulse(dotFrame.x, dotFrame.y, radius, coreSize, width, level);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedPulseCore(dotFrame.x, dotFrame.y, radius, coreSize, width, bellColor, level, peakContrast);
            }
        }

        private void SendWallNoise(BellRingerLightTexturePreset preset)
        {
            if (_wallTransform == null || !TryProjectWall(out float centerX, out float centerY, out float width, out float height, out float distanceBrightness))
            {
                ClearHardware();
                return;
            }

            int seed = Mathf.FloorToInt(Time.unscaledTime * 42f);
            float level = EffectiveLevel(Mathf.Min(wallNoiseBrightness, preset.MaximumBrightness) * Mathf.Clamp01(distanceBrightness));
            float density = BellRingerLightStyle.DensityFromLitScale(litPixelScale);
            DrawPreviewWall(centerX, centerY, width, height, level, seed, density);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedWallNoise(centerX, centerY, width, height, wallNoiseColor, level, seed, density, peakContrast);
            }
        }

        private void SendRainFloor()
        {
            if (!TryProjectRainBand(out float centerX, out float centerY, out float width, out float height, out float visibility))
            {
                ClearHardware();
                return;
            }

            int seed = Mathf.FloorToInt(Time.unscaledTime * 0.35f);
            float phase = Time.unscaledTime;
            float level = EffectiveLevel(rainBrightness * visibility);
            float density = BellRingerLightStyle.DensityFromLitScale(litPixelScale);
            float minimalHeight = Mathf.Max(0.6f, height * Mathf.Lerp(0.55f, 1f, litPixelScale));
            centerY = Mathf.Clamp((minimalHeight - 1f) * 0.5f, 0f, 7f);
            DrawPreviewRain(centerX, centerY, width, minimalHeight, level, seed, phase, density);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedRain(rainColor, level, seed, centerX, centerY, width, minimalHeight, phase, density, peakContrast);
            }
        }

        private bool TryProjectRainBand(out float centerX, out float centerY, out float width, out float height, out float visibility)
        {
            centerX = 7.5f;
            centerY = 0.5f;
            width = 16f;
            height = 0f;
            visibility = 0f;

            float forwardY = _listenerTransform.forward.y;
            float skySuppression = Mathf.Clamp01(1f - Mathf.InverseLerp(0.18f, 0.7f, forwardY));
            if (skySuppression <= 0.01f)
            {
                return false;
            }

            float lookDown = Mathf.Clamp01(-forwardY);
            height = Mathf.Lerp(Mathf.Max(1f, rainNeutralRows), Mathf.Max(rainNeutralRows, rainLookDownRows), lookDown) * skySuppression;
            if (height < 0.6f)
            {
                return false;
            }

            height = Mathf.Clamp(height, 0.6f, 5f);
            centerY = Mathf.Clamp((height - 1f) * 0.5f, 0f, 7f);
            visibility = Mathf.Clamp01(skySuppression * Mathf.Lerp(0.72f, 1f, lookDown));
            return visibility > 0.01f && height > 0.25f;
        }

        private bool TryProjectWall(out float centerX, out float centerY, out float width, out float height, out float brightness)
        {
            centerX = 7.5f;
            centerY = 3.5f;
            width = 1f;
            height = 1f;
            brightness = 0f;

            Camera camera = _listenerTransform == null ? null : _listenerTransform.GetComponent<Camera>();
            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                return false;
            }

            Vector3 right = _wallTransform.right * (_wallTransform.localScale.x * 0.5f);
            Vector3 up = _wallTransform.up * (_wallTransform.localScale.y * 0.5f);
            Vector3 center = _wallTransform.position;
            Vector3[] samples =
            {
                center,
                center - right - up,
                center + right - up,
                center - right + up,
                center + right + up,
            };

            float minX = 15f;
            float maxX = 0f;
            float minY = 7f;
            float maxY = 0f;
            int mappedCount = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                Vector3 viewportPoint = camera.WorldToViewportPoint(samples[i]);
                if (viewportPoint.z <= camera.nearClipPlane || viewportPoint.x < -0.2f || viewportPoint.x > 1.2f || viewportPoint.y < -0.2f || viewportPoint.y > 1.2f)
                {
                    continue;
                }

                float ledX = Mathf.Clamp01(viewportPoint.x) * (BellRingerAudioLedMapper.DisplayWidth - 1);
                float ledY = Mathf.Clamp01(viewportPoint.y) * (BellRingerAudioLedMapper.DisplayHeight - 1);
                minX = Mathf.Min(minX, ledX);
                maxX = Mathf.Max(maxX, ledX);
                minY = Mathf.Min(minY, ledY);
                maxY = Mathf.Max(maxY, ledY);
                mappedCount++;
            }

            if (mappedCount == 0)
            {
                return false;
            }

            centerX = (minX + maxX) * 0.5f;
            width = Mathf.Clamp(maxX - minX + 2.5f, 3f, 16f);
            height = Mathf.Clamp(maxY - minY + 2.5f, 3.5f, 8f);
            centerY = Mathf.Clamp((height - 1f) * 0.5f, 0f, 7f);
            float distance = Vector3.Distance(_listenerTransform.position, _wallTransform.position);
            brightness = 1f - Mathf.InverseLerp(1f, maxDistance, distance);
            return true;
        }

        private void ClearHardware()
        {
            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.ClearLedDisplay();
            }
        }

        private void DrawBoardPreviewWindow(Rect rect)
        {
            GUILayout.BeginArea(rect, "LED Board Preview", GUI.skin.window);
            GUI.Label(new Rect(16f, 22f, 260f, 20f), $"{mode} / UI x{Mathf.Max(1f, previewBrightnessBoost):0.0} boost");
            DrawPreview(new Rect(16f, 44f, 256f, 128f));
            GUI.Label(new Rect(16f, 170f, 260f, 20f), "Bottom row in preview = bottom row on board.");
            GUILayout.EndArea();
        }

        private void DrawPreview(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            float cellWidth = rect.width / BellRingerAudioLedMapper.DisplayWidth;
            float cellHeight = rect.height / BellRingerAudioLedMapper.DisplayHeight;
            Color color = mode == BellRingerSpatialLightSampleMode.WallNoise
                ? wallNoiseColor
                : mode == BellRingerSpatialLightSampleMode.RainFloor
                    ? rainColor
                    : bellColor;

            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    GUI.color = color * Mathf.Clamp01(_previewAlpha[x, y] * Mathf.Max(1f, previewBrightnessBoost));
                    GUI.DrawTexture(new Rect(rect.x + x * cellWidth + 1f, rect.y + (7 - y) * cellHeight + 1f, cellWidth - 2f, cellHeight - 2f), Texture2D.whiteTexture);
                }
            }

            GUI.color = previousColor;
        }

        private void ClearPreview()
        {
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    _previewAlpha[x, y] = 0f;
                }
            }
        }

        private void DrawPreviewBellPulse(float centerX, float centerY, float radius, float coreSize, float rippleWidth, float level)
        {
            float halfWidth = Mathf.Max(0.05f, rippleWidth * 0.5f);
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    float core = Mathf.Exp(-(distance * distance) / (2f * coreSize * coreSize));
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / halfWidth) * 0.75f;
                    _previewAlpha[x, y] = BellRingerLightStyle.ContrastAlpha(Mathf.Max(core, ring), peakContrast) * level;
                }
            }
        }

        private void DrawPreviewWall(float centerX, float centerY, float width, float height, float level, int seed, float density)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    float dx = Mathf.Abs(x - centerX);
                    float dy = Mathf.Abs(y - centerY);
                    if (dx > halfWidth || dy > halfHeight)
                    {
                        continue;
                    }

                    float edgeX = Mathf.Clamp01((halfWidth - dx) / 1.5f);
                    float edgeY = Mathf.Clamp01(((centerY + halfHeight) - y) / 1.5f);
                    float sparse = Hash01(seed + 71, x, y);
                    if (sparse > density)
                    {
                        continue;
                    }

                    float glitch = 0.25f + Hash01(seed, x, y) * 0.75f;
                    _previewAlpha[x, y] = BellRingerLightStyle.ContrastAlpha(glitch, peakContrast) * edgeX * edgeY * level;
                }
            }
        }

        private void DrawPreviewRain(float centerX, float centerY, float width, float height, float level, int seed, float phase, float density)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    float dxArea = Mathf.Abs(x - centerX);
                    float dyArea = Mathf.Abs(y - centerY);
                    if (dxArea > halfWidth || dyArea > halfHeight)
                    {
                        continue;
                    }

                    float alpha = 0f;
                    int dropCount = Mathf.Clamp(Mathf.RoundToInt(2f + density * 5f), 2, 7);
                    for (int drop = 0; drop < dropCount; drop++)
                    {
                        float dropX = Mathf.Lerp(centerX - halfWidth, centerX + halfWidth, Hash01(seed + drop * 19, 3, 11));
                        float dropY = Mathf.Lerp(centerY - halfHeight, centerY + halfHeight, Hash01(seed + drop * 23, 7, 5));
                        float radius = Mathf.Repeat((phase * 1.15f) + Hash01(seed + drop * 29, 13, 17) * 2.8f, 2.8f);
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(dropX, dropY));
                        alpha = Mathf.Max(alpha, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 1.15f)));
                    }

                    float edgeX = width >= 15.9f ? 1f : Mathf.Clamp01((halfWidth - dxArea) / 1.5f);
                    float edgeY = Mathf.Clamp01(((centerY + halfHeight) - y) / 1.25f);
                    _previewAlpha[x, y] = BellRingerLightStyle.ContrastAlpha(alpha, peakContrast) * edgeX * edgeY * level;
                }
            }
        }

        private float EffectiveLevel(float baseLevel)
        {
            return BellRingerLightStyle.ScaleLevel(baseLevel, averageLightScale, peakIntensityScale);
        }

        private void OnWallNoiseRead(float[] data)
        {
            FillNoise(data, ref _wallNoiseState, ref _wallNoiseFilter, 0.75f, 0.08f);
        }

        private void OnRainNoiseRead(float[] data)
        {
            FillNoise(data, ref _rainNoiseState, ref _rainNoiseFilter, 0.35f, 0.25f);
        }

        private static void FillNoise(float[] data, ref uint state, ref float filter, float amplitude, float smoothing)
        {
            for (int i = 0; i < data.Length; i++)
            {
                state = (state * 1664525u) + 1013904223u;
                float white = ((state & 0x00FFFFFF) / 16777215f) * 2f - 1f;
                filter = Mathf.Lerp(white, filter, smoothing);
                data[i] = filter * amplitude;
            }
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

        private static void SetMaterialColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
            }
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(110f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(200f));
            GUILayout.EndHorizontal();
            return nextValue;
        }
    }
}
