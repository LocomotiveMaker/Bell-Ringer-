using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoGlobalGlitchOverlay : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool effectEnabled = true;
        [SerializeField, Range(0f, 0.25f)] private float baseIntensity = 0.018f;
        [SerializeField] private float overlayDistance = 0.24f;
        [SerializeField] private Color regularColor = new Color(0.74f, 0.08f, 1f, 1f);
        [SerializeField] private Color bossColor = new Color(1f, 0.12f, 0.66f, 1f);

        private const string ShaderName = "Hidden/BellRinger/FinalDemo/GlobalGlitchOverlay";

        private MeshRenderer _renderer;
        private Material _material;
        private float _stageBoost;
        private bool _bossMode;

        public bool EffectEnabled => effectEnabled;
        public float BaseIntensity
        {
            get => baseIntensity;
            set => baseIntensity = Mathf.Clamp(value, 0f, 0.25f);
        }

        public void Initialize(Camera cameraTarget)
        {
            targetCamera = cameraTarget;
            EnsureOverlay();
        }

        public void SetEffectEnabled(bool enabled)
        {
            effectEnabled = enabled;
            if (_renderer != null)
            {
                _renderer.enabled = enabled;
            }
        }

        public void ApplyRuntimeState(float stageBoost, bool boss)
        {
            _stageBoost = Mathf.Clamp01(stageBoost);
            _bossMode = boss;
        }

        private void LateUpdate()
        {
            EnsureOverlay();
            if (_renderer == null || targetCamera == null)
            {
                return;
            }

            float totalIntensity = effectEnabled ? Mathf.Clamp01(baseIntensity + _stageBoost) : 0f;
            _renderer.enabled = totalIntensity > 0.001f;
            if (!_renderer.enabled)
            {
                return;
            }

            Transform cameraTransform = targetCamera.transform;
            transform.SetParent(cameraTransform, false);
            transform.localPosition = new Vector3(0f, 0f, Mathf.Max(0.12f, overlayDistance));
            transform.localRotation = Quaternion.identity;

            float height = Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * transform.localPosition.z * 2f;
            float width = height * targetCamera.aspect;
            transform.localScale = new Vector3(width, height, 1f);

            _material.SetFloat("_Intensity", totalIntensity);
            _material.SetFloat("_BossMode", _bossMode ? 1f : 0f);
            _material.SetColor("_RegularColor", regularColor);
            _material.SetColor("_BossColor", bossColor);
        }

        private void EnsureOverlay()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (_renderer != null)
            {
                return;
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FinalDemo_GlobalGlitchOverlay";
            quad.transform.SetParent(transform, false);
            quad.layer = LayerMask.NameToLayer("Ignore Raycast");
            Destroy(quad.GetComponent<Collider>());

            _renderer = quad.GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                return;
            }

            _material = new Material(shader)
            {
                name = "FinalDemo Global Glitch Overlay",
            };
            _renderer.sharedMaterial = _material;
        }
    }
}
