using System;
using BellRinger.Audio;
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
        [SerializeField] private FinalDemoAudioRouter audioRouter;
        [SerializeField] private FinalDemoLightRouter lightRouter;
        [SerializeField] private FinalDemoHapticRouter hapticRouter;
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
        private bool _stageOneShotPlayed;
        private bool _stageNarrationPlayed;
        private bool _rainLoopStarted;
        private float _nextBellCallAtRealtime;
        private float _nextRainDropAtRealtime;
        private float _nextRainLightAtRealtime;
        private float _rainStartedAtRealtime;
        private float _lastBellAssistAtRealtime;
        private float _lastPadShakeAssistAtRealtime;
        private float _currentBellFollowDistance = float.PositiveInfinity;
        private float _currentRainIntensity;
        private int _bellGazeSuccessCount;
        private float _bellGazeProgressSeconds;
        private float _bellGazeAngleDegrees = 180f;
        private float _currentBellGazeConeDegrees;
        private bool _bellGazeMoving;
        private Vector3 _bellGazeMoveStartPosition;
        private Vector3 _bellGazeMoveTargetPosition;
        private float _bellGazeMoveStartedAtRealtime;
        private bool _stageOutputCleared;
        private PadPoseMatchEvaluator _poseMatchEvaluator;
        private TinnitusAudioController _tinnitusAudioController;
        private bool _wasTinnitusInsideTolerance;
        private float _nextTinnitusLightAtRealtime;
        private float _nextTinnitusHapticAtRealtime;
        private float _currentTinnitusGazeAngleDegrees = 180f;
        private bool _bossWasInsideTolerance;
        private int _bossPatternFailureCount;
        private float _nextBossLightAtRealtime;
        private float _nextBossHapticAtRealtime;
        private float _currentBossApproachDistance = float.PositiveInfinity;
        private float _currentBossPatternProgress01;
        private float _currentBossPatternTargetMatch01;
        private int _currentBossPatternIndex = -1;
        private float _nextForestBellAtRealtime;
        private float _currentForestDistance = float.PositiveInfinity;

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
        public FinalDemoAudioRouter AudioRouter => audioRouter;
        public FinalDemoLightRouter LightRouter => lightRouter;
        public FinalDemoHapticRouter HapticRouter => hapticRouter;
        public Transform PlayerRig => playerRig;
        public string CurrentObjectiveLabel => BuildObjectiveLabel(_currentStage);
        public float CurrentBellFollowDistance => _currentBellFollowDistance;
        public float CurrentRainIntensity => _currentRainIntensity;
        public bool RainLoopStarted => _rainLoopStarted;
        public int BellGazeSuccessCount => _bellGazeSuccessCount;
        public float BellGazeProgress01 => tuningProfile != null ? Mathf.Clamp01(_bellGazeProgressSeconds / tuningProfile.BellGazeHoldSeconds) : 0f;
        public float BellGazeAngleDegrees => _bellGazeAngleDegrees;
        public float CurrentBellGazeConeDegrees => _currentBellGazeConeDegrees;
        public float GeneralTinnitusProgress01 => _poseMatchEvaluator != null ? _poseMatchEvaluator.Progress01 : 0f;
        public float GeneralTinnitusMatch01 => _poseMatchEvaluator != null ? _poseMatchEvaluator.TotalMatch01 : 0f;
        public bool GeneralTinnitusInsideTolerance => _poseMatchEvaluator != null && _poseMatchEvaluator.IsInsideTolerance;
        public float CurrentBossApproachDistance => _currentBossApproachDistance;
        public float CurrentBossPatternProgress01 => _currentBossPatternProgress01;
        public float CurrentBossPatternTargetMatch01 => _currentBossPatternTargetMatch01;
        public int BossPatternFailureCount => _bossPatternFailureCount;
        public float CurrentForestDistance => _currentForestDistance;
        public float CurrentObjectiveProgress01 => ResolveCurrentObjectiveProgress01();

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

            TickCurrentStage();

            if (tuningProfile != null &&
                tuningProfile.AutoAdvancePlaceholderStages &&
                !IsImplementedTimedStage(_currentStage) &&
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
            audioRouter ??= FindFirstObjectByType<FinalDemoAudioRouter>();
            audioRouter?.SetSpatializerEnabled(HrtfPreviewEnabled);
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

            audioRouter?.StopAllCues();
            hapticRouter?.StopAllHaptics();
            lightRouter?.Clear();
        }

        public string BuildStageSummary()
        {
            string summary = $"{CurrentStage}: {BuildObjectiveLabel(_currentStage)}";
            if (_currentStage == FinalDemoStage.BellFollowOne || _currentStage == FinalDemoStage.BellFollowRain)
            {
                float arrivalRadius = tuningProfile != null ? tuningProfile.BellArrivalRadius : 0f;
                summary += $" distance={_currentBellFollowDistance:0.00}/{arrivalRadius:0.00}m";
                if (_currentStage == FinalDemoStage.BellFollowRain)
                {
                    summary += $" rain={_currentRainIntensity:0.00}";
                }
            }
            else if (_currentStage == FinalDemoStage.BellGaze)
            {
                int required = tuningProfile != null ? tuningProfile.BellGazeRequiredSuccesses : 3;
                summary += $" gaze={_bellGazeSuccessCount}/{required} progress={BellGazeProgress01:0.00} angle={_bellGazeAngleDegrees:0.0}/{_currentBellGazeConeDegrees:0.0}";
            }
            else if (_currentStage == FinalDemoStage.GeneralTinnitusOne || _currentStage == FinalDemoStage.GeneralTinnitusTwo)
            {
                summary += $" match={GeneralTinnitusMatch01:0.00} cleanse={GeneralTinnitusProgress01:0.00} angle={_currentTinnitusGazeAngleDegrees:0.0}";
            }
            else if (_currentStage == FinalDemoStage.BossApproach)
            {
                float radius = tuningProfile != null ? tuningProfile.BossApproachRadius : 0f;
                summary += $" distance={_currentBossApproachDistance:0.00}/{radius:0.00}m";
            }
            else if (_currentStage == FinalDemoStage.BossPatternOne || _currentStage == FinalDemoStage.BossPatternTwo || _currentStage == FinalDemoStage.BossPatternThree)
            {
                summary += $" pattern={_currentBossPatternIndex + 1}/3 progress={_currentBossPatternProgress01:0.00} match={_currentBossPatternTargetMatch01:0.00} fails={_bossPatternFailureCount}";
            }
            else if (_currentStage == FinalDemoStage.ForestEnding)
            {
                float radius = tuningProfile != null ? tuningProfile.ForestBellArrivalRadius : 0f;
                summary += $" distance={_currentForestDistance:0.00}/{radius:0.00}m";
            }

            return summary;
        }

        public bool TryGetBellWorldPosition(out Vector3 worldPosition)
        {
            Transform bell = FindNamedTransform("FinalDemo_BellPlaceholder");
            if (bell != null)
            {
                worldPosition = bell.position;
                return true;
            }

            worldPosition = default;
            return false;
        }

        public bool TryGetBossWorldPosition(out Vector3 worldPosition)
        {
            Transform boss = FindNamedTransform("FinalDemo_BossTinnitus");
            if (boss != null)
            {
                worldPosition = boss.position;
                return true;
            }

            worldPosition = ResolveBossPosition();
            return _currentStage == FinalDemoStage.BossApproach ||
                   _currentStage == FinalDemoStage.BossPatternOne ||
                   _currentStage == FinalDemoStage.BossPatternTwo ||
                   _currentStage == FinalDemoStage.BossPatternThree ||
                   _currentStage == FinalDemoStage.BossDefeat;
        }

        public bool TryGetCurrentTinnitusWorldPosition(out Vector3 worldPosition)
        {
            if (_currentStage == FinalDemoStage.GeneralTinnitusOne || _currentStage == FinalDemoStage.GeneralTinnitusTwo)
            {
                worldPosition = ResolveGeneralTinnitusWorldPosition(_currentStage);
                return true;
            }

            worldPosition = default;
            return false;
        }

        private void EnterStage(FinalDemoStage nextStage)
        {
            _currentStage = nextStage;
            _stageStartedAtRealtime = Time.realtimeSinceStartup;
            audioRouter?.StopAllCues();
            hapticRouter?.StopAllHaptics();
            lightRouter?.Clear();
            StopProceduralTinnitus();
            SetPlayerMovementLocked(ShouldLockMovement(nextStage));
            MovePlaceholdersForStage(nextStage);
            _stageOneShotPlayed = false;
            _stageNarrationPlayed = false;
            _stageOutputCleared = false;
            _rainLoopStarted = false;
            _rainStartedAtRealtime = 0f;
            _nextRainDropAtRealtime = Time.realtimeSinceStartup;
            _nextRainLightAtRealtime = Time.realtimeSinceStartup;
            _lastBellAssistAtRealtime = Time.realtimeSinceStartup - 999f;
            _lastPadShakeAssistAtRealtime = Time.realtimeSinceStartup - 999f;
            _currentBellFollowDistance = float.PositiveInfinity;
            _currentRainIntensity = 0f;
            _bellGazeProgressSeconds = 0f;
            _bellGazeAngleDegrees = 180f;
            _currentBellGazeConeDegrees = tuningProfile != null ? tuningProfile.BellGazeConeDegrees : 14f;
            _bellGazeMoving = false;
            _wasTinnitusInsideTolerance = false;
            _nextTinnitusLightAtRealtime = Time.realtimeSinceStartup;
            _nextTinnitusHapticAtRealtime = Time.realtimeSinceStartup;
            _currentTinnitusGazeAngleDegrees = 180f;
            _bossWasInsideTolerance = false;
            _bossPatternFailureCount = 0;
            _nextBossLightAtRealtime = Time.realtimeSinceStartup;
            _nextBossHapticAtRealtime = Time.realtimeSinceStartup;
            _currentBossApproachDistance = float.PositiveInfinity;
            _currentBossPatternProgress01 = 0f;
            _currentBossPatternTargetMatch01 = 0f;
            _currentBossPatternIndex = -1;
            _nextForestBellAtRealtime = Time.realtimeSinceStartup;
            _currentForestDistance = float.PositiveInfinity;
            _nextBellCallAtRealtime = Time.realtimeSinceStartup;

            if (nextStage == FinalDemoStage.OpeningAmbience)
            {
                Vector3 ambiencePosition = ResolvePlayerRelativePosition(Vector3.forward * 2.4f);
                audioRouter?.StartLoop(FinalDemoCueId.BellOpeningOrbit, ambiencePosition, 0f);
            }
            else if (nextStage == FinalDemoStage.BellGaze)
            {
                BeginBellGazeStage();
            }
            else if (nextStage == FinalDemoStage.GeneralTinnitusOne || nextStage == FinalDemoStage.GeneralTinnitusTwo)
            {
                BeginGeneralTinnitusStage(nextStage);
            }
            else if (nextStage == FinalDemoStage.BossApproach)
            {
                BeginBossApproach();
            }
            else if (nextStage == FinalDemoStage.BossPatternOne || nextStage == FinalDemoStage.BossPatternTwo || nextStage == FinalDemoStage.BossPatternThree)
            {
                BeginBossPattern(nextStage);
            }
            else if (nextStage == FinalDemoStage.BossDefeat)
            {
                BeginBossDefeat();
            }
            else if (nextStage == FinalDemoStage.ForestEnding)
            {
                BeginForestEnding();
            }
        }

        private void TickCurrentStage()
        {
            switch (_currentStage)
            {
                case FinalDemoStage.OpeningAmbience:
                    TickOpeningAmbience();
                    break;
                case FinalDemoStage.OpeningSilence:
                    TickOpeningSilence();
                    break;
                case FinalDemoStage.OpeningCloseBell:
                    TickOpeningCloseBell();
                    break;
                case FinalDemoStage.BellOrbit:
                    TickBellOrbit();
                    break;
                case FinalDemoStage.BellFollowOne:
                    TickBellFollow(false);
                    break;
                case FinalDemoStage.BellFollowRain:
                    TickBellFollow(true);
                    break;
                case FinalDemoStage.BellGaze:
                    TickBellGaze();
                    break;
                case FinalDemoStage.BellAcquisition:
                    TickBellAcquisition();
                    break;
                case FinalDemoStage.GeneralTinnitusOne:
                case FinalDemoStage.GeneralTinnitusTwo:
                    TickGeneralTinnitus();
                    break;
                case FinalDemoStage.BossApproach:
                    TickBossApproach();
                    break;
                case FinalDemoStage.BossPatternOne:
                case FinalDemoStage.BossPatternTwo:
                case FinalDemoStage.BossPatternThree:
                    TickBossPattern();
                    break;
                case FinalDemoStage.BossDefeat:
                    TickBossDefeat();
                    break;
                case FinalDemoStage.ForestEnding:
                    TickForestEnding();
                    break;
            }
        }

        private void TickOpeningAmbience()
        {
            if (tuningProfile == null)
            {
                return;
            }

            float fade = Mathf.Clamp01(StageElapsedSeconds / tuningProfile.OpeningAmbienceFadeSeconds);
            audioRouter?.SetLoopVolumeScale(FinalDemoCueId.BellOpeningOrbit, tuningProfile.OpeningAmbienceVolume * fade);

            if (StageElapsedSeconds >= tuningProfile.OpeningAmbienceSeconds)
            {
                ForceNextStage();
            }
        }

        private float ResolveCurrentObjectiveProgress01()
        {
            if (tuningProfile == null)
            {
                return 0f;
            }

            return _currentStage switch
            {
                FinalDemoStage.Preflight => 0f,
                FinalDemoStage.OpeningAmbience => Mathf.Clamp01(StageElapsedSeconds / tuningProfile.OpeningAmbienceSeconds),
                FinalDemoStage.OpeningSilence => Mathf.Clamp01(StageElapsedSeconds / tuningProfile.OpeningSilenceSeconds),
                FinalDemoStage.OpeningCloseBell => Mathf.Clamp01(StageElapsedSeconds / tuningProfile.OpeningCloseBellSeconds),
                FinalDemoStage.BellOrbit => Mathf.Clamp01(StageElapsedSeconds / tuningProfile.BellOrbitSeconds),
                FinalDemoStage.BellFollowOne => 1f - Mathf.Clamp01(_currentBellFollowDistance / Mathf.Max(tuningProfile.BellArrivalRadius * 4f, 0.1f)),
                FinalDemoStage.BellFollowRain => 1f - Mathf.Clamp01(_currentBellFollowDistance / Mathf.Max(tuningProfile.BellArrivalRadius * 4f, 0.1f)),
                FinalDemoStage.BellGaze => Mathf.Clamp01((_bellGazeSuccessCount + BellGazeProgress01) / Mathf.Max(1f, tuningProfile.BellGazeRequiredSuccesses)),
                FinalDemoStage.BellAcquisition => Mathf.Clamp01(StageElapsedSeconds / Mathf.Max(0.1f, tuningProfile.BellAcquisitionEffectSeconds + tuningProfile.BellAcquisitionTransitionSilenceSeconds)),
                FinalDemoStage.GeneralTinnitusOne => GeneralTinnitusProgress01,
                FinalDemoStage.GeneralTinnitusTwo => GeneralTinnitusProgress01,
                FinalDemoStage.BossApproach => 1f - Mathf.Clamp01(_currentBossApproachDistance / Mathf.Max(tuningProfile.BossApproachRadius * 4f, 0.1f)),
                FinalDemoStage.BossPatternOne => Mathf.Clamp01((_currentBossPatternIndex + _currentBossPatternProgress01) / 3f),
                FinalDemoStage.BossPatternTwo => Mathf.Clamp01((_currentBossPatternIndex + _currentBossPatternProgress01) / 3f),
                FinalDemoStage.BossPatternThree => Mathf.Clamp01((_currentBossPatternIndex + _currentBossPatternProgress01) / 3f),
                FinalDemoStage.BossDefeat => Mathf.Clamp01(StageElapsedSeconds / Mathf.Max(0.1f, tuningProfile.BossDefeatEffectSeconds + tuningProfile.BossDefeatSilenceSeconds)),
                FinalDemoStage.ForestEnding => Mathf.Max(
                    1f - Mathf.Clamp01(_currentForestDistance / Mathf.Max(tuningProfile.ForestBellArrivalRadius * 4f, 0.1f)),
                    Mathf.Clamp01(StageElapsedSeconds / tuningProfile.ForestAutoEndSeconds)),
                FinalDemoStage.Complete => 1f,
                _ => 0f,
            };
        }

        private void TickOpeningSilence()
        {
            if (tuningProfile != null && StageElapsedSeconds >= tuningProfile.OpeningSilenceSeconds)
            {
                ForceNextStage();
            }
        }

        private void TickOpeningCloseBell()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = ResolvePlayerRelativePosition(tuningProfile.OpeningCloseBellOffset);
            MoveBellPlaceholder(bellPosition);

            if (!_stageOneShotPlayed)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.OpeningCloseBellVolume);
                lightRouter?.ShowBellPoint(bellPosition, tuningProfile.OpeningCloseBellLedIntensity, false);
                hapticRouter?.TriggerBellAssistPulse();
                _stageOneShotPlayed = true;
            }

            if (StageElapsedSeconds >= tuningProfile.OpeningCloseBellSeconds)
            {
                ForceNextStage();
            }
        }

        private void TickBellOrbit()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = ResolveBellOrbitPosition(StageElapsedSeconds, out float pointIntensity);
            MoveBellPlaceholder(bellPosition);

            if (Time.realtimeSinceStartup >= _nextBellCallAtRealtime)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.BellOrbitVolume);
                lightRouter?.ShowBellPoint(bellPosition, pointIntensity);
                _nextBellCallAtRealtime = Time.realtimeSinceStartup + tuningProfile.BellOrbitCallIntervalSeconds;
            }

            if (StageElapsedSeconds >= tuningProfile.BellOrbitSeconds)
            {
                ForceNextStage();
            }
        }

        private void TickBellFollow(bool rainStage)
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 targetPosition = rainStage ? tuningProfile.BellFollowTargetTwoPosition : tuningProfile.BellFollowTargetOnePosition;
            MoveBellPlaceholder(targetPosition);
            _currentBellFollowDistance = ResolvePlanarDistanceToPlayer(targetPosition);

            if (!_stageNarrationPlayed && !rainStage)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.NarrFollowBell, ResolvePlayerRelativePosition(Vector3.forward * 1.1f), 1f);
                _stageNarrationPlayed = true;
            }

            if (rainStage)
            {
                TickRainLayer();
            }

            bool assistOverdue = StageElapsedSeconds >= tuningProfile.BellAssistTimeoutSeconds;
            if (Time.realtimeSinceStartup >= _nextBellCallAtRealtime)
            {
                PlayBellFollowCall(targetPosition, assistOverdue);
                _nextBellCallAtRealtime = Time.realtimeSinceStartup + tuningProfile.BellFollowCallIntervalSeconds;
            }

            if (assistOverdue && Time.realtimeSinceStartup - _lastBellAssistAtRealtime >= tuningProfile.BellAssistRepeatSeconds)
            {
                TriggerBellAssist(targetPosition, FinalDemoCueId.BellStrongAssist);
            }

            TryPadShakeBellAssist(targetPosition);

            if (_currentBellFollowDistance <= tuningProfile.BellArrivalRadius)
            {
                if (rainStage)
                {
                    audioRouter?.StopLoop(FinalDemoCueId.RainLightBed);
                    audioRouter?.StopLoop(FinalDemoCueId.RainStrongBed);
                    _rainLoopStarted = false;
                    _currentRainIntensity = 0f;
                }

                ForceNextStage();
            }
        }

        private void TickRainLayer()
        {
            if (tuningProfile == null || playerRig == null)
            {
                return;
            }

            if (!_rainLoopStarted && ResolvePlanarDistance(playerRig.position, tuningProfile.RainZoneCenter) <= tuningProfile.RainZoneRadius)
            {
                _rainLoopStarted = true;
                _rainStartedAtRealtime = Time.realtimeSinceStartup;
                _nextRainDropAtRealtime = Time.realtimeSinceStartup;
                _nextRainLightAtRealtime = Time.realtimeSinceStartup;
                audioRouter?.StartLoop(FinalDemoCueId.RainLightBed, playerRig.position, 0f);
                audioRouter?.StartLoop(FinalDemoCueId.RainStrongBed, playerRig.position, 0f);
            }

            if (!_rainLoopStarted)
            {
                _currentRainIntensity = 0f;
                return;
            }

            float ramp = Mathf.Clamp01((Time.realtimeSinceStartup - _rainStartedAtRealtime) / tuningProfile.RainIntensityRampSeconds);
            float windRamp = Mathf.Clamp01((Time.realtimeSinceStartup - _rainStartedAtRealtime - tuningProfile.RainWindTextureDelaySeconds) / tuningProfile.RainIntensityRampSeconds);
            _currentRainIntensity = tuningProfile.RainMaxIntensity * ramp;
            float windTextureIntensity = tuningProfile.RainWindTextureMaxIntensity * windRamp;
            if (StageElapsedSeconds >= tuningProfile.BellAssistTimeoutSeconds)
            {
                _currentRainIntensity = Mathf.Min(_currentRainIntensity, tuningProfile.RainAssistVolumeFloor);
                windTextureIntensity = Mathf.Min(windTextureIntensity, tuningProfile.RainAssistVolumeFloor * 0.5f);
            }

            audioRouter?.SetLoopVolumeScale(FinalDemoCueId.RainLightBed, _currentRainIntensity);
            audioRouter?.SetLoopVolumeScale(FinalDemoCueId.RainStrongBed, windTextureIntensity);

            if (Time.realtimeSinceStartup >= _nextRainLightAtRealtime)
            {
                lightRouter?.ShowRainFloorBand(_currentRainIntensity);
                _nextRainLightAtRealtime = Time.realtimeSinceStartup + 0.18f;
            }

            if (Time.realtimeSinceStartup >= _nextRainDropAtRealtime)
            {
                float lateral = Mathf.Sin(Time.realtimeSinceStartup * 1.731f) * 0.95f;
                Vector3 dropPosition = ResolvePlayerRelativePosition(new Vector3(lateral, -0.7f, 1.1f));
                audioRouter?.PlayOneShot(FinalDemoCueId.RainCloseDrops, dropPosition, _currentRainIntensity);
                _nextRainDropAtRealtime = Time.realtimeSinceStartup + tuningProfile.RainCloseDropIntervalSeconds;
            }
        }

        private void PlayBellFollowCall(Vector3 targetPosition, bool assisted)
        {
            float volume = tuningProfile != null ? tuningProfile.BellFollowVolume : 0.6f;
            float intensity = tuningProfile != null ? tuningProfile.BellFollowLedIntensity : 0.6f;
            if (assisted && tuningProfile != null)
            {
                volume = Mathf.Clamp01(volume * tuningProfile.BellAssistGainMultiplier);
                intensity = Mathf.Clamp01(intensity * tuningProfile.BellAssistGainMultiplier);
            }

            audioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, targetPosition, volume);
            lightRouter?.ShowBellPoint(targetPosition, intensity);
        }

        private void TriggerBellAssist(Vector3 targetPosition, FinalDemoCueId cueId)
        {
            _lastBellAssistAtRealtime = Time.realtimeSinceStartup;
            float volume = tuningProfile != null ? Mathf.Clamp01(tuningProfile.BellFollowVolume * tuningProfile.BellAssistGainMultiplier) : 0.85f;
            float intensity = tuningProfile != null ? Mathf.Clamp01(tuningProfile.BellFollowLedIntensity * tuningProfile.BellAssistGainMultiplier) : 0.85f;
            audioRouter?.PlayOneShot(cueId, targetPosition, volume);
            lightRouter?.ShowBellPoint(targetPosition, intensity);
            hapticRouter?.TriggerBellAssistPulse();
        }

        private void TryPadShakeBellAssist(Vector3 targetPosition)
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            PadImuReceiver padImuReceiver = inputStatus != null ? inputStatus.PadImuReceiver : FindFirstObjectByType<PadImuReceiver>();
            if (padImuReceiver == null || !padImuReceiver.HasFreshSample || tuningProfile == null)
            {
                return;
            }

            if (padImuReceiver.MotionIntensity01 < tuningProfile.PadShakeAssistMotionThreshold)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _lastPadShakeAssistAtRealtime < tuningProfile.PadShakeAssistCooldownSeconds)
            {
                return;
            }

            _lastPadShakeAssistAtRealtime = Time.realtimeSinceStartup;
            TriggerBellAssist(targetPosition, FinalDemoCueId.BellPadShakeResponse);
        }

        private void BeginBellGazeStage()
        {
            _bellGazeSuccessCount = 0;
            _bellGazeProgressSeconds = 0f;
            _nextBellCallAtRealtime = Time.realtimeSinceStartup;
            audioRouter?.PlayOneShot(FinalDemoCueId.NarrLookBell, ResolvePlayerRelativePosition(Vector3.forward * 1.1f), 1f);
            BeginBellGazeCue(0, false);
        }

        private void TickBellGaze()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = UpdateBellGazeMove();
            TryPlayBellGazeCall(bellPosition);

            if (_bellGazeMoving)
            {
                return;
            }

            _currentBellGazeConeDegrees = ResolveBellGazeConeDegrees();
            _bellGazeAngleDegrees = ResolveGazeAngleTo(bellPosition);
            if (_bellGazeAngleDegrees <= _currentBellGazeConeDegrees)
            {
                _bellGazeProgressSeconds = Mathf.Min(tuningProfile.BellGazeHoldSeconds, _bellGazeProgressSeconds + Time.unscaledDeltaTime);
            }

            if (_bellGazeProgressSeconds >= tuningProfile.BellGazeHoldSeconds)
            {
                CompleteBellGazeHold(bellPosition);
            }
        }

        private void BeginBellGazeCue(int cueIndex, bool move)
        {
            Vector3 targetPosition = ResolveBellGazeCuePosition(cueIndex);
            _bellGazeProgressSeconds = 0f;
            _nextBellCallAtRealtime = Time.realtimeSinceStartup;

            if (move)
            {
                _bellGazeMoveStartPosition = GetBellPlaceholderPosition();
                _bellGazeMoveTargetPosition = targetPosition;
                _bellGazeMoveStartedAtRealtime = Time.realtimeSinceStartup;
                _bellGazeMoving = true;
                audioRouter?.PlayOneShot(FinalDemoCueId.BellMovementTexture, targetPosition, 0.55f);
            }
            else
            {
                _bellGazeMoving = false;
                _bellGazeMoveTargetPosition = targetPosition;
                MoveBellPlaceholder(targetPosition);
            }

            lightRouter?.ShowBellPoint(targetPosition, tuningProfile != null ? tuningProfile.BellGazeLedIntensity : 0.75f);
        }

        private Vector3 UpdateBellGazeMove()
        {
            if (!_bellGazeMoving || tuningProfile == null)
            {
                return GetBellPlaceholderPosition();
            }

            float move01 = Mathf.Clamp01((Time.realtimeSinceStartup - _bellGazeMoveStartedAtRealtime) / tuningProfile.BellGazeMoveSeconds);
            Vector3 position = Vector3.Lerp(_bellGazeMoveStartPosition, _bellGazeMoveTargetPosition, Smooth01(move01));
            MoveBellPlaceholder(position);

            if (move01 >= 1f)
            {
                _bellGazeMoving = false;
                _bellGazeProgressSeconds = 0f;
            }

            return position;
        }

        private void TryPlayBellGazeCall(Vector3 bellPosition)
        {
            if (tuningProfile == null || Time.realtimeSinceStartup < _nextBellCallAtRealtime)
            {
                return;
            }

            _nextBellCallAtRealtime = Time.realtimeSinceStartup + tuningProfile.BellGazeCallIntervalSeconds;
            audioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.BellFollowVolume);
            lightRouter?.ShowBellPoint(bellPosition, tuningProfile.BellGazeLedIntensity);
        }

        private void CompleteBellGazeHold(Vector3 bellPosition)
        {
            _bellGazeSuccessCount++;
            audioRouter?.PlayOneShot(FinalDemoCueId.BellGazeSuccess, bellPosition, 1f);
            lightRouter?.ShowBellPoint(bellPosition, 1f, false);
            hapticRouter?.TriggerBellAssistPulse();

            int required = tuningProfile != null ? tuningProfile.BellGazeRequiredSuccesses : 3;
            if (_bellGazeSuccessCount >= required)
            {
                ForceNextStage();
                return;
            }

            BeginBellGazeCue(_bellGazeSuccessCount, true);
        }

        private void TickBellAcquisition()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = ResolvePlayerRelativePosition(tuningProfile.BellAcquisitionLocalOffset);
            MoveBellPlaceholder(bellPosition);

            if (!_stageOneShotPlayed)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.BellAcquisition, bellPosition, 1f);
                lightRouter?.ShowBellPoint(bellPosition, 1f, false);
                hapticRouter?.TriggerBellAcquisitionPulse();
                _stageOneShotPlayed = true;
            }

            if (!_stageOutputCleared && StageElapsedSeconds >= tuningProfile.BellAcquisitionEffectSeconds)
            {
                lightRouter?.Clear();
                hapticRouter?.StopAllHaptics();
                _stageOutputCleared = true;
            }

            if (StageElapsedSeconds >= tuningProfile.BellAcquisitionEffectSeconds + tuningProfile.BellAcquisitionTransitionSilenceSeconds)
            {
                ForceNextStage();
            }
        }

        private void BeginGeneralTinnitusStage(FinalDemoStage stage)
        {
            if (tuningProfile == null)
            {
                return;
            }

            EnsurePoseMatchEvaluator();
            if (_poseMatchEvaluator != null)
            {
                _poseMatchEvaluator.PositionToleranceMeters = tuningProfile.GeneralTinnitusPositionToleranceMeters;
                _poseMatchEvaluator.YawToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.PitchToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.RollToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = tuningProfile.GeneralTinnitusMatchFeedbackRadiusMultiplier;
                _poseMatchEvaluator.TreatmentSeconds = tuningProfile.GeneralTinnitusTreatmentSeconds;

                Vector3 targetCameraSpace = stage == FinalDemoStage.GeneralTinnitusOne
                    ? tuningProfile.GeneralTinnitusOnePadTargetCameraSpace
                    : tuningProfile.GeneralTinnitusTwoPadTargetCameraSpace;
                Vector3 targetYawPitchRoll = stage == FinalDemoStage.GeneralTinnitusOne
                    ? tuningProfile.GeneralTinnitusOnePadTargetYawPitchRoll
                    : tuningProfile.GeneralTinnitusTwoPadTargetYawPitchRoll;
                _poseMatchEvaluator.SetTargetPose(targetCameraSpace, targetYawPitchRoll.x, targetYawPitchRoll.y, targetYawPitchRoll.z);
                _poseMatchEvaluator.ResetProgress();
            }

            Vector3 targetWorldPosition = ResolveGeneralTinnitusWorldPosition(stage);
            MoveGeneralTinnitusPlaceholder(stage, targetWorldPosition);
            StartProceduralTinnitus(targetWorldPosition);

            if (stage == FinalDemoStage.GeneralTinnitusOne)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.NarrFindTinnitusPose, ResolvePlayerRelativePosition(Vector3.forward * 1.1f), 1f);
            }
        }

        private void TickGeneralTinnitus()
        {
            if (tuningProfile == null)
            {
                return;
            }

            EnsurePoseMatchEvaluator();
            Vector3 targetWorldPosition = ResolveGeneralTinnitusWorldPosition(_currentStage);
            MoveGeneralTinnitusPlaceholder(_currentStage, targetWorldPosition);

            if (_tinnitusAudioController != null)
            {
                _tinnitusAudioController.transform.position = targetWorldPosition;
            }

            if (_poseMatchEvaluator == null)
            {
                return;
            }

            _poseMatchEvaluator.Evaluate();
            float cleanseProgress = _poseMatchEvaluator.Progress01;
            if (_tinnitusAudioController != null)
            {
                _tinnitusAudioController.CleanseStability = cleanseProgress;
            }

            bool isLooking = UpdateTinnitusLookAndLight(targetWorldPosition, cleanseProgress);
            bool isInsideTolerance = _poseMatchEvaluator.IsInsideTolerance;
            if (isInsideTolerance && !_wasTinnitusInsideTolerance)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.TinnitusPoseLock, targetWorldPosition, 1f);
                _tinnitusAudioController?.TriggerBurst(0.45f);
                hapticRouter?.TriggerTinnitusLockPulse();
            }
            else if (!isInsideTolerance && _wasTinnitusInsideTolerance)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.TinnitusPoseLost, targetWorldPosition, 0.65f);
            }

            _wasTinnitusInsideTolerance = isInsideTolerance;
            if (isInsideTolerance && Time.realtimeSinceStartup >= _nextTinnitusHapticAtRealtime)
            {
                float intensity = Mathf.Lerp(0.3f, 0.75f, cleanseProgress);
                hapticRouter?.StartTinnitusCleanseHum(intensity, tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds * 1.4f);
                _nextTinnitusHapticAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds;
            }
            else if (!isInsideTolerance && isLooking && Time.realtimeSinceStartup >= _nextTinnitusHapticAtRealtime)
            {
                hapticRouter?.TriggerBellAssistPulse();
                _nextTinnitusHapticAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds * 2f;
            }

            if (_poseMatchEvaluator.IsResolved)
            {
                CompleteGeneralTinnitus(targetWorldPosition);
            }
        }

        private bool UpdateTinnitusLookAndLight(Vector3 targetWorldPosition, float cleanseProgress)
        {
            _currentTinnitusGazeAngleDegrees = ResolveGazeAngleTo(targetWorldPosition);
            bool isLooking = _currentTinnitusGazeAngleDegrees <= tuningProfile.GeneralTinnitusRevealConeDegrees;
            if (!isLooking || Time.realtimeSinceStartup < _nextTinnitusLightAtRealtime)
            {
                return isLooking;
            }

            float intensity = tuningProfile.GeneralTinnitusLedIntensity * Mathf.Lerp(1f, 0.25f, cleanseProgress);
            lightRouter?.ShowTinnitusPoint(targetWorldPosition, intensity);
            _nextTinnitusLightAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusLightIntervalSeconds;
            return true;
        }

        private void CompleteGeneralTinnitus(Vector3 targetWorldPosition)
        {
            StopProceduralTinnitus();
            audioRouter?.PlayOneShot(FinalDemoCueId.TinnitusResolve, targetWorldPosition, 1f);
            lightRouter?.Clear();
            ForceNextStage();
        }

        private void BeginBossApproach()
        {
            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            audioRouter?.StartLoop(FinalDemoCueId.BossBasePulse, bossPosition, tuningProfile != null ? tuningProfile.BossBaseVolume : 0.42f);
            lightRouter?.ShowTinnitusPoint(bossPosition, tuningProfile != null ? tuningProfile.BossMassLedIntensity : 0.78f, true);
            _nextBossLightAtRealtime = Time.realtimeSinceStartup;
        }

        private void TickBossApproach()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            _currentBossApproachDistance = ResolvePlanarDistanceToPlayer(bossPosition);

            if (Time.realtimeSinceStartup >= _nextBossLightAtRealtime)
            {
                lightRouter?.ShowTinnitusPoint(bossPosition, tuningProfile.BossMassLedIntensity, true);
                _nextBossLightAtRealtime = Time.realtimeSinceStartup + tuningProfile.BossLightIntervalSeconds;
            }

            if (_currentBossApproachDistance <= tuningProfile.BossApproachRadius || StageElapsedSeconds >= tuningProfile.BossApproachAutoStartSeconds)
            {
                ForceNextStage();
            }
        }

        private void BeginBossPattern(FinalDemoStage stage)
        {
            if (tuningProfile == null)
            {
                return;
            }

            _currentBossPatternIndex = ResolveBossPatternIndex(stage);
            _bossWasInsideTolerance = false;
            _bossPatternFailureCount = 0;
            _nextBossLightAtRealtime = Time.realtimeSinceStartup;
            _nextBossHapticAtRealtime = Time.realtimeSinceStartup;

            EnsurePoseMatchEvaluator();
            if (_poseMatchEvaluator != null)
            {
                _poseMatchEvaluator.TreatmentSeconds = ResolveBossPatternSeconds(_currentBossPatternIndex);
                _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
                _poseMatchEvaluator.ResetProgress();
                UpdateBossPatternTarget();
            }

            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            audioRouter?.StartLoop(FinalDemoCueId.BossBasePulse, bossPosition, tuningProfile.BossBaseVolume);
            if (stage == FinalDemoStage.BossPatternOne)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.NarrBossTrack, ResolvePlayerRelativePosition(Vector3.forward * 1.1f), 1f);
            }
            else
            {
                hapticRouter?.TriggerBossTrackingPulse();
            }
        }

        private void TickBossPattern()
        {
            if (tuningProfile == null)
            {
                return;
            }

            EnsurePoseMatchEvaluator();
            if (_poseMatchEvaluator == null)
            {
                return;
            }

            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            if (Time.realtimeSinceStartup >= _nextBossLightAtRealtime)
            {
                float lightPulse = tuningProfile.BossMassLedIntensity * (0.8f + Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * 1.25f) * 0.12f);
                lightRouter?.ShowTinnitusPoint(bossPosition, Mathf.Clamp01(lightPulse), true);
                _nextBossLightAtRealtime = Time.realtimeSinceStartup + tuningProfile.BossLightIntervalSeconds;
            }

            UpdateBossPatternTarget();
            bool insideTolerance = _poseMatchEvaluator.IsInsideTolerance;
            if (_bossWasInsideTolerance && !insideTolerance)
            {
                ResetCurrentBossPatternAfterMiss(bossPosition);
                return;
            }

            _bossWasInsideTolerance = insideTolerance;
            if (insideTolerance && Time.realtimeSinceStartup >= _nextBossHapticAtRealtime)
            {
                hapticRouter?.TriggerBossTrackingPulse();
                _nextBossHapticAtRealtime = Time.realtimeSinceStartup + 0.75f;
            }

            _currentBossPatternProgress01 = _poseMatchEvaluator.Progress01;
            _currentBossPatternTargetMatch01 = _poseMatchEvaluator.TotalMatch01;
            if (_poseMatchEvaluator.IsResolved)
            {
                CompleteBossPattern(bossPosition);
            }
        }

        private void UpdateBossPatternTarget()
        {
            if (tuningProfile == null || _poseMatchEvaluator == null || _currentBossPatternIndex < 0)
            {
                return;
            }

            float totalSeconds = ResolveBossPatternSeconds(_currentBossPatternIndex);
            float holdSeconds = Mathf.Min(tuningProfile.BossOpeningHoldSeconds, totalSeconds);
            float progressSeconds = _poseMatchEvaluator.ProgressSeconds;
            bool moving = progressSeconds >= holdSeconds;
            float move01 = moving ? Mathf.Clamp01((progressSeconds - holdSeconds) / Mathf.Max(0.1f, totalSeconds - holdSeconds)) : 0f;
            float smoothMove01 = Smooth01(move01);
            Vector3 center = ResolveVectorAt(tuningProfile.BossWeakPointCentersCameraSpace, _currentBossPatternIndex, new Vector3(0f, 0f, 0.72f));
            Vector3 offset = ResolveVectorAt(tuningProfile.BossWeakPointMoveOffsetsCameraSpace, _currentBossPatternIndex, new Vector3(0.32f, 0.06f, 0.06f));
            Vector3 targetPosition = center + new Vector3(
                offset.x * smoothMove01,
                offset.y * smoothMove01 + Mathf.Sin(move01 * Mathf.PI) * 0.08f,
                offset.z * smoothMove01);
            Vector3 endRotation = ResolveVectorAt(tuningProfile.BossWeakPointEndYawPitchRoll, _currentBossPatternIndex, new Vector3(14f, -8f, 10f));
            float yaw = Mathf.Lerp(0f, endRotation.x, smoothMove01);
            float pitch = Mathf.Lerp(0f, endRotation.y, smoothMove01);
            float roll = Mathf.Lerp(0f, endRotation.z, smoothMove01);
            float toleranceMultiplier = 1f + _bossPatternFailureCount * tuningProfile.BossFailureToleranceGain;

            _poseMatchEvaluator.TreatmentSeconds = totalSeconds;
            _poseMatchEvaluator.PositionToleranceMeters = (moving ? tuningProfile.BossMovingToleranceMeters : tuningProfile.BossHoldPositionToleranceMeters) * toleranceMultiplier;
            _poseMatchEvaluator.YawToleranceDegrees = (moving ? tuningProfile.BossMovingToleranceDegrees : tuningProfile.BossHoldRotationToleranceDegrees) * toleranceMultiplier;
            _poseMatchEvaluator.PitchToleranceDegrees = _poseMatchEvaluator.YawToleranceDegrees;
            _poseMatchEvaluator.RollToleranceDegrees = _poseMatchEvaluator.YawToleranceDegrees;
            _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
            _poseMatchEvaluator.SetTargetPose(targetPosition, yaw, pitch, roll);
            _currentBossPatternProgress01 = _poseMatchEvaluator.Progress01;
            _currentBossPatternTargetMatch01 = _poseMatchEvaluator.TotalMatch01;
        }

        private void ResetCurrentBossPatternAfterMiss(Vector3 bossPosition)
        {
            _bossPatternFailureCount++;
            _bossWasInsideTolerance = false;
            _poseMatchEvaluator.ResetProgress();
            UpdateBossPatternTarget();
            audioRouter?.PlayOneShot(FinalDemoCueId.BossGlitchBurst, bossPosition, 0.75f);
            lightRouter?.ShowTinnitusPoint(bossPosition, 1f, true);
            hapticRouter?.TriggerBossFailurePulse();
        }

        private void CompleteBossPattern(Vector3 bossPosition)
        {
            audioRouter?.PlayOneShot(FinalDemoCueId.BossHit, bossPosition, 1f);
            lightRouter?.ShowTinnitusPoint(bossPosition, 1f, true);
            hapticRouter?.TriggerBossHitPulse();
            ForceNextStage();
        }

        private void BeginBossDefeat()
        {
            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            lightRouter?.Clear();
            audioRouter?.PlayOneShot(FinalDemoCueId.BossDefeatRise, bossPosition, 1f);
            audioRouter?.PlayOneShot(FinalDemoCueId.BossDefeatAir, ResolvePlayerRelativePosition(Vector3.forward * 1.5f), 0.85f);
            hapticRouter?.TriggerBossHitPulse();
        }

        private void TickBossDefeat()
        {
            if (tuningProfile == null)
            {
                return;
            }

            if (!_stageOutputCleared && StageElapsedSeconds >= tuningProfile.BossDefeatEffectSeconds)
            {
                lightRouter?.Clear();
                hapticRouter?.StopAllHaptics();
                audioRouter?.PlayOneShot(FinalDemoCueId.TransitionSoftCut, ResolvePlayerRelativePosition(Vector3.forward * 1.2f), 0.7f);
                _stageOutputCleared = true;
            }

            if (StageElapsedSeconds >= tuningProfile.BossDefeatEffectSeconds + tuningProfile.BossDefeatSilenceSeconds)
            {
                ForceNextStage();
            }
        }

        private void BeginForestEnding()
        {
            if (tuningProfile == null)
            {
                return;
            }

            MoveBellPlaceholder(tuningProfile.ForestBellWorldPosition);
            audioRouter?.StartLoop(FinalDemoCueId.ForestBed, ResolvePlayerRelativePosition(Vector3.forward * 2f), 0f);
            _nextForestBellAtRealtime = Time.realtimeSinceStartup;
        }

        private void TickForestEnding()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = tuningProfile.ForestBellWorldPosition;
            MoveBellPlaceholder(bellPosition);
            _currentForestDistance = ResolvePlanarDistanceToPlayer(bellPosition);

            float fade = Mathf.Clamp01(StageElapsedSeconds / tuningProfile.ForestBedFadeSeconds);
            audioRouter?.SetLoopVolumeScale(FinalDemoCueId.ForestBed, tuningProfile.ForestBedVolume * fade);

            if (Time.realtimeSinceStartup >= _nextForestBellAtRealtime)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.ForestBell, bellPosition, 0.7f);
                lightRouter?.ShowBellPoint(bellPosition, tuningProfile.ForestBellLedIntensity);
                _nextForestBellAtRealtime = Time.realtimeSinceStartup + tuningProfile.ForestBellCallIntervalSeconds;
            }

            if (_currentForestDistance <= tuningProfile.ForestBellArrivalRadius || StageElapsedSeconds >= tuningProfile.ForestAutoEndSeconds)
            {
                ForceNextStage();
            }
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
            EnsureRouters();
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

        private void EnsureRouters()
        {
            audioRouter ??= FindFirstObjectByType<FinalDemoAudioRouter>();
            if (audioRouter == null)
            {
                GameObject audioRoot = new GameObject("WorldAudio_FinalDemo");
                audioRoot.transform.SetParent(transform);
                audioRouter = audioRoot.AddComponent<FinalDemoAudioRouter>();
            }

            lightRouter ??= FindFirstObjectByType<FinalDemoLightRouter>();
            if (lightRouter == null)
            {
                GameObject lightRoot = new GameObject("WorldLight_FinalDemo");
                lightRoot.transform.SetParent(transform);
                lightRouter = lightRoot.AddComponent<FinalDemoLightRouter>();
            }

            hapticRouter ??= FindFirstObjectByType<FinalDemoHapticRouter>();
            if (hapticRouter == null)
            {
                GameObject hapticRoot = new GameObject("WorldHaptics_FinalDemo");
                hapticRoot.transform.SetParent(transform);
                hapticRouter = hapticRoot.AddComponent<FinalDemoHapticRouter>();
            }

            audioRouter.Initialize(cueLibrary, playerRig);
            audioRouter.SetSpatializerEnabled(HrtfPreviewEnabled);
            lightRouter.Initialize(playerRig, HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>());
        }

        private void EnsurePadInputRoot()
        {
            PadPoseProvider existingProvider = FindFirstObjectByType<PadPoseProvider>();
            if (existingProvider != null)
            {
                _poseMatchEvaluator = existingProvider.GetComponent<PadPoseMatchEvaluator>() ?? existingProvider.gameObject.AddComponent<PadPoseMatchEvaluator>();
                return;
            }

            GameObject padRoot = new GameObject("PadInput_FinalDemo");
            padRoot.transform.SetParent(transform);
            padRoot.AddComponent<PadTrackingReceiver>();
            padRoot.AddComponent<PadImuReceiver>();
            padRoot.AddComponent<PadPoseProvider>();
            _poseMatchEvaluator = padRoot.AddComponent<PadPoseMatchEvaluator>();
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
                    FinalDemoStage.BellFollowOne => tuningProfile != null ? tuningProfile.BellFollowTargetOnePosition : new Vector3(-1.4f, 1.5f, 3.2f),
                    FinalDemoStage.BellFollowRain => tuningProfile != null ? tuningProfile.BellFollowTargetTwoPosition : new Vector3(1.4f, 1.5f, 3.6f),
                    FinalDemoStage.BellGaze => new Vector3(0f, 1.55f, 2.2f),
                    FinalDemoStage.BellAcquisition => ResolvePlayerRelativePosition(tuningProfile != null ? tuningProfile.BellAcquisitionLocalOffset : new Vector3(0f, -0.08f, 0.85f)),
                    _ => new Vector3(0f, 1.6f, 2.4f),
                };
            }

            if (stage == FinalDemoStage.GeneralTinnitusOne || stage == FinalDemoStage.GeneralTinnitusTwo)
            {
                MoveGeneralTinnitusPlaceholder(stage, ResolveGeneralTinnitusWorldPosition(stage));
            }

            if (stage == FinalDemoStage.BossApproach || stage == FinalDemoStage.BossPatternOne || stage == FinalDemoStage.BossPatternTwo ||
                stage == FinalDemoStage.BossPatternThree || stage == FinalDemoStage.BossDefeat)
            {
                MoveBossPlaceholder(ResolveBossPosition());
            }

            if (stage == FinalDemoStage.ForestEnding && tuningProfile != null)
            {
                MoveBellPlaceholder(tuningProfile.ForestBellWorldPosition);
            }
        }

        private void MoveBellPlaceholder(Vector3 worldPosition)
        {
            Transform bell = FindNamedTransform("FinalDemo_BellPlaceholder");
            if (bell != null)
            {
                bell.position = worldPosition;
            }
        }

        private Vector3 ResolvePlayerRelativePosition(Vector3 localOffset)
        {
            Transform reference = playerCamera != null ? playerCamera.transform : playerRig;
            if (reference == null)
            {
                return localOffset;
            }

            Quaternion flatRotation = Quaternion.Euler(0f, reference.eulerAngles.y, 0f);
            Vector3 basePosition = playerRig != null ? playerRig.position : reference.position;
            return basePosition + (flatRotation * localOffset);
        }

        private float ResolvePlanarDistanceToPlayer(Vector3 worldPosition)
        {
            if (playerRig == null)
            {
                return float.PositiveInfinity;
            }

            return ResolvePlanarDistance(playerRig.position, worldPosition);
        }

        private static float ResolvePlanarDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private Vector3 GetBellPlaceholderPosition()
        {
            Transform bell = FindNamedTransform("FinalDemo_BellPlaceholder");
            return bell != null ? bell.position : ResolvePlayerRelativePosition(Vector3.forward * 1.4f);
        }

        private Vector3 ResolveBellGazeCuePosition(int cueIndex)
        {
            Vector3[] offsets = tuningProfile != null ? tuningProfile.BellGazeLocalOffsets : null;
            if (offsets == null || offsets.Length == 0)
            {
                return ResolvePlayerRelativePosition(new Vector3(0f, 0f, 1.5f));
            }

            int index = Mathf.Clamp(cueIndex, 0, offsets.Length - 1);
            return ResolvePlayerRelativePosition(offsets[index]);
        }

        private float ResolveBellGazeConeDegrees()
        {
            if (tuningProfile == null)
            {
                return 14f;
            }

            float assist01 = Mathf.Clamp01((StageElapsedSeconds - tuningProfile.BellGazeAssistStartSeconds) / 8f);
            return Mathf.Lerp(tuningProfile.BellGazeConeDegrees, tuningProfile.BellGazeAssistMaxConeDegrees, assist01);
        }

        private float ResolveGazeAngleTo(Vector3 worldPosition)
        {
            Transform gazeTransform = playerCamera != null ? playerCamera.transform : playerRig;
            if (gazeTransform == null)
            {
                return 180f;
            }

            Vector3 toTarget = worldPosition - gazeTransform.position;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Vector3.Angle(gazeTransform.forward, toTarget.normalized);
        }

        private void EnsurePoseMatchEvaluator()
        {
            if (_poseMatchEvaluator != null)
            {
                return;
            }

            _poseMatchEvaluator = FindFirstObjectByType<PadPoseMatchEvaluator>();
            if (_poseMatchEvaluator != null)
            {
                _poseMatchEvaluator.InitializeNow();
                return;
            }

            PadPoseProvider provider = FindFirstObjectByType<PadPoseProvider>();
            if (provider == null)
            {
                EnsurePadInputRoot();
                provider = FindFirstObjectByType<PadPoseProvider>();
            }

            if (provider == null)
            {
                return;
            }

            _poseMatchEvaluator = provider.GetComponent<PadPoseMatchEvaluator>() ?? provider.gameObject.AddComponent<PadPoseMatchEvaluator>();
            _poseMatchEvaluator.InitializeNow();
        }

        private void StartProceduralTinnitus(Vector3 worldPosition)
        {
            TinnitusAudioController controller = EnsureTinnitusAudioController();
            if (controller == null)
            {
                return;
            }

            controller.transform.position = worldPosition;
            controller.SetGlitchClips(cueLibrary != null ? cueLibrary.ResolveAudioClip(FinalDemoCueId.TinnitusLongGlitch) : null, cueLibrary != null ? cueLibrary.ResolveAudioClip(FinalDemoCueId.TinnitusBurst) : null);
            controller.Volume = 0.052f * (tuningProfile != null ? tuningProfile.GeneralTinnitusToneVolume : 0.8f);
            controller.CleanseStability = 0f;
            controller.enabled = true;
            _tinnitusAudioController = controller;
        }

        private void StopProceduralTinnitus()
        {
            if (_tinnitusAudioController == null)
            {
                _tinnitusAudioController = FindFirstObjectByType<TinnitusAudioController>();
            }

            if (_tinnitusAudioController == null)
            {
                return;
            }

            _tinnitusAudioController.CleanseStability = 1f;
            _tinnitusAudioController.enabled = false;
        }

        private TinnitusAudioController EnsureTinnitusAudioController()
        {
            if (_tinnitusAudioController != null)
            {
                return _tinnitusAudioController;
            }

            Transform existing = FindNamedTransform("FinalDemo_TinnitusAudio");
            GameObject sourceObject = existing != null ? existing.gameObject : new GameObject("FinalDemo_TinnitusAudio");
            sourceObject.transform.SetParent(transform);
            if (sourceObject.GetComponent<AudioSource>() == null)
            {
                sourceObject.AddComponent<AudioSource>();
            }

            _tinnitusAudioController = sourceObject.GetComponent<TinnitusAudioController>() ?? sourceObject.AddComponent<TinnitusAudioController>();
            return _tinnitusAudioController;
        }

        private Vector3 ResolveGeneralTinnitusWorldPosition(FinalDemoStage stage)
        {
            if (tuningProfile == null)
            {
                return stage == FinalDemoStage.GeneralTinnitusTwo
                    ? new Vector3(1.25f, 1.35f, 3.1f)
                    : new Vector3(-1.2f, 1.45f, 2.8f);
            }

            return stage == FinalDemoStage.GeneralTinnitusTwo
                ? tuningProfile.GeneralTinnitusTwoWorldPosition
                : tuningProfile.GeneralTinnitusOneWorldPosition;
        }

        private void MoveGeneralTinnitusPlaceholder(FinalDemoStage stage, Vector3 worldPosition)
        {
            string objectName = stage == FinalDemoStage.GeneralTinnitusTwo ? "FinalDemo_TinnitusB" : "FinalDemo_TinnitusA";
            Transform tinnitus = FindNamedTransform(objectName);
            if (tinnitus != null)
            {
                tinnitus.position = worldPosition;
            }
        }

        private Vector3 ResolveBossPosition()
        {
            return tuningProfile != null ? tuningProfile.BossWorldPosition : new Vector3(0f, 1.6f, 4.2f);
        }

        private void MoveBossPlaceholder(Vector3 worldPosition)
        {
            Transform boss = FindNamedTransform("FinalDemo_BossTinnitus");
            if (boss != null)
            {
                boss.position = worldPosition;
            }
        }

        private int ResolveBossPatternIndex(FinalDemoStage stage)
        {
            return stage switch
            {
                FinalDemoStage.BossPatternTwo => 1,
                FinalDemoStage.BossPatternThree => 2,
                _ => 0,
            };
        }

        private float ResolveBossPatternSeconds(int patternIndex)
        {
            if (tuningProfile == null)
            {
                return 8f;
            }

            return patternIndex switch
            {
                1 => tuningProfile.BossPatternTwoSeconds,
                2 => tuningProfile.BossPatternThreeSeconds,
                _ => tuningProfile.BossPatternOneSeconds,
            };
        }

        private static Vector3 ResolveVectorAt(Vector3[] values, int index, Vector3 fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            return values[Mathf.Clamp(index, 0, values.Length - 1)];
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private Vector3 ResolveBellOrbitPosition(float elapsedSeconds, out float intensity)
        {
            Vector3[] points = tuningProfile != null ? tuningProfile.BellOrbitLocalPoints : null;
            if (points == null || points.Length == 0)
            {
                intensity = 0.6f;
                return ResolvePlayerRelativePosition(Vector3.forward * 1.4f);
            }

            if (points.Length == 1)
            {
                intensity = tuningProfile != null ? tuningProfile.BellOrbitNearIntensity : 0.6f;
                return ResolvePlayerRelativePosition(points[0]);
            }

            float orbitDuration = tuningProfile != null ? tuningProfile.BellOrbitSeconds : 1f;
            float normalized = Mathf.Repeat(elapsedSeconds / Mathf.Max(0.01f, orbitDuration), 1f);
            float scaled = normalized * points.Length;
            int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, points.Length - 1);
            int nextIndex = (segmentIndex + 1) % points.Length;
            float segmentT = Mathf.SmoothStep(0f, 1f, scaled - segmentIndex);
            Vector3 localPosition = Vector3.Lerp(points[segmentIndex], points[nextIndex], segmentT);
            float distance01 = Mathf.InverseLerp(0.65f, 2.2f, localPosition.magnitude);
            intensity = Mathf.Lerp(tuningProfile.BellOrbitNearIntensity, tuningProfile.BellOrbitFarIntensity, distance01);
            return ResolvePlayerRelativePosition(localPosition);
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

        private static bool IsImplementedTimedStage(FinalDemoStage stage)
        {
            return stage switch
            {
                FinalDemoStage.OpeningAmbience => true,
                FinalDemoStage.OpeningSilence => true,
                FinalDemoStage.OpeningCloseBell => true,
                FinalDemoStage.BellOrbit => true,
                FinalDemoStage.BellFollowOne => true,
                FinalDemoStage.BellFollowRain => true,
                FinalDemoStage.BellGaze => true,
                FinalDemoStage.BellAcquisition => true,
                FinalDemoStage.GeneralTinnitusOne => true,
                FinalDemoStage.GeneralTinnitusTwo => true,
                FinalDemoStage.BossApproach => true,
                FinalDemoStage.BossPatternOne => true,
                FinalDemoStage.BossPatternTwo => true,
                FinalDemoStage.BossPatternThree => true,
                FinalDemoStage.BossDefeat => true,
                FinalDemoStage.ForestEnding => true,
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
            Vector3 followOne = tuningProfile != null ? tuningProfile.BellFollowTargetOnePosition : new Vector3(-1.4f, 1.5f, 3.2f);
            Vector3 followTwo = tuningProfile != null ? tuningProfile.BellFollowTargetTwoPosition : new Vector3(1.4f, 1.5f, 3.6f);
            float arrivalRadius = tuningProfile != null ? tuningProfile.BellArrivalRadius : 0.8f;
            Gizmos.DrawWireSphere(new Vector3(0f, 1.6f, 2.4f), 0.25f);
            Gizmos.DrawWireSphere(followOne, arrivalRadius);
            Gizmos.DrawWireSphere(followTwo, arrivalRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(tuningProfile != null ? tuningProfile.RainZoneCenter : new Vector3(0f, 1.6f, 2.9f), tuningProfile != null ? tuningProfile.RainZoneRadius : 2.4f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(0f, 1.6f, 4.2f), 0.45f);
        }
    }
}
