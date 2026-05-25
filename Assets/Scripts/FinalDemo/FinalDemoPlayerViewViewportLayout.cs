using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoPlayerViewViewportLayout : MonoBehaviour
    {
        [SerializeField] private Camera playerViewCamera;
        [SerializeField] private FinalDemoOperatorControls operatorControls;
        [SerializeField] private bool followOperatorHudBand = true;
        [SerializeField] private bool fillRemainingTopArea = true;
        [SerializeField, Range(0.25f, 1f)] private float viewportWidth01 = 0.64f;
        [SerializeField, Range(0.12f, 0.75f)] private float viewportHeight01 = 0.32f;
        [SerializeField] private float bottomHudBandPixels = 316f;
        [SerializeField] private float bottomGapPixels = 18f;
        [SerializeField] private float verticalOffsetPixels;
        [SerializeField] private Color outsideViewportColor = Color.black;

        private Camera _clearCamera;

        private void Awake()
        {
            ResolveCamera();
            EnsureClearCamera();
            ApplyLayout();
        }

        private void LateUpdate()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            viewportWidth01 = Mathf.Clamp(viewportWidth01, 0.25f, 1f);
            viewportHeight01 = Mathf.Clamp(viewportHeight01, 0.12f, 0.75f);
            bottomHudBandPixels = Mathf.Max(0f, bottomHudBandPixels);
            bottomGapPixels = Mathf.Max(0f, bottomGapPixels);
            ApplyLayout();
        }

        private void ResolveCamera()
        {
            playerViewCamera ??= GetComponent<Camera>();
            playerViewCamera ??= Camera.main;
            operatorControls ??= FindFirstObjectByType<FinalDemoOperatorControls>();
        }

        private void EnsureClearCamera()
        {
            if (_clearCamera != null || playerViewCamera == null)
            {
                return;
            }

            GameObject clearObject = new GameObject("FinalDemoViewportBlackBackdropCamera");
            clearObject.transform.SetParent(transform, false);
            _clearCamera = clearObject.AddComponent<Camera>();
            _clearCamera.clearFlags = CameraClearFlags.SolidColor;
            _clearCamera.backgroundColor = outsideViewportColor;
            _clearCamera.cullingMask = 0;
            _clearCamera.depth = playerViewCamera.depth - 100f;
            _clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _clearCamera.useOcclusionCulling = false;
            _clearCamera.allowHDR = false;
            _clearCamera.allowMSAA = false;
        }

        private void ApplyLayout()
        {
            ResolveCamera();
            if (playerViewCamera == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            EnsureClearCamera();
            if (_clearCamera != null)
            {
                _clearCamera.backgroundColor = outsideViewportColor;
                _clearCamera.depth = playerViewCamera.depth - 100f;
            }

            float hudBandPixels = followOperatorHudBand && operatorControls != null
                ? operatorControls.RuntimeHudTopPixelsFromBottom
                : bottomHudBandPixels;
            float yPixels = hudBandPixels + bottomGapPixels + verticalOffsetPixels;
            float y = Mathf.Clamp01(yPixels / Mathf.Max(1f, Screen.height));

            if (fillRemainingTopArea)
            {
                float height01 = Mathf.Clamp01(1f - y);
                playerViewCamera.rect = new Rect(0f, y, 1f, height01);
                return;
            }

            float width01 = Mathf.Clamp01(viewportWidth01);
            float manualHeight01 = Mathf.Clamp01(viewportHeight01);
            float x = (1f - width01) * 0.5f;
            if (y + manualHeight01 > 0.98f)
            {
                y = Mathf.Max(0f, 0.98f - manualHeight01);
            }

            playerViewCamera.rect = new Rect(x, y, width01, manualHeight01);
        }
    }
}
