using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverForestView : MonoBehaviour
    {
        private static readonly string[] TreeResourcePaths =
        {
            "ObserverAssets/Forest/Models/Pine_1",
            "ObserverAssets/Forest/Models/Pine_2",
            "ObserverAssets/Forest/Models/Pine_4",
            "ObserverAssets/Forest/Models/CommonTree_1",
            "ObserverAssets/Forest/Models/CommonTree_3",
            "ObserverAssets/Forest/Models/DeadTree_1",
            "ObserverAssets/Forest/Models/DeadTree_4",
            "ObserverAssets/Forest/Models/TwistedTree_2",
        };

        private static readonly string[] GroundResourcePaths =
        {
            "ObserverAssets/Forest/Models/Rock_Medium_1",
            "ObserverAssets/Forest/Models/Rock_Medium_2",
            "ObserverAssets/Forest/Models/Bush_Common",
            "ObserverAssets/Forest/Models/Grass_Common_Tall",
        };

        private Transform _root;
        private Transform[] _treeInstances;
        private Transform[] _groundInstances;
        private Renderer _fogRenderer;
        private ParticleSystem _motes;
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
            float fade01 = Mathf.Clamp01((Time.realtimeSinceStartup - _visibleStartedAtRealtime) / 2.8f);
            float time = Time.realtimeSinceStartup;
            _root.position = new Vector3(bellPosition.x, 0f, bellPosition.z + 1.1f);

            if (_fogRenderer != null)
            {
                _fogRenderer.transform.localScale = new Vector3(12f, 0.08f, 8.5f);
                _fogRenderer.material.color = Color.Lerp(new Color(0.02f, 0.03f, 0.03f), new Color(0.18f, 0.24f, 0.20f), fade01);
                ApplyEmission(_fogRenderer.material, new Color(0.08f, 0.12f, 0.10f) * fade01 * 0.12f);
            }

            PositionTrees(time, fade01);
            PositionGroundClutter(time, fade01);
            ConfigureMotes(fade01);
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
            _fogRenderer.material = CreateSurfaceMaterial(new Color(0.16f, 0.22f, 0.18f));

            _treeInstances = new Transform[TreeResourcePaths.Length];
            for (int index = 0; index < _treeInstances.Length; index++)
            {
                _treeInstances[index] = CreateResourceInstance(TreeResourcePaths[index], $"ForestTree_{index:00}", Vector3.one);
            }

            _groundInstances = new Transform[GroundResourcePaths.Length];
            for (int index = 0; index < _groundInstances.Length; index++)
            {
                _groundInstances[index] = CreateResourceInstance(GroundResourcePaths[index], $"ForestGround_{index:00}", Vector3.one);
            }

            _motes = CreateMoteParticles();
        }

        private void PositionTrees(float time, float fade01)
        {
            if (_treeInstances == null)
            {
                return;
            }

            for (int index = 0; index < _treeInstances.Length; index++)
            {
                Transform instance = _treeInstances[index];
                if (instance == null)
                {
                    continue;
                }

                float side = index < _treeInstances.Length / 2 ? -1f : 1f;
                float lane01 = (index % (_treeInstances.Length / 2)) / Mathf.Max(1f, (_treeInstances.Length / 2) - 1f);
                float x = side * Mathf.Lerp(2.2f, 4.5f, lane01);
                float z = Mathf.Lerp(-0.9f, 4.8f, lane01);
                float sway = Mathf.Sin(time * (0.55f + index * 0.07f)) * 0.08f;
                instance.localPosition = new Vector3(x + sway, 0f, z);
                instance.localRotation = Quaternion.Euler(0f, (index * 37f) + Mathf.Sin(time * 0.4f + index) * 6f, 0f);
                float scale = Mathf.Lerp(0.22f, 0.42f, lane01) * Mathf.Lerp(0.88f, 1f, fade01);
                instance.localScale = Vector3.one * scale;
                SetRenderersVisible(instance, fade01, ResolveTreeColor(TreeResourcePaths[index], index));
            }
        }

        private void PositionGroundClutter(float time, float fade01)
        {
            if (_groundInstances == null)
            {
                return;
            }

            Vector3[] positions =
            {
                new Vector3(-1.3f, 0f, 1.3f),
                new Vector3(1.55f, 0f, 2.1f),
                new Vector3(-0.55f, 0f, 3.2f),
                new Vector3(0.62f, 0f, 0.95f),
            };

            for (int index = 0; index < _groundInstances.Length; index++)
            {
                Transform instance = _groundInstances[index];
                if (instance == null)
                {
                    continue;
                }

                Vector3 position = positions[Mathf.Clamp(index, 0, positions.Length - 1)];
                position.y += Mathf.Sin(time * (0.8f + index * 0.12f)) * 0.02f;
                instance.localPosition = position;
                instance.localRotation = Quaternion.Euler(0f, index * 53f, 0f);
                instance.localScale = Vector3.one * Mathf.Lerp(0.48f, 0.82f, fade01);
                SetRenderersVisible(instance, fade01, ResolveGroundColor(GroundResourcePaths[index], index));
            }
        }

        private void ConfigureMotes(float fade01)
        {
            if (_motes == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _motes.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.52f, 0.78f, 0.58f, 0.08f + fade01 * 0.16f));
            ParticleSystem.EmissionModule emission = _motes.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 18f, fade01);
            _motes.Play(true);
        }

        private Transform CreateResourceInstance(string resourcePath, string objectName, Vector3 fallbackScale)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, _root);
                instance.name = objectName;
                StripColliders(instance);
                return instance.transform;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = objectName + "_Fallback";
            fallback.transform.SetParent(_root, false);
            fallback.transform.localScale = fallbackScale;
            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = fallback.GetComponent<Renderer>();
            renderer.material = CreateSurfaceMaterial(new Color(0.16f, 0.20f, 0.16f));
            return fallback.transform;
        }

        private ParticleSystem CreateMoteParticles()
        {
            Texture2D moteTexture = Resources.Load<Texture2D>("ObserverAssets/Rain/circle_03");
            GameObject moteObject = new GameObject("ForestMotes");
            moteObject.transform.SetParent(_root, false);
            moteObject.transform.localPosition = new Vector3(0f, 1.2f, 1.8f);
            ParticleSystem particleSystem = moteObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = moteObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial(moteTexture);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0.12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6.5f, 2.4f, 4.5f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
            return particleSystem;
        }

        private static void StripColliders(GameObject rootObject)
        {
            Collider[] colliders = rootObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                Destroy(collider);
            }
        }

        private static void SetRenderersVisible(Transform rootTransform, float fade01, Color tint)
        {
            Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = true;
                Color color = Color.Lerp(new Color(0.03f, 0.04f, 0.03f), tint, Mathf.Lerp(0.45f, 1f, fade01));
                color.a = 1f;
                EnsureObserverMaterial(renderer, color);
            }
        }

        private static Color ResolveTreeColor(string objectName, int index)
        {
            string lower = objectName.ToLowerInvariant();
            if (lower.Contains("dead") || lower.Contains("twisted"))
            {
                return new Color(0.35f, 0.30f, 0.23f);
            }

            if (lower.Contains("pine"))
            {
                return index % 2 == 0 ? new Color(0.13f, 0.34f, 0.20f) : new Color(0.18f, 0.42f, 0.24f);
            }

            return new Color(0.22f, 0.45f, 0.24f);
        }

        private static Color ResolveGroundColor(string objectName, int index)
        {
            string lower = objectName.ToLowerInvariant();
            if (lower.Contains("rock"))
            {
                return new Color(0.36f, 0.38f, 0.36f);
            }

            return index % 2 == 0 ? new Color(0.18f, 0.40f, 0.20f) : new Color(0.26f, 0.50f, 0.22f);
        }

        private void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        private static Material CreateSurfaceMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color,
            };
            return material;
        }

        private static void EnsureObserverMaterial(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = CreateSurfaceMaterial(color);
                return;
            }

            bool changed = false;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                bool needsReplacement = material == null ||
                                        material.shader == null ||
                                        (material.shader.name != "Unlit/Color" && material.shader.name != "Sprites/Default");
                if (needsReplacement)
                {
                    materials[index] = CreateSurfaceMaterial(color);
                    changed = true;
                    continue;
                }

                if (material.HasProperty("_Color"))
                {
                    material.color = color;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }

        private static Material CreateParticleMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find("Sprites/Default") ??
                            Shader.Find("Unlit/Transparent") ??
                            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit");
            Material material = new Material(shader)
            {
                color = Color.white,
                mainTexture = texture,
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
