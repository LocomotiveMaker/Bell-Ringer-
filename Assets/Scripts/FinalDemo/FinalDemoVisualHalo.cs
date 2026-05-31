using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoVisualHalo : MonoBehaviour
    {
        [SerializeField] private Color haloColor = new Color(0.18f, 1f, 0.36f);
        [SerializeField] private Vector3 localOffset = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private bool flattened;
        [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.05f;
        [SerializeField, Range(0f, 1f)] private float pulseAlpha = 0.02f;
        [SerializeField] private float pulseRate = 0.9f;

        private const string HaloChildName = "FinalDemo_VisualHalo";

        private Transform _haloTransform;
        private Renderer _haloRenderer;
        private Material _haloMaterial;

        public void Configure(Color color, Vector3 offset, Vector3 scale, bool useFlattenedShape)
        {
            haloColor = color;
            localOffset = offset;
            localScale = scale;
            flattened = useFlattenedShape;
            baseAlpha = useFlattenedShape ? 0.14f : 0.12f;
            pulseAlpha = useFlattenedShape ? 0.055f : 0.045f;
            pulseRate = useFlattenedShape ? 1.15f : 0.82f;
            EnsureHalo();
        }

        private void Awake()
        {
            EnsureHalo();
        }

        private void OnEnable()
        {
            EnsureHalo();
        }

        private void LateUpdate()
        {
            EnsureHalo();
            if (_haloTransform == null || _haloMaterial == null)
            {
                return;
            }

            _haloTransform.localPosition = localOffset;
            _haloTransform.localRotation = Quaternion.identity;
            _haloTransform.localScale = localScale;
            if (_haloRenderer != null)
            {
                _haloRenderer.enabled = true;
            }

            float pulse = 0.5f + Mathf.Sin(Time.time * Mathf.PI * 2f * pulseRate) * 0.5f;
            float alpha = Mathf.Clamp01(baseAlpha + pulse * pulseAlpha);
            Color color = new Color(haloColor.r * alpha, haloColor.g * alpha, haloColor.b * alpha, alpha);
            _haloMaterial.color = color;
            if (_haloMaterial.HasProperty("_BaseColor"))
            {
                _haloMaterial.SetColor("_BaseColor", color);
            }
        }

        private void EnsureHalo()
        {
            if (_haloTransform != null && _haloMaterial != null)
            {
                return;
            }

            Transform existing = transform.Find(HaloChildName);
            if (existing != null)
            {
                _haloTransform = existing;
                _haloRenderer = existing.GetComponent<Renderer>();
            }
            else
            {
                GameObject haloObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                haloObject.name = HaloChildName;
                haloObject.transform.SetParent(transform, false);
                Collider collider = haloObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                _haloTransform = haloObject.transform;
                _haloRenderer = haloObject.GetComponent<Renderer>();
            }

            if (_haloRenderer == null)
            {
                return;
            }

            _haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _haloRenderer.receiveShadows = false;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Legacy Shaders/Particles/Additive") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            _haloMaterial = new Material(shader)
            {
                name = $"FinalDemo Halo {name}",
            };
            _haloMaterial.renderQueue = 3000;
            if (_haloMaterial.HasProperty("_Surface"))
            {
                _haloMaterial.SetFloat("_Surface", 1f);
            }

            if (_haloMaterial.HasProperty("_Blend"))
            {
                _haloMaterial.SetFloat("_Blend", 1f);
            }

            _haloMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _haloMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _haloMaterial.SetInt("_ZWrite", 0);
            _haloMaterial.DisableKeyword("_ALPHATEST_ON");
            _haloMaterial.EnableKeyword("_ALPHABLEND_ON");
            _haloRenderer.sharedMaterial = _haloMaterial;
        }
    }
}
