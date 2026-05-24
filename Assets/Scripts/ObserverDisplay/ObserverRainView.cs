using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverRainView : MonoBehaviour
    {
        private const int SplashCount = 14;
        private const int WindLineCount = 3;

        private Transform _root;
        private Renderer _floorRenderer;
        private Renderer[] _splashRenderers;
        private LineRenderer[] _windLines;
        private bool _initialized;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director)
        {
            EnsureVisuals();

            bool visible = snapshot.stage == FinalDemoStage.BellFollowRain ||
                           (snapshot.stage == FinalDemoStage.BellGaze && snapshot.rainIntensity01 > 0.02f);
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            FinalDemoTuningProfile tuning = director != null ? director.TuningProfile : null;
            Vector3 center = tuning != null ? tuning.RainZoneCenter : snapshot.playerWorldPosition + new Vector3(0f, 0f, 2.9f);
            float radius = tuning != null ? tuning.RainZoneRadius : 2.4f;
            float intensity = Mathf.Clamp01(snapshot.rainIntensity01);
            if (snapshot.stage == FinalDemoStage.BellGaze)
            {
                intensity *= 0.35f;
            }

            _root.position = new Vector3(center.x, 0.01f, center.z);

            float time = Time.realtimeSinceStartup;
            if (_floorRenderer != null)
            {
                _floorRenderer.transform.localScale = new Vector3(radius * 1.4f, 0.008f, radius * 1.1f);
                Color floorColor = Color.Lerp(new Color(0.03f, 0.05f, 0.08f), new Color(0.05f, 0.12f, 0.26f), intensity);
                _floorRenderer.material.color = floorColor;
                ApplyEmission(_floorRenderer.material, floorColor * (0.08f + intensity * 0.16f));
            }

            for (int index = 0; index < SplashCount; index++)
            {
                Renderer splash = _splashRenderers[index];
                if (splash == null)
                {
                    continue;
                }

                float seed = index * 0.73f;
                float angle = time * (0.55f + index * 0.05f) + seed * 2.4f;
                float radial01 = 0.22f + Mathf.Repeat(seed * 0.41f, 0.58f);
                float localX = Mathf.Cos(angle * 0.81f) * radius * radial01;
                float localZ = Mathf.Sin(angle * 1.17f) * radius * radial01;
                float pulse = 0.45f + Mathf.Abs(Mathf.Sin(time * (5.6f + index * 0.21f) + seed)) * 0.55f;
                float splashScale = 0.05f + intensity * 0.22f * pulse;

                splash.transform.localPosition = new Vector3(localX, 0.002f + pulse * 0.006f, localZ);
                splash.transform.localScale = new Vector3(splashScale * 1.9f, 0.004f, splashScale);
                Color splashColor = Color.Lerp(new Color(0.04f, 0.12f, 0.32f), new Color(0.08f, 0.24f, 0.78f), pulse * intensity);
                splash.material.color = splashColor;
                ApplyEmission(splash.material, splashColor * (0.2f + intensity * 0.8f));
            }

            for (int index = 0; index < WindLineCount; index++)
            {
                LineRenderer windLine = _windLines[index];
                if (windLine == null)
                {
                    continue;
                }

                float lane01 = index / Mathf.Max(1f, WindLineCount - 1f);
                float z = Mathf.Lerp(-radius * 0.7f, radius * 0.7f, lane01);
                float sweep = Mathf.Sin(time * (1.5f + index * 0.18f)) * radius * 0.18f;
                Vector3 start = new Vector3(-radius * 0.9f + sweep, 0.42f + index * 0.06f, z - radius * 0.18f);
                Vector3 end = new Vector3(radius * 0.9f + sweep * 0.35f, 0.16f + index * 0.04f, z + radius * 0.18f);
                windLine.enabled = intensity > 0.08f;
                windLine.startWidth = 0.01f + intensity * 0.03f;
                windLine.endWidth = 0.006f + intensity * 0.018f;
                Color windColor = new Color(0.20f, 0.42f, 0.82f, 0.10f + intensity * 0.24f);
                windLine.startColor = windColor;
                windLine.endColor = windColor * new Color(1f, 1f, 1f, 0.68f);
                windLine.SetPosition(0, start);
                windLine.SetPosition(1, end);
            }
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _root = new GameObject("ObserverRainViewRoot").transform;
            _root.SetParent(transform, false);

            Material floorMaterial = CreateMaterial(new Color(0.03f, 0.05f, 0.08f));
            Material splashMaterial = CreateMaterial(new Color(0.08f, 0.24f, 0.78f));

            GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name = "RainFloor";
            floorObject.transform.SetParent(_root, false);
            Collider floorCollider = floorObject.GetComponent<Collider>();
            if (floorCollider != null)
            {
                Destroy(floorCollider);
            }

            _floorRenderer = floorObject.GetComponent<Renderer>();
            _floorRenderer.material = floorMaterial;

            _splashRenderers = new Renderer[SplashCount];
            for (int index = 0; index < SplashCount; index++)
            {
                GameObject splashObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                splashObject.name = $"RainSplash_{index:00}";
                splashObject.transform.SetParent(_root, false);
                Collider splashCollider = splashObject.GetComponent<Collider>();
                if (splashCollider != null)
                {
                    Destroy(splashCollider);
                }

                Renderer splashRenderer = splashObject.GetComponent<Renderer>();
                splashRenderer.material = new Material(splashMaterial);
                _splashRenderers[index] = splashRenderer;
            }

            _windLines = new LineRenderer[WindLineCount];
            for (int index = 0; index < WindLineCount; index++)
            {
                GameObject windObject = new GameObject($"WindLine_{index:00}");
                windObject.transform.SetParent(_root, false);
                LineRenderer windLine = windObject.AddComponent<LineRenderer>();
                windLine.material = CreateLineMaterial();
                windLine.useWorldSpace = false;
                windLine.positionCount = 2;
                windLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                windLine.receiveShadows = false;
                _windLines[index] = windLine;
            }
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
