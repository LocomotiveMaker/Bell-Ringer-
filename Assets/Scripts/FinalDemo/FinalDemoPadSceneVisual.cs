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
        [SerializeField] private float referenceForwardMeters = 1.05f;
        [SerializeField] private float verticalLiftMultiplier = 2.15f;
        [SerializeField] private float depthVisualMultiplier = 2.35f;
        [SerializeField] private float depthScaleMultiplier = 0.18f;
        [SerializeField] private float nearCameraSpaceZ = -0.25f;
        [SerializeField] private float farCameraSpaceZ = -1.15f;
        [SerializeField] private Vector2 depthLocalZRange = new Vector2(0.48f, 1.95f);
        [SerializeField] private Vector2 depthScaleRange = new Vector2(1.2f, 0.82f);
        [SerializeField] private Vector3 minViewLocalPosition = new Vector3(-0.95f, -0.78f, 0.55f);
        [SerializeField] private Vector3 maxViewLocalPosition = new Vector3(0.95f, -0.18f, 1.38f);
        [SerializeField] private float positionLerp = 22f;
        [SerializeField] private float rotationLerp = 22f;
        [SerializeField] private bool keepLastPoseWhenTrackingLost = true;

        private bool _hasLastPose;
        private Vector3 _lastLocalPosition;
        private Quaternion _lastLocalRotation = Quaternion.identity;
        private float _lastScaleMultiplier = 1f;
        private Vector3 _baseLocalScale;
        private bool _hasBaseScale;

        public bool HasCurrentViewPose => _hasLastPose;
        public Vector3 CurrentViewLocalPosition => _lastLocalPosition;

        private void Awake()
        {
            viewReference ??= Camera.main != null ? Camera.main.transform : null;
            padPoseProvider ??= FindFirstObjectByType<PadPoseProvider>();
            if (!_hasBaseScale)
            {
                _baseLocalScale = transform.localScale;
                _hasBaseScale = true;
            }
        }

        private void LateUpdate()
        {
            viewReference ??= Camera.main != null ? Camera.main.transform : null;
            padPoseProvider ??= FindFirstObjectByType<PadPoseProvider>();
            if (!_hasBaseScale)
            {
                _baseLocalScale = transform.localScale;
                _hasBaseScale = true;
            }

            if (viewReference == null)
            {
                return;
            }

            Vector3 targetLocal = baseViewLocalPosition;
            Quaternion targetLocalRotation = Quaternion.Euler(12f, 0f, 0f);
            float targetScaleMultiplier = 1f;
            bool hasFreshPose = padPoseProvider != null && padPoseProvider.HasPose;
            if (hasFreshPose)
            {
                Vector3 cameraSpace = padPoseProvider.CameraSpacePosition;
                float depth01 = ResolveDepth01(cameraSpace.z);
                float depthPosition01 = ApplyDepthStrength(depth01, depthVisualMultiplier, 2.35f);
                float depthScale01 = ApplyDepthStrength(depth01, depthScaleMultiplier, 0.18f);
                targetLocal += new Vector3(
                    cameraSpace.x * cameraSpaceScale.x,
                    cameraSpace.y * cameraSpaceScale.y * verticalLiftMultiplier,
                    0f);
                targetLocal.z = Mathf.Lerp(depthLocalZRange.x, depthLocalZRange.y, depthPosition01);
                targetLocal.x = Mathf.Clamp(targetLocal.x, minViewLocalPosition.x, maxViewLocalPosition.x);
                targetLocal.y = Mathf.Clamp(targetLocal.y, minViewLocalPosition.y, maxViewLocalPosition.y);
                targetLocal.z = Mathf.Clamp(
                    targetLocal.z,
                    Mathf.Min(minViewLocalPosition.z, depthLocalZRange.x),
                    Mathf.Max(maxViewLocalPosition.z, depthLocalZRange.y));
                targetLocalRotation = padPoseProvider.HasResolvedRotation ? padPoseProvider.RelativeRotation : targetLocalRotation;
                targetScaleMultiplier = Mathf.Lerp(depthScaleRange.x, depthScaleRange.y, depthScale01);
                _lastLocalPosition = targetLocal;
                _lastLocalRotation = targetLocalRotation;
                _lastScaleMultiplier = targetScaleMultiplier;
                _hasLastPose = true;
            }
            else if (keepLastPoseWhenTrackingLost && _hasLastPose)
            {
                targetLocal = _lastLocalPosition;
                targetLocalRotation = _lastLocalRotation;
                targetScaleMultiplier = _lastScaleMultiplier;
            }
            else
            {
                _lastLocalPosition = targetLocal;
                _lastLocalRotation = targetLocalRotation;
                _lastScaleMultiplier = targetScaleMultiplier;
                _hasLastPose = true;
            }

            Vector3 targetWorld = viewReference.TransformPoint(targetLocal);
            Quaternion targetWorldRotation = viewReference.rotation * targetLocalRotation;
            float position01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionLerp) * Time.unscaledDeltaTime);
            float rotation01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationLerp) * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetWorld, position01);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRotation, rotation01);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseLocalScale * targetScaleMultiplier, position01);
        }

        private float ResolveDepth01(float cameraSpaceZ)
        {
            if (Mathf.Approximately(nearCameraSpaceZ, farCameraSpaceZ))
            {
                return Mathf.InverseLerp(0.25f, Mathf.Max(0.26f, referenceForwardMeters), Mathf.Abs(cameraSpaceZ));
            }

            if (cameraSpaceZ < 0f)
            {
                return Mathf.InverseLerp(nearCameraSpaceZ, farCameraSpaceZ, cameraSpaceZ);
            }

            return Mathf.InverseLerp(Mathf.Abs(nearCameraSpaceZ), Mathf.Abs(farCameraSpaceZ), cameraSpaceZ);
        }

        private static float ApplyDepthStrength(float depth01, float strength, float defaultStrength)
        {
            float normalizedStrength = Mathf.Max(0.05f, strength / Mathf.Max(0.001f, defaultStrength));
            return Mathf.Clamp01(((Mathf.Clamp01(depth01) - 0.5f) * normalizedStrength) + 0.5f);
        }
    }
}
