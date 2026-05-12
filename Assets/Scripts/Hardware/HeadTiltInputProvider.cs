using UnityEngine;

namespace BellRinger.Hardware
{
    [DisallowMultipleComponent]
    public sealed class HeadTiltInputProvider : MonoBehaviour
    {
        private enum YawInputMode
        {
            PhysicalYaw,
            RollToYaw,
        }

        [SerializeField] private HeadImuReceiver headImuReceiver;
        [SerializeField] private YawInputMode yawInputMode = YawInputMode.PhysicalYaw;
        [SerializeField] private float staleAfterSeconds = 0.25f;
        [SerializeField] private float yawDeadzoneDegrees = 1.2f;
        [SerializeField] private float pitchDeadzoneDegrees = 1.8f;
        [SerializeField] private float rollDeadzoneDegrees = 1.8f;
        [SerializeField] private float yawSensitivity = 1.15f;
        [SerializeField] private float pitchSensitivity = 1.0f;
        [SerializeField] private float rollToYawSensitivity = 1.0f;
        [SerializeField] private float maximumVirtualPitchDegrees = 48f;
        [SerializeField] private float maximumVirtualYawDegrees = 70f;
        [SerializeField] private float diagonalAimBoost = 1.18f;
        [SerializeField] private float diagonalAimThresholdDegrees = 4f;
        [SerializeField] private float smoothingStrength = 16f;
        [SerializeField] private float stillnessHoldThreshold = 0.84f;
        [SerializeField] private float inferredStillFrameDeltaDegrees = 0.18f;
        [SerializeField] private float stillnessPhysicalDriftToleranceDegrees = 0.7f;
        [SerializeField] private float snapToNeutralVirtualDegrees = 0.18f;
        [SerializeField] private bool invertYaw = true;
        [SerializeField] private bool invertPitch;
        [SerializeField] private bool invertRollToYaw;

        private bool _hasNeutral;
        private bool _hasStillLock;
        private float _neutralYawDegrees;
        private float _neutralPitchDegrees;
        private float _neutralRollDegrees;
        private float _physicalYawDegrees;
        private float _physicalPitchDegrees;
        private float _physicalRollDegrees;
        private float _virtualYawDegrees;
        private float _virtualPitchDegrees;
        private float _stillLockedYawDegrees;
        private float _stillLockedPitchDegrees;
        private float _stillLockedRollDegrees;
        private bool _hasPreviousPhysicalSample;
        private float _previousPhysicalYawDegrees;
        private float _previousPhysicalPitchDegrees;
        private float _previousPhysicalRollDegrees;

        public HeadImuReceiver HeadImuReceiver => headImuReceiver;
        public bool HasFreshSample => headImuReceiver != null && headImuReceiver.HasFreshSample && headImuReceiver.LastSampleAgeSeconds <= staleAfterSeconds;
        public float PhysicalYawDegrees => _physicalYawDegrees;
        public float PhysicalPitchDegrees => _physicalPitchDegrees;
        public float PhysicalRollDegrees => _physicalRollDegrees;
        public float NeutralYawDegrees => _neutralYawDegrees;
        public float NeutralPitchDegrees => _neutralPitchDegrees;
        public float NeutralRollDegrees => _neutralRollDegrees;
        public float VirtualYawDegrees => _virtualYawDegrees;
        public float VirtualPitchDegrees => _virtualPitchDegrees;
        public float VirtualRollDegrees => 0f;
        public Quaternion VirtualRotation => Quaternion.Euler(-_virtualPitchDegrees, _virtualYawDegrees, 0f);

        private void Update()
        {
            headImuReceiver ??= GetComponent<HeadImuReceiver>() ?? FindFirstObjectByType<HeadImuReceiver>();
            UpdateFromTelemetry();
        }

        public void Recenter()
        {
            _neutralYawDegrees = _physicalYawDegrees;
            _neutralPitchDegrees = _physicalPitchDegrees;
            _neutralRollDegrees = _physicalRollDegrees;
            _hasNeutral = true;
            _hasStillLock = false;
        }

