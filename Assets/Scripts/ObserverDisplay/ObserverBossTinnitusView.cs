using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverBossTinnitusView : MonoBehaviour
    {
        private Transform _root;
        private Renderer[] _bodyRenderers;
        private Renderer _weakPointRenderer;
        private LineRenderer[] _crackLines;
        private bool _initialized;
        private FinalDemoStage _lastStage;
        private float _stageEnteredAtRealtime;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director)
        {
            EnsureVisuals();

            bool visible = snapshot.stage == FinalDemoStage.BossApproach ||
                           snapshot.stage == FinalDemoStage.BossPatternOne ||
                           snapshot.stage == FinalDemoStage.BossPatternTwo ||
                           snapshot.stage == FinalDemoStage.BossPatternThree ||
                           snapshot.stage == FinalDemoStage.BossDefeat;
            if (!visible || !snapshot.hasBoss)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            TrackStage(snapshot.stage);

            float time = Time.realtimeSinceStartup;
            float enteredSeconds = time - _stageEnteredAtRealtime;
            float defeat01 = snapshot.stage == FinalDemoStage.BossDefeat ? Mathf.Clamp01(enteredSeconds / 2.4f) : 0f;
            float pulse = 0.55f + Mathf.Sin(time * 2.3f) * 0.08f;
            float match = Mathf.Clamp01(snapshot.bossPatternMatch01);
            _root.position = snapshot.bossWorldPosition;
            _root.rotation = Quaternion.Euler(
                Mathf.Sin(time * 1.8f) * 3f,
                time * (8f + pulse * 6f),
                Mathf.Cos(time * 2.2f) * 2.5f);
            _root.localScale = Vector3.one * Mathf.Lerp(2.45f, 0.72f, defeat01 * 0.82f);

            for (int index = 0; index < _bodyRenderers.Length; index++)
            {
                Renderer body = _bodyRenderers[index];
                if (body == null)
                {
                    continue;
                }

                Color color = Color.Lerp(new Color(0.05f, 0.03f, 0.08f), new Color(0.24f, 0.08f, 0.36f), 0.55f + pulse * 0.35f);
                color = Color.Lerp(color, new Color(0.20f, 0.24f, 0.30f), defeat01 * 0.75f);
                body.material.color = color;
                ApplyEmission(body.material, color * (0.20f + pulse * 0.42f));
            }

            if (_weakPointRenderer != null)
            {
                Vector3 weakLocal = ResolveWeakPointLocalPosition(snapshot.stage, snapshot.bossPatternProgress01, director != null ? director.TuningProfile : null);
                _weakPointRenderer.transform.localPosition = weakLocal;
                _weakPointRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.17f, 0.09f, defeat01);
                Color weakColor = Color.Lerp(new Color(0.72f, 0.22f, 1f), new Color(1f, 0.76f, 1f), match * 0.75f);
                weakColor = Color.Lerp(weakColor, new Color(0.52f, 1f, 0.78f), defeat01);
                _weakPointRenderer.material.color = weakColor;
                ApplyEmission(_weakPointRenderer.material, weakColor * (2f + match * 2.8f));
            }

            if (_crackLines != null)
            {
                for (int index = 0; index < _crackLines.Length; index++)
                {
                    LineRenderer crack = _crackLines[index];
                    if (crack == null)
                    {
                        continue;
                    }

                    float angle = index / 3f * Mathf.PI * 2f + time * 0.25f;
                    Vector3 start = new Vector3(Mathf.Cos(angle) * 0.14f, Mathf.Sin(angle * 1.1f) * 0.11f, Mathf.Sin(angle) * 0.14f);
                    Vector3 mid = start * 1.8f + new Vector3(0f, Mathf.Sin(time * (3.2f + index)) * 0.09f, 0f);
                    Vector3 end = start * 2.8f;
                    crack.startWidth = 0.022f;
                    crack.endWidth = 0.005f;
                    Color crackColor = Color.Lerp(new Color(0.56f, 0.18f, 0.96f, 0.22f), new Color(0.92f, 0.54f, 1f, 0.46f), pulse);
                    crackColor = Color.Lerp(crackColor, new Color(0.42f, 0.82f, 0.72f, 0.20f), defeat01);
                    crack.startColor = crackColor;
                    crack.endColor = crackColor * new Color(1f, 1f, 1f, 0.1f);
                    crack.SetPosition(0, start);
                    crack.SetPosition(1, mid);
                    crack.SetPosition(2, end);
                }
            }
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _root = new GameObject("ObserverBossTinnitusViewRoot").transform;
            _root.SetParent(transform, false);

            _bodyRenderers = new[]
            {
                CreatePart("BossBodyCore", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.95f, 0.95f, 0.95f), new Color(0.12f, 0.05f, 0.18f)),
                CreatePart("BossBodyLeft", PrimitiveType.Sphere, new Vector3(-0.38f, 0.08f, 0.22f), new Vector3(0.56f, 0.56f, 0.56f), new Color(0.18f, 0.08f, 0.28f)),
                CreatePart("BossBodyRight", PrimitiveType.Sphere, new Vector3(0.40f, -0.12f, -0.18f), new Vector3(0.50f, 0.50f, 0.50f), new Color(0.18f, 0.08f, 0.28f)),
            };
            _weakPointRenderer = CreatePart("BossWeakPoint", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.42f), new Vector3(0.17f, 0.17f, 0.17f), new Color(0.92f, 0.42f, 1f));

            _crackLines = new LineRenderer[3];
            for (int index = 0; index < _crackLines.Length; index++)
            {
                GameObject crackObject = new GameObject($"BossCrack_{index:00}");
                crackObject.transform.SetParent(_root, false);
                LineRenderer crack = crackObject.AddComponent<LineRenderer>();
                crack.material = CreateLineMaterial();
                crack.useWorldSpace = false;
                crack.positionCount = 3;
                crack.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                crack.receiveShadows = false;
                _crackLines[index] = crack;
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

        private void TrackStage(FinalDemoStage stage)
        {
            if (_lastStage == stage)
            {
                return;
            }

            _lastStage = stage;
            _stageEnteredAtRealtime = Time.realtimeSinceStartup;
        }

        private static Vector3 ResolveWeakPointLocalPosition(FinalDemoStage stage, float progress01, FinalDemoTuningProfile tuning)
        {
            if (tuning == null)
            {
                return new Vector3(0f, 0f, 0.42f);
            }

            int patternIndex = stage switch
            {
                FinalDemoStage.BossPatternTwo => 1,
                FinalDemoStage.BossPatternThree => 2,
                _ => 0,
            };

            float totalSeconds = patternIndex switch
            {
                1 => tuning.BossPatternTwoSeconds,
                2 => tuning.BossPatternThreeSeconds,
                _ => tuning.BossPatternOneSeconds,
            };

            float holdSeconds = Mathf.Min(tuning.BossOpeningHoldSeconds, totalSeconds);
            float progressSeconds = Mathf.Clamp01(progress01) * totalSeconds;
            float move01 = progressSeconds >= holdSeconds
                ? Mathf.Clamp01((progressSeconds - holdSeconds) / Mathf.Max(0.1f, totalSeconds - holdSeconds))
                : 0f;
            float smooth = move01 * move01 * (3f - 2f * move01);
            Vector3 center = ResolveVectorAt(tuning.BossWeakPointCentersCameraSpace, patternIndex, new Vector3(0f, 0f, 0.72f));
            Vector3 offset = ResolveVectorAt(tuning.BossWeakPointMoveOffsetsCameraSpace, patternIndex, new Vector3(0.32f, 0.06f, 0.06f));
            return new Vector3(
                center.x + offset.x * smooth,
                center.y + offset.y * smooth + Mathf.Sin(move01 * Mathf.PI) * 0.08f,
                0.16f + (center.z - 0.72f) * 0.25f + offset.z * smooth * 0.35f) * 1.4f;
        }

        private static Vector3 ResolveVectorAt(Vector3[] values, int index, Vector3 fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            return values[Mathf.Clamp(index, 0, values.Length - 1)];
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

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
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
