using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GiantTinnitusEncounterController : MonoBehaviour
    {
        [SerializeField] private PadPoseMatchEvaluator poseMatchEvaluator;
        [SerializeField] private Vector3 targetCenterCameraSpace = new Vector3(0f, 0f, 0.7f);
        [SerializeField] private Vector3 moveOffsetCameraSpace = new Vector3(0.42f, 0.08f, 0.08f);
        [SerializeField] private float holdSeconds = 2f;
        [SerializeField] private float moveSeconds = 6f;
        [SerializeField] private float holdPositionToleranceMeters = 0.16f;
        [SerializeField] private float movePositionToleranceMeters = 0.38f;
        [SerializeField] private float holdRotationToleranceDegrees = 24f;
        [SerializeField] private float moveRotationToleranceDegrees = 46f;

        private bool _isRunning;
        private bool _isComplete;
        private bool _wasInsideTolerance;

        public bool IsRunning => _isRunning;
        public bool IsComplete => _isComplete;
        public float StageProgress01 => poseMatchEvaluator != null ? poseMatchEvaluator.Progress01 : 0f;
        public float OverallProgress01 => _isComplete ? 1f : StageProgress01;
        public float TotalSeconds => Mathf.Max(0.1f, holdSeconds + moveSeconds);
        public float HoldProgress01 => poseMatchEvaluator == null ? 0f : Mathf.Clamp01(poseMatchEvaluator.ProgressSeconds / Mathf.Max(0.1f, holdSeconds));
        public float MoveProgress01 => poseMatchEvaluator == null ? 0f : Mathf.Clamp01((poseMatchEvaluator.ProgressSeconds - holdSeconds) / Mathf.Max(0.1f, moveSeconds));
        public string PhaseName => !IsRunning && !IsComplete ? "Idle" : IsComplete ? "Complete" : poseMatchEvaluator != null && poseMatchEvaluator.ProgressSeconds >= holdSeconds ? "Move" : "Hold";
        public Vector3 TargetCenterCameraSpace
        {
            get => targetCenterCameraSpace;
            set => targetCenterCameraSpace = value;
        }

        private void Start()
        {
            InitializeNow();
        }

        public void InitializeNow()
        {
            poseMatchEvaluator ??= GetComponent<PadPoseMatchEvaluator>();
        }

        public void StartEncounter()
        {
            InitializeNow();
            if (poseMatchEvaluator == null)
            {
                return;
            }

            _isComplete = false;
            _isRunning = true;
            _wasInsideTolerance = false;
            poseMatchEvaluator.TreatmentSeconds = TotalSeconds;
            poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
            poseMatchEvaluator.ResetProgress();
            UpdateTargetFromProgress();
        }

        public void StopEncounter()
        {
            _isRunning = false;
            _isComplete = false;
            _wasInsideTolerance = false;
        }

        public void Tick()
        {
            if (!_isRunning || _isComplete || poseMatchEvaluator == null)
            {
                return;
            }

            UpdateTargetFromProgress();
            if (poseMatchEvaluator.IsResolved)
            {
                _isRunning = false;
                _isComplete = true;
                return;
            }

            if (_wasInsideTolerance && !poseMatchEvaluator.IsInsideTolerance)
            {
                poseMatchEvaluator.ResetProgress();
                UpdateTargetFromProgress();
            }

            _wasInsideTolerance = poseMatchEvaluator.IsInsideTolerance;
        }

        private void UpdateTargetFromProgress()
        {
            float progressSeconds = poseMatchEvaluator.ProgressSeconds;
            bool isMovePhase = progressSeconds >= holdSeconds;
            float move01 = Mathf.Clamp01((progressSeconds - holdSeconds) / Mathf.Max(0.1f, moveSeconds));
            float smoothMove01 = move01 * move01 * (3f - 2f * move01);
            Vector3 arcedOffset = new Vector3(
                moveOffsetCameraSpace.x * smoothMove01,
                moveOffsetCameraSpace.y * smoothMove01 + Mathf.Sin(move01 * Mathf.PI) * 0.08f,
                moveOffsetCameraSpace.z * smoothMove01);
            Vector3 targetPosition = targetCenterCameraSpace + (isMovePhase ? arcedOffset : Vector3.zero);

            float yaw = Mathf.Lerp(0f, 14f, smoothMove01);
            float pitch = Mathf.Lerp(0f, -8f, smoothMove01);
            float roll = Mathf.Lerp(0f, 10f, smoothMove01);
            poseMatchEvaluator.TreatmentSeconds = TotalSeconds;
            poseMatchEvaluator.PositionToleranceMeters = isMovePhase ? movePositionToleranceMeters : holdPositionToleranceMeters;
            poseMatchEvaluator.YawToleranceDegrees = isMovePhase ? moveRotationToleranceDegrees : holdRotationToleranceDegrees;
            poseMatchEvaluator.PitchToleranceDegrees = isMovePhase ? moveRotationToleranceDegrees : holdRotationToleranceDegrees;
            poseMatchEvaluator.RollToleranceDegrees = isMovePhase ? moveRotationToleranceDegrees : holdRotationToleranceDegrees;
            poseMatchEvaluator.MatchFeedbackRadiusMultiplier = 4f;
            poseMatchEvaluator.SetTargetPose(targetPosition, yaw, pitch, roll);
        }

        private void OnValidate()
        {
            holdSeconds = Mathf.Max(0.1f, holdSeconds);
            moveSeconds = Mathf.Max(0.1f, moveSeconds);
            holdPositionToleranceMeters = Mathf.Max(0.01f, holdPositionToleranceMeters);
            movePositionToleranceMeters = Mathf.Max(holdPositionToleranceMeters, movePositionToleranceMeters);
            holdRotationToleranceDegrees = Mathf.Max(1f, holdRotationToleranceDegrees);
            moveRotationToleranceDegrees = Mathf.Max(holdRotationToleranceDegrees, moveRotationToleranceDegrees);
        }
    }
}
