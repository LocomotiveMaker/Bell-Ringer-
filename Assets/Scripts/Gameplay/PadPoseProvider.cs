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

        private bool _initialized;
        private bool _hasRelativeRotation;
        private float _resolvedYawDegrees;
        private float _resolvedPitchDegrees;
        private float _resolvedRollDegrees;
        private Quaternion _relativeRotation = Quaternion.identity;

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
                targetPitch = padImuReceiver.MappedPitchDegrees;
                targetRoll = padImuReceiver.MappedRollDegrees;
                hasAnyRotationSource = true;
            }

            if (preferCameraYaw && trackingReceiver != null && trackingReceiver.HasFreshCameraYaw)
            {
                targetYaw = NormalizeSignedAngle(trackingReceiver.CameraYawDegrees + cameraYawOffsetDegrees);
                UsingCameraYaw = true;
                hasAnyRotationSource = true;
            }
            else if (allowImuYawFallback &&
                     padImuReceiver != null &&
                     padImuReceiver.HasFreshSample &&
                     (trackingReceiver == null || !_hasRelativeRotation || !trackingReceiver.HasPose))
            {
                targetYaw = padImuReceiver.MappedYawDegrees;
                UsingImuYawFallback = true;
                hasAnyRotationSource = true;
            }
            else if (preferCameraYaw && _hasRelativeRotation)
            {
                UsingHeldYaw = true;
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
