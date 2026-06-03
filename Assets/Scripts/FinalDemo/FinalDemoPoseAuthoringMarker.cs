using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoPoseAuthoringMarker : MonoBehaviour
    {
        [SerializeField] private bool useTransformLocalPose = true;
        [SerializeField] private Vector3 targetCameraSpacePosition = new Vector3(0f, 0f, 0.7f);
        [SerializeField] private Vector3 targetYawPitchRollDegrees;
        [SerializeField] private bool overrideTolerances;
        [SerializeField] private float positionToleranceMeters = 0.18f;
        [SerializeField] private float rotationToleranceDegrees = 28f;

        public bool OverrideTolerances => overrideTolerances;
        public float PositionToleranceMeters => Mathf.Max(0.01f, positionToleranceMeters);
        public float RotationToleranceDegrees => Mathf.Max(1f, rotationToleranceDegrees);
        public bool UseTransformLocalPose => useTransformLocalPose;
        public Vector3 StoredTargetCameraSpacePosition => targetCameraSpacePosition;
        public Vector3 StoredTargetYawPitchRollDegrees => targetYawPitchRollDegrees;

        public Vector3 TargetCameraSpacePosition => targetCameraSpacePosition;
        public Vector3 TargetYawPitchRollDegrees => targetYawPitchRollDegrees;

        private void Awake()
        {
            if (!useTransformLocalPose)
            {
                ApplyStoredPoseToTransform();
            }
        }

        private void OnValidate()
        {
            positionToleranceMeters = Mathf.Max(0.01f, positionToleranceMeters);
            rotationToleranceDegrees = Mathf.Max(1f, rotationToleranceDegrees);
            if (!useTransformLocalPose)
            {
                ApplyStoredPoseToTransform();
            }
        }

        public void CaptureFrom(PadPoseProvider provider)
        {
            if (provider == null || !provider.HasResolvedRotation)
            {
                return;
            }

            targetCameraSpacePosition = provider.CameraSpacePosition;
            targetYawPitchRollDegrees = new Vector3(
                NormalizeSignedAngle(provider.ResolvedYawDegrees),
                NormalizeSignedAngle(provider.ResolvedPitchDegrees),
                NormalizeSignedAngle(provider.ResolvedRollDegrees));
            useTransformLocalPose = false;
            ApplyStoredPoseToTransform();
        }

        public void ApplyTo(PadPoseMatchEvaluator evaluator)
        {
            if (evaluator == null)
            {
                return;
            }

            Vector3 ypr = TargetYawPitchRollDegrees;
            evaluator.SetTargetPose(TargetCameraSpacePosition, ypr.x, ypr.y, ypr.z);
        }

        private void ApplyStoredPoseToTransform()
        {
            transform.localPosition = targetCameraSpacePosition;
            transform.localRotation = Quaternion.Euler(-targetYawPitchRollDegrees.y, targetYawPitchRollDegrees.x, -targetYawPitchRollDegrees.z);
        }

        private Vector3 ResolveYawPitchRollFromTransform()
        {
            Vector3 euler = transform.localRotation.eulerAngles;
            return new Vector3(
                NormalizeSignedAngle(euler.y),
                NormalizeSignedAngle(-euler.x),
                NormalizeSignedAngle(-euler.z));
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            return Mathf.Approximately(wrapped, -180f) ? 180f : wrapped;
        }
    }
}
