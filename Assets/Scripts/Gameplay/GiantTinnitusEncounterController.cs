using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GiantTinnitusEncounterController : MonoBehaviour
    {
        [SerializeField] private PadPoseMatchEvaluator poseMatchEvaluator;
        [SerializeField] private Vector3 targetCenterCameraSpace = new Vector3(0f, 0f, 0.7f);
        [SerializeField] private float stage1Seconds = 7f;
        [SerializeField] private float stage2Seconds = 8f;
        [SerializeField] private float stage3Seconds = 10f;

        private int _stageIndex = -1;
        private float _stageStartRealtime;
        private bool _isRunning;
        private bool _isComplete;

        public bool IsRunning => _isRunning;
        public bool IsComplete => _isComplete;
        public int StageNumber => _stageIndex < 0 ? 0 : _stageIndex + 1;
        public float StageProgress01 => poseMatchEvaluator != null ? poseMatchEvaluator.Progress01 : 0f;
        public float OverallProgress01 => _isComplete
            ? 1f
            : (_stageIndex < 0 ? 0f : Mathf.Clamp01((_stageIndex + StageProgress01) / 3f));
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
            BeginStage(0);
        }

        public void StopEncounter()
        {
            _isRunning = false;
            _isComplete = false;
            _stageIndex = -1;
        }

        public void Tick()
        {
            if (!_isRunning || _isComplete || poseMatchEvaluator == null)
            {
                return;
            }

            UpdateMovingTarget();
            if (poseMatchEvaluator.IsResolved)
            {
                if (_stageIndex >= 2)
                {
                    _isRunning = false;
                    _isComplete = true;
                    return;
                }

                BeginStage(_stageIndex + 1);
            }
        }

        private void BeginStage(int stageIndex)
        {
            _stageIndex = Mathf.Clamp(stageIndex, 0, 2);
            _stageStartRealtime = Time.unscaledTime;
            poseMatchEvaluator.TreatmentSeconds = GetStageSeconds(_stageIndex);
            poseMatchEvaluator.PositionToleranceMeters = Mathf.Lerp(0.15f, 0.09f, _stageIndex / 2f);
            poseMatchEvaluator.YawToleranceDegrees = Mathf.Lerp(28f, 16f, _stageIndex / 2f);
            poseMatchEvaluator.PitchToleranceDegrees = Mathf.Lerp(24f, 14f, _stageIndex / 2f);
            poseMatchEvaluator.RollToleranceDegrees = Mathf.Lerp(24f, 14f, _stageIndex / 2f);
            poseMatchEvaluator.MatchFeedbackRadiusMultiplier = Mathf.Lerp(3f, 2.2f, _stageIndex / 2f);
            poseMatchEvaluator.ResetProgress();
            UpdateMovingTarget();
        }

        private void UpdateMovingTarget()
        {
            float stage01 = _stageIndex / 2f;
            float elapsed = Time.unscaledTime - _stageStartRealtime;
            float speed = Mathf.Lerp(0.55f, 1.15f, stage01);
            float positionAmplitude = Mathf.Lerp(0.035f, 0.095f, stage01);
            float rotationAmplitude = Mathf.Lerp(8f, 22f, stage01);

            Vector3 targetPosition = targetCenterCameraSpace + new Vector3(
                Mathf.Sin(elapsed * speed * 1.17f) * positionAmplitude,
                Mathf.Cos(elapsed * speed * 0.91f) * positionAmplitude * 0.55f,
                Mathf.Sin(elapsed * speed * 0.63f) * positionAmplitude * 0.45f);

            float yaw = Mathf.Sin(elapsed * speed * 0.83f) * rotationAmplitude;
            float pitch = Mathf.Cos(elapsed * speed * 0.71f) * rotationAmplitude * 0.75f;
            float roll = Mathf.Sin(elapsed * speed * 1.03f + 0.7f) * rotationAmplitude * 0.75f;
            poseMatchEvaluator.SetTargetPose(targetPosition, yaw, pitch, roll);
        }

        private float GetStageSeconds(int stageIndex)
        {
            return stageIndex switch
            {
                0 => stage1Seconds,
                1 => stage2Seconds,
                _ => stage3Seconds,
            };
        }
    }
}
