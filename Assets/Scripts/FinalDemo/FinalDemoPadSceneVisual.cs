using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoPadSceneVisual : MonoBehaviour
    {
        [SerializeField] private Transform viewReference;
        [SerializeField] private PadPoseProvider padPoseProvider;
        [SerializeField] private Vector3 baseViewLocalPosition = new Vector3(0f, -0.48f, 0.92f);
        [SerializeField] private Vector3 cameraSpaceScale = new Vector3(1.15f, 0.9f, 0.62f);
        [SerializeField] private Vector3 minViewLocalPosition = new Vector3(-0.95f, -0.78f, 0.55f);
        [SerializeField] private Vector3 maxViewLocalPosition = new Vector3(0.95f, -0.18f, 1.38f);
        [SerializeField] private float positionLerp = 22f;
        [SerializeField] private float rotationLerp = 22f;
        [SerializeField] private bool keepLastPoseWhenTrackingLost = true;

        private bool _hasLastPose;
        private Vector3 _lastLocalPosition;
        private Quaternion _lastLocalRotation = Quaternion.identity;

        private void Awake()
        {
            viewReference ??= Camera.main != null ? Camera.main.transform : null;
            padPoseProvider ??= FindFirstObjectByType<PadPoseProvider>();
        }

        private void LateUpdate()
        {
            viewReference ??= Camera.main != null ? Camera.main.transform : null;
            padPoseProvider ??= FindFirstObjectByType<PadPoseProvider>();
            if (viewReference == null)
            {
                return;
            }

            Vector3 targetLocal = baseViewLocalPosition;
            Quaternion targetLocalRotation = Quaternion.Euler(12f, 0f, 0f);
            bool hasFreshPose = padPoseProvider != null && padPoseProvider.HasPose;
            if (hasFreshPose)
            {
                Vector3 cameraSpace = padPoseProvider.CameraSpacePosition;
                targetLocal += new Vector3(cameraSpace.x * cameraSpaceScale.x, cameraSpace.y * cameraSpaceScale.y, (cameraSpace.z - 0.7f) * cameraSpaceScale.z);
                targetLocal.x = Mathf.Clamp(targetLocal.x, minViewLocalPosition.x, maxViewLocalPosition.x);
                targetLocal.y = Mathf.Clamp(targetLocal.y, minViewLocalPosition.y, maxViewLocalPosition.y);
                targetLocal.z = Mathf.Clamp(targetLocal.z, minViewLocalPosition.z, maxViewLocalPosition.z);
                targetLocalRotation = padPoseProvider.HasResolvedRotation ? padPoseProvider.RelativeRotation : targetLocalRotation;
                _lastLocalPosition = targetLocal;
                _lastLocalRotation = targetLocalRotation;
                _hasLastPose = true;
            }
            else if (keepLastPoseWhenTrackingLost && _hasLastPose)
            {
                targetLocal = _lastLocalPosition;
                targetLocalRotation = _lastLocalRotation;
            }
            else
            {
                _lastLocalPosition = targetLocal;
                _lastLocalRotation = targetLocalRotation;
                _hasLastPose = true;
            }

            Vector3 targetWorld = viewReference.TransformPoint(targetLocal);
            Quaternion targetWorldRotation = viewReference.rotation * targetLocalRotation;
            float position01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionLerp) * Time.unscaledDeltaTime);
            float rotation01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationLerp) * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetWorld, position01);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRotation, rotation01);
        }
    }
}
