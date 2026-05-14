using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PadPoseMatchEvaluator : MonoBehaviour
    {
        [SerializeField] private PadPoseProvider padPoseProvider;
        [SerializeField] private Vector3 targetCameraSpacePosition = new Vector3(0f, 0f, 0.7f);
        [SerializeField] private float targetYawDegrees;
        [SerializeField] private float targetPitchDegrees;
        [SerializeField] private float targetRollDegrees;
        [SerializeField] private float positionToleranceMeters = 0.12f;
        [SerializeField] private float yawToleranceDegrees = 22f;
        [SerializeField] private float pitchToleranceDegrees = 18f;
        [SerializeField] private float rollToleranceDegrees = 18f;
        [SerializeField] private float matchFeedbackRadiusMultiplier = 2.5f;
        [SerializeField] private float treatmentSeconds = 4f;

        private bool _initialized;
        private float _progressSeconds;

        public PadPoseProvider PadPoseProvider => padPoseProvider;
        public Vector3 TargetCameraSpacePosition => targetCameraSpacePosition;
        public float TargetYawDegrees => targetYawDegrees;
        public float TargetPitchDegrees => targetPitchDegrees;
        public float TargetRollDegrees => targetRollDegrees;
        public float PositionToleranceMeters
        {
            get => positionToleranceMeters;
            set => positionToleranceMeters = Mathf.Max(0.01f, value);
        }

        public float YawToleranceDegrees
        {
            get => yawToleranceDegrees;
            set => yawToleranceDegrees = Mathf.Max(1f, value);
        }

        public float PitchToleranceDegrees
        {
            get => pitchToleranceDegrees;
            set => pitchToleranceDegrees = Mathf.Max(1f, value);
        }

        public float RollToleranceDegrees
        {
            get => rollToleranceDegrees;
            set => rollToleranceDegrees = Mathf.Max(1f, value);
        }

        public float TreatmentSeconds
        {
            get => treatmentSeconds;
            set => treatmentSeconds = Mathf.Max(0.1f, value);
        }

        public float MatchFeedbackRadiusMultiplier
        {
            get => matchFeedbackRadiusMultiplier;
            set => matchFeedbackRadiusMultiplier = Mathf.Max(1f, value);
        }

        public bool HasCurrentPose => padPoseProvider != null &&
                                      padPoseProvider.HasFreshPosition &&
                                      padPoseProvider.HasFreshImu &&
                                      padPoseProvider.HasResolvedRotation;
        public Vector3 CurrentCameraSpacePosition => padPoseProvider != null ? padPoseProvider.CameraSpacePosition : Vector3.zero;
        public float CurrentYawDegrees => padPoseProvider != null ? padPoseProvider.ResolvedYawDegrees : 0f;
        public float CurrentPitchDegrees => padPoseProvider != null ? padPoseProvider.ResolvedPitchDegrees : 0f;
        public float CurrentRollDegrees => padPoseProvider != null ? padPoseProvider.ResolvedRollDegrees : 0f;
        public float PositionErrorMeters { get; private set; }
        public float YawErrorDegrees { get; private set; }
        public float PitchErrorDegrees { get; private set; }
        public float RollErrorDegrees { get; private set; }
        public float PositionMatch01 { get; private set; }
        public float RotationMatch01 { get; private set; }
        public float TotalMatch01 { get; private set; }
        public bool IsInsideTolerance { get; private set; }
        public float ProgressSeconds => _progressSeconds;
        public float Progress01 => Mathf.Clamp01(_progressSeconds / Mathf.Max(0.1f, treatmentSeconds));
        public bool IsResolved => Progress01 >= 1f;

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            InitializeNow();
            Evaluate();

            if (!IsResolved && IsInsideTolerance)
            {
                _progressSeconds = Mathf.Min(treatmentSeconds, _progressSeconds + Time.unscaledDeltaTime);
            }
        }

        private void OnValidate()
        {
            positionToleranceMeters = Mathf.Max(0.01f, positionToleranceMeters);
            yawToleranceDegrees = Mathf.Max(1f, yawToleranceDegrees);
            pitchToleranceDegrees = Mathf.Max(1f, pitchToleranceDegrees);
            rollToleranceDegrees = Mathf.Max(1f, rollToleranceDegrees);
            matchFeedbackRadiusMultiplier = Mathf.Max(1f, matchFeedbackRadiusMultiplier);
            treatmentSeconds = Mathf.Max(0.1f, treatmentSeconds);
        }

        public void InitializeNow()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            padPoseProvider ??= GetComponent<PadPoseProvider>();
        }

        public void CaptureCurrentPoseAsTarget()
        {
            InitializeNow();
            if (!HasCurrentPose)
            {
                return;
            }

            targetCameraSpacePosition = CurrentCameraSpacePosition;
            targetYawDegrees = NormalizeSignedAngle(CurrentYawDegrees);
            targetPitchDegrees = NormalizeSignedAngle(CurrentPitchDegrees);
            targetRollDegrees = NormalizeSignedAngle(CurrentRollDegrees);
            ResetProgress();
            Evaluate();
        }

        public void ResetProgress()
        {
            _progressSeconds = 0f;
        }

        public void SetTargetPose(Vector3 cameraSpacePosition, float yawDegrees, float pitchDegrees, float rollDegrees)
        {
            targetCameraSpacePosition = cameraSpacePosition;
            targetYawDegrees = NormalizeSignedAngle(yawDegrees);
            targetPitchDegrees = NormalizeSignedAngle(pitchDegrees);
            targetRollDegrees = NormalizeSignedAngle(rollDegrees);
            Evaluate();
        }

        public void Evaluate()
        {
            if (!HasCurrentPose)
            {
                PositionErrorMeters = float.PositiveInfinity;
                YawErrorDegrees = float.PositiveInfinity;
                PitchErrorDegrees = float.PositiveInfinity;
                RollErrorDegrees = float.PositiveInfinity;
                PositionMatch01 = 0f;
                RotationMatch01 = 0f;
                TotalMatch01 = 0f;
                IsInsideTolerance = false;
                return;
            }

            PositionErrorMeters = Vector3.Distance(CurrentCameraSpacePosition, targetCameraSpacePosition);
            YawErrorDegrees = Mathf.Abs(Mathf.DeltaAngle(CurrentYawDegrees, targetYawDegrees));
            PitchErrorDegrees = Mathf.Abs(Mathf.DeltaAngle(CurrentPitchDegrees, targetPitchDegrees));
            RollErrorDegrees = Mathf.Abs(Mathf.DeltaAngle(CurrentRollDegrees, targetRollDegrees));

            float feedbackScale = Mathf.Max(1f, matchFeedbackRadiusMultiplier);
            PositionMatch01 = 1f - Mathf.Clamp01(PositionErrorMeters / (positionToleranceMeters * feedbackScale));
            float yawMatch = 1f - Mathf.Clamp01(YawErrorDegrees / (yawToleranceDegrees * feedbackScale));
            float pitchMatch = 1f - Mathf.Clamp01(PitchErrorDegrees / (pitchToleranceDegrees * feedbackScale));
            float rollMatch = 1f - Mathf.Clamp01(RollErrorDegrees / (rollToleranceDegrees * feedbackScale));
            RotationMatch01 = Mathf.Min(yawMatch, Mathf.Min(pitchMatch, rollMatch));
            TotalMatch01 = Mathf.Min(PositionMatch01, RotationMatch01);
            IsInsideTolerance = PositionErrorMeters <= positionToleranceMeters &&
                                YawErrorDegrees <= yawToleranceDegrees &&
                                PitchErrorDegrees <= pitchToleranceDegrees &&
                                RollErrorDegrees <= rollToleranceDegrees;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            return Mathf.Approximately(wrapped, -180f) ? 180f : wrapped;
        }
    }
}
