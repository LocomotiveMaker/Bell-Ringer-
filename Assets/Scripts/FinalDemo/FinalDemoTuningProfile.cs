using UnityEngine;

namespace BellRinger.FinalDemo
{
    [CreateAssetMenu(menuName = "Bell Ringer/Final Demo Tuning Profile", fileName = "FinalDemoTuningProfile")]
    public sealed class FinalDemoTuningProfile : ScriptableObject
    {
        [Header("실행/입력 / Runtime")]
        [SerializeField] private bool autoAdvancePlaceholderStages;
        [SerializeField] private float placeholderStageSeconds = 3f;
        [SerializeField] private bool startWithMovementLocked = true;
        [SerializeField] private FinalDemoAssistLevel defaultAssistLevel = FinalDemoAssistLevel.Gentle;

        [Header("입력 포트 / Device Ports")]
        [SerializeField] private string hardwareSerialPort = "COM40";
        [SerializeField] private string headImuSerialPort = "COM40";
        [SerializeField] private string padImuSerialPort = "COM30";
        [SerializeField] private int padTrackingUdpPort = 39051;

        [Header("플레이어 / Player")]
        [SerializeField] private float playerFixedHeight = 1.6f;
        [SerializeField] private Vector3 playerStartPosition = new Vector3(0f, 1.6f, -1.5f);

        [Header("오디오 / Opening Audio")]
        [SerializeField, Range(0f, 1f)] private float preflightAmbienceVolume = 0.14f;
        [SerializeField] private float openingAmbienceSeconds = 5f;
        [SerializeField] private float openingAmbienceFadeSeconds = 1.6f;
        [SerializeField, Range(0f, 1f)] private float openingAmbienceVolume = 0.32f;
        [SerializeField] private float openingSilenceSeconds = 1.6f;
        [SerializeField] private float openingCloseBellSeconds = 2.2f;
        [SerializeField] private Vector3 openingCloseBellOffset = new Vector3(-0.55f, 0.02f, 0.35f);
        [SerializeField, Range(0f, 1f)] private float openingCloseBellVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float openingCloseBellLedIntensity = 0.75f;

        [Header("LED / Sound-Reactive Light")]
        [SerializeField] private bool soundReactiveLedEnabled = true;
        [SerializeField] private float soundReactiveLedUpdateIntervalSeconds = 0.09f;
        [SerializeField] private float bellSoundReactiveSensitivity = 1.45f;
        [SerializeField] private float tinnitusSoundReactiveSensitivity = 1.25f;
        [SerializeField] private float bellCallPostClipGapSeconds = 0.32f;

        [Header("종 경로 / Bell Orbit")]
        [SerializeField] private float bellOrbitSeconds = 19.1f;
        [SerializeField] private float bellOrbitCallIntervalSeconds = 0.39f;
        [SerializeField] private bool bellOrbitPreferProfilePath = true;
        [SerializeField] private float bellOrbitToFirstTargetMoveSeconds = 2.2f;
        [SerializeField] private float bellMovementTextureFadeSeconds = 0.15f;
        [SerializeField, Range(0f, 1f)] private float bellMovementTextureVolume = 0.48f;
        [SerializeField] private bool bellOrbitUseSmoothSplinePath = true;
        [SerializeField] private AnimationCurve bellOrbitProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool bellOrbitContinuousAnchorEnabled = true;
        [SerializeField] private float bellOrbitContinuousAnchorUpdateIntervalSeconds = 0.08f;
        [SerializeField, Range(0f, 1f)] private float bellOrbitContinuousAnchorIntensity = 0.38f;
        [SerializeField] private Vector3[] bellOrbitLocalPoints =
        {
            new Vector3(-1.35f, 0.02f, 0.85f),
            new Vector3(0f, 0f, 1.7f),
            new Vector3(1.35f, -0.02f, 0.85f),
            new Vector3(0f, 0f, -0.85f),
            new Vector3(-1.35f, 0.02f, 0.85f),
            new Vector3(0f, 0f, 1.7f),
            new Vector3(0f, 0.72f, 1.45f),
            new Vector3(0f, 0f, -0.85f),
            new Vector3(0f, -0.65f, 0.9f),
            new Vector3(0f, 0f, 1.7f),
        };
        [SerializeField, Range(0f, 1f)] private float bellOrbitNearIntensity = 0.78f;
        [SerializeField, Range(0f, 1f)] private float bellOrbitFarIntensity = 0.42f;
        [SerializeField, Range(0f, 1f)] private float bellOrbitVolume = 0.68f;

