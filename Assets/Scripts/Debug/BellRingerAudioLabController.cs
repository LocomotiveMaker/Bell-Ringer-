using System;
using BellRinger.Audio;
using UnityEngine;

namespace BellRinger.Debug
{
    public sealed class BellRingerAudioLabController : MonoBehaviour
    {
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private BellRingerAudioSourcePreset bell3dPreset;
        [SerializeField] private BellRingerAudioSourcePreset muffledPreset;
        [SerializeField] private BellRingerAudioSourcePreset tonePreset;
        [SerializeField] private BellRingerAudioSourcePreset flat2dPreset;
        [SerializeField] private float orbitRadius = 4f;
        [SerializeField] private float orbitHeight = 1.35f;
        [SerializeField] private float orbitSpeedDegreesPerSecond = 35f;

        private Transform _listener;
        private Transform _bellTransform;
        private Transform _toneTransform;
        private AudioSource _bellSource;
        private AudioSource _toneAudioSource;
        private BellRingerProceduralToneSource _toneSource;
        private BellRingerAudioSourcePreset _activeBellPreset;
        private BellRingerAudioSourcePreset _activeTonePreset;
        private string[] _spatializerNames = Array.Empty<string>();
        private bool _orbitSources = true;
        private bool _bellLoop;
        private bool _spatializeTone;
        private float _masterGain = 0.85f;
        private float _bellBusGain = 1f;
        private float _toneBusGain = 0.65f;

        private void Start()
        {
            _spatializerNames = AudioSettings.GetSpatializerPluginNames() ?? Array.Empty<string>();
            DisableHardwareDebugOverlay();
            EnsureSceneObjects();
            ApplyBellPreset(bell3dPreset);
            ApplyTonePreset(tonePreset);
            ApplyBusGains();
        }

        private void Update()
        {
            if (!_orbitSources || _listener == null)
            {
                return;
            }

            float angle = Time.unscaledTime * orbitSpeedDegreesPerSecond * Mathf.Deg2Rad;
            Vector3 orbitOffset = new Vector3(Mathf.Sin(angle) * orbitRadius, orbitHeight, Mathf.Cos(angle) * orbitRadius);

            if (_bellTransform != null)
            {
                _bellTransform.position = _listener.position + orbitOffset;
            }

            if (_toneTransform != null)
            {
                _toneTransform.position = _listener.position - orbitOffset;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 460f, 620f), "Bell Ringer Audio Lab", GUI.skin.window);
            GUILayout.Label("1. Presets / bus gains / distance rolloff");
            GUILayout.Label($"Active bell preset: {PresetName(_activeBellPreset)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell 3D"))
            {
                ApplyBellPreset(bell3dPreset);
            }

            if (GUILayout.Button("Muffled Far"))
            {
                ApplyBellPreset(muffledPreset);
            }