        private void UpdateFromTelemetry()
        {
            if (headImuReceiver == null)
            {
                return;
            }

            _physicalYawDegrees = headImuReceiver.YawDegrees;
            _physicalPitchDegrees = headImuReceiver.PitchDegrees;
            _physicalRollDegrees = headImuReceiver.RollDegrees;

            if (!_hasNeutral && HasFreshSample)
            {
                Recenter();
            }

            float targetYaw = 0f;
            float targetPitch = 0f;

            if (HasFreshSample)
            {
                float stableYawDegrees = _physicalYawDegrees;
                float stablePitchDegrees = _physicalPitchDegrees;
                float stableRollDegrees = _physicalRollDegrees;
                bool hasInferredStillness = false;
                if (_hasPreviousPhysicalSample)
                {
                    float yawFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalYawDegrees - _previousPhysicalYawDegrees));
                    float pitchFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalPitchDegrees - _previousPhysicalPitchDegrees));
                    float rollFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalRollDegrees - _previousPhysicalRollDegrees));
                    hasInferredStillness = yawFrameDelta <= inferredStillFrameDeltaDegrees &&
                                           pitchFrameDelta <= inferredStillFrameDeltaDegrees &&
                                           rollFrameDelta <= inferredStillFrameDeltaDegrees;
                }

                bool isStill = headImuReceiver.Stillness01 >= stillnessHoldThreshold || hasInferredStillness;
                if (isStill)
                {
                    if (!_hasStillLock)
                    {
                        _stillLockedYawDegrees = _physicalYawDegrees;
                        _stillLockedPitchDegrees = _physicalPitchDegrees;
                        _stillLockedRollDegrees = _physicalRollDegrees;
                        _hasStillLock = true;
                    }
                    else if (Mathf.Abs(NormalizeSignedAngle(_physicalYawDegrees - _stillLockedYawDegrees)) > stillnessPhysicalDriftToleranceDegrees ||
                             Mathf.Abs(NormalizeSignedAngle(_physicalPitchDegrees - _stillLockedPitchDegrees)) > stillnessPhysicalDriftToleranceDegrees ||
                             Mathf.Abs(NormalizeSignedAngle(_physicalRollDegrees - _stillLockedRollDegrees)) > stillnessPhysicalDriftToleranceDegrees)
                    {
                        isStill = false;
                    }
                }

                if (isStill)
                {
                    stableYawDegrees = _stillLockedYawDegrees;
                    stablePitchDegrees = _stillLockedPitchDegrees;
                    stableRollDegrees = _stillLockedRollDegrees;
                }
                else
                {
                    _hasStillLock = false;
                }

                float yawDelta = NormalizeSignedAngle(stableYawDegrees - _neutralYawDegrees);
                float pitchDelta = NormalizeSignedAngle(stablePitchDegrees - _neutralPitchDegrees);
                float rollDelta = NormalizeSignedAngle(stableRollDegrees - _neutralRollDegrees);

                if (invertYaw)
                {
                    yawDelta = -yawDelta;
                }

                if (invertPitch)
                {
                    pitchDelta = -pitchDelta;
                }

                if (invertRollToYaw)
                {
                    rollDelta = -rollDelta;
                }

                targetPitch = Mathf.Clamp(ApplyDeadzone(pitchDelta, pitchDeadzoneDegrees) * pitchSensitivity, -maximumVirtualPitchDegrees, maximumVirtualPitchDegrees);
                if (yawInputMode == YawInputMode.PhysicalYaw)
                {
                    targetYaw = Mathf.Clamp(ApplyDeadzone(yawDelta, yawDeadzoneDegrees) * yawSensitivity, -maximumVirtualYawDegrees, maximumVirtualYawDegrees);
                }
                else
                {
                    targetYaw = Mathf.Clamp(ApplyDeadzone(rollDelta, rollDeadzoneDegrees) * rollToYawSensitivity, -maximumVirtualYawDegrees, maximumVirtualYawDegrees);
                }

                ApplyDiagonalAimBoost(ref targetYaw, ref targetPitch);
            }

            _previousPhysicalYawDegrees = _physicalYawDegrees;
            _previousPhysicalPitchDegrees = _physicalPitchDegrees;
            _previousPhysicalRollDegrees = _physicalRollDegrees;
            _hasPreviousPhysicalSample = HasFreshSample;

            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0f, smoothingStrength) * deltaTime);
            _virtualYawDegrees = Mathf.Lerp(_virtualYawDegrees, targetYaw, lerpFactor);
            _virtualPitchDegrees = Mathf.Lerp(_virtualPitchDegrees, targetPitch, lerpFactor);
            if (Mathf.Abs(_virtualYawDegrees) <= snapToNeutralVirtualDegrees)
            {
                _virtualYawDegrees = 0f;
            }

            if (Mathf.Abs(_virtualPitchDegrees) <= snapToNeutralVirtualDegrees)
            {
                _virtualPitchDegrees = 0f;
            }
        }

        private static float ApplyDeadzone(float valueDegrees, float deadzoneDegrees)
        {
            float absoluteValue = Mathf.Abs(valueDegrees);
            if (absoluteValue <= deadzoneDegrees)
            {
                return 0f;
            }

            return Mathf.Sign(valueDegrees) * (absoluteValue - deadzoneDegrees);
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

        private void ApplyDiagonalAimBoost(ref float yawDegrees, ref float pitchDegrees)
        {
            if (diagonalAimBoost <= 1f ||
                Mathf.Abs(yawDegrees) < diagonalAimThresholdDegrees ||
                Mathf.Abs(pitchDegrees) < diagonalAimThresholdDegrees)
            {
                return;
            }

            yawDegrees = Mathf.Clamp(yawDegrees * diagonalAimBoost, -maximumVirtualYawDegrees, maximumVirtualYawDegrees);
            pitchDegrees = Mathf.Clamp(pitchDegrees * diagonalAimBoost, -maximumVirtualPitchDegrees, maximumVirtualPitchDegrees);
        }
    }
}
