using System;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoDirector : MonoBehaviour
    {
        [SerializeField] private FinalDemoTuningProfile tuningProfile;
        [SerializeField] private FinalDemoCueLibrary cueLibrary;
        [SerializeField] private Transform playerRig;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private BellRingerSimpleMoveLookController movementController;
        [SerializeField] private FinalDemoInputStatus inputStatus;
        [SerializeField] private FinalDemoOperatorControls operatorControls;
        [SerializeField] private bool createMissingRuntimeObjects = true;
        [SerializeField] private bool showRuntimeGizmos = true;

        private readonly FinalDemoStage[] _stageOrder =
        {
            FinalDemoStage.Preflight,
            FinalDemoStage.OpeningAmbience,
            FinalDemoStage.OpeningSilence,
            FinalDemoStage.OpeningCloseBell,
            FinalDemoStage.BellOrbit,
            FinalDemoStage.BellFollowOne,
            FinalDemoStage.BellFollowRain,
            FinalDemoStage.BellGaze,
            FinalDemoStage.BellAcquisition,
            FinalDemoStage.GeneralTinnitusOne,
            FinalDemoStage.GeneralTinnitusTwo,
            FinalDemoStage.BossApproach,
            FinalDemoStage.BossPatternOne,
            FinalDemoStage.BossPatternTwo,
            FinalDemoStage.BossPatternThree,
            FinalDemoStage.BossDefeat,
            FinalDemoStage.ForestEnding,
            FinalDemoStage.Complete,
        };

        private FinalDemoStage _currentStage = FinalDemoStage.Preflight;
        private float _stageStartedAtRealtime;
        private bool _playerMovementLocked;
        private bool _hasStarted;

        public FinalDemoStage CurrentStage => _currentStage;
        public int StageCount => _stageOrder.Length;
        public int StageIndex => Array.IndexOf(_stageOrder, _currentStage);
        public float StageElapsedSeconds => Time.realtimeSinceStartup - _stageStartedAtRealtime;
        public bool PlayerMovementLocked => _playerMovementLocked;
        public bool IsComplete => _currentStage == FinalDemoStage.Complete;
        public FinalDemoAssistLevel AssistLevel { get; private set; } = FinalDemoAssistLevel.Gentle;
        public bool HrtfPreviewEnabled { get; private set; }
        public FinalDemoTuningProfile TuningProfile => tuningProfile;
        public FinalDemoCueLibrary CueLibrary => cueLibrary;
        public FinalDemoInputStatus InputStatus => inputStatus;
        public Transform PlayerRig => playerRig;

        private void Awake()
        {
            EnsureAssets();
            if (createMissingRuntimeObjects)
            {
                EnsureRuntimeObjects();
            }

            operatorControls ??= GetComponent<FinalDemoOperatorControls>();
            inputStatus ??= GetComponent<FinalDemoInputStatus>() ?? FindFirstObjectByType<FinalDemoInputStatus>();
            operatorControls?.Initialize(this, inputStatus);
            inputStatus?.RefreshReferences();
        }

        private void Start()
        {
            AssistLevel = tuningProfile != null ? tuningProfile.DefaultAssistLevel : FinalDemoAssistLevel.Gentle;
            EnterStage(FinalDemoStage.Preflight);
        }

        private void Update()
        {
            if (!_hasStarted && _currentStage != FinalDemoStage.Preflight)
            {
                _hasStarted = true;
            }

            if (tuningProfile != null &&
                tuningProfile.AutoAdvancePlaceholderStages &&
                _currentStage != FinalDemoStage.Preflight &&
                _currentStage != FinalDemoStage.Complete &&
                StageElapsedSeconds >= tuningProfile.PlaceholderStageSeconds)
            {
                ForceNextStage();
            }
        }

        public void StartDemo()
        {
            if (_currentStage == FinalDemoStage.Preflight)
            {
                EnterStage(FinalDemoStage.OpeningAmbience);
            }
        }

        public void ForceNextStage()
        {
            int currentIndex = Mathf.Max(0, StageIndex);
            int nextIndex = Mathf.Min(currentIndex + 1, _stageOrder.Length - 1);
            EnterStage(_stageOrder[nextIndex]);
        }

        public void ForceCompleteCurrentObjective()
        {
            ForceNextStage();
        }

        public void ResetCurrentStage()
        {
            EnterStage(_currentStage);
        }

        public void ResetToPreflight()
        {
            _hasStarted = false;
            EnterStage(FinalDemoStage.Preflight);
        }

        public void ToggleMovementLock()
        {
            SetPlayerMovementLocked(!_playerMovementLocked);
        }

        public void SetPlayerMovementLocked(bool locked)
        {
            _playerMovementLocked = locked;
            movementController ??= playerRig != null ? playerRig.GetComponent<BellRingerSimpleMoveLookController>() : FindFirstObjectByType<BellRingerSimpleMoveLookController>();
            if (movementController != null)
            {
                movementController.MovementEnabled = !locked;
            }
        }

        public void CycleAssistLevel()
        {
            AssistLevel = AssistLevel switch
            {
                FinalDemoAssistLevel.Normal => FinalDemoAssistLevel.Gentle,
                FinalDemoAssistLevel.Gentle => FinalDemoAssistLevel.MaxSafety,
                _ => FinalDemoAssistLevel.Normal,
            };
        }

        public void ToggleHrtfPreview()
        {
            HrtfPreviewEnabled = !HrtfPreviewEnabled;
        }

        public void RecenterHead()
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            inputStatus?.HeadTiltInputProvider?.Recenter();
        }

        public void RecenterPad()
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            inputStatus?.PadImuReceiver?.Recenter();
        }

        public void StopAllOutputs()
        {
            try
            {
                InputSystem.ResetHaptics();
            }
            catch
            {
            }

            HardwareBridge bridge = HardwareBridge.Instance;
            if (bridge != null)
            {
                bridge.ClearLedDisplay();
            }
        }

        public string BuildStageSummary()
        {
            return $"{CurrentStage}: {BuildObjectiveLabel(_currentStage)}";
        }

        private void EnterStage(FinalDemoStage nextStage)
        {
            _currentStage = nextStage;
            _stageStartedAtRealtime = Time.realtimeSinceStartup;
            SetPlayerMovementLocked(ShouldLockMovement(nextStage));
            MovePlaceholdersForStage(nextStage);
        }

        private void EnsureAssets()
        {
            if (tuningProfile == null)
            {
                tuningProfile = ScriptableObject.CreateInstance<FinalDemoTuningProfile>();
            }

            if (cueLibrary == null)
            {
                cueLibrary = ScriptableObject.CreateInstance<FinalDemoCueLibrary>();
            }
        }

        private void EnsureRuntimeObjects()
        {
            if (playerRig == null)
            {
                BellRingerSimpleMoveLookController existingController = FindFirstObjectByType<BellRingerSimpleMoveLookController>();
                playerRig = existingController != null ? existingController.transform : CreatePlayerRig();
            }

            playerCamera ??= playerRig != null ? playerRig.GetComponentInChildren<Camera>() : Camera.main;
            movementController ??= playerRig != null ? playerRig.GetComponent<BellRingerSimpleMoveLookController>() : null;

            if (inputStatus == null)
            {
                inputStatus = GetComponent<FinalDemoInputStatus>() ?? gameObject.AddComponent<FinalDemoInputStatus>();
            }

            if (operatorControls == null)
            {
                operatorControls = GetComponent<FinalDemoOperatorControls>() ?? gameObject.AddComponent<FinalDemoOperatorControls>();
            }

            EnsureHardwareRoot();
            EnsurePadInputRoot();
            EnsurePlaceholderWorld();
        }

        private Transform CreatePlayerRig()
        {
            GameObject rig = new GameObject("PlayerRig_FinalDemo");
            rig.transform.SetParent(transform);
            rig.transform.position = tuningProfile != null ? tuningProfile.PlayerStartPosition : new Vector3(0f, 1.6f, -1.5f);
            rig.transform.rotation = Quaternion.identity;

            playerCamera = rig.AddComponent<Camera>();
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(0.015f, 0.015f, 0.018f);
            playerCamera.fieldOfView = 68f;
            rig.AddComponent<AudioListener>();
            rig.AddComponent<HeadImuReceiver>();
            rig.AddComponent<HeadTiltInputProvider>();
            movementController = rig.AddComponent<BellRingerSimpleMoveLookController>();
            return rig.transform;
        }

        private void EnsureHardwareRoot()
        {
            if (FindFirstObjectByType<HardwareBridge>() != null)
            {
                return;
            }

            GameObject hardwareRoot = new GameObject("HardwareBridge_FinalDemo");
            hardwareRoot.transform.SetParent(transform);
            hardwareRoot.AddComponent<HardwareBridge>();
        }

        private void EnsurePadInputRoot()
        {
            if (FindFirstObjectByType<PadPoseProvider>() != null)
            {
                return;
            }

            GameObject padRoot = new GameObject("PadInput_FinalDemo");
            padRoot.transform.SetParent(transform);
            padRoot.AddComponent<PadTrackingReceiver>();
            padRoot.AddComponent<PadImuReceiver>();
            padRoot.AddComponent<PadPoseProvider>();
        }

        private void EnsurePlaceholderWorld()
        {
            EnsurePlaceholder("FinalDemo_BellPlaceholder", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 2.4f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.1f, 0.85f, 0.25f));
            EnsurePlaceholder("FinalDemo_TinnitusA", PrimitiveType.Sphere, new Vector3(-1.2f, 1.45f, 2.8f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.45f, 0.1f, 0.9f));
            EnsurePlaceholder("FinalDemo_TinnitusB", PrimitiveType.Sphere, new Vector3(1.25f, 1.35f, 3.1f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.45f, 0.1f, 0.9f));
            EnsurePlaceholder("FinalDemo_BossTinnitus", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 4.2f), new Vector3(0.42f, 0.42f, 0.42f), new Color(0.7f, 0.05f, 0.95f));
            EnsurePlaceholder("FinalDemo_RainFloor", PrimitiveType.Cube, new Vector3(0f, -0.02f, 2f), new Vector3(5f, 0.02f, 5f), new Color(0.02f, 0.07f, 0.22f));
            EnsurePlaceholder("FinalDemo_WallNoisePlane", PrimitiveType.Cube, new Vector3(0f, 1.25f, 5f), new Vector3(4.5f, 2.5f, 0.05f), new Color(0.18f, 0.36f, 0.38f));
        }

        private void MovePlaceholdersForStage(FinalDemoStage stage)
        {
            Transform bell = FindNamedTransform("FinalDemo_BellPlaceholder");
            if (bell != null)
            {
                bell.position = stage switch
                {
                    FinalDemoStage.BellOrbit => new Vector3(0.8f, 1.65f, 1.6f),
                    FinalDemoStage.BellFollowOne => new Vector3(-1.4f, 1.5f, 3.2f),
                    FinalDemoStage.BellFollowRain => new Vector3(1.4f, 1.5f, 3.6f),
                    FinalDemoStage.BellGaze => new Vector3(0f, 1.55f, 2.2f),
                    FinalDemoStage.BellAcquisition => new Vector3(0f, 1.45f, 1.2f),
                    _ => new Vector3(0f, 1.6f, 2.4f),
                };
            }
        }

        private GameObject EnsurePlaceholder(string objectName, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
        {
            Transform existing = FindNamedTransform(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject placeholder = GameObject.CreatePrimitive(primitiveType);
            placeholder.name = objectName;
            placeholder.transform.SetParent(transform);
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

            return placeholder;
        }

        private Transform FindNamedTransform(string objectName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool ShouldLockMovement(FinalDemoStage stage)
        {
            return stage switch
            {
                FinalDemoStage.Preflight => true,
                FinalDemoStage.OpeningAmbience => true,
                FinalDemoStage.OpeningSilence => true,
                FinalDemoStage.OpeningCloseBell => true,
                FinalDemoStage.BellOrbit => true,
                FinalDemoStage.BellGaze => true,
                FinalDemoStage.BellAcquisition => true,
                FinalDemoStage.BossPatternOne => true,
                FinalDemoStage.BossPatternTwo => true,
                FinalDemoStage.BossPatternThree => true,
                FinalDemoStage.BossDefeat => true,
                FinalDemoStage.Complete => true,
                _ => false,
            };
        }

        private static string BuildObjectiveLabel(FinalDemoStage stage)
        {
            return stage switch
            {
                FinalDemoStage.Preflight => "Check hardware, input freshness, and operator readiness.",
                FinalDemoStage.OpeningAmbience => "Start in darkness and establish the closed-eye audio space.",
                FinalDemoStage.OpeningSilence => "Drop into short silence before the first close bell.",
                FinalDemoStage.OpeningCloseBell => "Play the first nearby bell and show a minimal green point.",
                FinalDemoStage.BellOrbit => "Bell circles the player as an attractor.",
                FinalDemoStage.BellFollowOne => "Player follows the bell to target 1.",
                FinalDemoStage.BellFollowRain => "Player follows the bell through rain and wind masking.",
                FinalDemoStage.BellGaze => "Head gaze holds on bell three times.",
                FinalDemoStage.BellAcquisition => "Give the bell to the player.",
                FinalDemoStage.GeneralTinnitusOne => "Treat general tinnitus 1 with pad pose matching.",
                FinalDemoStage.GeneralTinnitusTwo => "Treat general tinnitus 2 with pad pose matching.",
                FinalDemoStage.BossApproach => "Approach the giant tinnitus and lock into the fight.",
                FinalDemoStage.BossPatternOne => "Boss weak point pattern 1, 7 seconds.",
                FinalDemoStage.BossPatternTwo => "Boss weak point pattern 2, 8 seconds.",
                FinalDemoStage.BossPatternThree => "Boss weak point pattern 3, 10 seconds.",
                FinalDemoStage.BossDefeat => "Shared resolve effect, echo, silence, LED clear.",
                FinalDemoStage.ForestEnding => "Forest ending and post-demo quiet.",
                FinalDemoStage.Complete => "Demo route complete.",
                _ => string.Empty,
            };
        }

        private void OnDrawGizmos()
        {
            if (!showRuntimeGizmos)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(0f, 1.6f, 2.4f), 0.25f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(0f, 1.6f, 4.2f), 0.45f);
        }
    }
}
