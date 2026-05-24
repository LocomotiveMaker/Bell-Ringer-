using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverTinnitusView : MonoBehaviour
    {
        private const int TearCount = 4;

        private Transform _root;
        private Renderer _outerRenderer;
        private Renderer _innerRenderer;
        private Renderer _coreRenderer;
        private Renderer[] _wallRenderers;
        private LineRenderer[] _tears;
        private bool _initialized;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director)
        {
            EnsureVisuals();

            bool visible = (snapshot.stage == FinalDemoStage.GeneralTinnitusOne || snapshot.stage == FinalDemoStage.GeneralTinnitusTwo) && snapshot.hasTinnitus;
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            float cleanse = Mathf.Clamp01(snapshot.tinnitusProgress01);
            float match = Mathf.Clamp01(snapshot.tinnitusMatch01);
            float instability = 1f - cleanse;
            float time = Time.realtimeSinceStartup;
            float jitter = instability * 0.08f;
            Vector3 jitterOffset = new Vector3(
                Mathf.Sin(time * 8.6f) * jitter,
                Mathf.Cos(time * 6.2f) * jitter * 0.65f,
                Mathf.Sin(time * 7.1f + 0.6f) * jitter * 0.35f);

            _root.position = snapshot.tinnitusWorldPosition + jitterOffset;
            _root.rotation = Quaternion.Euler(
                Mathf.Sin(time * 5.6f) * instability * 8f,
                time * 22f * instability,
                Mathf.Cos(time * 6.8f) * instability * 7f);
            _root.localScale = Vector3.one * Mathf.Lerp(0.86f, 0.42f, cleanse * 0.78f);

            Color outerColor = Color.Lerp(new Color(0.11f, 0.05f, 0.18f), new Color(0.28f, 0.10f, 0.46f), 0.55f + instability * 0.45f);
            Color innerColor = Color.Lerp(new Color(0.14f, 0.05f, 0.24f), new Color(0.48f, 0.14f, 0.82f), 0.45f + match * 0.35f);
            Color coreColor = Color.Lerp(new Color(0.36f, 0.14f, 0.78f), new Color(0.78f, 0.36f, 1f), 0.45f + instability * 0.30f);

            if (_outerRenderer != null)
            {
                _outerRenderer.material.color = outerColor;
                ApplyEmission(_outerRenderer.material, outerColor * (0.08f + instability * 0.22f));
            }

            if (_innerRenderer != null)
            {
                _innerRenderer.material.color = innerColor;
                ApplyEmission(_innerRenderer.material, innerColor * (0.22f + instability * 0.45f));
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.09f, cleanse);
                _coreRenderer.material.color = coreColor;
                ApplyEmission(_coreRenderer.material, coreColor * (0.7f + match * 1.2f));
            }

            if (_wallRenderers != null)
            {
                for (int index = 0; index < _wallRenderers.Length; index++)
                {
                    Renderer wallRenderer = _wallRenderers[index];
                    if (wallRenderer == null)
                    {
                        continue;
                    }

                    float side = index == 0 ? -1f : 1f;
                    wallRenderer.transform.localPosition = new Vector3(side * 1.45f, 0f, 0.55f + Mathf.Sin(time * (4.2f + index)) * 0.04f);
                    Color wallColor = Color.Lerp(new Color(0.26f, 0.48f, 0.56f), new Color(0.82f, 0.90f, 0.96f), 0.28f + instability * 0.15f);
                    wallRenderer.material.color = wallColor;
                    ApplyEmission(wallRenderer.material, wallColor * 0.06f);
                }
            }

            for (int index = 0; index < TearCount; index++)
            {
                LineRenderer tear = _tears[index];
                if (tear == null)
                {
                    continue;
                }

                float angle = index / (float)TearCount * Mathf.PI * 2f + time * 0.6f;
                float length = Mathf.Lerp(0.22f, 0.58f, instability) * (0.8f + Mathf.Abs(Mathf.Sin(time * (2.6f + index * 0.7f))) * 0.45f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle * 1.2f) * 0.35f, Mathf.Sin(angle));
                tear.startWidth = 0.026f + instability * 0.03f;
                tear.endWidth = 0.004f;
                Color tearColor = new Color(0.72f, 0.28f, 1f, 0.22f + instability * 0.38f);
                tear.startColor = tearColor;
                tear.endColor = tearColor * new Color(1f, 1f, 1f, 0.15f);
                tear.SetPosition(0, Vector3.zero);
                tear.SetPosition(1, dir.normalized * length);
            }
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _root = new GameObject("ObserverTinnitusViewRoot").transform;
            _root.SetParent(transform, false);

            _outerRenderer = CreatePart("OuterMass", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.52f, 0.52f, 0.52f), new Color(0.16f, 0.06f, 0.24f));
            _innerRenderer = CreatePart("InnerMass", PrimitiveType.Sphere, new Vector3(0.04f, 0.02f, -0.01f), new Vector3(0.31f, 0.31f, 0.31f), new Color(0.42f, 0.12f, 0.70f));
            _coreRenderer = CreatePart("Core", PrimitiveType.Sphere, new Vector3(-0.02f, 0.01f, 0.02f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.72f, 0.32f, 1f));

            _wallRenderers = new Renderer[2];
            _wallRenderers[0] = CreatePart("LeftWall", PrimitiveType.Cube, new Vector3(-1.45f, 0f, 0.55f), new Vector3(0.08f, 1.05f, 2.2f), new Color(0.82f, 0.90f, 0.96f));
            _wallRenderers[1] = CreatePart("RightWall", PrimitiveType.Cube, new Vector3(1.45f, 0f, 0.55f), new Vector3(0.08f, 1.05f, 2.2f), new Color(0.82f, 0.90f, 0.96f));

            _tears = new LineRenderer[TearCount];
            for (int index = 0; index < TearCount; index++)
            {
                GameObject tearObject = new GameObject($"Tear_{index:00}");
                tearObject.transform.SetParent(_root, false);
                LineRenderer tear = tearObject.AddComponent<LineRenderer>();
                tear.material = CreateLineMaterial();
                tear.useWorldSpace = false;
                tear.positionCount = 2;
                tear.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tear.receiveShadows = false;
                _tears[index] = tear;
            }
        }

        private Renderer CreatePart(string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(_root, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = CreateMaterial(color);
            return renderer;
        }

        private void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Simple Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = color,
            };
            return material;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            return new Material(shader);
        }

        private static void ApplyEmission(Material material, Color emission)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                return;
            }

            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }
    }
}
