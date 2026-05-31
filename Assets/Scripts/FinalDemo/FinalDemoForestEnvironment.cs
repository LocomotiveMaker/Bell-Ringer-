using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoForestEnvironment : MonoBehaviour
    {
        private static readonly string[] TreeResourcePaths =
        {
            "ObserverAssets/Forest/Models/Pine_1",
            "ObserverAssets/Forest/Models/Pine_2",
            "ObserverAssets/Forest/Models/Pine_4",
            "ObserverAssets/Forest/Models/CommonTree_1",
            "ObserverAssets/Forest/Models/CommonTree_3",
            "ObserverAssets/Forest/Models/TwistedTree_2",
        };

        private static readonly string[] GroundResourcePaths =
        {
            "ObserverAssets/Forest/Models/Rock_Medium_1",
            "ObserverAssets/Forest/Models/Rock_Medium_2",
            "ObserverAssets/Forest/Models/Bush_Common",
            "ObserverAssets/Forest/Models/Grass_Common_Tall",
        };

        [SerializeField] private bool useResourceModelsWhenAvailable;
        [SerializeField] private float treeScale = 0.62f;
        [SerializeField] private float sideDistance = 4.6f;
        [SerializeField] private float forwardSpan = 8.2f;

        private Transform[] _treeInstances;
        private Transform[] _groundInstances;
        private ParticleSystem _motes;
        private Renderer _floorRenderer;
        private Material _leafMaterial;
        private Material _redLeafMaterial;
        private Material _trunkMaterial;
        private Material _groundMaterial;
        private Material _moteMaterial;
        private bool _initialized;

        public void Apply(Vector3 center, float fade01, bool visible)
        {
            EnsureInitialized();

            float fade = visible ? Mathf.Clamp01(fade01) : 0f;
            gameObject.SetActive(visible || fade > 0.001f);
            if (!gameObject.activeSelf)
            {
                return;
            }

            transform.position = new Vector3(center.x, 0f, center.z);
            UpdateFloor(fade);
            PositionTrees(fade);
            PositionGroundClutter(fade);
            ConfigureMotes(fade);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _leafMaterial = CreateSurfaceMaterial(new Color(0.14f, 0.30f, 0.08f));
            _redLeafMaterial = CreateSurfaceMaterial(new Color(0.32f, 0.10f, 0.08f));
            _trunkMaterial = CreateSurfaceMaterial(new Color(0.24f, 0.16f, 0.10f));
            _groundMaterial = CreateSurfaceMaterial(new Color(0.16f, 0.20f, 0.15f));
            _moteMaterial = CreateParticleMaterial();
            _floorRenderer = CreateFloor();
            _treeInstances = new Transform[TreeResourcePaths.Length];
            for (int index = 0; index < _treeInstances.Length; index++)
            {
                _treeInstances[index] = CreateResourceInstance(TreeResourcePaths[index], $"FinalDemoForestTree_{index:00}");
            }

            _groundInstances = new Transform[GroundResourcePaths.Length];
            for (int index = 0; index < _groundInstances.Length; index++)
            {
                _groundInstances[index] = CreateResourceInstance(GroundResourcePaths[index], $"FinalDemoForestGround_{index:00}");
            }

            _motes = CreateMoteParticles();
        }

        private Renderer CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FinalDemoForestFloorGlow";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.02f, 1.2f);
            floor.transform.localScale = new Vector3(18f, 0.035f, 16f);
            Collider collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = floor.GetComponent<Renderer>();
            renderer.material = CreateSurfaceMaterial(new Color(0.08f, 0.12f, 0.09f));
            return renderer;
        }

        private void UpdateFloor(float fade)
        {
            if (_floorRenderer == null)
            {
                return;
            }

            Color color = Color.Lerp(new Color(0.01f, 0.015f, 0.012f), new Color(0.16f, 0.22f, 0.16f), fade);
            _floorRenderer.material.color = color;
            if (_floorRenderer.material.HasProperty("_EmissionColor"))
            {
                _floorRenderer.material.SetColor("_EmissionColor", color * 0.08f * fade);
            }
        }

        private void PositionTrees(float fade)
        {
            for (int index = 0; index < _treeInstances.Length; index++)
            {
                Transform instance = _treeInstances[index];
                if (instance == null)
                {
                    continue;
                }

                float side = index < _treeInstances.Length / 2 ? -1f : 1f;
                float lane01 = (index % (_treeInstances.Length / 2)) / Mathf.Max(1f, (_treeInstances.Length / 2) - 1f);
                float depth = Mathf.Lerp(-0.8f, forwardSpan, lane01);
                float x = side * Mathf.Lerp(2.2f, sideDistance, lane01);
                instance.localPosition = new Vector3(x, 0f, depth);
                instance.localRotation = Quaternion.Euler(0f, index * 43f, 0f);
                instance.localScale = Vector3.one * treeScale * Mathf.Lerp(0.55f, 0.92f, lane01) * Mathf.Lerp(0.92f, 1f, fade);
                SetRendererFade(instance, fade);
            }
        }

        private void PositionGroundClutter(float fade)
        {
            Vector3[] positions =
            {
                new Vector3(-1.7f, 0f, 1.4f),
                new Vector3(1.9f, 0f, 2.4f),
                new Vector3(-0.7f, 0f, 3.8f),
                new Vector3(0.9f, 0f, 0.9f),
            };

            for (int index = 0; index < _groundInstances.Length; index++)
            {
                Transform instance = _groundInstances[index];
                if (instance == null)
                {
                    continue;
                }

                instance.localPosition = positions[Mathf.Clamp(index, 0, positions.Length - 1)];
                instance.localRotation = Quaternion.Euler(0f, index * 57f, 0f);
                instance.localScale = Vector3.one * Mathf.Lerp(0.34f, 0.58f, fade);
                SetRendererFade(instance, fade);
            }
        }

        private Transform CreateResourceInstance(string resourcePath, string objectName)
        {
            GameObject prefab = useResourceModelsWhenAvailable ? Resources.Load<GameObject>(resourcePath) : null;
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, transform);
                instance.name = objectName;
                instance.transform.SetParent(transform, false);
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                return instance.transform;
            }

            instance = objectName.Contains("Tree")
                ? CreateProceduralTree(objectName)
                : CreateProceduralGroundObject(objectName);
            return instance.transform;
        }

        private GameObject CreateProceduralTree(string objectName)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(transform, false);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            trunk.transform.localScale = new Vector3(0.22f, 0.75f, 0.22f);
            AssignMaterialAndRemoveCollider(trunk, _trunkMaterial);

            GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lower.name = "LeafMass_Lower";
            lower.transform.SetParent(root.transform, false);
            lower.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            lower.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
            AssignMaterialAndRemoveCollider(lower, objectName.GetHashCode() % 3 == 0 ? _redLeafMaterial : _leafMaterial);

            GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            upper.name = "LeafMass_Upper";
            upper.transform.SetParent(root.transform, false);
            upper.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            upper.transform.localScale = new Vector3(0.82f, 0.52f, 0.82f);
            AssignMaterialAndRemoveCollider(upper, objectName.GetHashCode() % 3 == 0 ? _redLeafMaterial : _leafMaterial);

            return root;
        }

        private GameObject CreateProceduralGroundObject(string objectName)
        {
            GameObject root = GameObject.CreatePrimitive(objectName.Contains("Grass") || objectName.Contains("Bush") ? PrimitiveType.Sphere : PrimitiveType.Cube);
            root.name = objectName;
            root.transform.SetParent(transform, false);
            root.transform.localScale = objectName.Contains("Rock")
                ? new Vector3(0.72f, 0.18f, 0.48f)
                : new Vector3(0.64f, 0.24f, 0.48f);
            AssignMaterialAndRemoveCollider(root, objectName.Contains("Rock") ? _groundMaterial : _leafMaterial);
            return root;
        }

        private ParticleSystem CreateMoteParticles()
        {
            GameObject motes = new GameObject("FinalDemoForestMotes");
            motes.transform.SetParent(transform, false);
            motes.transform.localPosition = new Vector3(0f, 1.8f, 2.5f);
            ParticleSystem particles = motes.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.maxParticles = 150;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 2.5f, 7f);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = _moteMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private void ConfigureMotes(float fade)
        {
            if (_motes == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _motes.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.92f, 0.72f, 0.10f + fade * 0.26f));

            ParticleSystem.EmissionModule emission = _motes.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 18f, fade);

            if (fade > 0.01f && !_motes.isPlaying)
            {
                _motes.Play(true);
            }
            else if (fade <= 0.01f && _motes.isPlaying)
            {
                _motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static void SetRendererFade(Transform root, float fade)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = fade > 0.01f;
            }
        }

        private static Material CreateSurfaceMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = "FinalDemo Forest Surface",
                color = color,
            };
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.38f);
            }

            return material;
        }

        private static Material CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
                            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                name = "FinalDemo Forest Mote",
                mainTexture = Texture2D.whiteTexture,
                color = new Color(0.7f, 0.25f, 1f, 0.35f),
            };
            material.renderQueue = 3000;
            return material;
        }

        private static void AssignMaterialAndRemoveCollider(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
