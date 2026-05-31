using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoWorldRainEnvironment : MonoBehaviour
    {
        [SerializeField] private float followHeight = 4.6f;
        [SerializeField] private Vector2 rainAreaMeters = new Vector2(28f, 24f);
        [SerializeField] private Vector2 rippleAreaMeters = new Vector2(22f, 18f);
        [SerializeField] private int maxRainParticles = 1600;
        [SerializeField] private int maxRippleParticles = 220;
        [SerializeField, Range(0f, 1f)] private float windDrift = 0.06f;

        private ParticleSystem _rainParticles;
        private ParticleSystem _rippleParticles;
        private Material _rainMaterial;
        private Material _rippleMaterial;
        private bool _initialized;

        public void Apply(float intensity01, Transform playerRig, bool visible)
        {
            EnsureInitialized();

            float intensity = visible ? Mathf.Clamp01(intensity01) : 0f;
            if (playerRig != null)
            {
                transform.position = new Vector3(playerRig.position.x, 0f, playerRig.position.z);
            }

            ConfigureRain(intensity);
            ConfigureRipples(intensity);
            SetPlaying(_rainParticles, intensity > 0.01f);
            SetPlaying(_rippleParticles, intensity > 0.02f);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _rainMaterial = CreateParticleMaterial(CreateRainStreakTexture(), "FinalDemo Rain Streak");
            _rippleMaterial = CreateParticleMaterial(CreateRippleTexture(), "FinalDemo Rain Ripple");

            _rainParticles = CreateParticleSystem("FinalDemoWorldRain", _rainMaterial, new Vector3(0f, followHeight, 0f));
            _rippleParticles = CreateParticleSystem("FinalDemoWorldRainRipples", _rippleMaterial, new Vector3(0f, 0.04f, 0f));
            InitializeRain(_rainParticles);
            InitializeRipples(_rippleParticles);
        }

        private void ConfigureRain(float intensity)
        {
            ParticleSystem.MainModule main = _rainParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.88f, 1f, Mathf.Lerp(0.10f, 0.42f, intensity)));
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.085f);
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.78f);

            ParticleSystem.ShapeModule shape = _rainParticles.shape;
            shape.scale = new Vector3(rainAreaMeters.x, 0.1f, rainAreaMeters.y);

            ParticleSystem.EmissionModule emission = _rainParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 1550f, intensity);

            ParticleSystem.VelocityOverLifetimeModule velocity = _rainParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-windDrift, windDrift);
            velocity.y = new ParticleSystem.MinMaxCurve(-12.5f, -16.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-windDrift * 0.45f, windDrift * 0.45f);
        }

        private void ConfigureRipples(float intensity)
        {
            ParticleSystem.MainModule main = _rippleParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.72f, 1f, Mathf.Lerp(0.035f, 0.18f, intensity)));
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);

            ParticleSystem.ShapeModule shape = _rippleParticles.shape;
            shape.scale = new Vector3(rippleAreaMeters.x, 0.01f, rippleAreaMeters.y);

            ParticleSystem.EmissionModule emission = _rippleParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 180f, intensity);
        }

        private ParticleSystem CreateParticleSystem(string objectName, Material material, Vector3 localPosition)
        {
            GameObject particleObject = new GameObject(objectName);
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = localPosition;
            ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = objectName.Contains("Ripple") ? ParticleSystemRenderMode.HorizontalBillboard : ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = objectName.Contains("Ripple") ? 1f : 2.6f;
            renderer.velocityScale = objectName.Contains("Ripple") ? 0f : 0.06f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particleSystem;
        }

        private void InitializeRain(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = maxRainParticles;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.NoiseModule noise = particleSystem.noise;
            noise.enabled = false;
            noise.strength = 0f;
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.28f;
        }

        private void InitializeRipples(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = maxRippleParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.1f);
            main.startSpeed = 0f;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
        }

        private static Material CreateParticleMaterial(Texture2D texture, string materialName)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
                            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                name = materialName,
                mainTexture = texture,
            };
            material.renderQueue = 3000;
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            return material;
        }

        private static Texture2D CreateRainStreakTexture()
        {
            Texture2D texture = new Texture2D(8, 64, TextureFormat.RGBA32, false)
            {
                name = "FinalDemoProceduralRainStreak",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < texture.height; y++)
            {
                float v = y / (float)(texture.height - 1);
                float tail = Mathf.Sin(v * Mathf.PI);
                for (int x = 0; x < texture.width; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / texture.width - 0.5f);
                    float alpha = Mathf.Clamp01(1f - dx * 5.2f) * tail * 0.72f;
                    texture.SetPixel(x, y, alpha > 0.01f ? new Color(0.62f, 0.82f, 1f, alpha) : clear);
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateRippleTexture()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "FinalDemoProceduralRainRipple",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x, y), center) / 31.5f;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.72f) / 0.055f);
                    float fade = Mathf.Clamp01(1f - radius);
                    float alpha = ring * fade * 0.52f;
                    texture.SetPixel(x, y, alpha > 0.01f ? new Color(0.45f, 0.78f, 1f, alpha) : clear);
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static void SetPlaying(ParticleSystem particleSystem, bool playing)
        {
            if (particleSystem == null)
            {
                return;
            }

            if (playing)
            {
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play(true);
                }
            }
            else if (particleSystem.isPlaying)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