        [Header("종 경로 / Bell Follow")]
        [SerializeField] private int bellFollowTargetCount = 2;
        [SerializeField] private Vector3 bellFollowTargetOnePosition = new Vector3(-1.4f, 1.5f, 3.2f);
        [SerializeField] private Vector3 bellFollowTargetTwoPosition = new Vector3(1.4f, 1.5f, 3.6f);
        [SerializeField] private float bellFollowRelocationSeconds = 2.6f;
        [SerializeField] private float bellArrivalRadius = 0.8f;
        [SerializeField] private float bellFollowCallIntervalSeconds = 2.3f;
        [SerializeField] private float bellFollowInitialCallIntervalSeconds = 11f;
        [SerializeField] private float bellFollowMinimumCallIntervalSeconds = 8f;
        [SerializeField] private float bellFollowMissIntervalReductionSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] private float bellFollowVolume = 0.62f;
        [SerializeField, Range(0f, 1f)] private float bellFollowLedIntensity = 0.62f;
        [SerializeField] private float bellAssistTimeoutSeconds = 7.5f;
        [SerializeField] private float bellAssistRepeatSeconds = 4f;
        [SerializeField] private float bellAssistGainMultiplier = 1.35f;
        [SerializeField] private bool bellFollowProgressBlockerEnabled = true;
        [SerializeField] private float bellFollowBlockerMarginMeters = 0.35f;

        [Header("패드 흔들기 / Pad Shake")]
        [SerializeField] private float padShakeAssistMotionThreshold = 0.58f;
        [SerializeField] private float padShakeAssistCooldownSeconds = 4.5f;
        [SerializeField] private float padShakeAssistNarrationCooldownSeconds = 19.8f;

        [Header("비/앰비언트 / Rain & Ambience")]
        [SerializeField] private Vector3 rainZoneCenter = new Vector3(0f, 1.6f, 2.9f);
        [SerializeField] private float rainZoneRadius = 2.4f;
        [SerializeField] private float rainIntensityRampSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float rainMaxIntensity = 0.58f;
        [SerializeField] private float rainWindTextureDelaySeconds = 1.8f;
        [SerializeField] private float rainFocusBellNarrationDelaySeconds = 8f;
        [SerializeField, Range(0f, 1f)] private float rainWindTextureMaxIntensity = 0.34f;
        [SerializeField, Range(0f, 1f)] private float rainAssistVolumeFloor = 0.24f;
        [SerializeField] private float rainCloseDropIntervalSeconds = 2.2f;
        [SerializeField] private int bellGazeRequiredSuccesses = 3;
        [SerializeField] private float bellGazeHoldSeconds = 1.4f;
        [SerializeField] private float bellGazeConeDegrees = 14f;
        [SerializeField] private float bellGazeAssistStartSeconds = 6f;
        [SerializeField] private float bellGazeAssistMaxConeDegrees = 28f;
        [SerializeField] private float bellGazeMoveSeconds = 1.1f;
        [SerializeField] private float bellGazeCallIntervalSeconds = 1.5f;
        [SerializeField, Range(0f, 1f)] private float bellGazeLedIntensity = 0.78f;
        [SerializeField] private Vector3[] bellGazeLocalOffsets =
        {
            new Vector3(0.15f, -0.02f, 1.45f),
            new Vector3(-0.85f, 0.06f, 1.75f),
            new Vector3(0.9f, -0.04f, 1.65f),
        };
        [SerializeField] private Vector3 bellAcquisitionLocalOffset = new Vector3(0f, -0.08f, 0.85f);
        [SerializeField] private float bellAcquisitionEffectSeconds = 1.8f;
        [SerializeField] private float bellAcquisitionTransitionSilenceSeconds = 0.8f;

