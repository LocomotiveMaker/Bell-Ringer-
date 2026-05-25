using System.Collections.Generic;
using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverBellView : MonoBehaviour
    {
        [SerializeField] private float positionLerp = 12f;
        [SerializeField] private float shakeDegrees = 11f;
        [SerializeField] private float ringDurationSeconds = 0.46f;

        private Transform _modelRoot;
        private Transform _ringRootA;
        private Transform _ringRootB;
        private Renderer[] _shellRenderers;
        private Color[] _shellBaseColors;
        private Renderer _coreRenderer;
        private Renderer _clapperRenderer;
        private LineRenderer _ringA;
        private LineRenderer _ringB;
        private bool _initialized;
        private bool _hasAppliedSnapshot;
        private float _ringStartedAtRealtime = -99f;
        private float _ringStrength01;
        private float _lastPadShakePulseAtRealtime = -99f;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director, List<FinalDemoAudioCueSnapshot> cueSnapshots, float padMotion01)
        {
            EnsureVisuals();

            bool visible = IsVisible(snapshot.stage, snapshot.activeSoundFocusLabel);
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            Vector3 targetPosition = snapshot.hasBell ? snapshot.bellWorldPosition : _modelRoot.position;
            float follow01 = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionLerp) * Time.unscaledDeltaTime);
            if (!_hasAppliedSnapshot)
            {
                _modelRoot.position = targetPosition;
                _hasAppliedSnapshot = true;
            }
            else
            {
                _modelRoot.position = Vector3.Lerp(_modelRoot.position, targetPosition, follow01);
            }

            float bellImpulse01 = ResolveBellImpulse(cueSnapshots);
            if (bellImpulse01 > 0.01f)
            {
                _ringStartedAtRealtime = Time.realtimeSinceStartup;
                _ringStrength01 = bellImpulse01;
            }
            else if (padMotion01 >= 0.62f && Time.realtimeSinceStartup - _lastPadShakePulseAtRealtime >= 0.16f)
            {
                _lastPadShakePulseAtRealtime = Time.realtimeSinceStartup;
                _ringStartedAtRealtime = Time.realtimeSinceStartup;
                _ringStrength01 = Mathf.Clamp01(0.35f + padMotion01 * 0.65f);
            }

            float gazeBoost = director != null && director.CurrentStage == FinalDemoStage.BellGaze
                ? Mathf.Clamp01(director.BellGazeProgress01)
                : 0f;
            float ringAge = Time.realtimeSinceStartup - _ringStartedAtRealtime;
            float ringPulse01 = ringAge <= ringDurationSeconds
                ? 1f - Mathf.Clamp01(ringAge / Mathf.Max(0.01f, ringDurationSeconds))
                : 0f;
            float swayDegrees = ringPulse01 * _ringStrength01 * shakeDegrees;
            swayDegrees += Mathf.Sin(Time.realtimeSinceStartup * 4.6f) * 1.8f;
            swayDegrees += padMotion01 * 2.3f;

            float bob = Mathf.Sin(Time.realtimeSinceStartup * 2.5f) * 0.03f;
            _modelRoot.position += new Vector3(0f, bob, 0f);
            _modelRoot.rotation = Quaternion.Euler(
                Mathf.Sin(Time.realtimeSinceStartup * 9.2f) * swayDegrees,
                Mathf.Sin(Time.realtimeSinceStartup * 5.4f) * 7f,
                Mathf.Cos(Time.realtimeSinceStartup * 8.1f) * swayDegrees * 0.72f);

            ApplyBellTint(snapshot.stage, gazeBoost, ringPulse01);
            UpdateRings(ringPulse01);
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _modelRoot = new GameObject("ObserverBellModel").transform;
            _modelRoot.SetParent(transform, false);
            _modelRoot.localScale = Vector3.one * 2.25f;

            Material shellMaterial = CreateStandardMaterial(new Color(0.26f, 0.22f, 0.16f), 0f);
            Material patinaMaterial = CreateStandardMaterial(new Color(0.24f, 0.39f, 0.31f), 0f);
            Material coreMaterial = CreateStandardMaterial(new Color(0.16f, 0.82f, 0.31f), 1.6f);
            Material clapperMaterial = CreateStandardMaterial(new Color(0.15f, 0.18f, 0.13f), 0f);

            List<Renderer> shellRenderers = new List<Renderer>(8)
            {
                CreatePart("HandleStem", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.032f, 0.10f, 0.032f), shellMaterial),
                CreatePart("HandleGrip", PrimitiveType.Capsule, new Vector3(0f, 0.30f, 0f), new Vector3(0.052f, 0.11f, 0.052f), patinaMaterial),
                CreatePart("HandleCap", PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.032f, 0.032f, 0.032f), shellMaterial),
                CreatePart("ShellUpper", PrimitiveType.Sphere, new Vector3(0f, -0.01f, 0f), new Vector3(0.23f, 0.15f, 0.23f), patinaMaterial),
                CreatePart("ShellLower", PrimitiveType.Cylinder, new Vector3(0f, -0.085f, 0f), new Vector3(0.19f, 0.10f, 0.19f), shellMaterial),
                CreatePart("Rim", PrimitiveType.Cylinder, new Vector3(0f, -0.19f, 0f), new Vector3(0.23f, 0.012f, 0.23f), patinaMaterial),
            };

            _clapperRenderer = CreatePart("ClapperStem", PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.012f, 0.06f, 0.012f), clapperMaterial);
            shellRenderers.Add(_clapperRenderer);
            Renderer clapperBall = CreatePart("ClapperBall", PrimitiveType.Sphere, new Vector3(0f, -0.15f, 0f), new Vector3(0.055f, 0.055f, 0.055f), clapperMaterial);
            shellRenderers.Add(clapperBall);
            _coreRenderer = CreatePart("LivingCore", PrimitiveType.Sphere, new Vector3(0f, -0.015f, 0f), new Vector3(0.082f, 0.082f, 0.082f), coreMaterial);

            _shellRenderers = shellRenderers.ToArray();
            _shellBaseColors = new Color[_shellRenderers.Length];
            for (int index = 0; index < _shellRenderers.Length; ++index)
            {
                _shellBaseColors[index] = _shellRenderers[index] != null ? _shellRenderers[index].material.color : Color.black;
            }

            _ringRootA = new GameObject("RingA").transform;
            _ringRootA.SetParent(_modelRoot, false);
            _ringRootA.localPosition = new Vector3(0f, -0.02f, 0f);
            _ringA = CreateRingRenderer(_ringRootA, new Color(0.34f, 0.96f, 0.54f, 0f));

            _ringRootB = new GameObject("RingB").transform;
            _ringRootB.SetParent(_modelRoot, false);
            _ringRootB.localPosition = new Vector3(0f, 0.04f, 0f);
            _ringB = CreateRingRenderer(_ringRootB, new Color(0.21f, 0.88f, 0.42f, 0f));

        }

        private Renderer CreatePart(string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
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
            return renderer;
        }

        private LineRenderer CreateRingRenderer(Transform parent, Color color)
        {
            GameObject ringObject = new GameObject(parent.name + "_Line");
            ringObject.transform.SetParent(parent, false);
            LineRenderer lineRenderer = ringObject.AddComponent<LineRenderer>();
            lineRenderer.material = CreateLineMaterial();
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.positionCount = 28;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            for (int index = 0; index < lineRenderer.positionCount; ++index)
            {
                float radians = index / (float)(lineRenderer.positionCount - 1) * Mathf.PI * 2f;
                lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * 0.35f);
            }

            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.015f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            return lineRenderer;
        }

        private void ApplyBellTint(FinalDemoStage stage, float gazeBoost, float ringPulse01)
        {
            float shellBrightness = 0.55f + ringPulse01 * 0.28f;
            float patinaBoost = 0.75f + gazeBoost * 0.25f;
            for (int index = 0; index < _shellRenderers.Length; ++index)
            {
                if (_shellRenderers[index] == null)
                {
                    continue;
                }

                Color baseColor = _shellBaseColors[index];
                float brightness = index <= 2 ? shellBrightness : patinaBoost;
                _shellRenderers[index].enabled = true;
                _shellRenderers[index].material.color = Color.Lerp(Color.black, baseColor, brightness);
            }

            if (_coreRenderer != null)
            {
                Color coreColor = stage == FinalDemoStage.ForestEnding
                    ? new Color(0.22f, 0.94f, 0.52f)
                    : new Color(0.18f, 0.92f, 0.36f);
                float emission = 1.4f + gazeBoost * 1.2f + ringPulse01 * 1.6f;
                _coreRenderer.material.color = coreColor;
                _coreRenderer.material.SetColor("_EmissionColor", coreColor * emission);
                _coreRenderer.material.EnableKeyword("_EMISSION");
            }

        }

        private void UpdateRings(float ringPulse01)
        {
            float secondRingPulse01 = Mathf.Clamp01(ringPulse01 - 0.18f);
            UpdateRing(_ringA, _ringRootA, ringPulse01, 0.38f, 1f);
            UpdateRing(_ringB, _ringRootB, secondRingPulse01, 0.28f, 0.88f);
        }

        private static void UpdateRing(LineRenderer lineRenderer, Transform ringRoot, float pulse01, float minScale, float maxScale)
        {
            if (lineRenderer == null || ringRoot == null)
            {
                return;
            }

            bool visible = pulse01 > 0.01f;
            lineRenderer.enabled = visible;
            if (!visible)
            {
                return;
            }

            float scale = Mathf.Lerp(minScale, maxScale, 1f - pulse01);
            ringRoot.localScale = Vector3.one * scale;
            float alpha = pulse01 * 0.9f;
            Color color = lineRenderer.startColor;
            color.a = alpha;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = Mathf.Lerp(0.026f, 0.008f, 1f - pulse01);
            lineRenderer.endWidth = lineRenderer.startWidth;
        }

        private void SetVisible(bool visible)
        {
            if (_shellRenderers != null)
            {
                for (int index = 0; index < _shellRenderers.Length; ++index)
                {
                    if (_shellRenderers[index] != null)
                    {
                        _shellRenderers[index].enabled = visible;
                    }
                }
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.enabled = visible;
            }

            if (_ringA != null)
            {
                _ringA.enabled = visible && _ringA.enabled;
            }

            if (_ringB != null)
            {
                _ringB.enabled = visible && _ringB.enabled;
            }
        }

        private static bool IsVisible(FinalDemoStage stage, string focusLabel)
        {
            return focusLabel == "LIVING BELL" ||
                   stage == FinalDemoStage.OpeningCloseBell ||
                   stage == FinalDemoStage.BellOrbit ||
                   stage == FinalDemoStage.BellFollowOne ||
                   stage == FinalDemoStage.BellFollowRain ||
                   stage == FinalDemoStage.BellGaze ||
                   stage == FinalDemoStage.BellAcquisition ||
                   stage == FinalDemoStage.ForestEnding;
        }

        private static float ResolveBellImpulse(List<FinalDemoAudioCueSnapshot> cueSnapshots)
        {
            if (cueSnapshots == null)
            {
                return 0f;
            }

            float impulse = 0f;
            for (int index = 0; index < cueSnapshots.Count; ++index)
            {
                FinalDemoAudioCueSnapshot cue = cueSnapshots[index];
                if (!IsBellCue(cue.CueId) || cue.IsLoop || cue.AgeSeconds > 0.06f)
                {
                    continue;
                }

                impulse = Mathf.Max(impulse, cue.Volume01);
            }

            return impulse;
        }

        private static bool IsBellCue(FinalDemoCueId cueId)
        {
            return cueId == FinalDemoCueId.BellMovementTexture ||
                   cueId == FinalDemoCueId.BellDistantCall ||
                   cueId == FinalDemoCueId.BellStrongAssist ||
                   cueId == FinalDemoCueId.BellPadShakeResponse ||
                   cueId == FinalDemoCueId.BellGazeSuccess ||
                   cueId == FinalDemoCueId.BellAcquisition ||
                   cueId == FinalDemoCueId.ForestBell;
        }

        private static Material CreateStandardMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Unlit/Color") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            if (emission > 0f)
            {
                material.SetColor("_EmissionColor", color * emission);
                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            return new Material(shader);
        }
    }
}
