using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoModelPresenter : MonoBehaviour
    {
        [Header("모델 / Targets")]
        [SerializeField] private Transform bellVisual;
        [SerializeField] private Transform padVisual;

        [Header("모델 / Prefabs")]
        [SerializeField] private GameObject bellModelPrefab;
        [SerializeField] private GameObject padModelPrefab;
        [SerializeField] private string bellModelResourcePath = "FinalDemoModels/Bell/BellModel";
        [SerializeField] private string padModelResourcePath = "FinalDemoModels/Pad/PadGamepad";

        [Header("모델 / Bell")]
        [SerializeField] private Vector3 bellModelLocalPosition = new Vector3(0f, -0.04f, 0f);
        [SerializeField] private Vector3 bellModelLocalEuler = new Vector3(-90f, 0f, 0f);
        [SerializeField] private float bellTargetMaxSize = 0.56f;

        [Header("모델 / Pad")]
        [SerializeField] private Vector3 padModelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 padModelLocalEuler = new Vector3(0f, 180f, 0f);
        [SerializeField] private float padTargetMaxSize = 0.72f;
        [SerializeField] private bool applyWhitePadMaterial = true;
        [SerializeField] private Color padWhite = new Color(0.92f, 0.94f, 0.9f);

        [Header("모델 / Visibility")]
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool hidePlaceholderRenderers = true;

        private const string BellModelChildName = "FinalDemo_ImportedBellModel";
        private const string PadModelChildName = "FinalDemo_ImportedPadModel";

        public void ConfigureTargets(Transform bell, Transform pad)
        {
            bellVisual = bell;
            padVisual = pad;
        }

        private void Awake()
        {
            if (Application.isPlaying && applyOnAwake)
            {
                ApplyModelVisuals();
            }
        }

        public void ApplyModelVisuals()
        {
            ResolveResourceFallbacks();
            Transform bellModel = EnsureModelChild(bellVisual, bellModelPrefab, BellModelChildName, bellModelLocalPosition, bellModelLocalEuler);
            FitModelToMaxSize(bellModel, bellTargetMaxSize);
            SetPlaceholderRenderersVisible(bellVisual, bellModel, !hidePlaceholderRenderers);

            Transform padModel = EnsureModelChild(padVisual, padModelPrefab, PadModelChildName, padModelLocalPosition, padModelLocalEuler);
            FitModelToMaxSize(padModel, padTargetMaxSize);
            if (applyWhitePadMaterial)
            {
                ApplyWhiteMaterial(padModel);
            }

            SetPlaceholderRenderersVisible(padVisual, padModel, !hidePlaceholderRenderers);
        }

        private void ResolveResourceFallbacks()
        {
            if (bellModelPrefab == null && !string.IsNullOrWhiteSpace(bellModelResourcePath))
            {
                bellModelPrefab = Resources.Load<GameObject>(bellModelResourcePath);
            }

            if (padModelPrefab == null && !string.IsNullOrWhiteSpace(padModelResourcePath))
            {
                padModelPrefab = Resources.Load<GameObject>(padModelResourcePath);
            }
        }

        private static Transform EnsureModelChild(Transform root, GameObject prefab, string childName, Vector3 localPosition, Vector3 localEuler)
        {
            if (root == null || prefab == null)
            {
                return null;
            }

            Transform child = root.Find(childName);
            if (child == null)
            {
                GameObject instance = InstantiateModel(prefab, root);
                instance.name = childName;
                child = instance.transform;
            }

            child.SetParent(root, false);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.Euler(localEuler);
            child.localScale = Vector3.one;
            return child;
        }

        private static GameObject InstantiateModel(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject editorInstance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (editorInstance != null)
                {
                    return editorInstance;
                }
            }
#endif
            return Instantiate(prefab, parent);
        }

        private static void FitModelToMaxSize(Transform modelRoot, float targetMaxSize)
        {
            if (modelRoot == null || targetMaxSize <= 0.001f)
            {
                return;
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float currentMax = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (currentMax <= 0.001f)
            {
                return;
            }

            modelRoot.localScale *= targetMaxSize / currentMax;
        }

        private static void SetPlaceholderRenderersVisible(Transform root, Transform modelRoot, bool visible)
        {
            if (root == null || modelRoot == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.transform == modelRoot || renderer.transform.IsChildOf(modelRoot))
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }

        private void ApplyWhiteMaterial(Transform modelRoot)
        {
            if (modelRoot == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = padWhite,
            };

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