        [Header("이명 / Tinnitus")]
        [SerializeField] private Vector3 generalTinnitusOneWorldPosition = new Vector3(-1.2f, 1.45f, 2.8f);
        [SerializeField] private Vector3 generalTinnitusTwoWorldPosition = new Vector3(1.25f, 1.35f, 3.1f);
        [SerializeField] private Vector3 generalTinnitusOnePadTargetCameraSpace = new Vector3(-0.08f, -0.02f, 0.66f);
        [SerializeField] private Vector3 generalTinnitusTwoPadTargetCameraSpace = new Vector3(0.1f, -0.02f, 0.72f);
        [SerializeField] private Vector3 generalTinnitusOnePadTargetYawPitchRoll = new Vector3(-12f, 8f, -14f);
        [SerializeField] private Vector3 generalTinnitusTwoPadTargetYawPitchRoll = new Vector3(18f, -10f, 16f);
        [SerializeField] private float generalTinnitusTreatmentSeconds = 4f;
        [SerializeField] private float generalTinnitusPositionToleranceMeters = 0.18f;
        [SerializeField] private float generalTinnitusRotationToleranceDegrees = 22f;
        [SerializeField] private float generalTinnitusMatchFeedbackRadiusMultiplier = 2.5f;
        [SerializeField] private float generalTinnitusRevealConeDegrees = 38f;
        [SerializeField, Range(0f, 1f)] private float generalTinnitusLedIntensity = 0.72f;
        [SerializeField, Range(0f, 1f)] private float generalTinnitusToneVolume = 0.8f;
        [SerializeField] private float generalTinnitusLightIntervalSeconds = 0.16f;
        [SerializeField] private float generalTinnitusCleanseHapticIntervalSeconds = 0.55f;

        [Header("대왕 이명 / Boss Tinnitus")]
        [SerializeField] private Vector3 bossWorldPosition = new Vector3(0f, 1.6f, 4.2f);
        [SerializeField] private float bossApproachRadius = 1.25f;
        [SerializeField] private float bossApproachAutoStartSeconds = 12f;
        [SerializeField, Range(0f, 1f)] private float bossBaseVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float bossMassLedIntensity = 0.78f;
        [SerializeField] private float bossPatternOneSeconds = 8f;
        [SerializeField] private float bossPatternTwoSeconds = 8.5f;
        [SerializeField] private float bossPatternThreeSeconds = 9f;
        [SerializeField] private float bossOpeningHoldSeconds = 2f;
        [SerializeField] private float bossHoldPositionToleranceMeters = 0.18f;
        [SerializeField] private float bossHoldRotationToleranceDegrees = 24f;
        [SerializeField] private float bossMovingToleranceMeters = 0.42f;
        [SerializeField] private float bossMovingToleranceDegrees = 38f;
        [SerializeField] private float bossFailureToleranceGain = 0.14f;
        [SerializeField] private float bossLightIntervalSeconds = 0.18f;
        [SerializeField] private Vector3[] bossWeakPointCentersCameraSpace =
        {
            new Vector3(-0.12f, -0.04f, 0.72f),
            new Vector3(0.1f, 0.03f, 0.7f),
            new Vector3(0f, -0.02f, 0.76f),
        };
        [SerializeField] private Vector3[] bossWeakPointMoveOffsetsCameraSpace =
        {
            new Vector3(0.34f, 0.06f, 0.04f),
            new Vector3(-0.32f, 0.1f, 0.08f),
            new Vector3(0.22f, -0.12f, 0.06f),
        };
        [SerializeField] private Vector3[] bossWeakPointEndYawPitchRoll =
        {
            new Vector3(14f, -8f, 10f),
            new Vector3(-18f, 10f, -12f),
            new Vector3(22f, -12f, 18f),
        };

        [Header("엔딩 / Ending")]
        [SerializeField] private float bossDefeatEffectSeconds = 3.2f;
        [SerializeField] private float bossDefeatSilenceSeconds = 1.2f;
        [SerializeField] private Vector3 forestBellWorldPosition = new Vector3(0f, 1.45f, 3.2f);
        [SerializeField] private float forestBedFadeSeconds = 3f;
        [SerializeField, Range(0f, 1f)] private float forestBedVolume = 0.55f;
        [SerializeField] private float forestBellCallIntervalSeconds = 2.4f;
        [SerializeField] private float forestBellArrivalRadius = 1f;
        [SerializeField] private float forestAutoEndSeconds = 10f;
        [SerializeField, Range(0f, 1f)] private float forestBellLedIntensity = 0.55f;

