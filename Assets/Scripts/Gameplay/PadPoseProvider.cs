using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PadPoseProvider : MonoBehaviour
    {
        [SerializeField] private PadTrackingReceiver trackingReceiver;
        [SerializeField] private PadImuReceiver padImuReceiver;
        [SerializeField] private bool preferCameraYaw = true;
        [SerializeField] private bool allowImuYawFallback = true;
        [SerializeField] private float cameraYawOffsetDegrees;
        [SerializeField] private float rotationSmoothingStrength = 22f;
        [SerializeField] private float maximumPadPitchDegrees = 85f;
        [SerializeField] private float maximumPadRollDegrees = 85f;
        [SerializeField] private float pitchRollDiagonalBoost = 1.12f;
        [SerializeField] private float pitchRollDiagonalThresholdDegrees = 5f;
        [SerializeField] private float maximumCameraYawReacquireJumpDegrees = 35f;
        [SerializeField] private int cameraYawReacquireStableFrames = 4;
        [SerializeField] private float cameraYawReacquireStableToleranceDegrees = 8f;
        [SerializeField] private bool useCameraPitchRollDriftCorrection;
        [SerializeField, Range(0f, 1f)] private float cameraPitchRollCorrectionStrength = 0.04f;
        [SerializeField] private float cameraPitchRollMinConfidence = 0.82f;
        [SerializeField] private float maximumCameraPitchRollCorrectionDegrees = 3f;

        private bool _initialized;
        private bool _hasRelativeRotation;
        private bool _hasStableImuAngles;
        private bool _hasCameraPitchRollReference;
        private bool _hasPendingCameraYaw;
        private float _resolvedYawDegrees;
        private float _resolvedPitchDegrees;
        private float _resolvedRollDegrees;
        private float _stablePitchDegrees;
        private float _stableRollDegrees;
        private float _cameraPitchReferenceOffsetDegrees;
        private float _cameraRollReferenceOffsetDegrees;
        private float _pendingCameraYawDegrees;
        private Quaternion _relativeRotation = Quaternion.identity;
        private int _consecutiveCameraYawFrames;
        private int _pendingCameraYawFrames;

        public PadTrackingReceiver TrackingReceiver => trackingReceiver;
        public PadImuReceiver PadImuReceiver => padImuReceiver;
        public bool HasPose => trackingReceiver != null && trackingReceiver.HasPose;
        public bool HasFreshPosition => trackingReceiver != null && trackingReceiver.HasFreshDetection;
        public bool HasFreshImu => padImuReceiver != null && padImuReceiver.HasFreshSample;
        public bool HasFreshCameraYaw => trackingReceiver != null && trackingReceiver.HasFreshCameraYaw;
        public bool HasFreshCameraPitchRoll => trackingReceiver != null && trackingReceiver.HasFreshCameraPitchRoll;
        public bool UsingCameraYaw { get; private set; }
        public bool UsingImuYawFallback { get; private set; }
        public bool UsingHeldYaw { get; private set; }
        public bool UsingCameraPitchRollCorrection { get; private set; }
        public bool HasResolvedRotation => _hasRelativeRotation;
        public Vector3 CameraSpacePosition => trackingReceiver != null ? trackingReceiver.ApproximateCameraSpacePosition : Vector3.zero;
        public Quaternion RelativeRotation => _relativeRotation;
        public float ResolvedYawDegrees => _resolvedYawDegrees;
        public float ResolvedPitchDegrees => _resolvedPitchDegrees;
        public float ResolvedRollDegrees => _resolvedRollDegrees;

        private void Start()
        {
            InitializeNow();
        }

        private void Update()
        {
            InitializeNow();
            UpdatePose();
        }

        public void InitializeNow()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            trackingReceiver ??= GetComponent<PadTrackingReceiver>();
            padImuReceiver ??= GetComponent<PadImuReceiver>();
        }

        private void UpdatePose()
        {
            float targetYaw = _resolvedYawDegrees;
            float targetPitch = _resolvedPitchDegrees;
            float targetRoll = _resolvedRollDegrees;
            bool hasAnyRotationSource = false;

            UsingCameraYaw = false;
            UsingImuYawFallback = false;
            UsingHeldYaw = false;
            UsingCameraPitchRollCorrection = false;

            if (padImuReceiver != null && padImuReceiver.HasFreshSample)
            {
                ResolveStableImuAngles(out targetPitch, out targetRoll);
                ApplyPitchRollDiagonalBoost(ref targetPitch, ref targetRoll);
                ApplyCameraPitchRollDriftCorrection(ref targetPitch, ref targetRoll);
                hasAnyRotationSource = true;
            }

            if (preferCameraYaw && trackingReceiver != null && trackingReceiver.HasFreshCameraYaw)
            {
                _consecutiveCameraYawFrames++;
                float candidateCameraYaw = NormalizeSignedAngle(trackingReceiver.CameraYawDegrees + cameraYawOffsetDegrees);
                float yawJumpDegrees = Mathf.Abs(NormalizeSignedAngle(candidateCameraYaw - _resolvedYawDegrees));
                bool shouldHoldDuringReacquire = ShouldHoldCameraYawDuringReacquire(candidateCameraYaw, yawJumpDegrees);
                if (shouldHoldDuringReacquire)
                {
                    UsingHeldYaw = true;
                }
                else
                {
                    targetYaw = candidateCameraYaw;
                    UsingCameraYaw = true;
                    hasAnyRotationSource = true;
                    ClearPendingCameraYaw();
                }
            }
            else if (allowImuYawFallback &&
                     padImuReceiver != null &&
                     padImuReceiver.HasFreshSample &&
                     (trackingReceiver == null || !_hasRelativeRotation || !trackingReceiver.HasPose))
            {
                _consecutiveCameraYawFrames = 0;
                targetYaw = padImuReceiver.MappedYawDegrees;
                UsingImuYawFallback = true;
                hasAnyRotationSource = true;
            }
            else if (preferCameraYaw && _hasRelativeRotation)
            {
                _consecutiveCameraYawFrames = 0;
                UsingHeldYaw = true;
                ClearPendingCameraYaw();
            }
            else
            {
                _consecutiveCameraYawFrames = 0;
                ClearPendingCameraYaw();
            }

            if (!hasAnyRotationSource)
            {
                return;
            }

            _resolvedYawDegrees = NormalizeSignedAngle(targetYaw);
            _resolvedPitchDegrees = NormalizeSignedAngle(targetPitch);
            _resolvedRollDegrees = NormalizeSignedAngle(targetRoll);

            Quaternion targetRotation = Quaternion.Euler(-_resolvedPitchDegrees, _resolvedYawDegrees, -_resolvedRollDegrees);
            if (!_hasRelativeRotation)
            {
                _relativeRotation = targetRotation;
                _hasRelativeRotation = true;
                return;
            }

            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0f, rotationSmoothingStrength) * deltaTime);
            _relativeRotation = Quaternion.Slerp(_relativeRotation, targetRotation, lerpFactor);
        }

        private void ResolveStableImuAngles(out float pitchDegrees, out float rollDegrees)
        {
            float candidatePitch = NormalizeSignedAngle(padImuReceiver.MappedPitchDegrees);
            float candidateRoll = NormalizeSignedAngle(padImuReceiver.MappedRollDegrees);
            if (!_hasStableImuAngles)
            {
                _stablePitchDegrees = candidatePitch;
                _stableRollDegrees = candidateRoll;
                _hasStableImuAngles = true;
            }

            bool candidateInPitchRange = Mathf.Abs(candidatePitch) <= maximumPadPitchDegrees;
            bool candidateInRollRange = Mathf.Abs(candidateRoll) <= maximumPadRollDegrees;

            if (candidateInPitchRange)
            {
                _stablePitchDegrees = candidatePitch;
            }

            if (candidateInRollRange)
            {
                _stableRollDegrees = candidateRoll;
            }

            pitchDegrees = _stablePitchDegrees;
            rollDegrees = _stableRollDegrees;
        }

        private void ApplyPitchRollDiagonalBoost(ref float pitchDegrees, ref float rollDegrees)
        {
            if (pitchRollDiagonalBoost <= 1f ||
                Mathf.Abs(pitchDegrees) < pitchRollDiagonalThresholdDegrees ||
                Mathf.Abs(rollDegrees) < pitchRollDiagonalThresholdDegrees)
            {
                return;
            }

            pitchDegrees = Mathf.Clamp(pitchDegrees * pitchRollDiagonalBoost, -maximumPadPitchDegrees, maximumPadPitchDegrees);
            rollDegrees = Mathf.Clamp(rollDegrees * pitchRollDiagonalBoost, -maximumPadRollDegrees, maximumPadRollDegrees);
        }

        private void ApplyCameraPitchRollDriftCorrection(ref float pitchDegrees, ref float rollDegrees)
        {
            if (!useCameraPitchRollDriftCorrection ||
                trackingReceiver == null ||
                !trackingReceiver.HasFreshCameraPitchRoll ||
                trackingReceiver.Confidence < cameraPitchRollMinConfidence)
            {
                return;
            }

            float cameraPitch = NormalizeSignedAngle(trackingReceiver.CameraPitchDegrees);
            float cameraRoll = NormalizeSignedAngle(trackingReceiver.CameraRollDegrees);
            if (!_hasCameraPitchRollReference)
            {
                _cameraPitchReferenceOffsetDegrees = NormalizeSignedAngle(pitchDegrees - cameraPitch);
                _cameraRollReferenceOffsetDegrees = NormalizeSignedAngle(rollDegrees - cameraRoll);
                _hasCameraPitchRollReference = true;
            }

            float referencedCameraPitch = NormalizeSignedAngle(cameraPitch + _cameraPitchReferenceOffsetDegrees);
            float referencedCameraRoll = NormalizeSignedAngle(cameraRoll + _cameraRollReferenceOffsetDegrees);
            float pitchCorrection = Mathf.Clamp(
                NormalizeSignedAngle(referencedCameraPitch - pitchDegrees),
                -maximumCameraPitchRollCorrectionDegrees,
                maximumCameraPitchRollCorrectionDegrees);
            float rollCorrection = Mathf.Clamp(
                NormalizeSignedAngle(referencedCameraRoll - rollDegrees),
                -maximumCameraPitchRollCorrectionDegrees,
                maximumCameraPitchRollCorrectionDegrees);

            pitchDegrees = NormalizeSignedAngle(pitchDegrees + (pitchCorrection * cameraPitchRollCorrectionStrength));
            rollDegrees = NormalizeSignedAngle(rollDegrees + (rollCorrection * cameraPitchRollCorrectionStrength));
            UsingCameraPitchRollCorrection = true;
        }

        private bool ShouldHoldCameraYawDuringReacquire(float candidateCameraYaw, float yawJumpDegrees)
        {
            if (!_hasRelativeRotation || yawJumpDegrees <= maximumCameraYawReacquireJumpDegrees)
            {
                return false;
            }

            if (!_hasPendingCameraYaw ||
                Mathf.Abs(NormalizeSignedAngle(candidateCameraYaw - _pendingCameraYawDegrees)) > cameraYawReacquireStableToleranceDegrees)
            {
                _pendingCameraYawDegrees = candidateCameraYaw;
                _pendingCameraYawFrames = 1;
                _hasPendingCameraYaw = true;
            }
            else
            {
                _pendingCameraYawFrames++;
            }

            return _pendingCameraYawFrames < Mathf.Max(1, cameraYawReacquireStableFrames);
        }

        private void ClearPendingCameraYaw()
        {
            _hasPendingCameraYaw = false;
            _pendingCameraYawFrames = 0;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            while (degrees > 180f)
            {
                degrees -= 360f;
            }

            while (degrees < -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }
    }
}
