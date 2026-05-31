using System;
using System.Collections.Generic;
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
        [SerializeField] private FinalDemoSceneReferences sceneReferences;
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
        private Vector3 _lockedPlayerWorldPosition;
        private bool _hasStarted;
        private bool _stageOneShotPlayed;
        private bool _stageNarrationPlayed;
        private bool _rainLoopStarted;
        private float _nextBellCallAtRealtime;
        private float _nextBellOrbitAnchorAtRealtime;
        private float _bellOrbitAnchorSuppressedUntilRealtime;
        private float _bellLightActiveUntilRealtime;
        private bool _bellOrbitMovingToFirstFollow;
        private Vector3 _bellOrbitMoveStartPosition;
        private Vector3 _bellOrbitMoveTargetPosition;
        private float _bellOrbitMoveStartedAtRealtime;
        private bool _bellFollowRelocating;
        private Vector3 _bellFollowRelocationStartPosition;
        private Vector3 _bellFollowRelocationTargetPosition;
        private float _bellFollowRelocationStartedAtRealtime;
        private Vector3 _bellFollowStageStartPosition;
        private int _bellFollowCallCount;
        private float _nextRainDropAtRealtime;
        private float _nextRainLightAtRealtime;
        private float _rainStartedAtRealtime;
        private float _lastBellAssistAtRealtime;
        private float _lastPadShakeAssistAtRealtime;
        private float _lastPadShakeNarrationAtRealtime;
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
        private FinalDemoAudioReactiveLight _tinnitusReactiveLight;
        private FinalDemoAudioReactiveLight _bossReactiveLight;
        private bool _wasTinnitusInsideTolerance;
        private float _nextTinnitusLightAtRealtime;
        private float _nextTinnitusHapticAtRealtime;
        private float _currentTinnitusGazeAngleDegrees = 180f;
        private bool _generalTinnitusPoseLocked;
        private bool _hasGeneralTinnitusStageWorldPosition;
        private Vector3 _generalTinnitusStageWorldPosition;
        private float _currentGeneralTinnitusDistance = float.PositiveInfinity;
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
        private FinalDemoWorldRainEnvironment _worldRainEnvironment;
        private FinalDemoForestEnvironment _forestEnvironment;
        private FinalDemoWorldFloorSurface _worldFloorSurface;
        private FinalDemoGlobalGlitchOverlay _globalGlitchOverlay;
        private float _nextPadAnchorLightAtRealtime;
        private readonly Queue<NarrationRequest> _narrationQueue = new Queue<NarrationRequest>();
        private readonly HashSet<FinalDemoCueId> _queuedNarrationCueIds = new HashSet<FinalDemoCueId>();
        private readonly HashSet<FinalDemoCueId> _playedNarrationCueIds = new HashSet<FinalDemoCueId>();
        private FinalDemoCueId _activeNarrationCueId = FinalDemoCueId.None;
        private float _activeNarrationUntilRealtime;
        private bool _pendingOpeningAfterFaceForwardNarration;

        private struct NarrationRequest
        {
            public FinalDemoCueId cueId;
            public FinalDemoStage stage;
            public bool requireCurrentStage;
            public bool oncePerRun;
            public float notBeforeRealtime;
        }

        public FinalDemoStage CurrentStage => _currentStage;
        public int StageCount => _stageOrder.Length;
        public int StageIndex => Array.IndexOf(_stageOrder, _currentStage);
        public float StageElapsedSeconds => Time.realtimeSinceStartup - _stageStartedAtRealtime;
        public bool PlayerMovementLocked => _playerMovementLocked;
        public bool IsComplete => _currentStage == FinalDemoStage.Complete;
        public FinalDemoAssistLevel AssistLevel { get; private set; } = FinalDemoAssistLevel.Gentle;
        public bool HrtfPreviewEnabled { get; private set; } = true;
        public bool GlobalGlitchEnabled => _globalGlitchOverlay != null && _globalGlitchOverlay.EffectEnabled;
        public FinalDemoTuningProfile TuningProfile => tuningProfile;
        public FinalDemoCueLibrary CueLibrary => cueLibrary;
        public FinalDemoSceneReferences SceneReferences => sceneReferences;
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

            sceneReferences ??= GetComponent<FinalDemoSceneReferences>() ?? FindFirstObjectByType<FinalDemoSceneReferences>();
            playerCamera ??= sceneReferences != null ? sceneReferences.PlayerCamera : null;
            operatorControls ??= GetComponent<FinalDemoOperatorControls>();
            inputStatus ??= GetComponent<FinalDemoInputStatus>() ?? FindFirstObjectByType<FinalDemoInputStatus>();
            ApplyTuningSerialSettings();
            EnsurePresentationHelpers();
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
            EnforceLockedPlayerPosition();

            if (!_hasStarted && _currentStage != FinalDemoStage.Preflight)
            {
                _hasStarted = true;
            }

            TickCurrentStage();
            TickPersistentAmbience();
            TickEnvironmentPresentation();
            TickGlobalGlitchOverlay();
            TickPadAnchorLight();
            ApplyBellFollowProgressBlocker();
            TickNarrationQueue();

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
                QueueNarration(FinalDemoCueId.NarrFaceForwardWait, FinalDemoStage.Preflight, false, true);
                _pendingOpeningAfterFaceForwardNarration = true;
                TickNarrationQueue();
                if (_pendingOpeningAfterFaceForwardNarration && _activeNarrationCueId == FinalDemoCueId.None)
                {
                    _pendingOpeningAfterFaceForwardNarration = false;
                    EnterStage(FinalDemoStage.OpeningAmbience);
                }
            }
        }

        public void ForceNextStage()
        {
            ClearQueuedNarration();
            int currentIndex = Mathf.Max(0, StageIndex);
            int nextIndex = Mathf.Min(currentIndex + 1, _stageOrder.Length - 1);
            EnterStage(_stageOrder[nextIndex]);
        }

        public void ForceCompleteCurrentObjective()
        {
            ForceNextStage();
        }

        public void ForceStage(FinalDemoStage stage)
        {
            ClearQueuedNarration();
            _hasStarted = stage != FinalDemoStage.Preflight;
            EnterStage(stage);
        }

        public void ResetCurrentStage()
        {
            ClearQueuedNarration();
            EnterStage(_currentStage);
        }

        public void ResetToPreflight()
        {
            ClearAllNarrationState();
            _hasStarted = false;
            _pendingOpeningAfterFaceForwardNarration = false;
            EnterStage(FinalDemoStage.Preflight);
        }

        public void ToggleMovementLock()
        {
            SetPlayerMovementLocked(!_playerMovementLocked);
        }

        public void SetPlayerMovementLocked(bool locked)
        {
            _playerMovementLocked = locked;
            if (locked && playerRig != null)
            {
                _lockedPlayerWorldPosition = playerRig.position;
            }

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
            _tinnitusAudioController?.SetBinauralPreview(ResolveAudioListenerTransform(), HrtfPreviewEnabled);
        }

        public void ToggleGlobalGlitch()
        {
            EnsureGlobalGlitchOverlay();
            if (_globalGlitchOverlay == null)
            {
                return;
            }

            _globalGlitchOverlay.SetEffectEnabled(!_globalGlitchOverlay.EffectEnabled);
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
            ClearQueuedNarration();
            _pendingOpeningAfterFaceForwardNarration = false;
        }

        private void QueueNarration(FinalDemoCueId cueId, FinalDemoStage stage, bool requireCurrentStage = true, bool oncePerRun = true)
        {
            QueueNarrationDelayed(cueId, stage, 0f, requireCurrentStage, oncePerRun);
        }

        private void QueueNarrationDelayed(FinalDemoCueId cueId, FinalDemoStage stage, float delaySeconds, bool requireCurrentStage = true, bool oncePerRun = true)
        {
            if (cueId == FinalDemoCueId.None)
            {
                return;
            }

            if (oncePerRun && _playedNarrationCueIds.Contains(cueId))
            {
                return;
            }

            if (_activeNarrationCueId == cueId || _queuedNarrationCueIds.Contains(cueId))
            {
                return;
            }

            _narrationQueue.Enqueue(new NarrationRequest
            {
                cueId = cueId,
                stage = stage,
                requireCurrentStage = requireCurrentStage,
                oncePerRun = oncePerRun,
                notBeforeRealtime = Time.realtimeSinceStartup + Mathf.Max(0f, delaySeconds),
            });
            _queuedNarrationCueIds.Add(cueId);
        }

        private void TickNarrationQueue()
        {
            if (_activeNarrationCueId != FinalDemoCueId.None)
            {
                if (Time.realtimeSinceStartup < _activeNarrationUntilRealtime)
                {
                    return;
                }

                _activeNarrationCueId = FinalDemoCueId.None;
                if (_pendingOpeningAfterFaceForwardNarration && _currentStage == FinalDemoStage.Preflight)
                {
                    _pendingOpeningAfterFaceForwardNarration = false;
                    EnterStage(FinalDemoStage.OpeningAmbience);
                    return;
                }
            }

            while (_narrationQueue.Count > 0)
            {
                NarrationRequest request = _narrationQueue.Peek();
                if (Time.realtimeSinceStartup < request.notBeforeRealtime)
                {
                    return;
                }

                _narrationQueue.Dequeue();
                _queuedNarrationCueIds.Remove(request.cueId);
                if (!IsNarrationRequestStillValid(request))
                {
                    continue;
                }

                AudioSource source = audioRouter != null
                    ? audioRouter.PlayOneShot(request.cueId, ResolvePlayerRelativePosition(Vector3.forward * 1.1f), 1f)
                    : null;
                if (source == null)
                {
                    continue;
                }

                _activeNarrationCueId = request.cueId;
                float cueLength = audioRouter != null ? audioRouter.GetCueLengthSeconds(request.cueId) : 0f;
                _activeNarrationUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(0.1f, cueLength);
                if (request.oncePerRun)
                {
                    _playedNarrationCueIds.Add(request.cueId);
                }

                return;
            }
        }

        private bool IsNarrationRequestStillValid(NarrationRequest request)
        {
            if (request.oncePerRun && _playedNarrationCueIds.Contains(request.cueId))
            {
                return false;
            }

            return !request.requireCurrentStage || _currentStage == request.stage;
        }

        private void ClearQueuedNarration()
        {
            _narrationQueue.Clear();
            _queuedNarrationCueIds.Clear();
        }

        private void ClearAllNarrationState()
        {
            ClearQueuedNarration();
            _playedNarrationCueIds.Clear();
            _activeNarrationCueId = FinalDemoCueId.None;
            _activeNarrationUntilRealtime = 0f;
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
                summary += $" distance={_currentGeneralTinnitusDistance:0.00}/{(tuningProfile != null ? tuningProfile.GeneralTinnitusApproachRadius : 0f):0.00}m locked={_generalTinnitusPoseLocked} match={GeneralTinnitusMatch01:0.00} cleanse={GeneralTinnitusProgress01:0.00} angle={_currentTinnitusGazeAngleDegrees:0.0}";
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
            Transform bell = sceneReferences != null && sceneReferences.BellVisual != null
                ? sceneReferences.BellVisual
                : FindNamedTransform("FinalDemo_BellPlaceholder");
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
            Transform boss = sceneReferences != null && sceneReferences.BossVisual != null
                ? sceneReferences.BossVisual
                : FindNamedTransform("FinalDemo_BossTinnitus");
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
                worldPosition = _hasGeneralTinnitusStageWorldPosition
                    ? _generalTinnitusStageWorldPosition
                    : ResolveGeneralTinnitusWorldPosition(_currentStage);
                return true;
            }

            worldPosition = default;
            return false;
        }

        private void EnterStage(FinalDemoStage nextStage)
        {
            Vector3 previousBellPosition = GetBellPlaceholderPosition();
            _currentStage = nextStage;
            _stageStartedAtRealtime = Time.realtimeSinceStartup;
            if (audioRouter != null)
            {
                if (ShouldKeepOpeningAmbience(nextStage))
                {
                    audioRouter.StopAllCuesExcept(FinalDemoCueId.OpeningAmbienceBed);
                }
                else
                {
                    audioRouter.StopAllCues();
                }
            }
            hapticRouter?.StopAllHaptics();
            lightRouter?.Clear();
            StopProceduralTinnitus();
            _bossReactiveLight = null;
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
            _lastPadShakeNarrationAtRealtime = Time.realtimeSinceStartup - 999f;
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
            _generalTinnitusPoseLocked = false;
            _hasGeneralTinnitusStageWorldPosition = false;
            _currentGeneralTinnitusDistance = float.PositiveInfinity;
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
            _nextBellOrbitAnchorAtRealtime = Time.realtimeSinceStartup;
            _nextPadAnchorLightAtRealtime = Time.realtimeSinceStartup;
            _bellOrbitAnchorSuppressedUntilRealtime = 0f;
            _bellLightActiveUntilRealtime = 0f;
            _bellOrbitMovingToFirstFollow = false;
            _bellFollowRelocating = false;
            _bellFollowStageStartPosition = playerRig != null ? playerRig.position : Vector3.zero;
            _bellFollowCallCount = 0;

            if (nextStage == FinalDemoStage.OpeningAmbience)
            {
                audioRouter?.StartLoop(FinalDemoCueId.OpeningAmbienceBed, ResolvePlayerRelativePosition(Vector3.forward * 2f), 0f);
            }
            else if (nextStage == FinalDemoStage.BellFollowRain)
            {
                BeginRainLayer();
                QueueNarrationDelayed(FinalDemoCueId.NarrBellEscaped, FinalDemoStage.BellFollowRain, 2f);
                BeginBellFollowRelocation(previousBellPosition, ResolveBellFollowTargetPosition(true));
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
                audioRouter?.StopLoop(FinalDemoCueId.OpeningAmbienceBed);
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
            audioRouter?.SetLoopVolumeScale(FinalDemoCueId.OpeningAmbienceBed, tuningProfile.OpeningAmbienceVolume * fade);

            if (StageElapsedSeconds >= tuningProfile.OpeningAmbienceSeconds)
            {
                ForceNextStage();
            }
        }

        private void LateUpdate()
        {
            EnforceLockedPlayerPosition();
        }

        private void TickPersistentAmbience()
        {
            if (tuningProfile == null || audioRouter == null)
            {
                return;
            }

            if (ShouldKeepOpeningAmbience(_currentStage))
            {
                float scale = tuningProfile.OpeningAmbienceVolume;
                if (_currentStage == FinalDemoStage.OpeningAmbience)
                {
                    float fade = Mathf.Clamp01(StageElapsedSeconds / tuningProfile.OpeningAmbienceFadeSeconds);
                    scale *= fade;
                }

                Vector3 position = ResolvePlayerRelativePosition(Vector3.forward * 2f);
                audioRouter.StartLoop(FinalDemoCueId.OpeningAmbienceBed, position, scale);
                audioRouter.SetLoopVolumeScale(FinalDemoCueId.OpeningAmbienceBed, scale);
                return;
            }

            audioRouter.StopLoop(FinalDemoCueId.OpeningAmbienceBed);
        }

        private void TickEnvironmentPresentation()
        {
            if (_worldRainEnvironment != null)
            {
                bool rainVisible = _currentStage == FinalDemoStage.BellFollowRain || _currentRainIntensity > 0.01f;
                _worldRainEnvironment.Apply(_currentRainIntensity, playerRig, rainVisible);
            }

            if (_forestEnvironment != null)
            {
                bool forestVisible = _currentStage == FinalDemoStage.ForestEnding || _currentStage == FinalDemoStage.Complete;
                Vector3 center = tuningProfile != null ? ResolveForestBellPosition() : ResolvePlayerRelativePosition(Vector3.forward * 3f);
                float fade = forestVisible && tuningProfile != null
                    ? Mathf.Clamp01(StageElapsedSeconds / tuningProfile.ForestBedFadeSeconds)
                    : 0f;
                _forestEnvironment.Apply(center, fade, forestVisible);
            }

            if (_worldFloorSurface != null)
            {
                bool forestVisible = _currentStage == FinalDemoStage.ForestEnding || _currentStage == FinalDemoStage.Complete;
                _worldFloorSurface.Apply(_currentRainIntensity, forestVisible);
            }
        }

        private void TickPadAnchorLight()
        {
            if (lightRouter == null || Time.realtimeSinceStartup < _nextPadAnchorLightAtRealtime)
            {
                return;
            }

            Transform pad = sceneReferences != null ? sceneReferences.PadVisual : null;
            if (pad == null)
            {
                return;
            }

            lightRouter.ShowPadAnchor(pad.position, 0.22f);
            _nextPadAnchorLightAtRealtime = Time.realtimeSinceStartup + 0.24f;
        }

        private void TickGlobalGlitchOverlay()
        {
            if (_globalGlitchOverlay == null)
            {
                return;
            }

            bool bossStage = _currentStage == FinalDemoStage.BossApproach ||
                             _currentStage == FinalDemoStage.BossPatternOne ||
                             _currentStage == FinalDemoStage.BossPatternTwo ||
                             _currentStage == FinalDemoStage.BossPatternThree ||
                             _currentStage == FinalDemoStage.BossDefeat;

            float stageBoost = 0f;
            if (_currentStage == FinalDemoStage.GeneralTinnitusOne || _currentStage == FinalDemoStage.GeneralTinnitusTwo)
            {
                stageBoost = Mathf.Lerp(0.2f, 0.04f, GeneralTinnitusProgress01);
            }
            else if (bossStage)
            {
                stageBoost = _currentStage == FinalDemoStage.BossDefeat ? 0.12f : 0.26f;
            }
            else if (_currentStage == FinalDemoStage.BellFollowRain)
            {
                stageBoost = _currentRainIntensity * 0.04f;
            }

            _globalGlitchOverlay.ApplyRuntimeState(stageBoost, bossStage);
        }

        private void EnforceLockedPlayerPosition()
        {
            if (!_playerMovementLocked || playerRig == null)
            {
                return;
            }

            playerRig.position = new Vector3(_lockedPlayerWorldPosition.x, playerRig.position.y, _lockedPlayerWorldPosition.z);
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
            TickBellContinuousAnchor(bellPosition, tuningProfile.OpeningCloseBellLedIntensity * 0.48f);

            if (!_stageOneShotPlayed)
            {
                PlayReactiveBellCue(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.OpeningCloseBellVolume, tuningProfile.OpeningCloseBellLedIntensity);
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

            if (_bellOrbitMovingToFirstFollow)
            {
                TickBellOrbitToFirstFollowMove();
                return;
            }

            Vector3 bellPosition = ResolveBellOrbitPosition(Mathf.Min(StageElapsedSeconds, tuningProfile.BellOrbitSeconds), out float pointIntensity);
            MoveBellPlaceholder(bellPosition);
            UpdateBellMovementTextureLoop(bellPosition, 0f, false);

            TickBellContinuousAnchor(bellPosition, pointIntensity, 1f, false);

            if (Time.realtimeSinceStartup >= _nextBellCallAtRealtime)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.BellOrbitVolume);
                ScheduleNextCueAfterPlayback(FinalDemoCueId.BellDistantCall, tuningProfile.BellOrbitCallIntervalSeconds);
            }

            if (StageElapsedSeconds >= tuningProfile.BellOrbitSeconds)
            {
                BeginBellOrbitToFirstFollowMove(bellPosition);
            }
        }

        private void BeginBellOrbitToFirstFollowMove(Vector3 startPosition)
        {
            _bellOrbitMovingToFirstFollow = true;
            _bellOrbitMoveStartPosition = startPosition;
            _bellOrbitMoveTargetPosition = ResolveBellFollowTargetPosition(false);
            _bellOrbitMoveStartedAtRealtime = Time.realtimeSinceStartup;
            _nextBellCallAtRealtime = float.PositiveInfinity;
            _bellLightActiveUntilRealtime = 0f;
            lightRouter?.Clear();
        }

        private void TickBellOrbitToFirstFollowMove()
        {
            if (tuningProfile == null)
            {
                return;
            }

            float elapsed = Time.realtimeSinceStartup - _bellOrbitMoveStartedAtRealtime;
            float duration = tuningProfile.BellOrbitToFirstTargetMoveSeconds;
            float move01 = Mathf.Clamp01(elapsed / duration);
            Vector3 bellPosition = Vector3.Lerp(_bellOrbitMoveStartPosition, _bellOrbitMoveTargetPosition, Smooth01(move01));
            MoveBellPlaceholder(bellPosition);

            float envelope = ResolveBellMovementEnvelope(elapsed, duration, tuningProfile.BellMovementTextureFadeSeconds);
            UpdateBellMovementTextureLoop(bellPosition, tuningProfile.BellMovementTextureVolume * envelope, envelope > 0.001f);

            if (move01 < 1f)
            {
                return;
            }

            UpdateBellMovementTextureLoop(bellPosition, 0f, false);
            _bellOrbitMovingToFirstFollow = false;
            _bellFollowStageStartPosition = playerRig != null ? playerRig.position : _bellOrbitMoveTargetPosition;
            ForceNextStage();
        }

        private static float ResolveBellMovementEnvelope(float elapsedSeconds, float durationSeconds, float fadeSeconds)
        {
            float fadeIn = Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.01f, fadeSeconds));
            float fadeOut = Mathf.Clamp01((durationSeconds - elapsedSeconds) / Mathf.Max(0.01f, fadeSeconds));
            return Mathf.Min(fadeIn, fadeOut);
        }

        private void TickBellContinuousAnchor(Vector3 bellPosition, float pointIntensity, float anchorScale = 1f, bool requireActiveBellLight = true)
        {
            if (tuningProfile == null || !tuningProfile.BellOrbitContinuousAnchorEnabled)
            {
                return;
            }

            if (requireActiveBellLight && Time.realtimeSinceStartup < _bellOrbitAnchorSuppressedUntilRealtime)
            {
                return;
            }

            if (requireActiveBellLight && Time.realtimeSinceStartup > _bellLightActiveUntilRealtime)
            {
                return;
            }

            if (Time.realtimeSinceStartup < _nextBellOrbitAnchorAtRealtime)
            {
                return;
            }

            float anchorIntensity = Mathf.Clamp01(pointIntensity * tuningProfile.BellOrbitContinuousAnchorIntensity * Mathf.Max(0f, anchorScale));
            lightRouter?.ShowBellAnchor(bellPosition, anchorIntensity);
            _nextBellOrbitAnchorAtRealtime = Time.realtimeSinceStartup + tuningProfile.BellOrbitContinuousAnchorUpdateIntervalSeconds;
        }

        private void SuppressBellOrbitAnchorForCue(FinalDemoCueId cueId)
        {
            float clipSeconds = audioRouter != null ? audioRouter.GetCueLengthSeconds(cueId) : 0f;
            _bellOrbitAnchorSuppressedUntilRealtime = Time.realtimeSinceStartup + clipSeconds + 0.05f;
        }

        private void TickBellFollow(bool rainStage)
        {
            if (tuningProfile == null)
            {
                return;
            }

            if (_bellFollowRelocating)
            {
                if (rainStage)
                {
                    TickRainLayer();
                }

                TickBellFollowRelocation();
                return;
            }

            Vector3 targetPosition = ResolveBellFollowTargetPosition(rainStage);
            MoveBellPlaceholder(targetPosition);
            _currentBellFollowDistance = ResolvePlanarDistanceToPlayer(targetPosition);
            TickBellContinuousAnchor(targetPosition, tuningProfile.BellFollowLedIntensity * 0.42f);

            if (rainStage)
            {
                TickRainLayer();
            }

            bool assistOverdue = StageElapsedSeconds >= tuningProfile.BellAssistTimeoutSeconds;
            if (Time.realtimeSinceStartup >= _nextBellCallAtRealtime)
            {
                PlayBellFollowCall(targetPosition, assistOverdue);
                if (!_stageNarrationPlayed && !rainStage)
                {
                    QueueNarration(FinalDemoCueId.NarrFollowBell, FinalDemoStage.BellFollowOne);
                    _stageNarrationPlayed = true;
                }

                ScheduleNextBellFollowCall();
            }

            if (assistOverdue && Time.realtimeSinceStartup - _lastBellAssistAtRealtime >= tuningProfile.BellAssistRepeatSeconds)
            {
                TriggerBellAssist(targetPosition, FinalDemoCueId.BellStrongAssist);
                TryQueuePadShakeAssistNarration();
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

            BeginRainLayer();

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

            if (!_stageNarrationPlayed &&
                Time.realtimeSinceStartup - _rainStartedAtRealtime >= tuningProfile.RainFocusBellNarrationDelaySeconds)
            {
                QueueNarration(FinalDemoCueId.NarrRainFocusBell, FinalDemoStage.BellFollowRain);
                _stageNarrationPlayed = true;
            }

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

        private void BeginRainLayer()
        {
            if (_rainLoopStarted || playerRig == null)
            {
                return;
            }

            _rainLoopStarted = true;
            _rainStartedAtRealtime = Time.realtimeSinceStartup;
            _nextRainDropAtRealtime = Time.realtimeSinceStartup;
            _nextRainLightAtRealtime = Time.realtimeSinceStartup;
            audioRouter?.StartLoop(FinalDemoCueId.RainLightBed, playerRig.position, 0f);
            audioRouter?.StartLoop(FinalDemoCueId.RainStrongBed, playerRig.position, 0f);
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

            PlayReactiveBellCue(FinalDemoCueId.BellDistantCall, targetPosition, volume, intensity);
        }

        private void TriggerBellAssist(Vector3 targetPosition, FinalDemoCueId cueId)
        {
            _lastBellAssistAtRealtime = Time.realtimeSinceStartup;
            float volume = tuningProfile != null ? Mathf.Clamp01(tuningProfile.BellFollowVolume * tuningProfile.BellAssistGainMultiplier) : 0.85f;
            float intensity = tuningProfile != null ? Mathf.Clamp01(tuningProfile.BellFollowLedIntensity * tuningProfile.BellAssistGainMultiplier) : 0.85f;
            PlayReactiveBellCue(cueId, targetPosition, volume, intensity);
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
            TryQueuePadShakeAssistNarration();
        }

        private void BeginBellFollowRelocation(Vector3 startPosition, Vector3 targetPosition)
        {
            _bellFollowRelocating = true;
            _bellFollowRelocationStartPosition = startPosition;
            _bellFollowRelocationTargetPosition = targetPosition;
            _bellFollowRelocationStartedAtRealtime = Time.realtimeSinceStartup;
            _nextBellCallAtRealtime = float.PositiveInfinity;
            _bellLightActiveUntilRealtime = 0f;
            MoveBellPlaceholder(startPosition);
            lightRouter?.Clear();
        }

        private void TickBellFollowRelocation()
        {
            if (tuningProfile == null)
            {
                return;
            }

            float elapsed = Time.realtimeSinceStartup - _bellFollowRelocationStartedAtRealtime;
            float duration = tuningProfile.BellFollowRelocationSeconds;
            float move01 = Mathf.Clamp01(elapsed / duration);
            Vector3 bellPosition = Vector3.Lerp(_bellFollowRelocationStartPosition, _bellFollowRelocationTargetPosition, Smooth01(move01));
            MoveBellPlaceholder(bellPosition);
            _currentBellFollowDistance = float.PositiveInfinity;

            float envelope = ResolveBellMovementEnvelope(elapsed, duration, tuningProfile.BellMovementTextureFadeSeconds);
            UpdateBellMovementTextureLoop(bellPosition, tuningProfile.BellMovementTextureVolume * envelope, envelope > 0.001f);

            if (move01 < 1f)
            {
                return;
            }

            UpdateBellMovementTextureLoop(bellPosition, 0f, false);
            _bellFollowRelocating = false;
            _bellFollowStageStartPosition = playerRig != null ? playerRig.position : _bellFollowRelocationTargetPosition;
            MoveBellPlaceholder(_bellFollowRelocationTargetPosition);
            PlayBellFollowCall(_bellFollowRelocationTargetPosition, false);
            ScheduleNextBellFollowCall();
        }

        private void ScheduleNextBellFollowCall()
        {
            if (tuningProfile == null)
            {
                ScheduleNextCueAfterPlayback(FinalDemoCueId.BellDistantCall, 8f);
                return;
            }

            float requestedInterval = Mathf.Max(
                tuningProfile.BellFollowMinimumCallIntervalSeconds,
                tuningProfile.BellFollowInitialCallIntervalSeconds -
                (_bellFollowCallCount * tuningProfile.BellFollowMissIntervalReductionSeconds));
            _bellFollowCallCount++;
            ScheduleNextCueAfterPlayback(FinalDemoCueId.BellDistantCall, requestedInterval);
        }

        private void TryQueuePadShakeAssistNarration()
        {
            if (tuningProfile == null)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _lastPadShakeNarrationAtRealtime < tuningProfile.PadShakeAssistNarrationCooldownSeconds)
            {
                return;
            }

            _lastPadShakeNarrationAtRealtime = Time.realtimeSinceStartup;
            QueueNarration(FinalDemoCueId.NarrPadShakeAssist, _currentStage, true, false);
        }

        private void ApplyBellFollowProgressBlocker()
        {
            if (tuningProfile == null ||
                !tuningProfile.BellFollowProgressBlockerEnabled ||
                playerRig == null ||
                _bellFollowRelocating ||
                (_currentStage != FinalDemoStage.BellFollowOne && _currentStage != FinalDemoStage.BellFollowRain))
            {
                return;
            }

            Vector3 targetPosition = ResolveBellFollowTargetPosition(_currentStage == FinalDemoStage.BellFollowRain);
            Vector2 start = new Vector2(_bellFollowStageStartPosition.x, _bellFollowStageStartPosition.z);
            Vector2 target = new Vector2(targetPosition.x, targetPosition.z);
            Vector2 current = new Vector2(playerRig.position.x, playerRig.position.z);
            Vector2 route = target - start;
            float routeLength = route.magnitude;
            if (routeLength <= 0.001f)
            {
                return;
            }

            Vector2 direction = route / routeLength;
            float currentDistance = Vector2.Dot(current - start, direction);
            float maxDistance = routeLength + tuningProfile.BellFollowBlockerMarginMeters;
            if (currentDistance <= maxDistance)
            {
                return;
            }

            Vector2 lateral = current - (start + direction * currentDistance);
            Vector2 clamped = start + direction * maxDistance + lateral;
            playerRig.position = new Vector3(clamped.x, tuningProfile.PlayerFixedHeight, clamped.y);
        }

        private void BeginBellGazeStage()
        {
            _bellGazeSuccessCount = 0;
            _bellGazeProgressSeconds = 0f;
            _nextBellCallAtRealtime = Time.realtimeSinceStartup;
            QueueNarration(FinalDemoCueId.NarrLookBell, FinalDemoStage.BellGaze);
            BeginBellGazeCue(0, false);
        }

        private void TickBellGaze()
        {
            if (tuningProfile == null)
            {
                return;
            }

            Vector3 bellPosition = UpdateBellGazeMove();
            UpdateBellMovementTextureLoop(bellPosition, 0.28f, _bellGazeMoving);
            TickBellContinuousAnchor(bellPosition, tuningProfile.BellGazeLedIntensity * 0.38f);
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
            }
            else
            {
                _bellGazeMoving = false;
                _bellGazeMoveTargetPosition = targetPosition;
                MoveBellPlaceholder(targetPosition);
            }
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

            PlayReactiveBellCue(FinalDemoCueId.BellDistantCall, bellPosition, tuningProfile.BellFollowVolume, tuningProfile.BellGazeLedIntensity);
            ScheduleNextCueAfterPlayback(FinalDemoCueId.BellDistantCall, tuningProfile.BellGazeCallIntervalSeconds);
        }

        private void CompleteBellGazeHold(Vector3 bellPosition)
        {
            _bellGazeSuccessCount++;
            PlayReactiveBellCue(FinalDemoCueId.BellGazeSuccess, bellPosition, 1f, 1f);
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
                PlayReactiveBellCue(FinalDemoCueId.BellAcquisition, bellPosition, 1f, 1f);
                hapticRouter?.TriggerBellAcquisitionPulse();
                QueueNarration(FinalDemoCueId.NarrBellInHand, FinalDemoStage.BellAcquisition);
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
                _poseMatchEvaluator.RequireRotation = true;
                _poseMatchEvaluator.PositionToleranceMeters = tuningProfile.GeneralTinnitusPositionToleranceMeters;
                _poseMatchEvaluator.YawToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.PitchToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.RollToleranceDegrees = tuningProfile.GeneralTinnitusRotationToleranceDegrees;
                _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = tuningProfile.GeneralTinnitusMatchFeedbackRadiusMultiplier;
                _poseMatchEvaluator.TreatmentSeconds = tuningProfile.GeneralTinnitusTreatmentSeconds;

                FinalDemoPoseAuthoringMarker marker = ResolveGeneralTinnitusPoseMarker(stage);
                if (marker != null && marker.OverrideTolerances)
                {
                    _poseMatchEvaluator.PositionToleranceMeters = marker.PositionToleranceMeters;
                    _poseMatchEvaluator.YawToleranceDegrees = marker.RotationToleranceDegrees;
                    _poseMatchEvaluator.PitchToleranceDegrees = marker.RotationToleranceDegrees;
                    _poseMatchEvaluator.RollToleranceDegrees = marker.RotationToleranceDegrees;
                }

                ResolveGeneralTinnitusPoseTarget(stage, out Vector3 targetCameraSpace, out Vector3 targetYawPitchRoll);
                _poseMatchEvaluator.SetTargetPose(targetCameraSpace, targetYawPitchRoll.x, targetYawPitchRoll.y, targetYawPitchRoll.z);
                _poseMatchEvaluator.ResetProgress();
            }

            Vector3 targetWorldPosition = ResolveGeneralTinnitusWorldPosition(stage);
            _generalTinnitusStageWorldPosition = targetWorldPosition;
            _hasGeneralTinnitusStageWorldPosition = true;
            MoveGeneralTinnitusPlaceholder(stage, targetWorldPosition);
            StartProceduralTinnitus(targetWorldPosition);

            if (stage == FinalDemoStage.GeneralTinnitusOne)
            {
                QueueNarration(FinalDemoCueId.NarrApproachTinnitus, FinalDemoStage.GeneralTinnitusOne);
            }
        }

        private void TickGeneralTinnitus()
        {
            if (tuningProfile == null)
            {
                return;
            }

            EnsurePoseMatchEvaluator();
            Vector3 targetWorldPosition = _hasGeneralTinnitusStageWorldPosition
                ? _generalTinnitusStageWorldPosition
                : ResolveGeneralTinnitusWorldPosition(_currentStage);
            MoveGeneralTinnitusPlaceholder(_currentStage, targetWorldPosition);
            _currentGeneralTinnitusDistance = ResolvePlanarDistanceToPlayer(targetWorldPosition);

            if (_tinnitusAudioController != null)
            {
                _tinnitusAudioController.transform.position = targetWorldPosition;
            }

            if (_poseMatchEvaluator == null)
            {
                return;
            }

            if (!_generalTinnitusPoseLocked)
            {
                UpdateTinnitusLookAndLight(targetWorldPosition, 0f);
                if (_currentGeneralTinnitusDistance <= tuningProfile.GeneralTinnitusApproachRadius)
                {
                    LockGeneralTinnitusPoseCheck();
                }

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
            UpdateTinnitusHealingLoop(targetWorldPosition, cleanseProgress, isInsideTolerance);
            TickPoseMatchGuidanceHaptics(isInsideTolerance, false, ref _nextTinnitusHapticAtRealtime);
            if (isInsideTolerance && !_wasTinnitusInsideTolerance)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.TinnitusPoseLock, targetWorldPosition, 1f);
                _tinnitusAudioController?.TriggerBurst(0.45f);
                hapticRouter?.TriggerTinnitusLockPulse();
                if (_currentStage == FinalDemoStage.GeneralTinnitusOne)
                {
                    QueueNarration(FinalDemoCueId.NarrHoldPose, FinalDemoStage.GeneralTinnitusOne);
                }
            }
            else if (!isInsideTolerance && _wasTinnitusInsideTolerance)
            {
                audioRouter?.PlayOneShot(FinalDemoCueId.TinnitusPoseLost, targetWorldPosition, 0.65f);
            }

            _wasTinnitusInsideTolerance = isInsideTolerance;
            if (_currentStage == FinalDemoStage.GeneralTinnitusOne &&
                isLooking &&
                cleanseProgress <= 0.001f &&
                StageElapsedSeconds >= tuningProfile.BellAssistTimeoutSeconds)
            {
                QueueNarration(FinalDemoCueId.NarrFindTinnitusPose, FinalDemoStage.GeneralTinnitusOne);
            }

            if (isInsideTolerance && Time.realtimeSinceStartup >= _nextTinnitusHapticAtRealtime)
            {
                float intensity = Mathf.Lerp(0.3f, 0.75f, cleanseProgress);
                hapticRouter?.StartTinnitusCleanseHum(intensity, tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds * 1.4f);
                _nextTinnitusHapticAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds;
            }

            if (_poseMatchEvaluator.IsResolved)
            {
                CompleteGeneralTinnitus(targetWorldPosition);
            }
        }

        private void LockGeneralTinnitusPoseCheck()
        {
            _generalTinnitusPoseLocked = true;
            _wasTinnitusInsideTolerance = false;
            _poseMatchEvaluator?.ResetProgress();
            SetPlayerMovementLocked(true);
            hapticRouter?.TriggerTinnitusLockPulse();
            _nextTinnitusHapticAtRealtime = Time.realtimeSinceStartup + 0.35f;
        }

        private void TickPoseMatchGuidanceHaptics(bool insideTolerance, bool boss, ref float nextHapticAtRealtime)
        {
            if (_poseMatchEvaluator == null || Time.realtimeSinceStartup < nextHapticAtRealtime)
            {
                return;
            }

            if (insideTolerance)
            {
                float progress = _poseMatchEvaluator.Progress01;
                float intensity = Mathf.Lerp(0.3f, 0.75f, progress);
                if (boss)
                {
                    hapticRouter?.TriggerBossTrackingPulse();
                    nextHapticAtRealtime = Time.realtimeSinceStartup + 0.75f;
                    return;
                }

                hapticRouter?.StartTinnitusCleanseHum(intensity, tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds * 1.4f);
                nextHapticAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusCleanseHapticIntervalSeconds;
                return;
            }

            float match = Mathf.Clamp01(_poseMatchEvaluator.TotalMatch01);
            if (match < 0.18f)
            {
                nextHapticAtRealtime = Time.realtimeSinceStartup + 0.18f;
                return;
            }

            float guidance = Mathf.InverseLerp(0.18f, 0.78f, match);
            if (boss)
            {
                hapticRouter?.TriggerBossTrackingPulse();
                nextHapticAtRealtime = Time.realtimeSinceStartup + Mathf.Lerp(0.72f, 0.32f, guidance);
                return;
            }

            hapticRouter?.StartTinnitusCleanseHum(Mathf.Lerp(0.18f, 0.52f, guidance), Mathf.Lerp(0.18f, 0.38f, guidance));
            nextHapticAtRealtime = Time.realtimeSinceStartup + Mathf.Lerp(0.85f, 0.28f, guidance);
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
            lightRouter?.ShowTinnitusPattern(targetWorldPosition, intensity, cleanseProgress, false, tuningProfile.GeneralTinnitusRadiusScale);
            _nextTinnitusLightAtRealtime = Time.realtimeSinceStartup + tuningProfile.GeneralTinnitusLightIntervalSeconds;
            return true;
        }

        private void CompleteGeneralTinnitus(Vector3 targetWorldPosition)
        {
            FinalDemoStage completedStage = _currentStage;
            audioRouter?.StopLoop(FinalDemoCueId.TinnitusHealingLoop);
            StopProceduralTinnitus();
            PlayReactiveTinnitusCue(FinalDemoCueId.TinnitusResolve, targetWorldPosition, 1f, 0.85f, false);
            lightRouter?.Clear();
            ForceNextStage();
            if (completedStage == FinalDemoStage.GeneralTinnitusOne)
            {
                QueueNarrationDelayed(FinalDemoCueId.NarrNoiseStillExists, _currentStage, 2f);
            }
            else if (completedStage == FinalDemoStage.GeneralTinnitusTwo)
            {
                QueueNarrationDelayed(FinalDemoCueId.NarrBossAhead, _currentStage, 2f);
            }
        }

        private void BeginBossApproach()
        {
            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            audioRouter?.StartLoop(FinalDemoCueId.BossBasePulse, bossPosition, tuningProfile != null ? tuningProfile.BossBaseVolume : 0.42f);
            _bossReactiveLight = null;
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
                lightRouter?.ShowTinnitusPattern(bossPosition, tuningProfile.BossMassLedIntensity, 0f, true);
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
                _poseMatchEvaluator.RequireRotation = true;
                _poseMatchEvaluator.TreatmentSeconds = ResolveBossPatternSeconds(_currentBossPatternIndex);
                _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
                _poseMatchEvaluator.ResetProgress();
                UpdateBossPatternTarget();
            }

            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            audioRouter?.StartLoop(FinalDemoCueId.BossBasePulse, bossPosition, tuningProfile.BossBaseVolume);
            _bossReactiveLight = null;
            if (stage == FinalDemoStage.BossPatternOne)
            {
                QueueNarration(FinalDemoCueId.NarrFindSoundOrigin, FinalDemoStage.BossPatternOne);
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
                lightRouter?.ShowTinnitusPattern(bossPosition, Mathf.Clamp01(lightPulse), 0f, true);
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
            TickPoseMatchGuidanceHaptics(insideTolerance, true, ref _nextBossHapticAtRealtime);

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
            _poseMatchEvaluator.RequireRotation = true;
            if (_currentBossPatternIndex == 0 && moving)
            {
                QueueNarration(FinalDemoCueId.NarrBossTrack, FinalDemoStage.BossPatternOne);
            }

            Vector3 center = ResolveVectorAt(tuningProfile.BossWeakPointCentersCameraSpace, _currentBossPatternIndex, new Vector3(0f, 0f, 0.72f));
            Vector3 offset = ResolveVectorAt(tuningProfile.BossWeakPointMoveOffsetsCameraSpace, _currentBossPatternIndex, new Vector3(0.32f, 0.06f, 0.06f));
            Vector3 targetPosition = center + new Vector3(
                offset.x * smoothMove01,
                offset.y * smoothMove01 + Mathf.Sin(move01 * Mathf.PI) * 0.08f,
                offset.z * smoothMove01);
            Vector3 weakpointWorldPosition = PlayerCameraSpaceToWorld(targetPosition);

            if (TryResolveBossAuthoringPathTarget(_currentBossPatternIndex, moving ? move01 : 0f, out Vector3 authoredCameraSpace, out Vector3 authoredWorldPosition))
            {
                targetPosition = authoredCameraSpace;
                weakpointWorldPosition = authoredWorldPosition;
                MoveBossWeakpointVisual(authoredWorldPosition);
            }
            else
            {
                MoveBossWeakpointVisual(weakpointWorldPosition);
            }

            FinalDemoPoseAuthoringMarker marker = ResolveBossPoseMarker(_currentBossPatternIndex);
            Vector3 startRotation = marker != null ? marker.TargetYawPitchRollDegrees : Vector3.zero;
            Vector3 endRotation = ResolveVectorAt(tuningProfile.BossWeakPointEndYawPitchRoll, _currentBossPatternIndex, new Vector3(14f, -8f, 10f));
            float yaw = Mathf.Lerp(startRotation.x, endRotation.x, smoothMove01);
            float pitch = Mathf.Lerp(startRotation.y, endRotation.y, smoothMove01);
            float roll = Mathf.Lerp(startRotation.z, endRotation.z, smoothMove01);
            float toleranceMultiplier = 1f + _bossPatternFailureCount * tuningProfile.BossFailureToleranceGain;

            _poseMatchEvaluator.TreatmentSeconds = totalSeconds;
            _poseMatchEvaluator.PositionToleranceMeters = (moving ? tuningProfile.BossMovingToleranceMeters : tuningProfile.BossHoldPositionToleranceMeters) * toleranceMultiplier;
            _poseMatchEvaluator.YawToleranceDegrees = (moving ? tuningProfile.BossMovingToleranceDegrees : tuningProfile.BossHoldRotationToleranceDegrees) * toleranceMultiplier;
            _poseMatchEvaluator.PitchToleranceDegrees = _poseMatchEvaluator.YawToleranceDegrees;
            _poseMatchEvaluator.RollToleranceDegrees = _poseMatchEvaluator.YawToleranceDegrees;
            if (marker != null && marker.OverrideTolerances && !moving)
            {
                _poseMatchEvaluator.PositionToleranceMeters = marker.PositionToleranceMeters * toleranceMultiplier;
                _poseMatchEvaluator.YawToleranceDegrees = marker.RotationToleranceDegrees * toleranceMultiplier;
                _poseMatchEvaluator.PitchToleranceDegrees = marker.RotationToleranceDegrees * toleranceMultiplier;
                _poseMatchEvaluator.RollToleranceDegrees = marker.RotationToleranceDegrees * toleranceMultiplier;
            }

            _poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
            _poseMatchEvaluator.SetTargetPose(targetPosition, yaw, pitch, roll);
            _currentBossPatternProgress01 = _poseMatchEvaluator.Progress01;
            _currentBossPatternTargetMatch01 = _poseMatchEvaluator.TotalMatch01;
            UpdateBossWeakpointMoveLoop(weakpointWorldPosition, Mathf.Lerp(0.32f, 0.52f, smoothMove01), moving);
        }

        private void ResetCurrentBossPatternAfterMiss(Vector3 bossPosition)
        {
            _bossPatternFailureCount++;
            _bossWasInsideTolerance = false;
            _poseMatchEvaluator.ResetProgress();
            UpdateBossPatternTarget();
            PlayReactiveTinnitusCue(FinalDemoCueId.BossGlitchBurst, bossPosition, 0.75f, 1f, true);
            hapticRouter?.TriggerBossFailurePulse();
        }

        private void CompleteBossPattern(Vector3 bossPosition)
        {
            PlayReactiveTinnitusCue(FinalDemoCueId.BossHit, bossPosition, 1f, 1f, true);
            hapticRouter?.TriggerBossHitPulse();
            ForceNextStage();
        }

        private void BeginBossDefeat()
        {
            Vector3 bossPosition = ResolveBossPosition();
            MoveBossPlaceholder(bossPosition);
            lightRouter?.Clear();
            PlayReactiveTinnitusCue(FinalDemoCueId.BossDefeatRise, bossPosition, 1f, 1f, true);
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
                PlayReactiveBellCue(FinalDemoCueId.ForestBell, bellPosition, 0.7f, tuningProfile.ForestBellLedIntensity);
                ScheduleForestBellAfterPlayback();
            }

            if (_currentForestDistance <= tuningProfile.ForestBellArrivalRadius || StageElapsedSeconds >= tuningProfile.ForestAutoEndSeconds)
            {
                ForceNextStage();
            }
        }

        private AudioSource PlayReactiveBellCue(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale, float intensityScale)
        {
            AudioSource source = audioRouter?.PlayOneShot(cueId, worldPosition, volumeScale);
            float cueLength = audioRouter != null ? audioRouter.GetCueLengthSeconds(cueId) : 0.2f;
            _bellLightActiveUntilRealtime = Mathf.Max(_bellLightActiveUntilRealtime, Time.realtimeSinceStartup + Mathf.Max(0.1f, cueLength));
            lightRouter?.ShowBellWave(worldPosition, 0.22f, 0.32f, intensityScale);
            AttachReactiveLight(source, worldPosition, FinalDemoAudioReactiveLight.ReactiveLightKind.Bell, intensityScale);
            return source;
        }

        private void UpdateBellMovementTextureLoop(Vector3 worldPosition, float volumeScale, bool moving)
        {
            if (audioRouter == null)
            {
                return;
            }

            if (!moving || volumeScale <= 0.001f)
            {
                audioRouter.StopLoop(FinalDemoCueId.BellMovementTexture);
                return;
            }

            audioRouter.StartLoop(FinalDemoCueId.BellMovementTexture, worldPosition, volumeScale);
            if (Time.realtimeSinceStartup >= _nextBellOrbitAnchorAtRealtime)
            {
                lightRouter?.ShowBellAnchor(worldPosition, Mathf.Clamp01(volumeScale * 0.35f));
                float interval = tuningProfile != null ? tuningProfile.SoundReactiveLedUpdateIntervalSeconds : 0.12f;
                _nextBellOrbitAnchorAtRealtime = Time.realtimeSinceStartup + interval;
            }
        }

        private void UpdateTinnitusHealingLoop(Vector3 worldPosition, float cleanseProgress, bool enabled)
        {
            if (audioRouter == null)
            {
                return;
            }

            if (!enabled)
            {
                audioRouter.StopLoop(FinalDemoCueId.TinnitusHealingLoop);
                return;
            }

            float loopVolume = Mathf.Lerp(0.24f, 0.52f, Mathf.Clamp01(cleanseProgress));
            audioRouter.StartLoop(FinalDemoCueId.TinnitusHealingLoop, worldPosition, loopVolume);
        }

        private void UpdateBossWeakpointMoveLoop(Vector3 worldPosition, float volumeScale, bool moving)
        {
            if (audioRouter == null)
            {
                return;
            }

            if (!moving || volumeScale <= 0.001f)
            {
                audioRouter.StopLoop(FinalDemoCueId.BossWeakpointMove);
                return;
            }

            audioRouter.StartLoop(FinalDemoCueId.BossWeakpointMove, worldPosition, volumeScale);
        }

        private AudioSource PlayReactiveTinnitusCue(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale, float intensityScale, bool boss)
        {
            AudioSource source = audioRouter?.PlayOneShot(cueId, worldPosition, volumeScale);
            lightRouter?.ShowTinnitusPattern(worldPosition, intensityScale, boss ? 0f : GeneralTinnitusProgress01, boss, boss ? 1f : tuningProfile != null ? tuningProfile.GeneralTinnitusRadiusScale : 1f);
            return source;
        }

        private FinalDemoAudioReactiveLight AttachReactiveLight(
            AudioSource source,
            Vector3 worldPosition,
            FinalDemoAudioReactiveLight.ReactiveLightKind kind,
            float intensityScale)
        {
            if (source == null || lightRouter == null || tuningProfile == null || !tuningProfile.SoundReactiveLedEnabled)
            {
                return null;
            }

            FinalDemoAudioReactiveLight reactiveLight = source.GetComponent<FinalDemoAudioReactiveLight>() ?? source.gameObject.AddComponent<FinalDemoAudioReactiveLight>();
            float sensitivity = kind == FinalDemoAudioReactiveLight.ReactiveLightKind.Bell
                ? tuningProfile.BellSoundReactiveSensitivity
                : tuningProfile.TinnitusSoundReactiveSensitivity;
            reactiveLight.Configure(
                source,
                lightRouter,
                kind,
                worldPosition,
                intensityScale,
                sensitivity,
                tuningProfile.SoundReactiveLedUpdateIntervalSeconds);
            return reactiveLight;
        }

        private void UpdateBossReactiveLight(Vector3 bossPosition, float intensityScale)
        {
            if (_bossReactiveLight == null)
            {
                return;
            }

            _bossReactiveLight.SetWorldPosition(bossPosition);
            _bossReactiveLight.IntensityScale = intensityScale;
            _bossReactiveLight.EmissionEnabled = true;
        }

        private void ScheduleNextCueAfterPlayback(FinalDemoCueId cueId, float requestedIntervalSeconds)
        {
            float clipSeconds = audioRouter != null ? audioRouter.GetCueLengthSeconds(cueId) : 0f;
            float nextDelay = Mathf.Max(requestedIntervalSeconds, clipSeconds + (tuningProfile != null ? tuningProfile.BellCallPostClipGapSeconds : 0.18f));
            _nextBellCallAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.05f, nextDelay);
        }

        private void ScheduleForestBellAfterPlayback()
        {
            float clipSeconds = audioRouter != null ? audioRouter.GetCueLengthSeconds(FinalDemoCueId.ForestBell) : 0f;
            float nextDelay = tuningProfile != null ? tuningProfile.ForestBellCallIntervalSeconds : 2.4f;
            nextDelay = Mathf.Max(nextDelay, clipSeconds + (tuningProfile != null ? tuningProfile.BellCallPostClipGapSeconds : 0.18f));
            _nextForestBellAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.05f, nextDelay);
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

        private void ApplyTuningSerialSettings()
        {
            if (tuningProfile == null)
            {
                return;
            }

            HardwareBridge hardwareBridge = HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>();
            if (hardwareBridge != null)
            {
                ApplySerialPortSetting(
                    HardwareBridge.SerialPortEnvName,
                    tuningProfile.HardwareSerialPort,
                    hardwareBridge.SetPreferredPortName);
                ApplySerialBaudSetting(HardwareBridge.SerialBaudEnvName, 115200, hardwareBridge.SetBaudRate);
            }

            HeadImuReceiver headImuReceiver = FindFirstObjectByType<HeadImuReceiver>();
            if (headImuReceiver != null)
            {
                if (!HasEnvironmentOverride(HeadImuReceiver.SerialPortEnvName))
                {
                    headImuReceiver.SetUseSharedHardwareBridgeTelemetry(true);
                    headImuReceiver.SetPreferredPortName(string.Empty);
                }
                else
                {
                    UnityEngine.Debug.LogWarning(
                        $"[FinalDemoDirector] {HeadImuReceiver.SerialPortEnvName} is set. Head IMU can fall back to that dedicated override if shared HardwareBridge telemetry is unavailable.");
                }

                ApplySerialBaudSetting(HeadImuReceiver.SerialBaudEnvName, 230400, headImuReceiver.SetBaudRate);
            }

            PadImuReceiver padImuReceiver = FindFirstObjectByType<PadImuReceiver>();
            if (padImuReceiver != null)
            {
                ApplySerialPortSetting(
                    PadImuReceiver.SerialPortEnvName,
                    tuningProfile.PadImuSerialPort,
                    padImuReceiver.SetPreferredPortName);
                ApplySerialBaudSetting(PadImuReceiver.SerialBaudEnvName, 230400, padImuReceiver.SetBaudRate);
            }
        }

        private static void ApplySerialPortSetting(string environmentVariableName, string profilePortName, Action<string> apply)
        {
            string overrideValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                if (!string.IsNullOrWhiteSpace(profilePortName) &&
                    !string.Equals(overrideValue.Trim(), profilePortName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[FinalDemoDirector] {environmentVariableName}={overrideValue.Trim()} overrides tuning profile port {profilePortName.Trim()}.");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(profilePortName))
            {
                apply(profilePortName);
            }
        }

        private static void ApplySerialBaudSetting(string environmentVariableName, int profileBaudRate, Action<int> apply)
        {
            if (HasEnvironmentOverride(environmentVariableName))
            {
                return;
            }

            apply(profileBaudRate);
        }

        private static bool HasEnvironmentOverride(string environmentVariableName)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariableName));
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

            audioRouter.Initialize(cueLibrary, ResolveAudioListenerTransform());
            audioRouter.SetSpatializerEnabled(HrtfPreviewEnabled);
            lightRouter.Initialize(ResolveAudioListenerTransform(), HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>());
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

        private void EnsurePresentationHelpers()
        {
            FinalDemoKoreanGuide guide = GetComponent<FinalDemoKoreanGuide>();
            if (guide == null)
            {
                guide = gameObject.AddComponent<FinalDemoKoreanGuide>();
            }

            FinalDemoModelPresenter presenter = GetComponent<FinalDemoModelPresenter>();
            if (presenter == null)
            {
                presenter = gameObject.AddComponent<FinalDemoModelPresenter>();
            }

            presenter.ConfigureTargets(
                sceneReferences != null ? sceneReferences.BellVisual : FindNamedTransform("FinalDemo_BellPlaceholder"),
                sceneReferences != null ? sceneReferences.PadVisual : FindNamedTransform("PadVisual_Authoring_FollowsPose"));
            presenter.ApplyModelVisuals();

            RemoveLegacyEnvironmentAuthoringObjects();
            EnsureEnvironmentPresenters();
            EnsureGlitchVisuals();
            EnsureGlobalGlitchOverlay();
        }

        private void EnsureGlitchVisuals()
        {
            AttachGlitchVisual(sceneReferences != null ? sceneReferences.TinnitusOneVisual : FindNamedTransform("FinalDemo_TinnitusA"), false);
            AttachGlitchVisual(sceneReferences != null ? sceneReferences.TinnitusTwoVisual : FindNamedTransform("FinalDemo_TinnitusB"), false);
            AttachGlitchVisual(sceneReferences != null ? sceneReferences.BossVisual : FindNamedTransform("FinalDemo_BossTinnitus"), true);
        }

        private void EnsureGlobalGlitchOverlay()
        {
            _globalGlitchOverlay = FindFirstObjectByType<FinalDemoGlobalGlitchOverlay>();
            if (_globalGlitchOverlay == null)
            {
                GameObject overlayObject = new GameObject("FinalDemoGlobalGlitchOverlay");
                overlayObject.transform.SetParent(transform, false);
                _globalGlitchOverlay = overlayObject.AddComponent<FinalDemoGlobalGlitchOverlay>();
            }

            _globalGlitchOverlay.Initialize(playerCamera != null ? playerCamera : Camera.main);
        }

        private static void AttachGlitchVisual(Transform target, bool boss)
        {
            if (target == null)
            {
                return;
            }

            FinalDemoGlitchVisual glitch = target.GetComponent<FinalDemoGlitchVisual>();
            if (glitch == null)
            {
                glitch = target.gameObject.AddComponent<FinalDemoGlitchVisual>();
            }

            glitch.Configure(boss);
        }

        private void RemoveLegacyEnvironmentAuthoringObjects()
        {
            string[] objectNames =
            {
                "RainSkySheet",
                "RainFogVolume",
                "RainSkyParticles",
                "RainGroundRippleParticles",
                "ClearBlueSky_Forest_Authoring",
                "BackWall_White_Authoring",
                "LeftSoftWall_Authoring",
                "RightSoftWall_Authoring",
                "Ground_Grey_Authoring",
                "DistantFogBand",
                "DarkSkyPlane",
            };

            foreach (string objectName in objectNames)
            {
                Transform target = FindNamedTransform(objectName);
                if (target == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(target.gameObject);
                }
                else
                {
                    DestroyImmediate(target.gameObject);
                }
            }
        }

        private void EnsureEnvironmentPresenters()
        {
            Transform rainRoot = sceneReferences != null ? sceneReferences.RainRoot : null;
            Transform forestRoot = sceneReferences != null ? sceneReferences.ForestRoot : null;
            Transform worldRoot = sceneReferences != null ? sceneReferences.WorldRoot : transform;

            _worldRainEnvironment = FindFirstObjectByType<FinalDemoWorldRainEnvironment>();
            if (_worldRainEnvironment == null)
            {
                GameObject rainObject = new GameObject("FinalDemoWorldRainEnvironment");
                rainObject.transform.SetParent(rainRoot != null ? rainRoot : worldRoot, false);
                _worldRainEnvironment = rainObject.AddComponent<FinalDemoWorldRainEnvironment>();
            }

            _forestEnvironment = FindFirstObjectByType<FinalDemoForestEnvironment>();
            if (_forestEnvironment == null)
            {
                GameObject forestObject = new GameObject("FinalDemoForestEnvironment");
                forestObject.transform.SetParent(forestRoot != null ? forestRoot : worldRoot, false);
                _forestEnvironment = forestObject.AddComponent<FinalDemoForestEnvironment>();
            }

            _worldFloorSurface = FindFirstObjectByType<FinalDemoWorldFloorSurface>();
            if (_worldFloorSurface == null)
            {
                GameObject floorObject = new GameObject("FinalDemoWorldFloorSurface");
                floorObject.transform.SetParent(worldRoot, false);
                _worldFloorSurface = floorObject.AddComponent<FinalDemoWorldFloorSurface>();
            }
        }

        private void EnsurePlaceholderWorld()
        {
            EnsurePlaceholder("FinalDemo_BellPlaceholder", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 2.4f), new Vector3(0.22f, 0.22f, 0.22f), new Color(0.22f, 0.85f, 0.34f));
            EnsurePlaceholder("FinalDemo_TinnitusA", PrimitiveType.Sphere, new Vector3(-1.2f, 1.45f, 2.8f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.30f, 0.82f, 0.38f));
            EnsurePlaceholder("FinalDemo_TinnitusB", PrimitiveType.Sphere, new Vector3(1.25f, 1.35f, 3.1f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.30f, 0.82f, 0.38f));
            EnsurePlaceholder("FinalDemo_BossTinnitus", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 4.2f), new Vector3(0.42f, 0.42f, 0.42f), new Color(0.24f, 0.78f, 0.33f));
            EnsurePlaceholder("FinalDemo_RainFloor", PrimitiveType.Cube, new Vector3(0f, -0.02f, 2f), new Vector3(12f, 0.02f, 12f), new Color(0.42f, 0.44f, 0.46f));
            EnsurePlaceholder("FinalDemo_WallNoisePlane", PrimitiveType.Cube, new Vector3(0f, 1.25f, 5f), new Vector3(8f, 2.7f, 0.05f), new Color(0.92f, 0.94f, 0.95f));
        }

        private void MovePlaceholdersForStage(FinalDemoStage stage)
        {
            Transform bell = FindNamedTransform("FinalDemo_BellPlaceholder");
            if (bell != null)
            {
                bell.position = stage switch
                {
                    FinalDemoStage.BellOrbit => new Vector3(0.8f, 1.65f, 1.6f),
                    FinalDemoStage.BellFollowOne => ResolveBellFollowTargetPosition(false),
                    FinalDemoStage.BellFollowRain => ResolveBellFollowTargetPosition(true),
                    FinalDemoStage.BellGaze => ResolveBellGazeCuePosition(_bellGazeSuccessCount),
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
                MoveBellPlaceholder(ResolveForestBellPosition());
            }
        }

        private void MoveBellPlaceholder(Vector3 worldPosition)
        {
            Transform bell = sceneReferences != null && sceneReferences.BellVisual != null
                ? sceneReferences.BellVisual
                : FindNamedTransform("FinalDemo_BellPlaceholder");
            if (bell != null)
            {
                bell.position = worldPosition;
            }
        }

        private Transform ResolveAudioListenerTransform()
        {
            if (playerCamera != null)
            {
                return playerCamera.transform;
            }

            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener != null)
            {
                return listener.transform;
            }

            return playerRig;
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
            Transform bell = sceneReferences != null && sceneReferences.BellVisual != null
                ? sceneReferences.BellVisual
                : FindNamedTransform("FinalDemo_BellPlaceholder");
            return bell != null ? bell.position : ResolvePlayerRelativePosition(Vector3.forward * 1.4f);
        }

        private Vector3 ResolveBellFollowTargetPosition(bool rainStage)
        {
            FinalDemoAuthoringPath path = sceneReferences != null ? sceneReferences.BellFollowAuthoringPath : null;
            int index = rainStage ? 1 : 0;
            if (path != null && path.TryGetWorldPoint(index, out Vector3 authoredPosition))
            {
                return authoredPosition;
            }

            if (tuningProfile != null)
            {
                return rainStage ? tuningProfile.BellFollowTargetTwoPosition : tuningProfile.BellFollowTargetOnePosition;
            }

            return rainStage ? new Vector3(1.4f, 1.5f, 3.6f) : new Vector3(-1.4f, 1.5f, 3.2f);
        }

        private Vector3 ResolveBellGazeCuePosition(int cueIndex)
        {
            FinalDemoAuthoringPath path = sceneReferences != null ? sceneReferences.BellGazeAuthoringPath : null;
            if (path != null && path.TryGetWorldPoint(cueIndex, out Vector3 authoredPosition))
            {
                return authoredPosition;
            }

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
            controller.SetSpatialRangeScale(tuningProfile != null ? tuningProfile.GeneralTinnitusRadiusScale : 1f);
            controller.SetBinauralPreview(ResolveAudioListenerTransform(), HrtfPreviewEnabled);
            _tinnitusAudioController = controller;
            _tinnitusReactiveLight = null;
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
            if (_tinnitusReactiveLight != null)
            {
                _tinnitusReactiveLight.EmissionEnabled = false;
                _tinnitusReactiveLight = null;
            }
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
            Transform authored = stage == FinalDemoStage.GeneralTinnitusTwo
                ? sceneReferences != null ? sceneReferences.TinnitusTwoVisual : null
                : sceneReferences != null ? sceneReferences.TinnitusOneVisual : null;
            if (authored != null)
            {
                return authored.position;
            }

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
            Transform tinnitus = stage == FinalDemoStage.GeneralTinnitusTwo
                ? sceneReferences != null ? sceneReferences.TinnitusTwoVisual : null
                : sceneReferences != null ? sceneReferences.TinnitusOneVisual : null;
            if (tinnitus == null)
            {
                string objectName = stage == FinalDemoStage.GeneralTinnitusTwo ? "FinalDemo_TinnitusB" : "FinalDemo_TinnitusA";
                tinnitus = FindNamedTransform(objectName);
            }

            if (tinnitus != null)
            {
                tinnitus.position = worldPosition;
            }
        }

        private Vector3 ResolveBossPosition()
        {
            if (sceneReferences != null && sceneReferences.BossVisual != null)
            {
                return sceneReferences.BossVisual.position;
            }

            return tuningProfile != null ? tuningProfile.BossWorldPosition : new Vector3(0f, 1.6f, 4.2f);
        }

        private void MoveBossPlaceholder(Vector3 worldPosition)
        {
            Transform boss = sceneReferences != null && sceneReferences.BossVisual != null
                ? sceneReferences.BossVisual
                : FindNamedTransform("FinalDemo_BossTinnitus");
            if (boss != null)
            {
                boss.position = worldPosition;
            }
        }

        private void MoveBossWeakpointVisual(Vector3 worldPosition)
        {
            Transform weakpoint = FindNamedTransform("BossWeakpoint_Current");
            if (weakpoint != null)
            {
                weakpoint.position = worldPosition;
            }
        }

        private FinalDemoPoseAuthoringMarker ResolveGeneralTinnitusPoseMarker(FinalDemoStage stage)
        {
            if (sceneReferences == null)
            {
                return null;
            }

            return stage == FinalDemoStage.GeneralTinnitusTwo
                ? sceneReferences.TinnitusTwoHealMarker
                : sceneReferences.TinnitusOneHealMarker;
        }

        private void ResolveGeneralTinnitusPoseTarget(FinalDemoStage stage, out Vector3 cameraSpacePosition, out Vector3 yawPitchRoll)
        {
            FinalDemoPoseAuthoringMarker marker = ResolveGeneralTinnitusPoseMarker(stage);
            if (marker != null)
            {
                cameraSpacePosition = marker.TargetCameraSpacePosition;
                yawPitchRoll = marker.TargetYawPitchRollDegrees;
                return;
            }

            cameraSpacePosition = stage == FinalDemoStage.GeneralTinnitusOne
                ? tuningProfile.GeneralTinnitusOnePadTargetCameraSpace
                : tuningProfile.GeneralTinnitusTwoPadTargetCameraSpace;
            yawPitchRoll = stage == FinalDemoStage.GeneralTinnitusOne
                ? tuningProfile.GeneralTinnitusOnePadTargetYawPitchRoll
                : tuningProfile.GeneralTinnitusTwoPadTargetYawPitchRoll;
        }

        private FinalDemoPoseAuthoringMarker ResolveBossPoseMarker(int patternIndex)
        {
            if (sceneReferences == null)
            {
                return null;
            }

            return patternIndex switch
            {
                1 => sceneReferences.BossPoseTwoMarker,
                2 => sceneReferences.BossPoseThreeMarker,
                _ => sceneReferences.BossPoseOneMarker,
            };
        }

        private FinalDemoAuthoringPath ResolveBossAuthoringPath(int patternIndex)
        {
            if (sceneReferences == null)
            {
                return null;
            }

            return patternIndex switch
            {
                1 => sceneReferences.BossWeakpointPathTwoAuthoring,
                2 => sceneReferences.BossWeakpointPathThreeAuthoring,
                _ => sceneReferences.BossWeakpointPathOneAuthoring,
            };
        }

        private bool TryResolveBossAuthoringPathTarget(int patternIndex, float move01, out Vector3 cameraSpacePosition, out Vector3 worldPosition)
        {
            FinalDemoAuthoringPath path = ResolveBossAuthoringPath(patternIndex);
            if (path == null || !path.TryEvaluateWorldPosition01(move01, out worldPosition))
            {
                cameraSpacePosition = default;
                worldPosition = default;
                return false;
            }

            cameraSpacePosition = WorldToPlayerCameraSpace(worldPosition);
            return true;
        }

        private Vector3 WorldToPlayerCameraSpace(Vector3 worldPosition)
        {
            Transform reference = playerCamera != null ? playerCamera.transform : playerRig;
            return reference != null ? reference.InverseTransformPoint(worldPosition) : worldPosition;
        }

        private Vector3 PlayerCameraSpaceToWorld(Vector3 cameraSpacePosition)
        {
            Transform reference = playerCamera != null ? playerCamera.transform : playerRig;
            return reference != null ? reference.TransformPoint(cameraSpacePosition) : cameraSpacePosition;
        }

        private Vector3 ResolveForestBellPosition()
        {
            if (sceneReferences != null && sceneReferences.ForestSet != null)
            {
                return sceneReferences.ForestSet.position + new Vector3(0f, 1.45f, 0.8f);
            }

            return tuningProfile != null ? tuningProfile.ForestBellWorldPosition : new Vector3(0f, 1.6f, 5.2f);
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
            FinalDemoAuthoringPath authoredPath = sceneReferences != null ? sceneReferences.BellOrbitAuthoringPath : null;
            if (authoredPath != null && authoredPath.Count > 0 && (tuningProfile == null || !tuningProfile.BellOrbitPreferProfilePath))
            {
                float authoredOrbitDuration = tuningProfile != null ? tuningProfile.BellOrbitSeconds : 1f;
                float authoredNormalized = EvaluateBellOrbitProgress(elapsedSeconds / Mathf.Max(0.01f, authoredOrbitDuration));
                if (authoredPath.TryEvaluateWorldPosition01(authoredNormalized, out Vector3 authoredPosition))
                {
                    float distance = playerRig != null ? Vector3.Distance(playerRig.position, authoredPosition) : 1.2f;
                    float nearIntensity = tuningProfile != null ? tuningProfile.BellOrbitNearIntensity : 0.85f;
                    float farIntensity = tuningProfile != null ? tuningProfile.BellOrbitFarIntensity : 0.35f;
                    float authoredDistance01 = Mathf.InverseLerp(0.65f, 2.2f, distance);
                    intensity = Mathf.Lerp(nearIntensity, farIntensity, authoredDistance01);
                    return authoredPosition;
                }
            }

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
            float normalized = EvaluateBellOrbitProgress(elapsedSeconds / Mathf.Max(0.01f, orbitDuration));
            float scaled = normalized * (points.Length - 1);
            int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, points.Length - 2);
            int nextIndex = segmentIndex + 1;
            float segmentT = scaled - segmentIndex;
            Vector3 localPosition = tuningProfile != null && tuningProfile.BellOrbitUseSmoothSplinePath && points.Length >= 4
                ? ResolveOpenCatmullRom(points, segmentIndex, segmentT)
                : Vector3.Lerp(points[segmentIndex], points[nextIndex], Smooth01(segmentT));
            float distance01 = Mathf.InverseLerp(0.65f, 2.2f, localPosition.magnitude);
            intensity = Mathf.Lerp(tuningProfile.BellOrbitNearIntensity, tuningProfile.BellOrbitFarIntensity, distance01);
            return ResolvePlayerRelativePosition(localPosition);
        }

        private float EvaluateBellOrbitProgress(float normalizedElapsed)
        {
            float time01 = Mathf.Clamp01(normalizedElapsed);
            AnimationCurve curve = tuningProfile != null ? tuningProfile.BellOrbitProgressCurve : null;
            float curved = curve != null && curve.length > 0 ? curve.Evaluate(time01) : time01;
            return Mathf.Min(0.9999f, Mathf.Clamp01(curved));
        }

        private static Vector3 ResolveOpenCatmullRom(Vector3[] points, int segmentIndex, float t)
        {
            int count = points.Length;
            Vector3 p0 = points[Mathf.Clamp(segmentIndex - 1, 0, count - 1)];
            Vector3 p1 = points[Mathf.Clamp(segmentIndex, 0, count - 1)];
            Vector3 p2 = points[Mathf.Clamp(segmentIndex + 1, 0, count - 1)];
            Vector3 p3 = points[Mathf.Clamp(segmentIndex + 2, 0, count - 1)];
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
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
                renderer.sharedMaterial = CreateRuntimeMaterial(color);
            }

            return placeholder;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Simple Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = color,
            };
            return material;
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

        private static bool ShouldKeepOpeningAmbience(FinalDemoStage stage)
        {
            return stage >= FinalDemoStage.OpeningAmbience && stage <= FinalDemoStage.BossDefeat;
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
