using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverRainView : MonoBehaviour
    {
        private Transform _root;
        private Renderer _floorRenderer;
        private ParticleSystem _rainParticles;
        private ParticleSystem _rippleParticles;
        private ParticleSystem _fogParticles;
        private Material _rainMaterial;
        private Material _rippleMaterial;
        private Material _fogMaterial;
        private bool _initialized;

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot, FinalDemoDirector director)
        {
            EnsureVisuals();

            bool visible = snapshot.stage == FinalDemoStage.BellFollowRain ||
                           (snapshot.stage == FinalDemoStage.BellGaze && snapshot.rainIntensity01 > 0.02f);
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            FinalDemoTuningProfile tuning = director != null ? director.TuningProfile : null;
            Vector3 center = tuning != null ? tuning.RainZoneCenter : snapshot.playerWorldPosition + new Vector3(0f, 1.6f, 2.9f);
            float radius = tuning != null ? tuning.RainZoneRadius : 2.4f;
            float intensity = Mathf.Clamp01(snapshot.rainIntensity01);
            if (snapshot.stage == FinalDemoStage.BellGaze)
            {
                intensity *= 0.35f;
            }

            _root.position = new Vector3(center.x, 0f, center.z);
            float time = Time.realtimeSinceStartup;

            if (_floorRenderer != null)
            {
                _floorRenderer.transform.localScale = new Vector3(radius * 1.7f, 0.01f, radius * 1.55f);
                Color floorColor = Color.Lerp(new Color(0.04f, 0.05f, 0.06f), new Color(0.14f, 0.16f, 0.18f), 0.40f + intensity * 0.50f);
                _floorRenderer.material.color = floorColor;
                ApplyEmission(_floorRenderer.material, floorColor * (0.04f + intensity * 0.10f));
            }

            ConfigureRain(intensity, radius);
            ConfigureRipples(intensity, radius);
            ConfigureFog(intensity, radius, time);
        }

        private void EnsureVisuals()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _root = new GameObject("ObserverRainViewRoot").transform;
            _root.SetParent(transform, false);

            GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name = "RainFloor";
            floorObject.transform.SetParent(_root, false);
            Collider floorCollider = floorObject.GetComponent<Collider>();
            if (floorCollider != null)
            {
                Destroy(floorCollider);
            }

            _floorRenderer = floorObject.GetComponent<Renderer>();
            _floorRenderer.material = CreateSurfaceMaterial(new Color(0.05f, 0.06f, 0.07f));

            _rainMaterial = CreateParticleMaterial(
                Resources.Load<Texture2D>("ObserverAssets/Rain/rain_drop_2") ??
                Resources.Load<Texture2D>("ObserverAssets/Rain/trace_06"));
            _rippleMaterial = CreateParticleMaterial(
                Resources.Load<Texture2D>("ObserverAssets/Rain/circle_05") ??
                Resources.Load<Texture2D>("ObserverAssets/Rain/circle_03"));
            _fogMaterial = CreateParticleMaterial(
                Resources.Load<Texture2D>("ObserverAssets/Rain/smoke_09") ??
                Resources.Load<Texture2D>("ObserverAssets/Rain/smoke_06"));

            _rainParticles = CreateParticleSystem("RainParticles", _rainMaterial, new Vector3(0f, 4.2f, 0f));
            _rippleParticles = CreateParticleSystem("RippleParticles", _rippleMaterial, new Vector3(0f, 0.05f, 0f));
            _fogParticles = CreateParticleSystem("FogParticles", _fogMaterial, new Vector3(0f, 0.35f, 0f));

            InitializeRainSystem(_rainParticles);
            InitializeRippleSystem(_rippleParticles);
            InitializeFogSystem(_fogParticles);
        }

        private void ConfigureRain(float intensity, float radius)
        {
            if (_rainParticles == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _rainParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.88f, 0.98f, 0.52f + intensity * 0.34f));
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(9.5f, 13.8f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);

            ParticleSystem.ShapeModule shape = _rainParticles.shape;
            shape.scale = new Vector3(radius * 2.6f, 0.1f, radius * 2.2f);

            ParticleSystem.EmissionModule emission = _rainParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 1150f, intensity);

            ParticleSystem.VelocityOverLifetimeModule velocity = _rainParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(Mathf.Lerp(-0.2f, 1.45f, intensity));
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.1f, 0.45f, intensity));

            _rainParticles.Play(true);
        }

        private void ConfigureRipples(float intensity, float radius)
        {
            if (_rippleParticles == null)
            {
                return;
            }

            ParticleSystem.MainModule main = _rippleParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.92f, 1f, 0.22f + intensity * 0.38f));
            main.startSize = new ParticleSystem.MinMaxCurve(0.32f, 0.68f);

            ParticleSystem.ShapeModule shape = _rippleParticles.shape;
            shape.scale = new Vector3(radius * 2.2f, 0.01f, radius * 2f);

            ParticleSystem.EmissionModule emission = _rippleParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 140f, intensity);

            _rippleParticles.Play(true);
        }

        private void ConfigureFog(float intensity, float radius, float time)
        {
            if (_fogParticles == null)
            {
                return;
            }

            _fogParticles.transform.localPosition = new Vector3(Mathf.Sin(time * 0.18f) * 0.25f, 0.42f, Mathf.Cos(time * 0.12f) * 0.18f);

            ParticleSystem.MainModule main = _fogParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.86f, 0.90f, 0.06f + intensity * 0.13f));
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.42f, radius * 0.78f);

            ParticleSystem.ShapeModule shape = _fogParticles.shape;
            shape.scale = new Vector3(radius * 1.9f, 0.25f, radius * 1.7f);

            ParticleSystem.EmissionModule emission = _fogParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 34f, intensity);

            _fogParticles.Play(true);
        }

        private ParticleSystem CreateParticleSystem(string objectName, Material material, Vector3 localPosition)
        {
            GameObject particleObject = new GameObject(objectName);
            particleObject.transform.SetParent(_root, false);
            particleObject.transform.localPosition = localPosition;
            ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particleSystem;
        }

        private static void InitializeRainSystem(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 1500;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.NoiseModule noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.16f;
            noise.frequency = 0.22f;
            noise.scrollSpeed = 0.35f;
        }

        private static void InitializeRippleSystem(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 220;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startSpeed = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.25f);
            curve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.82f, 0.90f, 1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.45f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void InitializeFogSystem(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 80;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = 0.08f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
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