            if (GUILayout.Button("Flat 2D"))
            {
                ApplyBellPreset(flat2dPreset);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Bell"))
            {
                PlayBellOnce();
            }

            if (GUILayout.Button(_bellLoop ? "Stop Bell Loop" : "Start Bell Loop"))
            {
                ToggleBellLoop();
            }

            if (GUILayout.Button(_orbitSources ? "Stop Orbit" : "Start Orbit"))
            {
                _orbitSources = !_orbitSources;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("2. Mixer-style gain staging");
            _masterGain = Slider("Master", _masterGain, 0f, 1f);
            _bellBusGain = Slider("Bell bus", _bellBusGain, 0f, 1.2f);
            _toneBusGain = Slider("Generated tone bus", _toneBusGain, 0f, 1.2f);
            ApplyBusGains();

            GUILayout.Space(10f);
            GUILayout.Label("3. Filter / distance texture");
            GUILayout.Label("Muffled Far preset enables low-pass filtering. Use it to compare clear vs blocked/distant sound.");

            GUILayout.Space(10f);
            GUILayout.Label("4. Procedural frequency tone");
            GUILayout.Label($"Tone: {_toneSource.FrequencyHz:0} Hz / {_toneSource.Waveform} / {(_toneSource.IsToneEnabled ? "On" : "Off")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_toneSource.IsToneEnabled ? "Stop Tone" : "Start Tone"))
            {
                _toneSource.ToggleTone();
            }

            if (GUILayout.Button("220"))
            {
                _toneSource.FrequencyHz = 220f;
            }

            if (GUILayout.Button("440"))
            {
                _toneSource.FrequencyHz = 440f;
            }

            if (GUILayout.Button("880"))
            {
                _toneSource.FrequencyHz = 880f;
            }

            if (GUILayout.Button("1760"))
            {
                _toneSource.FrequencyHz = 1760f;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sine"))
            {
                _toneSource.Waveform = BellRingerToneWaveform.Sine;
            }

            if (GUILayout.Button("Triangle"))
            {
                _toneSource.Waveform = BellRingerToneWaveform.Triangle;
            }

            if (GUILayout.Button("Square"))
            {
                _toneSource.Waveform = BellRingerToneWaveform.Square;
            }

            if (GUILayout.Button("Saw"))
            {
                _toneSource.Waveform = BellRingerToneWaveform.Saw;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("5. HRTF spatializer A/B");
            GUILayout.Label($"Current spatializer: {CurrentSpatializerName()}");
            GUILayout.Label($"Available spatializers: {(_spatializerNames.Length == 0 ? "(none)" : string.Join(", ", _spatializerNames))}");
            if (GUILayout.Button(_spatializeTone ? "Tone Spatialize: On" : "Tone Spatialize: Off"))
            {
                _spatializeTone = !_spatializeTone;
                ApplySpatializeToggle();
            }

            if (_spatializerNames.Length == 0)
            {
                GUILayout.Label("No spatializer plugin is installed. This toggle will not produce real HRTF yet.");
            }

            GUILayout.EndArea();
        }

        private void EnsureSceneObjects()
        {
            Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("AudioLabCamera");
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.02f, 0.018f, 1f);
                cameraObject.transform.position = new Vector3(0f, 1.6f, -5f);
            }

            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = camera.gameObject.AddComponent<AudioListener>();
            }

            _listener = listener.transform;

            CreateLight();
            CreateFloor();
            CreateBellSource();
            CreateToneSource();
        }

        private static void DisableHardwareDebugOverlay()
        {
            BellRingerDebugOverlay[] overlays = FindObjectsByType<BellRingerDebugOverlay>(FindObjectsSortMode.None);
            for (int i = 0; i < overlays.Length; i++)
            {
                overlays[i].enabled = false;
            }
        }

        private void CreateBellSource()
        {
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "AudioLab Bell Source";
            bell.transform.position = new Vector3(0f, orbitHeight, orbitRadius);
            bell.transform.localScale = Vector3.one * 0.35f;
            SetMaterialColor(bell, new Color(0.1f, 0.9f, 0.28f, 1f));

            _bellTransform = bell.transform;
            _bellSource = bell.AddComponent<AudioSource>();
            _bellSource.clip = bellClip;
            _bellSource.playOnAwake = false;
            _bellSource.loop = false;
        }

        private void CreateToneSource()
        {
            GameObject tone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tone.name = "AudioLab Generated Tone Source";
            tone.transform.position = new Vector3(0f, orbitHeight, -orbitRadius);
            tone.transform.localScale = Vector3.one * 0.35f;
            SetMaterialColor(tone, new Color(0.1f, 0.55f, 1f, 1f));

            _toneTransform = tone.transform;
            _toneAudioSource = tone.AddComponent<AudioSource>();
            _toneSource = tone.AddComponent<BellRingerProceduralToneSource>();
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "AudioLabFloor";
            floor.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
            SetMaterialColor(floor, new Color(0.04f, 0.055f, 0.045f, 1f));
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("AudioLabKeyLight");
            lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void SetMaterialColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
            }
        }

        private void ApplyBellPreset(BellRingerAudioSourcePreset preset)
        {
            if (preset == null || _bellSource == null)
            {
                return;
            }

            _activeBellPreset = preset;
            preset.ApplyTo(_bellSource);
            _bellSource.loop = _bellLoop;
            ApplyBusGains();
        }

        private void ApplyTonePreset(BellRingerAudioSourcePreset preset)
        {
            if (preset == null || _toneAudioSource == null)
            {
                return;
            }

            _activeTonePreset = preset;
            preset.ApplyTo(_toneAudioSource);
            ApplySpatializeToggle();
            ApplyBusGains();
        }

        private void ApplyBusGains()
        {
            if (_bellSource != null)
            {
                float presetVolume = _activeBellPreset == null ? 0.85f : _activeBellPreset.Volume;
                _bellSource.volume = Mathf.Clamp01(presetVolume * _masterGain * _bellBusGain);
            }

            if (_toneAudioSource != null)
            {
                float presetVolume = _activeTonePreset == null ? 0.25f : _activeTonePreset.Volume;
                _toneAudioSource.volume = Mathf.Clamp01(presetVolume * _masterGain * _toneBusGain);
            }
        }

        private void ApplySpatializeToggle()
        {
            if (_toneAudioSource == null)
            {
                return;
            }

            _toneAudioSource.spatialize = _spatializeTone;
            _toneAudioSource.spatializePostEffects = false;
        }

        private void PlayBellOnce()
        {
            if (_bellSource == null || _bellSource.clip == null)
            {
                UnityEngine.Debug.LogWarning("[BellRingerAudioLabController] Bell clip is missing.");
                return;
            }

            _bellSource.loop = false;
            _bellSource.Stop();
            _bellSource.Play();
        }

        private void ToggleBellLoop()
        {
            if (_bellSource == null || _bellSource.clip == null)
            {
                return;
            }

            _bellLoop = !_bellLoop;
            _bellSource.loop = _bellLoop;
            if (_bellLoop)
            {
                _bellSource.Play();
            }
            else
            {
                _bellSource.Stop();
            }
        }

        private static string CurrentSpatializerName()
        {
            string pluginName = AudioSettings.GetSpatializerPluginName();
            return string.IsNullOrWhiteSpace(pluginName) ? "(none)" : pluginName;
        }

        private static string PresetName(BellRingerAudioSourcePreset preset)
        {
            return preset == null ? "(none)" : preset.DisplayName;
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(160f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(240f));
            GUILayout.EndHorizontal();
            return nextValue;
        }
    }
}
