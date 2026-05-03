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
        [SerializeField] private float serialRefreshRate = 18f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private Color wallNoiseColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] [Range(0f, 0.35f)] private float wallNoiseBrightness = 0.084f;
        [SerializeField] private Color rainColor = new Color(0.47f, 0.67f, 1f, 1f);
        [SerializeField] [Range(0f, 0.35f)] private float rainBrightness = 0.078f;
        [SerializeField] private float rainPatchRadiusMeters = 3.2f;
        [SerializeField] private int rainProjectionSamplesPerAxis = 11;

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

            GUILayout.BeginArea(new Rect(312f, 252f, 340f, 292f), "Spatial Light Texture", GUI.skin.window);
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
            GUILayout.Label("WASD move, right mouse drag look. Wall/Rain are area textures, not point textures.");
            DrawPreview(new Rect(16f, 146f, 256f, 128f));
            GUILayout.EndArea();
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
            SetMaterialColor(bell, new Color(0.12f, 1f, 0.35f, 1f));

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
            wall.transform.position = new Vector3(-3.2f, 1.8f, 4.5f);
            wall.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            wall.transform.localScale = new Vector3(4.5f, 2.6f, 0.08f);
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

            float radius = preset.StartRadiusPixels + Mathf.Repeat(Time.unscaledTime * preset.RippleSpeedPixelsPerSecond, Mathf.Max(0.1f, preset.MaxRadiusPixels));
            DrawPreviewRipple(dotFrame.x, dotFrame.y, radius, preset.RippleWidthPixels, dotFrame.brightnessNormalized);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedRipple(dotFrame.x, dotFrame.y, radius, preset.RippleWidthPixels, preset.Color, dotFrame.brightnessNormalized);
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
            float level = Mathf.Min(wallNoiseBrightness, preset.MaximumBrightness) * Mathf.Clamp01(distanceBrightness);
            DrawPreviewWall(centerX, centerY, width, height, level, seed);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedWallNoise(centerX, centerY, width, height, wallNoiseColor, level, seed);
            }
        }

        private void SendRainFloor()
        {
            if (!TryProjectRainPatch(out float centerX, out float centerY, out float width, out float height, out float visibility))
            {
                ClearHardware();
                return;
            }

            int seed = Mathf.FloorToInt(Time.unscaledTime * 0.35f);
            float phase = Time.unscaledTime;
            float level = rainBrightness * visibility;
            DrawPreviewRain(centerX, centerY, width, height, level, seed, phase);

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedRain(rainColor, level, seed, centerX, centerY, width, height, phase);
            }
        }

        private bool TryProjectRainPatch(out float centerX, out float centerY, out float width, out float height, out float visibility)
        {
            centerX = 7.5f;
            centerY = 1f;
            width = 16f;
            height = 0f;
            visibility = 0f;

            Camera camera = _listenerTransform == null ? null : _listenerTransform.GetComponent<Camera>();
            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                return false;
            }

            float forwardY = _listenerTransform.forward.y;
            float skySuppression = Mathf.Clamp01(1f - Mathf.InverseLerp(0.2f, 0.75f, forwardY));
            if (skySuppression <= 0.01f)
            {
                return false;
            }

            float lookDown = Mathf.Clamp01(-forwardY);
            float maxRows = Mathf.Lerp(3f, 5f, lookDown) * skySuppression;
            if (maxRows < 0.5f)
            {
                return false;
            }

            int samplesPerAxis = Mathf.Clamp(rainProjectionSamplesPerAxis, 5, 17);
            float radius = Mathf.Max(0.5f, rainPatchRadiusMeters);
            Vector3 center = new Vector3(_listenerTransform.position.x, 0f, _listenerTransform.position.z);

            float minX = 15f;
            float maxX = 0f;
            float minY = 7f;
            float maxY = 0f;
            int mappedCount = 0;

            for (int z = 0; z < samplesPerAxis; z++)
            {
                float normalizedZ = samplesPerAxis == 1 ? 0f : Mathf.Lerp(-1f, 1f, z / (samplesPerAxis - 1f));
                for (int x = 0; x < samplesPerAxis; x++)
                {
                    float normalizedX = samplesPerAxis == 1 ? 0f : Mathf.Lerp(-1f, 1f, x / (samplesPerAxis - 1f));
                    if ((normalizedX * normalizedX) + (normalizedZ * normalizedZ) > 1f)
                    {
                        continue;
                    }

                    Vector3 worldPoint = center + new Vector3(normalizedX * radius, 0f, normalizedZ * radius);
                    Vector3 viewportPoint = camera.WorldToViewportPoint(worldPoint);
                    if (viewportPoint.z <= camera.nearClipPlane || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
                    {
                        continue;
                    }

                    float ledX = viewportPoint.x * (BellRingerAudioLedMapper.DisplayWidth - 1);
                    float ledY = viewportPoint.y * (BellRingerAudioLedMapper.DisplayHeight - 1);
                    minX = Mathf.Min(minX, ledX);
                    maxX = Mathf.Max(maxX, ledX);
                    minY = Mathf.Min(minY, ledY);
                    maxY = Mathf.Max(maxY, ledY);
                    mappedCount++;
                }
            }

            if (mappedCount == 0)
            {
                return false;
            }

            centerX = (minX + maxX) * 0.5f;
            width = Mathf.Clamp(maxX - minX + 1.5f, 2f, 16f);

            float visibleHeight = Mathf.Min(maxY - minY + 1.5f, maxRows);
            centerY = Mathf.Clamp(minY + visibleHeight * 0.5f, 0f, 7f);
            height = Mathf.Clamp(visibleHeight, 0.5f, 5f);
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
                if (!BellRingerAudioLedMapper.TryMap(
                    _listenerTransform,
                    samples[i],
                    0.3f,
                    maxDistance,
                    1f,
                    1f,
                    1f,
                    0.01f,
                    110f,
                    55f,
                    out BellRingerLedDotFrame frame))
                {
                    continue;
                }

                minX = Mathf.Min(minX, frame.x);
                maxX = Mathf.Max(maxX, frame.x);
                minY = Mathf.Min(minY, frame.y);
                maxY = Mathf.Max(maxY, frame.y);
                brightness = Mathf.Max(brightness, frame.brightnessNormalized);
                mappedCount++;
            }

            if (mappedCount == 0)
            {
                return false;
            }

            centerX = (minX + maxX) * 0.5f;
            centerY = (minY + maxY) * 0.5f;
            width = Mathf.Clamp(maxX - minX + 2f, 1.5f, 16f);
            height = Mathf.Clamp(maxY - minY + 1.5f, 1.5f, 8f);
            return true;
        }

        private void ClearHardware()
        {
            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.ClearLedDisplay();
            }
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
                    : ActivePreset == null ? Color.white : ActivePreset.Color;

            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    GUI.color = color * Mathf.Clamp01(_previewAlpha[x, y]);
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

        private void DrawPreviewRipple(float centerX, float centerY, float radius, float rippleWidth, float level)
        {
            float halfWidth = Mathf.Max(0.05f, rippleWidth * 0.5f);
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    _previewAlpha[x, y] = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / halfWidth) * level;
                }
            }
        }

        private void DrawPreviewWall(float centerX, float centerY, float width, float height, float level, int seed)
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

                    float edge = Mathf.Clamp01((halfWidth - dx) / 1.5f) * Mathf.Clamp01((halfHeight - dy) / 1.5f);
                    _previewAlpha[x, y] = Hash01(seed, x, y) * edge * level;
                }
            }
        }

        private void DrawPreviewRain(float centerX, float centerY, float width, float height, float level, int seed, float phase)
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
                    for (int drop = 0; drop < 7; drop++)
                    {
                        float dropX = Mathf.Lerp(centerX - halfWidth, centerX + halfWidth, Hash01(seed + drop * 19, 3, 11));
                        float dropY = Mathf.Lerp(centerY - halfHeight, centerY + halfHeight, Hash01(seed + drop * 23, 7, 5));
                        float radius = Mathf.Repeat((phase * 1.15f) + Hash01(seed + drop * 29, 13, 17) * 2.8f, 2.8f);
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(dropX, dropY));
                        alpha = Mathf.Max(alpha, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 1.15f)));
                    }

                    float edge = Mathf.Clamp01((halfWidth - dxArea) / 1.5f) * Mathf.Clamp01((halfHeight - dyArea) / 1.25f);
                    _previewAlpha[x, y] = alpha * edge * level;
                }
            }
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
    }
}
