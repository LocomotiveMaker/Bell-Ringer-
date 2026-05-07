using System.Collections.Generic;
using BellRinger.Hardware;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.Debug
{
    [DisallowMultipleComponent]
    public sealed class PadImuPoseCalibrationTool : MonoBehaviour
    {
        private static readonly string[] PoseLabels =
        {
            "0 Center",
            "1 Left 90",
            "2 Right 90",
            "3 Forward 90",
            "4 Backward 90",
            "5 Clockwise 90",
            "6 CounterCW 90",
        };

        private static readonly Quaternion[] TargetRotations =
        {
            Quaternion.identity,
            Quaternion.AngleAxis(90f, Vector3.forward),
            Quaternion.AngleAxis(-90f, Vector3.forward),
            Quaternion.AngleAxis(90f, Vector3.right),
            Quaternion.AngleAxis(-90f, Vector3.right),
            Quaternion.AngleAxis(90f, Vector3.up),
            Quaternion.AngleAxis(-90f, Vector3.up),
        };

        [SerializeField] private float captureDurationSeconds = 0.22f;
        [SerializeField] private int minimumSamplesPerCapture = 6;
        [SerializeField] private float applyRequiresCenterMatchDegrees = 20f;

        private readonly Quaternion[] _capturedRawQuaternions = new Quaternion[7];
        private readonly bool[] _hasCapturedRawQuaternion = new bool[7];
        private readonly List<Quaternion> _pendingSamples = new List<Quaternion>(32);

        private PadImuReceiver _imuReceiver;
        private int _pendingSlotIndex = -1;
        private float _pendingCaptureEndsAt;
        private float _lastMeanErrorDegrees;
        private float _lastMaxErrorDegrees;
        private string _lastStatus = "Capture poses 0-6, then press R to apply and save.";

        public bool IsCapturePending => _pendingSlotIndex >= 0;
        public float CaptureProgress01 => !IsCapturePending || captureDurationSeconds <= 0.0001f
            ? 0f
            : Mathf.Clamp01(1f - ((_pendingCaptureEndsAt - Time.unscaledTime) / captureDurationSeconds));
        public float LastMeanErrorDegrees => _lastMeanErrorDegrees;
        public float LastMaxErrorDegrees => _lastMaxErrorDegrees;
        public string LastStatus => _lastStatus;
        public string PendingPoseLabel => IsCapturePending ? PoseLabels[_pendingSlotIndex] : string.Empty;

        private void Start()
        {
            EnsureReceiver();
        }

        private void Update()
        {
            EnsureReceiver();
            HandleKeyboardShortcuts();
            ContinuePendingCapture();
        }

        public string GetPoseLabel(int slotIndex)
        {
            return PoseLabels[Mathf.Clamp(slotIndex, 0, PoseLabels.Length - 1)];
        }

        public bool HasCaptureForSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _hasCapturedRawQuaternion.Length && _hasCapturedRawQuaternion[slotIndex];
        }

        public void BeginCapture(int slotIndex)
        {
            EnsureReceiver();

            if (_imuReceiver == null)
            {
                _lastStatus = "PadImuReceiver was not found.";
                return;
            }

            if (!_imuReceiver.HasQuaternionTelemetry || !_imuReceiver.TryGetRawQuaternion(out _))
            {
                _lastStatus = "IMU quaternion is not fresh yet. Hold the pose still and try again.";
                return;
            }

            _pendingSamples.Clear();
            _pendingSlotIndex = Mathf.Clamp(slotIndex, 0, PoseLabels.Length - 1);
            _pendingCaptureEndsAt = Time.unscaledTime + Mathf.Max(0.05f, captureDurationSeconds);
            _lastStatus = $"Capturing {PoseLabels[_pendingSlotIndex]}...";
        }

        public void ApplyCapturedCalibration()
        {
            EnsureReceiver();

            if (_imuReceiver == null)
            {
                _lastStatus = "PadImuReceiver was not found.";
                return;
            }

            for (int index = 0; index < _hasCapturedRawQuaternion.Length; ++index)
            {
                if (!_hasCapturedRawQuaternion[index])
                {
                    _lastStatus = $"{PoseLabels[index]} has not been captured yet.";
                    return;
                }
            }

            if (!TrySolveCalibration(out Quaternion centerRawQuaternion, out Quaternion basisQuaternion, out float meanErrorDegrees, out float maxErrorDegrees, out string failureReason))
            {
                _lastStatus = failureReason;
                return;
            }

            if (!_imuReceiver.TryGetRawQuaternion(out Quaternion currentRawQuaternion))
            {
                _lastStatus = "Live IMU quaternion is not available. Return to pose 0 and try again.";
                return;
            }

            if (Quaternion.Angle(currentRawQuaternion, centerRawQuaternion) > applyRequiresCenterMatchDegrees)
            {
                _lastStatus = "Return to pose 0, then press R again.";
                return;
            }

            _imuReceiver.Recenter();
            _imuReceiver.ApplySavedMountCalibration(basisQuaternion);
            PadImuCalibrationStore.SaveBasisQuaternion(basisQuaternion);
            _lastMeanErrorDegrees = meanErrorDegrees;
            _lastMaxErrorDegrees = maxErrorDegrees;
            _lastStatus = $"Calibration applied and saved. mean={meanErrorDegrees:0.0}deg, max={maxErrorDegrees:0.0}deg";
        }

        private void EnsureReceiver()
        {
            if (_imuReceiver == null)
            {
                _imuReceiver = GetComponent<PadImuReceiver>();
            }
        }

        private void HandleKeyboardShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasDigitPressed(keyboard, 0))
            {
                BeginCapture(0);
            }
            else if (WasDigitPressed(keyboard, 1))
            {
                BeginCapture(1);
            }
            else if (WasDigitPressed(keyboard, 2))
            {
                BeginCapture(2);
            }
            else if (WasDigitPressed(keyboard, 3))
            {
                BeginCapture(3);
            }
            else if (WasDigitPressed(keyboard, 4))
            {
                BeginCapture(4);
            }
            else if (WasDigitPressed(keyboard, 5))
            {
                BeginCapture(5);
            }
            else if (WasDigitPressed(keyboard, 6))
            {
                BeginCapture(6);
            }

            if (keyboard.rKey.wasPressedThisFrame && !IsCapturePending)
            {
                ApplyCapturedCalibration();
            }
        }

        private void ContinuePendingCapture()
        {
            if (!IsCapturePending)
            {
                return;
            }

            if (_imuReceiver != null && _imuReceiver.TryGetRawQuaternion(out Quaternion rawQuaternion))
            {
                _pendingSamples.Add(rawQuaternion);
            }

            if (Time.unscaledTime < _pendingCaptureEndsAt)
            {
                return;
            }

            if (_pendingSamples.Count < minimumSamplesPerCapture)
            {
                _lastStatus = $"{PoseLabels[_pendingSlotIndex]} capture failed. Not enough IMU samples.";
                _pendingSlotIndex = -1;
                _pendingSamples.Clear();
                return;
            }

            Quaternion averageQuaternion = AverageQuaternions(_pendingSamples);
            _capturedRawQuaternions[_pendingSlotIndex] = averageQuaternion;
            _hasCapturedRawQuaternion[_pendingSlotIndex] = true;
            _lastStatus = $"{PoseLabels[_pendingSlotIndex]} captured ({_pendingSamples.Count} samples)";
            _pendingSlotIndex = -1;
            _pendingSamples.Clear();
        }

        private bool TrySolveCalibration(
            out Quaternion centerRawQuaternion,
            out Quaternion basisQuaternion,
            out float meanErrorDegrees,
            out float maxErrorDegrees,
            out string failureReason)
        {
            centerRawQuaternion = Quaternion.identity;
            basisQuaternion = Quaternion.identity;
            meanErrorDegrees = 0f;
            maxErrorDegrees = 0f;
            failureReason = string.Empty;

            centerRawQuaternion = NormalizeQuaternion(_capturedRawQuaternions[0]);

            if (!TryEstimateAxis(centerRawQuaternion, _capturedRawQuaternions[1], _capturedRawQuaternions[2], out Vector3 rawForwardAxis, out failureReason))
            {
                return false;
            }

            if (!TryEstimateAxis(centerRawQuaternion, _capturedRawQuaternions[3], _capturedRawQuaternions[4], out Vector3 rawRightAxis, out failureReason))
            {
                return false;
            }

            if (!TryEstimateAxis(centerRawQuaternion, _capturedRawQuaternions[5], _capturedRawQuaternions[6], out Vector3 rawUpAxis, out failureReason))
            {
                return false;
            }

            Vector3 xAxis = rawRightAxis.normalized;
            Vector3 yAxis = (rawUpAxis - (Vector3.Dot(rawUpAxis, xAxis) * xAxis)).normalized;
            if (yAxis.sqrMagnitude <= 0.0001f)
            {
                failureReason = "Clockwise/counterclockwise samples overlap too much with forward/backward. Capture them again more precisely.";
                return false;
            }

            Vector3 zAxis = Vector3.Cross(xAxis, yAxis).normalized;
            if (zAxis.sqrMagnitude <= 0.0001f)
            {
                failureReason = "Calibration axes could not be solved. Capture poses 1-6 again.";
                return false;
            }

            if (Vector3.Dot(zAxis, rawForwardAxis) < 0f)
            {
                zAxis = -zAxis;
                yAxis = -yAxis;
            }

            basisQuaternion = Quaternion.LookRotation(zAxis, yAxis);

            float errorSum = 0f;
            for (int index = 0; index < _capturedRawQuaternions.Length; ++index)
            {
                Quaternion predicted = ApplyPreviewCalibration(_capturedRawQuaternions[index], centerRawQuaternion, basisQuaternion);
                float errorDegrees = Quaternion.Angle(predicted, TargetRotations[index]);
                errorSum += errorDegrees;
                maxErrorDegrees = Mathf.Max(maxErrorDegrees, errorDegrees);
            }

            meanErrorDegrees = errorSum / _capturedRawQuaternions.Length;
            return true;
        }

        private static Quaternion ApplyPreviewCalibration(Quaternion rawQuaternion, Quaternion centerRawQuaternion, Quaternion basisQuaternion)
        {
            Quaternion relativeQuaternion = Quaternion.Normalize(Quaternion.Inverse(centerRawQuaternion) * NormalizeQuaternion(rawQuaternion));
            return Quaternion.Normalize(Quaternion.Inverse(basisQuaternion) * relativeQuaternion * basisQuaternion);
        }

        private static bool TryEstimateAxis(Quaternion centerRawQuaternion, Quaternion positivePoseQuaternion, Quaternion negativePoseQuaternion, out Vector3 axis, out string failureReason)
        {
            axis = Vector3.zero;
            failureReason = string.Empty;

            Quaternion positiveRelativeQuaternion = Quaternion.Normalize(Quaternion.Inverse(centerRawQuaternion) * NormalizeQuaternion(positivePoseQuaternion));
            Quaternion negativeRelativeQuaternion = Quaternion.Normalize(Quaternion.Inverse(centerRawQuaternion) * NormalizeQuaternion(negativePoseQuaternion));

            if (!TryExtractAxis(positiveRelativeQuaternion, out Vector3 positiveAxis) ||
                !TryExtractAxis(negativeRelativeQuaternion, out Vector3 negativeAxis))
            {
                failureReason = "A rotation axis could not be estimated. Hold the 90-degree poses more clearly.";
                return false;
            }

            Vector3 combinedAxis = Vector3.Dot(positiveAxis, negativeAxis) > 0f
                ? (positiveAxis + negativeAxis)
                : (positiveAxis - negativeAxis);

            if (combinedAxis.sqrMagnitude <= 0.0001f)
            {
                failureReason = "Opposite poses are not separated enough. Push the pad further and capture again.";
                return false;
            }

            axis = combinedAxis.normalized;
            return true;
        }

        private static bool TryExtractAxis(Quaternion quaternion, out Vector3 axis)
        {
            Quaternion normalizedQuaternion = NormalizeQuaternion(quaternion);
            if (normalizedQuaternion.w < 0f)
            {
                normalizedQuaternion.x = -normalizedQuaternion.x;
                normalizedQuaternion.y = -normalizedQuaternion.y;
                normalizedQuaternion.z = -normalizedQuaternion.z;
                normalizedQuaternion.w = -normalizedQuaternion.w;
            }

            float sinHalfAngle = Mathf.Sqrt(Mathf.Max(0f, 1f - (normalizedQuaternion.w * normalizedQuaternion.w)));
            if (sinHalfAngle <= 0.0001f)
            {
                axis = Vector3.zero;
                return false;
            }

            axis = new Vector3(
                normalizedQuaternion.x / sinHalfAngle,
                normalizedQuaternion.y / sinHalfAngle,
                normalizedQuaternion.z / sinHalfAngle).normalized;
            return axis.sqrMagnitude > 0.0001f;
        }

        private static Quaternion AverageQuaternions(List<Quaternion> quaternions)
        {
            Quaternion referenceQuaternion = NormalizeQuaternion(quaternions[0]);
            Vector4 accumulator = new Vector4(referenceQuaternion.x, referenceQuaternion.y, referenceQuaternion.z, referenceQuaternion.w);

            for (int index = 1; index < quaternions.Count; ++index)
            {
                Quaternion sampleQuaternion = NormalizeQuaternion(quaternions[index]);
                if (Quaternion.Dot(referenceQuaternion, sampleQuaternion) < 0f)
                {
                    sampleQuaternion.x = -sampleQuaternion.x;
                    sampleQuaternion.y = -sampleQuaternion.y;
                    sampleQuaternion.z = -sampleQuaternion.z;
                    sampleQuaternion.w = -sampleQuaternion.w;
                }

                accumulator.x += sampleQuaternion.x;
                accumulator.y += sampleQuaternion.y;
                accumulator.z += sampleQuaternion.z;
                accumulator.w += sampleQuaternion.w;
            }

            return NormalizeQuaternion(new Quaternion(accumulator.x, accumulator.y, accumulator.z, accumulator.w));
        }

        private static bool WasDigitPressed(Keyboard keyboard, int digit)
        {
            return digit switch
            {
                0 => keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame,
                1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
                4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
                5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
                6 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
                _ => false,
            };
        }

        private static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            float magnitude = Mathf.Sqrt(
                (quaternion.x * quaternion.x) +
                (quaternion.y * quaternion.y) +
                (quaternion.z * quaternion.z) +
                (quaternion.w * quaternion.w));

            if (magnitude <= 0.00001f)
            {
                return Quaternion.identity;
            }

            float inverseMagnitude = 1f / magnitude;
            return new Quaternion(
                quaternion.x * inverseMagnitude,
                quaternion.y * inverseMagnitude,
                quaternion.z * inverseMagnitude,
                quaternion.w * inverseMagnitude);
        }
    }
}
