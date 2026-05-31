using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoWorldFloorSurface : MonoBehaviour
    {
        [SerializeField] private Vector2 sizeMeters = new Vector2(80f, 80f);
        [SerializeField] private Color baseColor = new Color(0.075f, 0.078f, 0.082f);
        [SerializeField] private Color wetColor = new Color(0.10f, 0.12f, 0.14f);

        private Renderer _renderer;
        private Material _material;

        public void Apply(float rainIntensity01, bool forestVisible)
        {
            EnsureInitialized();
            float wet = forestVisible ? 0.15f : Mathf.Clamp01(rainIntensity01);
            Color color = Color.Lerp(baseColor, wetColor, wet);
            _material.color = color;
            if (_material.HasProperty("_Smoothness"))
            {
                _material.SetFloat("_Smoothness", Mathf.Lerp(0.34f, 0.58f, wet));
            }

            if (_material.HasProperty("_Metallic"))
            {
                _material.SetFloat("_Metallic", 0f);
            }

            if (_material.HasProperty("_EmissionColor"))
            {
                _material.SetColor("_EmissionColor", color * Mathf.Lerp(0.02f, 0.07f, wet));
            }
        }

        private void EnsureInitialized()
        {
            if (_renderer != null)
            {
                return;
            }

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FinalDemoWideReflectiveFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.045f, 2f);
            floor.transform.localScale = new Vector3(sizeMeters.x, 0.035f, sizeMeters.y);
            Collider collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _renderer = floor.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _material = new Material(shader)
            {
                name = "FinalDemo Wide Reflective Floor",
                color = baseColor,
            };
            _renderer.material = _material;
        }
    }
}
