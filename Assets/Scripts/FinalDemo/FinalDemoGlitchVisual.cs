using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoGlitchVisual : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float baseIntensity = 0.16f;
        [SerializeField] private bool bossScale;
        [SerializeField] private float pulseRate = 1.4f;
        [SerializeField] private Color glitchColor = new Color(0.75f, 0.08f, 1f);
        [SerializeField] private float blockDensity = 18f;
        [SerializeField] private float scanlineDensity = 120f;
        [SerializeField] private float channelSplit = 0.008f;

        private const string ShaderName = "Hidden/BellRinger/FinalDemo/ObjectGlitch";

        private Renderer[] _renderers;
        private Material[][] _runtimeMaterials;
        private Shader _glitchShader;
        private bool _initialized;

        public void Configure(bool boss)
        {
            bossScale = boss;
            baseIntensity = boss ? 0.26f : 0.16f;
            pulseRate = boss ? 1.05f : 1.55f;
            glitchColor = boss ? new Color(1f, 0.12f, 0.72f) : new Color(0.72f, 0.08f, 1f);
            blockDensity = boss ? 12f : 18f;
            scanlineDensity = boss ? 96f : 132f;
            channelSplit = boss ? 0.014f : 0.008f;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            Initialize();
            if (_runtimeMaterials == null)
            {
                return;
            }

            float pulse = 0.5f + Mathf.Sin(Time.time * Mathf.PI * 2f * pulseRate) * 0.5f;
            float eventGate = Mathf.Pow(pulse, bossScale ? 3.2f : 4.6f);
            float strength = Mathf.Clamp01(baseIntensity + eventGate * (bossScale ? 0.74f : 0.58f));
            ApplyShaderState(strength);
        }

        private void OnDisable()
        {
            ApplyShaderState(0f);
        }

        private void Initialize()
        {
            if (_initialized || !Application.isPlaying)
            {
                return;
            }

            _initialized = true;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _glitchShader = Shader.Find(ShaderName);
            if (_renderers == null || _renderers.Length == 0 || _glitchShader == null)
            {
                return;
            }

            _runtimeMaterials = new Material[_renderers.Length][];
            for (int rendererIndex = 0; rendererIndex < _renderers.Length; rendererIndex++)
            {
                Renderer renderer = _renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                Material[] sourceMaterials = renderer.materials;
                Material[] glitchMaterials = new Material[sourceMaterials.Length];
                for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    Material source = sourceMaterials[materialIndex];
                    Material glitch = new Material(_glitchShader)
                    {
                        name = $"FinalDemo Object Glitch ({rendererIndex}:{materialIndex})",
                    };

                    if (source != null)
                    {
                        if (source.HasProperty("_BaseMap"))
                        {
                            glitch.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                        }
                        else if (source.HasProperty("_MainTex"))
                        {
                            glitch.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                        }

                        Color baseColor = Color.white;
                        if (source.HasProperty("_BaseColor"))
                        {
                            baseColor = source.GetColor("_BaseColor");
                        }
                        else if (source.HasProperty("_Color"))
                        {
                            baseColor = source.GetColor("_Color");
                        }

                        glitch.SetColor("_BaseColor", baseColor);
                    }

                    glitch.SetColor("_GlitchColor", glitchColor);
                    glitchMaterials[materialIndex] = glitch;
                }

                renderer.materials = glitchMaterials;
                _runtimeMaterials[rendererIndex] = glitchMaterials;
            }
        }

        private void ApplyShaderState(float strength)
        {
            if (_runtimeMaterials == null)
            {
                return;
            }

            float channelScale = bossScale ? 1.35f : 1f;
            for (int rendererIndex = 0; rendererIndex < _runtimeMaterials.Length; rendererIndex++)
            {
                Material[] materials = _runtimeMaterials[rendererIndex];
                if (materials == null)
                {
                    continue;
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    material.SetColor("_GlitchColor", glitchColor);
                    material.SetFloat("_Intensity", strength);
                    material.SetFloat("_BlockDensity", blockDensity);
                    material.SetFloat("_ScanlineDensity", scanlineDensity);
                    material.SetFloat("_ChannelSplit", channelSplit * channelScale);
                    material.SetFloat("_Rim", bossScale ? 0.42f : 0.28f);
                    material.SetFloat("_SurfaceAlpha", 1f);
                }
            }
        }
    }
}
