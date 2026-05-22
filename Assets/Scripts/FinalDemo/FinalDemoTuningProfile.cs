using UnityEngine;

namespace BellRinger.FinalDemo
{
    [CreateAssetMenu(menuName = "Bell Ringer/Final Demo Tuning Profile", fileName = "FinalDemoTuningProfile")]
    public sealed class FinalDemoTuningProfile : ScriptableObject
    {
        [Header("Runtime")]
        [SerializeField] private bool autoAdvancePlaceholderStages;
        [SerializeField] private float placeholderStageSeconds = 3f;
        [SerializeField] private bool startWithMovementLocked = true;
        [SerializeField] private FinalDemoAssistLevel defaultAssistLevel = FinalDemoAssistLevel.Gentle;

        [Header("Input Defaults")]
        [SerializeField] private string hardwareSerialPort = "COM9";
        [SerializeField] private string headImuSerialPort = "COM40";
        [SerializeField] private string padImuSerialPort = "COM30";
        [SerializeField] private int padTrackingUdpPort = 39051;

        [Header("Player")]
        [SerializeField] private float playerFixedHeight = 1.6f;
        [SerializeField] private Vector3 playerStartPosition = new Vector3(0f, 1.6f, -1.5f);

        [Header("Bell Route")]
        [SerializeField] private int bellFollowTargetCount = 2;
        [SerializeField] private int bellGazeRequiredSuccesses = 3;
        [SerializeField] private float bellGazeHoldSeconds = 1.4f;
        [SerializeField] private float bellGazeConeDegrees = 14f;

        [Header("Tinnitus")]
        [SerializeField] private float generalTinnitusTreatmentSeconds = 4f;
        [SerializeField] private float generalTinnitusPositionToleranceMeters = 0.18f;
        [SerializeField] private float generalTinnitusRotationToleranceDegrees = 22f;

        [Header("Boss")]
        [SerializeField] private float bossPatternOneSeconds = 7f;
        [SerializeField] private float bossPatternTwoSeconds = 8f;
        [SerializeField] private float bossPatternThreeSeconds = 10f;
        [SerializeField] private float bossOpeningHoldSeconds = 2f;
        [SerializeField] private float bossMovingToleranceMeters = 0.42f;
        [SerializeField] private float bossMovingToleranceDegrees = 38f;

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
        public int BellFollowTargetCount => Mathf.Max(1, bellFollowTargetCount);
        public int BellGazeRequiredSuccesses => Mathf.Max(1, bellGazeRequiredSuccesses);
        public float BellGazeHoldSeconds => Mathf.Max(0.1f, bellGazeHoldSeconds);
        public float BellGazeConeDegrees => Mathf.Clamp(bellGazeConeDegrees, 1f, 90f);
        public float GeneralTinnitusTreatmentSeconds => Mathf.Max(0.1f, generalTinnitusTreatmentSeconds);
        public float GeneralTinnitusPositionToleranceMeters => Mathf.Max(0.01f, generalTinnitusPositionToleranceMeters);
        public float GeneralTinnitusRotationToleranceDegrees => Mathf.Clamp(generalTinnitusRotationToleranceDegrees, 1f, 180f);
        public float BossPatternOneSeconds => Mathf.Max(0.1f, bossPatternOneSeconds);
        public float BossPatternTwoSeconds => Mathf.Max(0.1f, bossPatternTwoSeconds);
        public float BossPatternThreeSeconds => Mathf.Max(0.1f, bossPatternThreeSeconds);
        public float BossOpeningHoldSeconds => Mathf.Max(0.1f, bossOpeningHoldSeconds);
        public float BossMovingToleranceMeters => Mathf.Max(0.01f, bossMovingToleranceMeters);
        public float BossMovingToleranceDegrees => Mathf.Clamp(bossMovingToleranceDegrees, 1f, 180f);
    }
}
