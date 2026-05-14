using BellRinger.Audio;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.Debug
{
    public sealed class TinnitusTestController : MonoBehaviour
    {
        private enum TinnitusTestMode
        {
            NormalTinnitusTreatment,
            GiantTinnitusTreatment,
            BellGazeTutorial,
            Combined,
        }

        [SerializeField] private TinnitusTestMode testMode = TinnitusTestMode.NormalTinnitusTreatment;
        [SerializeField] private AudioClip continuousGlitchClip;
        [SerializeField] private AudioClip shortGlitchClip;
        [SerializeField] private AudioClip resolveClip;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private bool showRuntimeControls = true;
        [SerializeField] private bool enablePadTreatmentPrototype = true;
        [SerializeField] private bool enableTreatmentHaptics = true;
        [SerializeField] private bool enableGiantTinnitusPrototype = true;
        [SerializeField] private bool enableBellGazeTutorialPrototype = true;
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private float previewBrightnessBoost = 5f;
        [SerializeField] private float treatmentSeconds = 4f;
        [SerializeField] private float treatmentLockPulseSeconds = 0.16f;
        [SerializeField] private float treatmentHealingPulseHz = 2.1f;

        private Transform _listenerTransform;
        private Transform _tinnitusTransform;
        private TinnitusAudioController _audioController;
        private TinnitusLightPatternController _lightController;
        private PadTrackingReceiver _padTrackingReceiver;
        private PadImuReceiver _padImuReceiver;
        private PadPoseProvider _padPoseProvider;
        private PadPoseMatchEvaluator _poseMatchEvaluator;
        private GiantTinnitusEncounterController _giantEncounter;
        private BellGazeTutorialController _bellTutorial;
        private TinnitusTreatmentCompletionEffect _completionEffect;
        private Transform _currentPadGhostTransform;
        private Transform _targetPadGhostTransform;
        private Transform _tutorialBellTransform;
        private Renderer _currentPadGhostRenderer;
        private Renderer _targetPadGhostRenderer;
        private Renderer _tutorialBellRenderer;
        private bool _cameraInitialized;
        private bool _staticSceneCreated;
        private bool _sharedSettingsInitialized;
        private bool _treatmentResolved;
        private bool _wasInsideTreatmentTolerance;
        private bool _treatmentHapticsActive;
        private bool _hasTreatmentRestoreLevels;
        private float _treatmentLockPulseUntilRealtime;
        private float _nextTreatmentHapticRefreshAtRealtime;
        private float _treatmentRestoreVolume;
        private float _treatmentRestoreCoreIntensity;
        private string _sharedSettingsStatus = "(pending)";
        private string _treatmentHapticStatus = "(idle)";

        private bool WantsTinnitusSource => testMode != TinnitusTestMode.BellGazeTutorial;
        private bool WantsPadTreatment => enablePadTreatmentPrototype &&
                                          (testMode == TinnitusTestMode.NormalTinnitusTreatment ||
                                           testMode == TinnitusTestMode.GiantTinnitusTreatment ||
                                           testMode == TinnitusTestMode.Combined);
        private bool WantsNormalTreatment => testMode == TinnitusTestMode.NormalTinnitusTreatment ||
                                             testMode == TinnitusTestMode.Combined;
        private bool WantsGiantTreatment => enableGiantTinnitusPrototype &&
                                            (testMode == TinnitusTestMode.GiantTinnitusTreatment ||
                                             testMode == TinnitusTestMode.Combined);
        private bool WantsBellGazeTutorial => enableBellGazeTutorialPrototype &&
                                              (testMode == TinnitusTestMode.BellGazeTutorial ||
                                               testMode == TinnitusTestMode.Combined);

        private void Start()
        {
            DisableHardwareDebugOverlay();
            EnsureScene();
        }

        private void Update()
        {
            EnsureScene();
            UpdateTreatmentPrototype();
            UpdatePadPoseGhosts();
        }

        private void OnDisable()
        {
            StopTreatmentHaptics();
        }

        private void OnDestroy()
        {
            StopTreatmentHaptics();
        }

        private void OnGUI()
        {
            if (!showRuntimeControls)
            {
                return;
            }

            if (WantsTinnitusSource && _audioController != null && _lightController != null)
            {
                DrawControlPanel(new Rect(16f, 16f, 520f, 860f));
                DrawBoardPreview(new Rect(552f, 16f, 330f, 226f));
            }

            if (WantsPadTreatment)
            {
                DrawTreatmentPanel(new Rect(552f, 258f, 520f, 680f));
            }

            if (WantsBellGazeTutorial)
            {
                DrawBellTutorialPanel(testMode == TinnitusTestMode.BellGazeTutorial
                    ? new Rect(16f, 16f, 520f, 180f)
                    : new Rect(1090f, 16f, 430f, 180f));
            }
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

            if (WantsTinnitusSource && _tinnitusTransform == null)
            {
                CreateTinnitusSource();
            }

            if (WantsTinnitusSource)
            {
                EnsureCompletionEffect();
            }

            if (WantsPadTreatment)
            {
                EnsurePadTreatmentPrototype();
            }

            if (WantsBellGazeTutorial)
            {
                EnsureBellGazeTutorialPrototype();
            }

            if (WantsTinnitusSource && _lightController != null)
            {
                _lightController.Configure(_listenerTransform, _tinnitusTransform, _audioController, outputToHardware);
                if (!_sharedSettingsInitialized)
                {
                    ApplySavedOrDefaultSettings();
                    _sharedSettingsInitialized = true;
                }
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

        private void EnsureCompletionEffect()
        {
            if (_completionEffect == null)
            {
                _completionEffect = GetComponent<TinnitusTreatmentCompletionEffect>();
                if (_completionEffect == null)
                {
                    _completionEffect = gameObject.AddComponent<TinnitusTreatmentCompletionEffect>();
                }
            }

            _completionEffect.ResolveClip = resolveClip;
        }

        private void EnsurePadTreatmentPrototype()
        {
            if (_padTrackingReceiver == null)
            {
                _padTrackingReceiver = GetComponent<PadTrackingReceiver>();
                if (_padTrackingReceiver == null)
                {
                    _padTrackingReceiver = gameObject.AddComponent<PadTrackingReceiver>();
                }
            }

            if (_padImuReceiver == null)
            {
                _padImuReceiver = GetComponent<PadImuReceiver>();
                if (_padImuReceiver == null)
                {
                    _padImuReceiver = gameObject.AddComponent<PadImuReceiver>();
                }
            }

            if (_padPoseProvider == null)
            {
                _padPoseProvider = GetComponent<PadPoseProvider>();
                if (_padPoseProvider == null)
                {
                    _padPoseProvider = gameObject.AddComponent<PadPoseProvider>();
                }
            }

            if (_poseMatchEvaluator == null)
            {
                _poseMatchEvaluator = GetComponent<PadPoseMatchEvaluator>();
                if (_poseMatchEvaluator == null)
                {
                    _poseMatchEvaluator = gameObject.AddComponent<PadPoseMatchEvaluator>();
                }
            }

            if (WantsGiantTreatment && _giantEncounter == null)
            {
                _giantEncounter = GetComponent<GiantTinnitusEncounterController>();
                if (_giantEncounter == null)
                {
                    _giantEncounter = gameObject.AddComponent<GiantTinnitusEncounterController>();
                }
            }

            _padImuReceiver.InitializeNow();
            _padPoseProvider.InitializeNow();
            _poseMatchEvaluator.InitializeNow();
            _giantEncounter?.InitializeNow();
            _poseMatchEvaluator.TreatmentSeconds = treatmentSeconds;
            CreatePadPoseGhostsIfNeeded();
        }

        private void EnsureBellGazeTutorialPrototype()
        {
            if (_tutorialBellTransform == null)
            {
                GameObject bellObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bellObject.name = "Bell Gaze Tutorial Bell";
                bellObject.transform.localScale = Vector3.one * 0.22f;
                _tutorialBellTransform = bellObject.transform;
                _tutorialBellRenderer = bellObject.GetComponent<Renderer>();
                _tutorialBellRenderer.sharedMaterial = CreateMaterial(BellRingerLightStyle.BellGreen);
            }

            if (_bellTutorial == null)
            {
                _bellTutorial = GetComponent<BellGazeTutorialController>();
                if (_bellTutorial == null)
                {
                    _bellTutorial = gameObject.AddComponent<BellGazeTutorialController>();
                }
            }

            _bellTutorial.Configure(_listenerTransform, _tutorialBellTransform, bellClip);
        }

        private void CreatePadPoseGhostsIfNeeded()
        {
            if (_currentPadGhostTransform != null && _targetPadGhostTransform != null)
            {
                return;
            }

            GameObject currentGhost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentGhost.name = "Current Pad Pose Ghost";
            currentGhost.transform.localScale = new Vector3(0.24f, 0.035f, 0.13f);
            _currentPadGhostTransform = currentGhost.transform;
            _currentPadGhostRenderer = currentGhost.GetComponent<Renderer>();
            _currentPadGhostRenderer.sharedMaterial = CreateMaterial(new Color(1f, 0.56f, 0.14f, 1f));

            GameObject targetGhost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetGhost.name = "Target Pad Pose Ghost";
            targetGhost.transform.localScale = new Vector3(0.28f, 0.018f, 0.16f);
            _targetPadGhostTransform = targetGhost.transform;
            _targetPadGhostRenderer = targetGhost.GetComponent<Renderer>();
            _targetPadGhostRenderer.sharedMaterial = CreateMaterial(new Color(0.15f, 1f, 0.45f, 1f));
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

        private void UpdateTreatmentPrototype()
        {
            if (!WantsPadTreatment || _poseMatchEvaluator == null || _audioController == null || _lightController == null)
            {
                return;
            }

            _poseMatchEvaluator.TreatmentSeconds = treatmentSeconds;
            bool giantActive = WantsGiantTreatment && _giantEncounter != null && (_giantEncounter.IsRunning || _giantEncounter.IsComplete);
            if (_giantEncounter != null && _giantEncounter.IsRunning)
            {
                _giantEncounter.Tick();
            }

            if (!_treatmentResolved)
            {
                _treatmentRestoreVolume = _audioController.Volume;
                _treatmentRestoreCoreIntensity = _lightController.CoreIntensity;
                _hasTreatmentRestoreLevels = true;
            }

            float stability = giantActive && _giantEncounter != null
                ? _giantEncounter.OverallProgress01
                : WantsNormalTreatment ? _poseMatchEvaluator.Progress01 : 0f;
            _audioController.CleanseStability = stability;
            _lightController.CleanseStability = stability;

            bool treatmentComplete = giantActive && _giantEncounter != null
                ? _giantEncounter.IsComplete
                : WantsNormalTreatment && _poseMatchEvaluator.IsResolved;
            if (treatmentComplete && !_treatmentResolved)
            {
                CompleteTreatmentPrototype();
            }

            if (_treatmentResolved)
            {
                _audioController.Volume = 0f;
                _lightController.CoreIntensity = 0f;
            }

            if (WantsNormalTreatment || giantActive)
            {
                UpdateTreatmentHaptics();
            }
            else
            {
                StopTreatmentHaptics();
            }
        }

        private void CompleteTreatmentPrototype()
        {
            _treatmentResolved = true;
            _completionEffect?.Complete(_tinnitusTransform, _audioController, _lightController);
            StopTreatmentHaptics();
        }

        private void ResetTreatmentPrototype()
        {
            if (_poseMatchEvaluator == null || _audioController == null || _lightController == null)
            {
                return;
            }

            _poseMatchEvaluator.ResetProgress();
            _audioController.CleanseStability = 0f;
            _lightController.CleanseStability = 0f;
            if (_hasTreatmentRestoreLevels)
            {
                _audioController.Volume = _treatmentRestoreVolume;
                _lightController.CoreIntensity = _treatmentRestoreCoreIntensity;
            }

            _treatmentResolved = false;
            _giantEncounter?.StopEncounter();
            _wasInsideTreatmentTolerance = false;
            _treatmentLockPulseUntilRealtime = 0f;
            StopTreatmentHaptics();
        }

        private void CaptureCurrentTreatmentTarget()
        {
            if (_poseMatchEvaluator == null)
            {
                return;
            }

            ResetTreatmentPrototype();
            _giantEncounter?.StopEncounter();
            _poseMatchEvaluator.CaptureCurrentPoseAsTarget();
        }

        private void StartGiantEncounterPrototype()
        {
            if (_giantEncounter == null || _poseMatchEvaluator == null)
            {
                return;
            }

            ResetTreatmentPrototype();
            _giantEncounter.TargetCenterCameraSpace = _poseMatchEvaluator.HasCurrentPose
                ? _poseMatchEvaluator.CurrentCameraSpacePosition
                : _poseMatchEvaluator.TargetCameraSpacePosition;
            _giantEncounter.StartEncounter();
        }

        private void UpdateTreatmentHaptics()
        {
            if (!enableTreatmentHaptics || _poseMatchEvaluator == null || _treatmentResolved)
            {
                if (_treatmentHapticsActive)
                {
                    StopTreatmentHaptics();
                }

                _wasInsideTreatmentTolerance = false;
                return;
            }

            bool insideTolerance = _poseMatchEvaluator.IsInsideTolerance;
            if (insideTolerance && !_wasInsideTreatmentTolerance)
            {
                _treatmentLockPulseUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(0.04f, treatmentLockPulseSeconds);
                SetTreatmentHaptics(0.82f, 1f, "lock pulse");
            }

            _wasInsideTreatmentTolerance = insideTolerance;

            if (Time.realtimeSinceStartup < _treatmentLockPulseUntilRealtime)
            {
                return;
            }

            if (Time.realtimeSinceStartup < _nextTreatmentHapticRefreshAtRealtime)
            {
                return;
            }

            _nextTreatmentHapticRefreshAtRealtime = Time.realtimeSinceStartup + 0.045f;

            if (insideTolerance)
            {
                float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * 2f * Mathf.Max(0.1f, treatmentHealingPulseHz)) * 0.5f;
                float progress = _poseMatchEvaluator.Progress01;
                float match = Mathf.Clamp01(_poseMatchEvaluator.TotalMatch01);
                float low = Mathf.Lerp(0.22f, 0.46f, pulse) + progress * 0.08f;
                float high = Mathf.Lerp(0.20f, 0.50f, pulse) + match * 0.1f;
                SetTreatmentHaptics(low, high, "cleansing pulse");
                return;
            }

            float approach = Mathf.Clamp01(_poseMatchEvaluator.TotalMatch01);
            if (approach <= 0.04f)
            {
                StopTreatmentHaptics();
                _treatmentHapticStatus = "approach too far";
                return;
            }

            float curved = approach * approach;
            SetTreatmentHaptics(
                Mathf.Lerp(0.02f, 0.24f, curved),
                Mathf.Lerp(0.05f, 0.48f, curved),
                "approach");
        }

        private void SetTreatmentHaptics(float lowMotor, float highMotor, string status)
        {
            Gamepad gamepad = ResolveTreatmentGamepad();
            if (gamepad == null)
            {
                _treatmentHapticsActive = false;
                _treatmentHapticStatus = "no gamepad";
                return;
            }

            InputSystem.ResumeHaptics();
            gamepad.SetMotorSpeeds(Mathf.Clamp01(lowMotor), Mathf.Clamp01(highMotor));
            _treatmentHapticsActive = true;
            _treatmentHapticStatus = $"{status} {Mathf.Clamp01(lowMotor):0.00}/{Mathf.Clamp01(highMotor):0.00}";
        }

        private void StopTreatmentHaptics()
        {
            if (!_treatmentHapticsActive)
            {
                return;
            }

            Gamepad gamepad = ResolveTreatmentGamepad();
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
            else
            {
                InputSystem.ResetHaptics();
            }

            _treatmentHapticsActive = false;
            _treatmentHapticStatus = "stopped";
        }

        private static Gamepad ResolveTreatmentGamepad()
        {
            return Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null);
        }

        private void UpdatePadPoseGhosts()
        {
            if (!enablePadTreatmentPrototype || _listenerTransform == null || _poseMatchEvaluator == null)
            {
                return;
            }

            if (_currentPadGhostTransform != null && _padPoseProvider != null && _padPoseProvider.HasPose)
            {
                _currentPadGhostTransform.position = CameraSpaceToWorld(_padPoseProvider.CameraSpacePosition);
                _currentPadGhostTransform.rotation = _listenerTransform.rotation * _padPoseProvider.RelativeRotation;
                if (_currentPadGhostRenderer != null)
                {
                    _currentPadGhostRenderer.sharedMaterial.color = _poseMatchEvaluator.IsInsideTolerance
                        ? new Color(0.25f, 1f, 0.38f, 1f)
                        : new Color(1f, 0.56f, 0.14f, 1f);
                }
            }

            if (_targetPadGhostTransform != null)
            {
                _targetPadGhostTransform.position = CameraSpaceToWorld(_poseMatchEvaluator.TargetCameraSpacePosition);
                _targetPadGhostTransform.rotation = _listenerTransform.rotation * Quaternion.Euler(
                    -_poseMatchEvaluator.TargetPitchDegrees,
                    _poseMatchEvaluator.TargetYawDegrees,
                    -_poseMatchEvaluator.TargetRollDegrees);
                if (_targetPadGhostRenderer != null)
                {
                    _targetPadGhostRenderer.sharedMaterial.color = _treatmentResolved
                        ? new Color(0.18f, 0.35f, 0.24f, 1f)
                        : new Color(0.15f, 1f, 0.45f, 1f);
                }
            }
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

        private void DrawTreatmentPanel(Rect rect)
        {
            if (!WantsPadTreatment || _poseMatchEvaluator == null)
            {
                return;
            }

            GUILayout.BeginArea(rect, WantsGiantTreatment ? "Giant Tinnitus Pose Treatment" : "Normal Tinnitus Pose Treatment", GUI.skin.window);
            GUILayout.Label("Run PadTracker and keep pad position + yaw/pitch/roll inside the target range.");
            GUILayout.Label($"Pad pose: {_poseMatchEvaluator.HasCurrentPose}  position fresh: {(_padPoseProvider != null && _padPoseProvider.HasFreshPosition)}  imu fresh: {(_padPoseProvider != null && _padPoseProvider.HasFreshImu)}");
            GUILayout.Label($"Yaw source: {BuildPadYawSourceLabel()}  resolved: {_treatmentResolved}");
            enableTreatmentHaptics = GUILayout.Toggle(enableTreatmentHaptics, $"Treatment haptics: {_treatmentHapticStatus}");

            if (WantsNormalTreatment)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Capture Current As Target", GUILayout.Height(28f)))
                {
                    CaptureCurrentTreatmentTarget();
                }

                if (GUILayout.Button("Reset", GUILayout.Height(28f)))
                {
                    ResetTreatmentPrototype();
                }

                if (GUILayout.Button("Resolve Now", GUILayout.Height(28f)))
                {
                    _poseMatchEvaluator.ResetProgress();
                    _poseMatchEvaluator.SetTargetPose(
                        _poseMatchEvaluator.CurrentCameraSpacePosition,
                        _poseMatchEvaluator.CurrentYawDegrees,
                        _poseMatchEvaluator.CurrentPitchDegrees,
                        _poseMatchEvaluator.CurrentRollDegrees);
                    CompleteTreatmentPrototype();
                }

                GUILayout.EndHorizontal();
            }

            if (WantsGiantTreatment && _giantEncounter != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Giant 3-Stage", GUILayout.Height(28f)))
                {
                    StartGiantEncounterPrototype();
                }

                if (GUILayout.Button("Stop Giant", GUILayout.Height(28f)))
                {
                    _giantEncounter.StopEncounter();
                    ResetTreatmentPrototype();
                }

                GUILayout.EndHorizontal();
                GUILayout.Label($"Giant: running {_giantEncounter.IsRunning}  complete {_giantEncounter.IsComplete}  stage {_giantEncounter.StageNumber}/3  stage progress {_giantEncounter.StageProgress01:0.00}  overall {_giantEncounter.OverallProgress01:0.00}");
            }

            treatmentSeconds = TreatmentSlider("treatment seconds", treatmentSeconds, 0.5f, 10f);
            _poseMatchEvaluator.PositionToleranceMeters = TreatmentSlider("position tolerance m", _poseMatchEvaluator.PositionToleranceMeters, 0.03f, 0.4f);
            _poseMatchEvaluator.YawToleranceDegrees = TreatmentSlider("yaw tolerance deg", _poseMatchEvaluator.YawToleranceDegrees, 3f, 70f);
            _poseMatchEvaluator.PitchToleranceDegrees = TreatmentSlider("pitch tolerance deg", _poseMatchEvaluator.PitchToleranceDegrees, 3f, 70f);
            _poseMatchEvaluator.RollToleranceDegrees = TreatmentSlider("roll tolerance deg", _poseMatchEvaluator.RollToleranceDegrees, 3f, 70f);
            _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = TreatmentSlider("haptic match radius", _poseMatchEvaluator.MatchFeedbackRadiusMultiplier, 1f, 4f);

            GUILayout.Space(6f);
            GUILayout.Label($"Current pos: {_poseMatchEvaluator.CurrentCameraSpacePosition.x:0.000}, {_poseMatchEvaluator.CurrentCameraSpacePosition.y:0.000}, {_poseMatchEvaluator.CurrentCameraSpacePosition.z:0.000}");
            GUILayout.Label($"Target pos: {_poseMatchEvaluator.TargetCameraSpacePosition.x:0.000}, {_poseMatchEvaluator.TargetCameraSpacePosition.y:0.000}, {_poseMatchEvaluator.TargetCameraSpacePosition.z:0.000}");
            GUILayout.Label($"Current Y/P/R: {_poseMatchEvaluator.CurrentYawDegrees:0.0} / {_poseMatchEvaluator.CurrentPitchDegrees:0.0} / {_poseMatchEvaluator.CurrentRollDegrees:0.0}");
            GUILayout.Label($"Target Y/P/R: {_poseMatchEvaluator.TargetYawDegrees:0.0} / {_poseMatchEvaluator.TargetPitchDegrees:0.0} / {_poseMatchEvaluator.TargetRollDegrees:0.0}");
            GUILayout.Label($"Errors pos/y/p/r: {_poseMatchEvaluator.PositionErrorMeters:0.000}m / {_poseMatchEvaluator.YawErrorDegrees:0.0} / {_poseMatchEvaluator.PitchErrorDegrees:0.0} / {_poseMatchEvaluator.RollErrorDegrees:0.0}");
            GUILayout.Label($"Inside tolerance: {_poseMatchEvaluator.IsInsideTolerance}");
            DrawReadOnlyBar("position match", _poseMatchEvaluator.PositionMatch01);
            DrawReadOnlyBar("rotation match", _poseMatchEvaluator.RotationMatch01);
            DrawReadOnlyBar("total match", _poseMatchEvaluator.TotalMatch01);
            DrawReadOnlyBar("cleanse progress", _poseMatchEvaluator.Progress01);
            GUILayout.Label("If the pad leaves range, progress pauses but does not reset.");

            GUILayout.EndArea();
        }

        private void DrawBellTutorialPanel(Rect rect)
        {
            if (_bellTutorial == null)
            {
                return;
            }

            GUILayout.BeginArea(rect, "Bell Gaze Tutorial", GUI.skin.window);
            GUILayout.Label("Head rotation is the main gaze input. Keep looking at the moving bell.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Bell Tutorial", GUILayout.Height(28f)))
            {
                _bellTutorial.StartTutorial();
            }

            if (GUILayout.Button("Reset Bell Tutorial", GUILayout.Height(28f)))
            {
                _bellTutorial.ResetTutorial();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label($"Phase: {_bellTutorial.PhaseName}  complete: {_bellTutorial.IsComplete}");
            GUILayout.Label($"Gaze angle: {_bellTutorial.GazeAngleDegrees:0.0}  progress: {_bellTutorial.GazeProgress01:0.00}");
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

        private static float TreatmentSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.000}", GUILayout.Width(180f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(280f));
            GUILayout.EndHorizontal();
            return nextValue;
        }

        private static void DrawReadOnlyBar(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(180f));
            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            GUILayout.HorizontalSlider(Mathf.Clamp01(value), 0f, 1f, GUILayout.Width(280f));
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
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
                renderer.sharedMaterial = CreateMaterial(color);
            }
        }

        private Vector3 CameraSpaceToWorld(Vector3 cameraSpacePosition)
        {
            return _listenerTransform.position
                + (_listenerTransform.right * cameraSpacePosition.x)
                + (_listenerTransform.up * cameraSpacePosition.y)
                + (_listenerTransform.forward * cameraSpacePosition.z);
        }

        private string BuildPadYawSourceLabel()
        {
            if (_padPoseProvider == null)
            {
                return "None";
            }

            if (_padPoseProvider.UsingCameraYaw)
            {
                return "Camera";
            }

            if (_padPoseProvider.UsingImuYawFallback)
            {
                return "IMU fallback";
            }

            if (_padPoseProvider.UsingHeldYaw)
            {
                return "Hold";
            }

            return "None";
        }

        private static Material CreateMaterial(Color color)
        {
            return new Material(Shader.Find("Standard")) { color = color };
        }
    }
}