        public bool AutoAdvancePlaceholderStages => autoAdvancePlaceholderStages;
        public float PlaceholderStageSeconds => Mathf.Max(0.1f, placeholderStageSeconds);
        public bool StartWithMovementLocked => startWithMovementLocked;
        public FinalDemoAssistLevel DefaultAssistLevel => defaultAssistLevel;
        public string HardwareSerialPort => hardwareSerialPort;
        public string HeadImuSerialPort => headImuSerialPort;
        public string PadImuSerialPort => padImuSerialPort;
        public int PadTrackingUdpPort => padTrackingUdpPort;
        public float PlayerFixedHeight => playerFixedHeight;
        public Vector3 PlayerStartPosition => playerStartPosition;
        public float PreflightAmbienceVolume => Mathf.Clamp01(preflightAmbienceVolume);
        public float OpeningAmbienceSeconds => Mathf.Max(0.1f, openingAmbienceSeconds);
        public float OpeningAmbienceFadeSeconds => Mathf.Max(0.01f, openingAmbienceFadeSeconds);
        public float OpeningAmbienceVolume => Mathf.Clamp01(openingAmbienceVolume);
        public float OpeningSilenceSeconds => Mathf.Max(0.1f, openingSilenceSeconds);
        public float OpeningCloseBellSeconds => Mathf.Max(0.1f, openingCloseBellSeconds);
        public Vector3 OpeningCloseBellOffset => openingCloseBellOffset;
        public float OpeningCloseBellVolume => Mathf.Clamp01(openingCloseBellVolume);
        public float OpeningCloseBellLedIntensity => Mathf.Clamp01(openingCloseBellLedIntensity);
        public bool SoundReactiveLedEnabled => soundReactiveLedEnabled;
        public float SoundReactiveLedUpdateIntervalSeconds => Mathf.Clamp(soundReactiveLedUpdateIntervalSeconds, 0.09f, 0.25f);
        public float BellSoundReactiveSensitivity => Mathf.Max(0.1f, bellSoundReactiveSensitivity);
        public float TinnitusSoundReactiveSensitivity => Mathf.Max(0.1f, tinnitusSoundReactiveSensitivity);
        public float BellCallPostClipGapSeconds => Mathf.Max(0f, bellCallPostClipGapSeconds);
        public float BellOrbitSeconds => Mathf.Max(0.1f, bellOrbitSeconds);
        public float BellOrbitCallIntervalSeconds => Mathf.Max(0.2f, bellOrbitCallIntervalSeconds);
        public bool BellOrbitPreferProfilePath => bellOrbitPreferProfilePath;
        public float BellOrbitToFirstTargetMoveSeconds => Mathf.Max(0.1f, bellOrbitToFirstTargetMoveSeconds);
        public float BellMovementTextureFadeSeconds => Mathf.Clamp(bellMovementTextureFadeSeconds, 0.01f, BellOrbitToFirstTargetMoveSeconds * 0.45f);
        public float BellMovementTextureVolume => Mathf.Clamp01(bellMovementTextureVolume);
        public bool BellOrbitUseSmoothSplinePath => bellOrbitUseSmoothSplinePath;
        public AnimationCurve BellOrbitProgressCurve => bellOrbitProgressCurve;
        public bool BellOrbitContinuousAnchorEnabled => bellOrbitContinuousAnchorEnabled;
        public float BellOrbitContinuousAnchorUpdateIntervalSeconds => Mathf.Clamp(bellOrbitContinuousAnchorUpdateIntervalSeconds, 0.06f, 0.25f);
        public float BellOrbitContinuousAnchorIntensity => Mathf.Clamp01(bellOrbitContinuousAnchorIntensity);
        public Vector3[] BellOrbitLocalPoints => bellOrbitLocalPoints;
        public float BellOrbitNearIntensity => Mathf.Clamp01(bellOrbitNearIntensity);
        public float BellOrbitFarIntensity => Mathf.Clamp01(bellOrbitFarIntensity);
        public float BellOrbitVolume => Mathf.Clamp01(bellOrbitVolume);
        public int BellFollowTargetCount => Mathf.Max(1, bellFollowTargetCount);
        public Vector3 BellFollowTargetOnePosition => bellFollowTargetOnePosition;
        public Vector3 BellFollowTargetTwoPosition => bellFollowTargetTwoPosition;
        public float BellFollowRelocationSeconds => Mathf.Max(0.1f, bellFollowRelocationSeconds);
        public float BellArrivalRadius => Mathf.Max(0.05f, bellArrivalRadius);
        public float BellFollowCallIntervalSeconds => Mathf.Max(0.2f, bellFollowCallIntervalSeconds);
        public float BellFollowInitialCallIntervalSeconds => Mathf.Max(0.2f, bellFollowInitialCallIntervalSeconds);
        public float BellFollowMinimumCallIntervalSeconds => Mathf.Clamp(bellFollowMinimumCallIntervalSeconds, 0.2f, BellFollowInitialCallIntervalSeconds);
        public float BellFollowMissIntervalReductionSeconds => Mathf.Max(0f, bellFollowMissIntervalReductionSeconds);
        public float BellFollowVolume => Mathf.Clamp01(bellFollowVolume);
        public float BellFollowLedIntensity => Mathf.Clamp01(bellFollowLedIntensity);
        public float BellAssistTimeoutSeconds => Mathf.Max(0.1f, bellAssistTimeoutSeconds);
        public float BellAssistRepeatSeconds => Mathf.Max(0.2f, bellAssistRepeatSeconds);
        public float BellAssistGainMultiplier => Mathf.Max(1f, bellAssistGainMultiplier);
        public bool BellFollowProgressBlockerEnabled => bellFollowProgressBlockerEnabled;
        public float BellFollowBlockerMarginMeters => Mathf.Max(0f, bellFollowBlockerMarginMeters);
        public float PadShakeAssistMotionThreshold => Mathf.Clamp01(padShakeAssistMotionThreshold);
        public float PadShakeAssistCooldownSeconds => Mathf.Max(0.1f, padShakeAssistCooldownSeconds);
        public float PadShakeAssistNarrationCooldownSeconds => Mathf.Max(0.5f, padShakeAssistNarrationCooldownSeconds);
        public Vector3 RainZoneCenter => rainZoneCenter;
        public float RainZoneRadius => Mathf.Max(0.1f, rainZoneRadius);
        public float RainIntensityRampSeconds => Mathf.Max(0.1f, rainIntensityRampSeconds);
        public float RainMaxIntensity => Mathf.Clamp01(rainMaxIntensity);
        public float RainWindTextureDelaySeconds => Mathf.Max(0f, rainWindTextureDelaySeconds);
        public float RainFocusBellNarrationDelaySeconds => Mathf.Max(0f, rainFocusBellNarrationDelaySeconds);
        public float RainWindTextureMaxIntensity => Mathf.Clamp01(rainWindTextureMaxIntensity);
        public float RainAssistVolumeFloor => Mathf.Clamp01(rainAssistVolumeFloor);
        public float RainCloseDropIntervalSeconds => Mathf.Max(0.2f, rainCloseDropIntervalSeconds);
        public int BellGazeRequiredSuccesses => Mathf.Max(1, bellGazeRequiredSuccesses);
        public float BellGazeHoldSeconds => Mathf.Max(0.1f, bellGazeHoldSeconds);
        public float BellGazeConeDegrees => Mathf.Clamp(bellGazeConeDegrees, 1f, 90f);
        public float BellGazeAssistStartSeconds => Mathf.Max(0.1f, bellGazeAssistStartSeconds);
        public float BellGazeAssistMaxConeDegrees => Mathf.Clamp(bellGazeAssistMaxConeDegrees, BellGazeConeDegrees, 90f);
        public float BellGazeMoveSeconds => Mathf.Max(0.05f, bellGazeMoveSeconds);
        public float BellGazeCallIntervalSeconds => Mathf.Max(0.2f, bellGazeCallIntervalSeconds);
        public float BellGazeLedIntensity => Mathf.Clamp01(bellGazeLedIntensity);
        public Vector3[] BellGazeLocalOffsets => bellGazeLocalOffsets;
        public Vector3 BellAcquisitionLocalOffset => bellAcquisitionLocalOffset;
        public float BellAcquisitionEffectSeconds => Mathf.Max(0.1f, bellAcquisitionEffectSeconds);
        public float BellAcquisitionTransitionSilenceSeconds => Mathf.Max(0f, bellAcquisitionTransitionSilenceSeconds);
        public Vector3 GeneralTinnitusOneWorldPosition => generalTinnitusOneWorldPosition;
        public Vector3 GeneralTinnitusTwoWorldPosition => generalTinnitusTwoWorldPosition;
        public Vector3 GeneralTinnitusOnePadTargetCameraSpace => generalTinnitusOnePadTargetCameraSpace;
        public Vector3 GeneralTinnitusTwoPadTargetCameraSpace => generalTinnitusTwoPadTargetCameraSpace;
        public Vector3 GeneralTinnitusOnePadTargetYawPitchRoll => generalTinnitusOnePadTargetYawPitchRoll;
        public Vector3 GeneralTinnitusTwoPadTargetYawPitchRoll => generalTinnitusTwoPadTargetYawPitchRoll;
        public float GeneralTinnitusTreatmentSeconds => Mathf.Max(0.1f, generalTinnitusTreatmentSeconds);
        public float GeneralTinnitusPositionToleranceMeters => Mathf.Max(0.01f, generalTinnitusPositionToleranceMeters);
        public float GeneralTinnitusRotationToleranceDegrees => Mathf.Clamp(generalTinnitusRotationToleranceDegrees, 1f, 180f);
        public float GeneralTinnitusMatchFeedbackRadiusMultiplier => Mathf.Max(1f, generalTinnitusMatchFeedbackRadiusMultiplier);
        public float GeneralTinnitusRevealConeDegrees => Mathf.Clamp(generalTinnitusRevealConeDegrees, 1f, 120f);
        public float GeneralTinnitusLedIntensity => Mathf.Clamp01(generalTinnitusLedIntensity);
        public float GeneralTinnitusToneVolume => Mathf.Clamp01(generalTinnitusToneVolume);
        public float GeneralTinnitusLightIntervalSeconds => Mathf.Max(0.05f, generalTinnitusLightIntervalSeconds);
        public float GeneralTinnitusCleanseHapticIntervalSeconds => Mathf.Max(0.1f, generalTinnitusCleanseHapticIntervalSeconds);
        public Vector3 BossWorldPosition => bossWorldPosition;
        public float BossApproachRadius => Mathf.Max(0.05f, bossApproachRadius);
        public float BossApproachAutoStartSeconds => Mathf.Max(0.1f, bossApproachAutoStartSeconds);
        public float BossBaseVolume => Mathf.Clamp01(bossBaseVolume);
        public float BossMassLedIntensity => Mathf.Clamp01(bossMassLedIntensity);
        public float BossPatternOneSeconds => Mathf.Max(0.1f, bossPatternOneSeconds);
        public float BossPatternTwoSeconds => Mathf.Max(0.1f, bossPatternTwoSeconds);
        public float BossPatternThreeSeconds => Mathf.Max(0.1f, bossPatternThreeSeconds);
        public float BossOpeningHoldSeconds => Mathf.Max(0.1f, bossOpeningHoldSeconds);
        public float BossHoldPositionToleranceMeters => Mathf.Max(0.01f, bossHoldPositionToleranceMeters);
        public float BossHoldRotationToleranceDegrees => Mathf.Clamp(bossHoldRotationToleranceDegrees, 1f, 180f);
        public float BossMovingToleranceMeters => Mathf.Max(0.01f, bossMovingToleranceMeters);
        public float BossMovingToleranceDegrees => Mathf.Clamp(bossMovingToleranceDegrees, 1f, 180f);
        public float BossFailureToleranceGain => Mathf.Max(0f, bossFailureToleranceGain);
        public float BossLightIntervalSeconds => Mathf.Max(0.05f, bossLightIntervalSeconds);
        public Vector3[] BossWeakPointCentersCameraSpace => bossWeakPointCentersCameraSpace;
        public Vector3[] BossWeakPointMoveOffsetsCameraSpace => bossWeakPointMoveOffsetsCameraSpace;
        public Vector3[] BossWeakPointEndYawPitchRoll => bossWeakPointEndYawPitchRoll;
        public float BossDefeatEffectSeconds => Mathf.Max(0.1f, bossDefeatEffectSeconds);
        public float BossDefeatSilenceSeconds => Mathf.Max(0f, bossDefeatSilenceSeconds);
        public Vector3 ForestBellWorldPosition => forestBellWorldPosition;
        public float ForestBedFadeSeconds => Mathf.Max(0.1f, forestBedFadeSeconds);
        public float ForestBedVolume => Mathf.Clamp01(forestBedVolume);
        public float ForestBellCallIntervalSeconds => Mathf.Max(0.2f, forestBellCallIntervalSeconds);
        public float ForestBellArrivalRadius => Mathf.Max(0.05f, forestBellArrivalRadius);
        public float ForestAutoEndSeconds => Mathf.Max(0.1f, forestAutoEndSeconds);
        public float ForestBellLedIntensity => Mathf.Clamp01(forestBellLedIntensity);
    }
}
