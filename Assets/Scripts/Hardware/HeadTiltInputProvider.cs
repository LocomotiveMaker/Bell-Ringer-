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

        private enum HeadAxisSource
        {
            PhysicalYaw,
            PhysicalPitch,
            PhysicalRoll,
        }

        [SerializeField] private HeadImuReceiver headImuReceiver;
        [SerializeField] private YawInputMode yawInputMode = YawInputMode.PhysicalYaw;
        [SerializeField] private HeadAxisSource yawAxisSource = HeadAxisSource.PhysicalYaw;
        [SerializeField] private HeadAxisSource pitchAxisSource = HeadAxisSource.PhysicalRoll;
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
        [SerializeField] private float inferredStillMaxDegreesPerSecond = 8f;
        [SerializeField] private float stillnessPhysicalDriftToleranceDegrees = 0.7f;
        [SerializeField] private float snapToNeutralVirtualDegrees = 0.3f;
        [SerializeField] private float centerReturnSoftZoneDegrees = 4f;
        [SerializeField, Range(0f, 1f)] private float centerReturnDamping = 0.55f;
        [SerializeField] private bool holdPitchWhenStill;
        [SerializeField] private bool invertYaw = true;
        [SerializeField] private bool invertPitch = true;
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
                    float inferredStillnessThreshold = Mathf.Max(
                        inferredStillFrameDeltaDegrees,
                        Mathf.Max(0.0001f, Time.unscaledDeltaTime) * Mathf.Max(0f, inferredStillMaxDegreesPerSecond));
                    float yawFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalYawDegrees - _previousPhysicalYawDegrees));
                    float pitchFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalPitchDegrees - _previousPhysicalPitchDegrees));
                    float rollFrameDelta = Mathf.Abs(NormalizeSignedAngle(_physicalRollDegrees - _previousPhysicalRollDegrees));
                    hasInferredStillness = yawFrameDelta <= inferredStillnessThreshold &&
                                           pitchFrameDelta <= inferredStillnessThreshold &&
                                           rollFrameDelta <= inferredStillnessThreshold;
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
                    else if (ShouldBreakStillLock(HeadAxisSource.PhysicalYaw, _physicalYawDegrees, _stillLockedYawDegrees) ||
                             ShouldBreakStillLock(HeadAxisSource.PhysicalPitch, _physicalPitchDegrees, _stillLockedPitchDegrees) ||
                             ShouldBreakStillLock(HeadAxisSource.PhysicalRoll, _physicalRollDegrees, _stillLockedRollDegrees))
                    {
                        isStill = false;
                    }
                }

                if (isStill)
                {
                    stableYawDegrees = ShouldHoldAxisWhenStill(HeadAxisSource.PhysicalYaw) ? _stillLockedYawDegrees : _physicalYawDegrees;
                    stablePitchDegrees = ShouldHoldAxisWhenStill(HeadAxisSource.PhysicalPitch) ? _stillLockedPitchDegrees : _physicalPitchDegrees;
                    stableRollDegrees = ShouldHoldAxisWhenStill(HeadAxisSource.PhysicalRoll) ? _stillLockedRollDegrees : _physicalRollDegrees;
                }
                else
                {
                    _hasStillLock = false;
                }

                float yawDelta = NormalizeSignedAngle(stableYawDegrees - _neutralYawDegrees);
                float pitchDelta = NormalizeSignedAngle(stablePitchDegrees - _neutralPitchDegrees);
                float rollDelta = NormalizeSignedAngle(stableRollDegrees - _neutralRollDegrees);

                float pitchInputDelta = SelectAxisDelta(pitchAxisSource, yawDelta, pitchDelta, rollDelta);
                if (invertPitch)
                {
                    pitchInputDelta = -pitchInputDelta;
                }

                targetPitch = Mathf.Clamp(ApplyDeadzone(pitchInputDelta, pitchDeadzoneDegrees) * pitchSensitivity, -maximumVirtualPitchDegrees, maximumVirtualPitchDegrees);
                if (yawInputMode == YawInputMode.PhysicalYaw)
                {
                    float yawInputDelta = SelectAxisDelta(yawAxisSource, yawDelta, pitchDelta, rollDelta);
                    if (invertYaw)
                    {
                        yawInputDelta = -yawInputDelta;
                    }

                    targetYaw = Mathf.Clamp(ApplyDeadzone(yawInputDelta, yawDeadzoneDegrees) * yawSensitivity, -maximumVirtualYawDegrees, maximumVirtualYawDegrees);
                }
                else
                {
                    float rollToYawDelta = invertRollToYaw ? -rollDelta : rollDelta;
                    targetYaw = Mathf.Clamp(ApplyDeadzone(rollToYawDelta, rollDeadzoneDegrees) * rollToYawSensitivity, -maximumVirtualYawDegrees, maximumVirtualYawDegrees);
                }

                ApplyDiagonalAimBoost(ref targetYaw, ref targetPitch);
                targetYaw = ApplyCenterReturnDamping(targetYaw, maximumVirtualYawDegrees);
                targetPitch = ApplyCenterReturnDamping(targetPitch, maximumVirtualPitchDegrees);
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

        private float SelectAxisDelta(HeadAxisSource axisSource, float yawDelta, float pitchDelta, float rollDelta)
        {
            return axisSource switch
            {
                HeadAxisSource.PhysicalPitch => pitchDelta,
                HeadAxisSource.PhysicalRoll => rollDelta,
                _ => yawDelta,
            };
        }

        private bool ShouldHoldAxisWhenStill(HeadAxisSource axisSource)
        {
            if (!AxisAffectsOutput(axisSource))
            {
                return false;
            }

            return holdPitchWhenStill || pitchAxisSource != axisSource;
        }

        private bool ShouldBreakStillLock(HeadAxisSource axisSource, float physicalDegrees, float lockedDegrees)
        {
            return ShouldHoldAxisWhenStill(axisSource) &&
                   Mathf.Abs(NormalizeSignedAngle(physicalDegrees - lockedDegrees)) > stillnessPhysicalDriftToleranceDegrees;
        }

        private bool AxisAffectsOutput(HeadAxisSource axisSource)
        {
            if (pitchAxisSource == axisSource)
            {
                return true;
            }

            if (yawInputMode == YawInputMode.PhysicalYaw)
            {
                return yawAxisSource == axisSource;
            }

            return axisSource == HeadAxisSource.PhysicalRoll;
        }

        private float ApplyCenterReturnDamping(float valueDegrees, float maximumDegrees)
        {
            float softZone = Mathf.Max(snapToNeutralVirtualDegrees, centerReturnSoftZoneDegrees);
            float absoluteValue = Mathf.Abs(valueDegrees);
            if (absoluteValue <= snapToNeutralVirtualDegrees)
            {
                return 0f;
            }

            if (absoluteValue >= softZone)
            {
                return valueDegrees;
            }

            float t = absoluteValue / softZone;
            float damping = Mathf.Lerp(Mathf.Clamp01(centerReturnDamping), 1f, t * t);
            return Mathf.Clamp(valueDegrees * damping, -maximumDegrees, maximumDegrees);
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
