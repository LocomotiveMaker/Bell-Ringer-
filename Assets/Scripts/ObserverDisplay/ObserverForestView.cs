using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverForestView : MonoBehaviour
    {
        private Transform _root;
        private Renderer[] _trunks;
        private Renderer[] _canopies;
        private Renderer _fogRenderer;
        private Renderer[] _motes;
        private bool _initialized;
        private bool _wasVisible;
        private float _visibleStartedAtRealtime;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director)
        {
            EnsureVisuals();

            bool visible = snapshot.stage == FinalDemoStage.ForestEnding || snapshot.stage == FinalDemoStage.Complete;
            if (!visible)
            {
                SetVisible(false);
                _wasVisible = false;
                return;
            }

            if (!_wasVisible)
            {
                _visibleStartedAtRealtime = Time.realtimeSinceStartup;
                _wasVisible = true;
            }

            SetVisible(true);

            Vector3 bellPosition = snapshot.hasForestBell ? snapshot.forestBellWorldPosition : snapshot.playerWorldPosition + new Vector3(0f, 0f, 3.2f);
            float fade01 = Mathf.Clamp01((Time.realtimeSinceStartup - _visibleStartedAtRealtime) / 2.6f);
            _root.position = new Vector3(bellPosition.x, 0f, bellPosition.z + 0.8f);

            if (_fogRenderer != null)
            {
                _fogRenderer.transform.localScale = new Vector3(9.5f, 0.06f, 6.5f);
                Color fogColor = Color.Lerp(new Color(0.04f, 0.05f, 0.06f), new Color(0.18f, 0.28f, 0.24f), fade01);
                _fogRenderer.material.color = fogColor;
                ApplyEmission(_fogRenderer.material, fogColor * 0.05f);
            }

            for (int index = 0; index < _trunks.Length; index++)
            {
                Renderer trunk = _trunks[index];
                Renderer canopy = _canopies[index];
                if (trunk == null || canopy == null)
                {
                    continue;
                }

                float side = index < 4 ? -1f : 1f;
                float lane = (index % 4) / 3f;
                float z = Mathf.Lerp(-2.2f, 2.4f, lane);
                float x = side * (1.8f + (index % 2) * 0.95f);
                float sway = Mathf.Sin(Time.realtimeSinceStartup * (0.75f + index * 0.09f)) * 0.06f;
                trunk.transform.localPosition = new Vector3(x + sway, 0.95f, z);
                trunk.transform.localScale = new Vector3(0.18f + (index % 3) * 0.03f, 1.9f + lane * 0.7f, 0.18f + (index % 2) * 0.02f);
                canopy.transform.localPosition = trunk.transform.localPosition + new Vector3(0f, 1.1f + lane * 0.32f, 0f);
                canopy.transform.localScale = new Vector3(0.82f + lane * 0.26f, 0.62f + lane * 0.14f, 0.82f + lane * 0.26f);
                Color trunkColor = Color.Lerp(new Color(0.06f, 0.07f, 0.06f), new Color(0.16f, 0.18f, 0.14f), fade01);
                Color canopyColor = Color.Lerp(new Color(0.04f, 0.06f, 0.05f), new Color(0.20f, 0.34f, 0.24f), fade01);
                trunk.material.color = trunkColor;
                canopy.material.color = canopyColor;
                ApplyEmission(canopy.material, canopyColor * 0.08f);
            }

            for (int index = 0; index < _motes.Length; index++)
            {
                Renderer mote = _motes[index];
                if (mote == null)
                {
                    continue;
                }

                float seed = index * 0.71f;
                float time = Time.realtimeSinceStartup * (0.55f + index * 0.03f);
                mote.transform.localPosition = new Vector3(
                    Mathf.Sin(time + seed) * 2.4f,
                    0.55f + Mathf.Abs(Mathf.Cos(time * 1.4f + seed)) * 1.25f,
                    Mathf.Cos(time * 0.9f + seed) * 1.8f);
                mote.transform.localScale = Vector3.one * (0.035f + (index % 3) * 0.012f);
                Color moteColor = Color.Lerp(new Color(0.18f, 0.28f, 0.22f), new Color(0.46f, 0.78f, 0.56f), fade01);
                mote.material.color = moteColor;
                ApplyEmission(mote.material, moteColor * (0.15f + fade01 * 0.25f));
            }
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _root = new GameObject("ObserverForestViewRoot").transform;
            _root.SetParent(transform, false);

            GameObject fogObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fogObject.name = "ForestFog";
            fogObject.transform.SetParent(_root, false);
            Collider fogCollider = fogObject.GetComponent<Collider>();
            if (fogCollider != null)
            {
                Destroy(fogCollider);
            }

            _fogRenderer = fogObject.GetComponent<Renderer>();
            _fogRenderer.material = CreateMaterial(new Color(0.18f, 0.28f, 0.24f));

            _trunks = new Renderer[8];
            _canopies = new Renderer[8];
            for (int index = 0; index < _trunks.Length; index++)
            {
                _trunks[index] = CreatePart($"TreeTrunk_{index:00}", PrimitiveType.Cylinder, Vector3.zero, Vector3.one, new Color(0.16f, 0.18f, 0.14f));
                _canopies[index] = CreatePart($"TreeCanopy_{index:00}", PrimitiveType.Sphere, Vector3.zero, Vector3.one, new Color(0.20f, 0.34f, 0.24f));
            }

            _motes = new Renderer[9];
            for (int index = 0; index < _motes.Length; index++)
            {
                _motes[index] = CreatePart($"ForestMote_{index:00}", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.05f, new Color(0.46f, 0.78f, 0.56f));
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
