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
        [SerializeField] private float rotationSmoothingStrength = 12f;
        [SerializeField] private float imuStillnessHoldThreshold = 0.84f;
        [SerializeField] private float inferredStillGyroDegreesPerSecond = 1.15f;
        [SerializeField] private float maximumPadPitchDegrees = 85f;
        [SerializeField] private float maximumPadRollDegrees = 85f;
        [SerializeField] private float maximumCameraYawReacquireJumpDegrees = 35f;

        private bool _initialized;
        private bool _hasRelativeRotation;
        private bool _hasStableImuAngles;
        private float _resolvedYawDegrees;
        private float _resolvedPitchDegrees;
        private float _resolvedRollDegrees;
        private float _stablePitchDegrees;
        private float _stableRollDegrees;
        private Quaternion _relativeRotation = Quaternion.identity;
        private int _consecutiveCameraYawFrames;

        public PadTrackingReceiver TrackingReceiver => trackingReceiver;
        public PadImuReceiver PadImuReceiver => padImuReceiver;
        public bool HasPose => trackingReceiver != null && trackingReceiver.HasPose;
        public bool HasFreshPosition => trackingReceiver != null && trackingReceiver.HasFreshDetection;
        public bool HasFreshImu => padImuReceiver != null && padImuReceiver.HasFreshSample;
        public bool HasFreshCameraYaw => trackingReceiver != null && trackingReceiver.HasFreshCameraYaw;
        public bool UsingCameraYaw { get; private set; }
        public bool UsingImuYawFallback { get; private set; }
        public bool UsingHeldYaw { get; private set; }
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

            if (padImuReceiver != null && padImuReceiver.HasFreshSample)
            {
                ResolveStableImuAngles(out targetPitch, out targetRoll);
                hasAnyRotationSource = true;
            }

            if (preferCameraYaw && trackingReceiver != null && trackingReceiver.HasFreshCameraYaw)
            {
                _consecutiveCameraYawFrames++;
                float candidateCameraYaw = NormalizeSignedAngle(trackingReceiver.CameraYawDegrees + cameraYawOffsetDegrees);
                float yawJumpDegrees = Mathf.Abs(NormalizeSignedAngle(candidateCameraYaw - _resolvedYawDegrees));
                bool shouldHoldDuringReacquire = _hasRelativeRotation &&
                                                _consecutiveCameraYawFrames <= 2 &&
                                                yawJumpDegrees > maximumCameraYawReacquireJumpDegrees;
                if (shouldHoldDuringReacquire)
                {
                    UsingHeldYaw = true;
                }
                else
                {
                    targetYaw = candidateCameraYaw;
                    UsingCameraYaw = true;
                    hasAnyRotationSource = true;
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
            }
            else
            {
                _consecutiveCameraYawFrames = 0;
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
            float gyroMagnitude = padImuReceiver.GyroDegreesPerSecond.magnitude;
            bool shouldHoldStill = padImuReceiver.Stillness01 >= imuStillnessHoldThreshold ||
                                   gyroMagnitude <= inferredStillGyroDegreesPerSecond;

            if (candidateInPitchRange && !shouldHoldStill)
            {
                _stablePitchDegrees = candidatePitch;
            }

            if (candidateInRollRange && !shouldHoldStill)
            {
                _stableRollDegrees = candidateRoll;
            }

            pitchDegrees = _stablePitchDegrees;
            rollDegrees = _stableRollDegrees;
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
