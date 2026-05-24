using BellRinger.FinalDemo;
using System.IO;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverPadView : MonoBehaviour
    {
        [SerializeField] private float positionLerp = 16f;
        [SerializeField] private float rotationLerp = 18f;
        [SerializeField] private float maxShakeDegrees = 5.4f;
        [SerializeField] private float linkWidth = 0.028f;

        private Transform _modelRoot;
        private Transform _topSurface;
        private LineRenderer _linkRenderer;
        private TintedRenderer[] _tintedRenderers;
        private Renderer _sourcedCardRenderer;
        private bool _usingSourcedCard;
        private bool _initialized;
        private bool _hasKnownPose;
        private Vector3 _lastKnownPosition;
        private Quaternion _lastKnownRotation = Quaternion.identity;

        private struct TintedRenderer
        {
            public Renderer renderer;
            public Color baseColor;
        }

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoInputStatus inputStatus, bool hasBell, Vector3 bellWorldPosition)
        {
            EnsureVisuals();

            Vector3 targetPosition = _modelRoot.position;
            Quaternion targetRotation = _modelRoot.rotation;
            float visible01 = 0.34f;

            if (snapshot.hasPadPose)
            {
                targetPosition = snapshot.padWorldPosition;
                targetRotation = snapshot.padWorldRotation;
                visible01 = 1f;
                _lastKnownPosition = targetPosition;
                _lastKnownRotation = targetRotation;
                _hasKnownPose = true;
            }
            else if (_hasKnownPose)
            {
                targetPosition = _lastKnownPosition;
                targetRotation = _lastKnownRotation;
                visible01 = 0.45f;
            }
            else
            {
                targetPosition = snapshot.playerWorldPosition + new Vector3(0.28f, -0.18f, 0.55f);
                targetRotation = Quaternion.Euler(18f, -24f, -10f);
            }

            float motion01 = inputStatus != null && inputStatus.PadImuReceiver != null
                ? Mathf.Clamp01(inputStatus.PadImuReceiver.MotionIntensity01)
                : 0f;
            float jitterDegrees = maxShakeDegrees * motion01;
            float time = Time.realtimeSinceStartup * (11f + motion01 * 17f);
            Vector3 jitterEuler = new Vector3(
                (Mathf.PerlinNoise(time, 0.17f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0.37f, time) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time, time * 0.41f) - 0.5f) * 2f) * jitterDegrees;
            targetRotation *= Quaternion.Euler(jitterEuler);

            float positionLerp01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionLerp) * Time.unscaledDeltaTime);
            float rotationLerp01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationLerp) * Time.unscaledDeltaTime);
            _modelRoot.position = Vector3.Lerp(_modelRoot.position, targetPosition, positionLerp01);
            _modelRoot.rotation = Quaternion.Slerp(_modelRoot.rotation, targetRotation, rotationLerp01);

            ApplyTint(visible01);
            UpdateLink(snapshot, hasBell, bellWorldPosition, motion01, visible01);
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _modelRoot = new GameObject("ObserverPadModel").transform;
            _modelRoot.SetParent(transform, false);

            Material shellMaterial = CreateStandardMaterial(new Color(0.10f, 0.11f, 0.12f));
            Material trimMaterial = CreateStandardMaterial(new Color(0.80f, 0.64f, 0.19f));
            Material markerMaterial = CreateStandardMaterial(new Color(0.88f, 0.90f, 0.92f));
            Material buttonXMaterial = CreateStandardMaterial(new Color(0.13f, 0.55f, 0.93f));
            Material buttonYMaterial = CreateStandardMaterial(new Color(0.94f, 0.83f, 0.18f));
            Material buttonAMaterial = CreateStandardMaterial(new Color(0.22f, 0.78f, 0.34f));
            Material buttonBMaterial = CreateStandardMaterial(new Color(0.86f, 0.22f, 0.22f));
            Material controlMaterial = CreateStandardMaterial(new Color(0.22f, 0.23f, 0.24f));

            TintedRenderer[] tinted = new TintedRenderer[18];
            int tintedCount = 0;

            tinted[tintedCount++] = CreatePart("BodyCore", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.042f, 0.18f), shellMaterial);
            tinted[tintedCount++] = CreatePart("LeftGrip", PrimitiveType.Sphere, new Vector3(-0.16f, -0.008f, -0.01f), new Vector3(0.20f, 0.05f, 0.18f), shellMaterial);
            tinted[tintedCount++] = CreatePart("RightGrip", PrimitiveType.Sphere, new Vector3(0.16f, -0.008f, -0.01f), new Vector3(0.20f, 0.05f, 0.18f), shellMaterial);
            tinted[tintedCount++] = CreatePart("LeftShoulder", PrimitiveType.Cube, new Vector3(-0.11f, 0.014f, 0.103f), new Vector3(0.09f, 0.018f, 0.04f), controlMaterial);
            tinted[tintedCount++] = CreatePart("RightShoulder", PrimitiveType.Cube, new Vector3(0.11f, 0.014f, 0.103f), new Vector3(0.09f, 0.018f, 0.04f), controlMaterial);
            tinted[tintedCount++] = CreatePart("LeftStickBase", PrimitiveType.Cylinder, new Vector3(-0.11f, 0.021f, 0.05f), new Vector3(0.045f, 0.006f, 0.045f), controlMaterial);
            tinted[tintedCount++] = CreatePart("LeftStick", PrimitiveType.Sphere, new Vector3(-0.11f, 0.042f, 0.05f), new Vector3(0.055f, 0.028f, 0.055f), controlMaterial);
            tinted[tintedCount++] = CreatePart("RightStickBase", PrimitiveType.Cylinder, new Vector3(0.098f, 0.021f, -0.018f), new Vector3(0.045f, 0.006f, 0.045f), controlMaterial);
            tinted[tintedCount++] = CreatePart("RightStick", PrimitiveType.Sphere, new Vector3(0.098f, 0.042f, -0.018f), new Vector3(0.055f, 0.028f, 0.055f), controlMaterial);
            tinted[tintedCount++] = CreatePart("DPadH", PrimitiveType.Cube, new Vector3(-0.118f, 0.031f, -0.042f), new Vector3(0.078f, 0.012f, 0.024f), controlMaterial);
            tinted[tintedCount++] = CreatePart("DPadV", PrimitiveType.Cube, new Vector3(-0.118f, 0.031f, -0.042f), new Vector3(0.024f, 0.012f, 0.078f), controlMaterial);
            tinted[tintedCount++] = CreatePart("CenterLeft", PrimitiveType.Sphere, new Vector3(-0.032f, 0.028f, 0.002f), new Vector3(0.019f, 0.011f, 0.019f), controlMaterial);
            tinted[tintedCount++] = CreatePart("CenterRight", PrimitiveType.Sphere, new Vector3(0.032f, 0.028f, 0.002f), new Vector3(0.019f, 0.011f, 0.019f), controlMaterial);
            tinted[tintedCount++] = CreatePart("MarkerPlate", PrimitiveType.Cube, new Vector3(0f, 0.027f, 0.098f), new Vector3(0.10f, 0.004f, 0.10f), trimMaterial);
            tinted[tintedCount++] = CreatePart("MarkerInset", PrimitiveType.Cube, new Vector3(0f, 0.031f, 0.098f), new Vector3(0.078f, 0.003f, 0.078f), markerMaterial);
            tinted[tintedCount++] = CreatePart("ButtonX", PrimitiveType.Sphere, new Vector3(0.165f, 0.03f, 0.046f), new Vector3(0.025f, 0.012f, 0.025f), buttonXMaterial);
            tinted[tintedCount++] = CreatePart("ButtonY", PrimitiveType.Sphere, new Vector3(0.187f, 0.03f, 0.069f), new Vector3(0.025f, 0.012f, 0.025f), buttonYMaterial);
            tinted[tintedCount++] = CreatePart("ButtonA", PrimitiveType.Sphere, new Vector3(0.187f, 0.03f, 0.023f), new Vector3(0.025f, 0.012f, 0.025f), buttonAMaterial);
            tinted[tintedCount++] = CreatePart("ButtonB", PrimitiveType.Sphere, new Vector3(0.209f, 0.03f, 0.046f), new Vector3(0.025f, 0.012f, 0.025f), buttonBMaterial);

            _tintedRenderers = new TintedRenderer[tintedCount];
            for (int index = 0; index < tintedCount; ++index)
            {
                _tintedRenderers[index] = tinted[index];
            }

            _topSurface = new GameObject("TopSurface").transform;
            _topSurface.SetParent(_modelRoot, false);
            _topSurface.localPosition = new Vector3(0f, 0.036f, 0.02f);

            Texture2D sourcedTexture = ObserverSourceTextureLoader.LoadColorKeyedJpeg("ThirdParty/ObserverSource/PolyPizza/controller-poly-pizza.jpg", 0.14f);
            if (sourcedTexture != null)
            {
                _usingSourcedCard = true;
                _sourcedCardRenderer = CreateSourcedCard("ControllerCard", sourcedTexture, new Vector3(0f, 0.045f, 0f), new Vector3(0.52f, 0.30f, 1f));
            }

            GameObject linkObject = new GameObject("BellLink");
            linkObject.transform.SetParent(_modelRoot, false);
            _linkRenderer = linkObject.AddComponent<LineRenderer>();
            _linkRenderer.material = CreateLineMaterial();
            _linkRenderer.textureMode = LineTextureMode.Stretch;
            _linkRenderer.alignment = LineAlignment.View;
            _linkRenderer.positionCount = 2;
            _linkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _linkRenderer.receiveShadows = false;
            _linkRenderer.enabled = false;
        }

        private TintedRenderer CreatePart(string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(_modelRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = material;
            return new TintedRenderer
            {
                renderer = renderer,
                baseColor = material.color,
            };
        }

        private void ApplyTint(float visible01)
        {
            if (_usingSourcedCard && _sourcedCardRenderer != null)
            {
                _sourcedCardRenderer.enabled = visible01 > 0.02f;
                Color tint = Color.Lerp(Color.black, Color.white, Mathf.Lerp(0.38f, 1f, Mathf.Clamp01(visible01)));
                _sourcedCardRenderer.material.color = tint;
            }

            if (_tintedRenderers == null)
            {
                return;
            }

            float brightness = Mathf.Lerp(0.28f, 1f, Mathf.Clamp01(visible01));
            for (int index = 0; index < _tintedRenderers.Length; ++index)
            {
                Renderer renderer = _tintedRenderers[index].renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = !_usingSourcedCard && visible01 > 0.02f;
                renderer.material.color = Color.Lerp(Color.black, _tintedRenderers[index].baseColor, brightness);
            }
        }

        private void UpdateLink(ObserverDisplaySnapshot snapshot, bool hasBell, Vector3 bellWorldPosition, float motion01, float visible01)
        {
            if (_linkRenderer == null)
            {
                return;
            }

            bool bellStage = snapshot.activeSoundFocusLabel == "LIVING BELL" ||
                             snapshot.stage == FinalDemoStage.BellAcquisition ||
                             snapshot.stage == FinalDemoStage.ForestEnding;
            if (!hasBell || !bellStage || visible01 <= 0.02f)
            {
                _linkRenderer.enabled = false;
                return;
            }

            float alpha = Mathf.Clamp01(Mathf.Lerp(0.10f, 0.78f, motion01));
            _linkRenderer.enabled = alpha > 0.01f;
            if (!_linkRenderer.enabled)
            {
                return;
            }

            _linkRenderer.SetPosition(0, _topSurface.position);
            _linkRenderer.SetPosition(1, bellWorldPosition + new Vector3(0f, 0.02f, 0f));
            _linkRenderer.startWidth = Mathf.Lerp(0.004f, linkWidth, alpha);
            _linkRenderer.endWidth = Mathf.Lerp(0.003f, linkWidth * 0.75f, alpha);
            _linkRenderer.startColor = new Color(0.95f, 0.78f, 0.20f, alpha);
            _linkRenderer.endColor = new Color(0.22f, 0.92f, 0.46f, alpha * 0.85f);
        }

        private static Material CreateStandardMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Simple Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            material.SetFloat("_Surface", 0f);
            return material;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            return new Material(shader);
        }

        private Renderer CreateSourcedCard(string objectName, Texture2D texture, Vector3 localPosition, Vector3 localScale)
        {
            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            card.name = objectName;
            card.transform.SetParent(_modelRoot, false);
            card.transform.localPosition = localPosition;
            card.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            card.transform.localScale = localScale;
            Collider collider = card.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Material material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Unlit/Texture"));
            material.mainTexture = texture;
            material.color = Color.white;
            Renderer renderer = card.GetComponent<Renderer>();
            renderer.material = material;
            return renderer;
        }
    }
}
