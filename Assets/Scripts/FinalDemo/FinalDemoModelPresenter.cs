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
        [SerializeField] private Vector3 bellModelLocalEuler = new Vector3(-180f, 0f, 0f);
        [SerializeField] private float bellTargetMaxSize = 0.56f;

        [Header("모델 / Pad")]
        [SerializeField] private Vector3 padModelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 padModelLocalEuler = new Vector3(0f, 270f, 0f);
        [SerializeField] private float padTargetMaxSize = 0.72f;
        [SerializeField] private Texture2D padAlbedoTexture;
        [SerializeField] private string padTextureResourcePath = "FinalDemoModels/Pad/5";
        [SerializeField] private bool applyPadTextureMaterial = true;
        [SerializeField] private bool applyWhitePadMaterial = false;
        [SerializeField] private Color padWhite = Color.white;
        [SerializeField] private bool showSinglePadModel = true;
        [SerializeField] private int padVisibleModelIndex = 0;

        [Header("모델 / Visibility")]
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool hidePlaceholderRenderers = true;
        [SerializeField] private bool centerModelBoundsOnLocalPosition = true;

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
            CenterModelBoundsOnLocalPosition(bellModel, bellModelLocalPosition);
            SetPlaceholderRenderersVisible(bellVisual, bellModel, !hidePlaceholderRenderers);

            Transform padModel = EnsureModelChild(padVisual, padModelPrefab, PadModelChildName, padModelLocalPosition, padModelLocalEuler);
            if (showSinglePadModel)
            {
                ShowSinglePadModel(padModel, padVisibleModelIndex);
            }

            FitModelToMaxSize(padModel, padTargetMaxSize);
            CenterModelBoundsOnLocalPosition(padModel, padModelLocalPosition);
            if (applyPadTextureMaterial || applyWhitePadMaterial)
            {
                ApplyPadTextureMaterial(padModel);
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

            if (padAlbedoTexture == null && !string.IsNullOrWhiteSpace(padTextureResourcePath))
            {
                padAlbedoTexture = Resources.Load<Texture2D>(padTextureResourcePath);
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

            if (!TryGetRenderableBounds(modelRoot, out Bounds bounds))
            {
                return;
            }

            float currentMax = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (currentMax <= 0.001f)
            {
                return;
            }

            modelRoot.localScale *= targetMaxSize / currentMax;
        }

        private void CenterModelBoundsOnLocalPosition(Transform modelRoot, Vector3 targetLocalPosition)
        {
            if (!centerModelBoundsOnLocalPosition || modelRoot == null || modelRoot.parent == null)
            {
                return;
            }

            if (!TryGetRenderableBounds(modelRoot, out Bounds bounds))
            {
                return;
            }

            Vector3 targetWorldCenter = modelRoot.parent.TransformPoint(targetLocalPosition);
            modelRoot.position += targetWorldCenter - bounds.center;
        }

        private static bool TryGetRenderableBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
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

        private void ApplyPadTextureMaterial(Transform modelRoot)
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
            if (padAlbedoTexture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", padAlbedoTexture);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", padAlbedoTexture);
                }
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int index = 0; index < sharedMaterials.Length; index++)
                {
                    sharedMaterials[index] = material;
                }

                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static void ShowSinglePadModel(Transform modelRoot, int visibleModelIndex)
        {
            Transform groupingRoot = FindPadGroupingRoot(modelRoot);
            if (groupingRoot == null)
            {
                return;
            }

            Transform[] candidates = GetDirectRenderableChildren(groupingRoot);
            if (candidates.Length != 2 || !HaveSimilarRenderableFootprint(candidates[0], candidates[1]))
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(visibleModelIndex, 0, candidates.Length - 1);
            for (int index = 0; index < candidates.Length; index++)
            {
                candidates[index].gameObject.SetActive(index == clampedIndex);
            }
        }

        private static Transform FindPadGroupingRoot(Transform modelRoot)
        {
            Transform groupingRoot = modelRoot;
            while (groupingRoot != null && groupingRoot.childCount == 1 && groupingRoot.GetComponent<Renderer>() == null)
            {
                groupingRoot = groupingRoot.GetChild(0);
            }

            return groupingRoot;
        }

        private static Transform[] GetDirectRenderableChildren(Transform root)
        {
            if (root == null)
            {
                return System.Array.Empty<Transform>();
            }

            Transform[] scratch = new Transform[root.childCount];
            int count = 0;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    continue;
                }

                scratch[count] = child;
                count++;
            }

            if (count == scratch.Length)
            {
                return scratch;
            }

            System.Array.Resize(ref scratch, count);
            return scratch;
        }

        private static bool HaveSimilarRenderableFootprint(Transform first, Transform second)
        {
            if (!TryGetAnyRendererBounds(first, out Bounds firstBounds) ||
                !TryGetAnyRendererBounds(second, out Bounds secondBounds))
            {
                return false;
            }

            float firstMax = Mathf.Max(firstBounds.size.x, firstBounds.size.y, firstBounds.size.z);
            float secondMax = Mathf.Max(secondBounds.size.x, secondBounds.size.y, secondBounds.size.z);
            if (firstMax <= 0.001f || secondMax <= 0.001f)
            {
                return false;
            }

            float ratio = Mathf.Min(firstMax, secondMax) / Mathf.Max(firstMax, secondMax);
            return ratio >= 0.75f;
        }

        private static bool TryGetAnyRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }
    }
}
