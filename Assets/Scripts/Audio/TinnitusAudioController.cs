using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TinnitusAudioController : MonoBehaviour
    {
        [SerializeField] [Range(0f, 0.2f)] private float volume = 0.052f;
        [SerializeField] [Range(1000f, 14000f)] private float baseFrequency = 6627.497f;
        [SerializeField] [Range(0.1f, 80f)] private float beatFrequencyOffset = 10.013f;
        [SerializeField] [Range(0f, 0.05f)] private float pitchWobble = 0.016f;
        [SerializeField] [Range(0f, 1.5f)] private float glitchDensity = 0.455f;
        [SerializeField] [Range(0f, 1f)] private float roughness = 0.528f;
        [SerializeField] [Range(0f, 1f)] private float burstIntensity = 0.353f;
        [SerializeField] [Range(0f, 1f)] private float cleanseStability;
        [SerializeField] private AudioClip continuousGlitchClip;
        [SerializeField] private AudioClip shortGlitchClip;
        [SerializeField] [Range(0f, 0.08f)] private float continuousGlitchVolume = 0.012f;
        [SerializeField] [Range(0f, 0.12f)] private float shortGlitchVolume = 0.035f;

        private AudioSource _toneSource;
        private AudioSource _continuousGlitchSource;
        private AudioSource _shortGlitchSource;
        private AudioClip _silentClip;
        private int _sampleRate;
        private double _phaseA;
        private double _phaseB;
        private double _wobblePhase;
        private uint _noiseState = 0x1234ABCDu;
        private int _nextSyntheticGlitchSamples;
        private int _syntheticGlitchSamples;
        private float _syntheticGlitchStrength;
        private float _triggeredBurst;
        private float _nextClipGlitchAt;

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp(value, 0f, 0.2f);
        }

        public float BaseFrequency
        {
            get => baseFrequency;
            set => baseFrequency = Mathf.Clamp(value, 1000f, 14000f);
        }

        public float BeatFrequencyOffset
        {
            get => beatFrequencyOffset;
            set => beatFrequencyOffset = Mathf.Clamp(value, 0.1f, 80f);
        }

        public float PitchWobble
        {
            get => pitchWobble;
            set => pitchWobble = Mathf.Clamp(value, 0f, 0.05f);
        }

        public float GlitchDensity
        {
            get => glitchDensity;
            set => glitchDensity = Mathf.Clamp(value, 0f, 1.5f);
        }

        public float Roughness
        {
            get => roughness;
            set => roughness = Mathf.Clamp01(value);
        }

        public float BurstIntensity
        {
            get => burstIntensity;
            set => burstIntensity = Mathf.Clamp01(value);
        }

        public float CleanseStability
        {
            get => cleanseStability;
            set => cleanseStability = Mathf.Clamp01(value);
        }

        public float EffectiveBurstIntensity => Mathf.Clamp01(burstIntensity + _triggeredBurst);
        public float EffectiveInstability => Mathf.Clamp01((1f - cleanseStability) * (0.25f + roughness * 0.55f + glitchDensity * 0.2f) + EffectiveBurstIntensity * 0.75f);

        public void SetGlitchClips(AudioClip continuousClip, AudioClip shortClip)
        {
            continuousGlitchClip = continuousClip;
            shortGlitchClip = shortClip;
            ConfigureGlitchSources();
        }

        public void SetBinauralPreview(Transform listener, bool enabled, float binauralStrength = 1f)
        {
            EnsureAudioSources();
            ConfigureSourceBinaural(_toneSource, listener, enabled, binauralStrength);
            ConfigureSourceBinaural(_continuousGlitchSource, listener, enabled, binauralStrength);
            ConfigureSourceBinaural(_shortGlitchSource, listener, enabled, binauralStrength);
        }

        public void SetSpatialRangeScale(float scale)
        {
            EnsureAudioSources();
            float clampedScale = Mathf.Max(0.1f, scale);
            ApplySpatialRange(_toneSource, clampedScale);
            ApplySpatialRange(_continuousGlitchSource, clampedScale);
            ApplySpatialRange(_shortGlitchSource, clampedScale);
        }

        public void TriggerBurst(float intensity = 1f)
        {
            _triggeredBurst = Mathf.Max(_triggeredBurst, Mathf.Clamp01(intensity));
            PlayShortGlitch(Mathf.Clamp01(intensity));
        }

        public void ApplySharedSettings(TinnitusSharedSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            Volume = settings.volume;
            BaseFrequency = settings.baseFrequency;
            BeatFrequencyOffset = settings.beatFrequencyOffset;
            PitchWobble = settings.pitchWobble;
            GlitchDensity = settings.glitchDensity;
            Roughness = settings.roughness;
            BurstIntensity = settings.burstIntensity;
            CleanseStability = settings.cleanseStability;
        }

        public void FillSharedSettings(TinnitusSharedSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.volume = Volume;
            settings.baseFrequency = BaseFrequency;
            settings.beatFrequencyOffset = BeatFrequencyOffset;
            settings.pitchWobble = PitchWobble;
            settings.glitchDensity = GlitchDensity;
            settings.roughness = Roughness;
            settings.burstIntensity = BurstIntensity;
            settings.cleanseStability = CleanseStability;
        }

        private void Awake()
        {
            LoadSharedSettingsIfPresent();
            EnsureAudioSources();
        }

        private void OnEnable()
        {
            EnsureAudioSources();
            if (_toneSource != null && !_toneSource.isPlaying)
            {
                _toneSource.Play();
            }
        }

        private void Update()
        {
            EnsureAudioSources();
            _triggeredBurst = Mathf.MoveTowards(_triggeredBurst, 0f, Time.unscaledDeltaTime * 1.25f);
            UpdateGlitchClipPlayback();
        }

        private void OnDisable()
        {
            if (_toneSource != null)
            {
                _toneSource.Stop();
            }

            if (_continuousGlitchSource != null)
            {
                _continuousGlitchSource.Stop();
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp(volume, 0f, 0.2f);
            baseFrequency = Mathf.Clamp(baseFrequency, 1000f, 14000f);
            beatFrequencyOffset = Mathf.Clamp(beatFrequencyOffset, 0.1f, 80f);
            pitchWobble = Mathf.Clamp(pitchWobble, 0f, 0.05f);
            glitchDensity = Mathf.Clamp(glitchDensity, 0f, 1.5f);
            roughness = Mathf.Clamp01(roughness);
            burstIntensity = Mathf.Clamp01(burstIntensity);
            cleanseStability = Mathf.Clamp01(cleanseStability);
        }

        private void LoadSharedSettingsIfPresent()
        {
            if (!Application.isPlaying || !TinnitusSharedSettingsStore.HasSavedSettings)
            {
                return;
            }

            ApplySharedSettings(TinnitusSharedSettingsStore.Load());
        }

        private void EnsureAudioSources()
        {
            if (_toneSource == null)
            {
                _toneSource = GetComponent<AudioSource>();
            }

            if (_silentClip == null)
            {
                _sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
                _silentClip = AudioClip.Create("Tinnitus Procedural Carrier", _sampleRate, 1, _sampleRate, false);
                _toneSource.clip = _silentClip;
                _toneSource.loop = true;
                _toneSource.playOnAwake = true;
                _toneSource.volume = 1f;
                _toneSource.spatialBlend = 1f;
                _toneSource.minDistance = 0.5f;
                _toneSource.maxDistance = 10f;
                _toneSource.rolloffMode = AudioRolloffMode.Linear;
                _toneSource.dopplerLevel = 0f;
            }

            ConfigureGlitchSources();
        }

        private void ConfigureGlitchSources()
        {
            _continuousGlitchSource = EnsureChildAudioSource(_continuousGlitchSource, "Tinnitus Continuous Glitch Source");
            _shortGlitchSource = EnsureChildAudioSource(_shortGlitchSource, "Tinnitus Short Glitch Source");

            _continuousGlitchSource.clip = continuousGlitchClip;
            _continuousGlitchSource.loop = true;
            _continuousGlitchSource.playOnAwake = false;

            _shortGlitchSource.clip = shortGlitchClip;
            _shortGlitchSource.loop = false;
            _shortGlitchSource.playOnAwake = false;
        }

        private AudioSource EnsureChildAudioSource(AudioSource source, string childName)
        {
            if (source != null)
            {
                return source;
            }

            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;

            source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            source.spatialBlend = 1f;
            source.minDistance = 0.5f;
            source.maxDistance = 10f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            return source;
        }

        private static void ConfigureSourceBinaural(AudioSource source, Transform listener, bool enabled, float binauralStrength)
        {
            if (source == null)
            {
                return;
            }

            BellRingerBinauralSpatializer binaural = source.GetComponent<BellRingerBinauralSpatializer>();
            if (enabled)
            {
                source.spatialBlend = 0f;
                source.spatialize = false;
                if (binaural == null)
                {
                    binaural = source.gameObject.AddComponent<BellRingerBinauralSpatializer>();
                }

                binaural.Configure(source, listener, true, binauralStrength);
                return;
            }

            source.spatialBlend = 1f;
            source.spatialize = false;
            if (binaural != null)
            {
                binaural.Configure(source, listener, false, binauralStrength);
            }
        }

        private static void ApplySpatialRange(AudioSource source, float scale)
        {
            if (source == null)
            {
                return;
            }

            source.minDistance = 0.5f * scale;
            source.maxDistance = 10f * scale;
        }

        private void UpdateGlitchClipPlayback()
        {
            float unstable = EffectiveInstability;
            if (_continuousGlitchSource != null && continuousGlitchClip != null)
            {
                _continuousGlitchSource.volume = continuousGlitchVolume * Mathf.Clamp01(0.2f + unstable * 0.8f) * (1f - cleanseStability);
                if (!_continuousGlitchSource.isPlaying)
                {
                    _continuousGlitchSource.Play();
                }
            }

            if (shortGlitchClip == null || _shortGlitchSource == null)
            {
                return;
            }

            float density = (glitchDensity * 0.45f + EffectiveBurstIntensity * 1.15f) * (1f - cleanseStability);
            if (density <= 0.01f)
            {
                _nextClipGlitchAt = Time.unscaledTime + 0.25f;
                return;
            }

            if (Time.unscaledTime >= _nextClipGlitchAt)
            {
                PlayShortGlitch(Mathf.Clamp01(density));
                _nextClipGlitchAt = Time.unscaledTime + Random.Range(0.25f, 1.4f) / Mathf.Max(0.1f, density);
            }
        }

        private void PlayShortGlitch(float intensity)
        {
            if (_shortGlitchSource == null || shortGlitchClip == null)
            {
                return;
            }

            _shortGlitchSource.pitch = Random.Range(0.92f, 1.08f + intensity * 0.2f);
            _shortGlitchSource.PlayOneShot(shortGlitchClip, shortGlitchVolume * Mathf.Clamp01(intensity) * (1f - cleanseStability));
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (_sampleRate <= 0)
            {
                _sampleRate = 48000;
            }

            float stability = Mathf.Clamp01(cleanseStability);
            float burst = Mathf.Clamp01(burstIntensity + _triggeredBurst);
            float unstable = Mathf.Clamp01((1f - stability) * (0.25f + roughness * 0.55f + glitchDensity * 0.2f) + burst * 0.75f);
            float effectiveVolume = volume * Mathf.Lerp(1f, 0.38f, stability) * (1f + burst * 0.18f);
            float effectiveRoughness = roughness * (1f - stability) + burst * 0.35f;
            float effectiveGlitchDensity = (glitchDensity + burst * 1.3f) * (1f - stability);

            for (int frame = 0; frame < data.Length; frame += channels)
            {
                _wobblePhase += 0.65 / _sampleRate;
                if (_wobblePhase >= 1.0)
                {
                    _wobblePhase -= 1.0;
                }

                float wobbleLfo = Mathf.Sin((float)_wobblePhase * Mathf.PI * 2f);
                float pitchBendHz = baseFrequency * pitchWobble * unstable * wobbleLfo;
                pitchBendHz += baseFrequency * 0.012f * burst * Mathf.Sin((float)_wobblePhase * Mathf.PI * 13.7f);

                float frequencyA = Mathf.Clamp(baseFrequency + pitchBendHz, 100f, _sampleRate * 0.45f);
                float frequencyB = Mathf.Clamp(baseFrequency + beatFrequencyOffset * (1f + burst * 0.45f) - pitchBendHz * 0.35f, 100f, _sampleRate * 0.45f);

                _phaseA += frequencyA / _sampleRate;
                _phaseB += frequencyB / _sampleRate;
                if (_phaseA >= 1.0)
                {
                    _phaseA -= System.Math.Floor(_phaseA);
                }

                if (_phaseB >= 1.0)
                {
                    _phaseB -= System.Math.Floor(_phaseB);
                }

                float carrier = Mathf.Sin((float)_phaseA * Mathf.PI * 2f) * 0.58f;
                carrier += Mathf.Sin((float)_phaseB * Mathf.PI * 2f) * 0.42f;
                carrier *= 0.72f + Mathf.Sin((float)(_phaseA - _phaseB) * Mathf.PI * 2f) * 0.18f;

                float sample = carrier;
                sample += NextSignedNoise() * effectiveRoughness * 0.08f;
                sample += EvaluateSyntheticGlitch(effectiveGlitchDensity, burst);
                sample *= effectiveVolume;

                for (int channel = 0; channel < channels; channel++)
                {
                    data[frame + channel] = Mathf.Clamp(sample, -0.65f, 0.65f);
                }
            }
        }

        private float EvaluateSyntheticGlitch(float effectiveDensity, float burst)
        {
            if (_syntheticGlitchSamples > 0)
            {
                _syntheticGlitchSamples--;
                float envelope = Mathf.Sin(Mathf.Clamp01(_syntheticGlitchSamples / Mathf.Max(1f, _sampleRate * 0.035f)) * Mathf.PI);
                return NextSignedNoise() * _syntheticGlitchStrength * envelope;
            }

            _nextSyntheticGlitchSamples--;
            if (_nextSyntheticGlitchSamples > 0)
            {
                return 0f;
            }

            if (effectiveDensity <= 0.01f)
            {
                _nextSyntheticGlitchSamples = Mathf.RoundToInt(_sampleRate * 0.5f);
                return 0f;
            }

            float duration = Mathf.Lerp(0.008f, 0.045f, Mathf.Clamp01(effectiveDensity + burst));
            _syntheticGlitchSamples = Mathf.RoundToInt(_sampleRate * duration);
            _syntheticGlitchStrength = Mathf.Lerp(0.08f, 0.32f, Mathf.Clamp01(effectiveDensity + burst));
            float waitSeconds = Mathf.Lerp(1.4f, 0.16f, Mathf.Clamp01(effectiveDensity));
            _nextSyntheticGlitchSamples = Mathf.RoundToInt(_sampleRate * waitSeconds * (0.75f + Next01() * 0.5f));
            return 0f;
        }

        private float NextSignedNoise()
        {
            return Next01() * 2f - 1f;
        }

        private float Next01()
        {
            _noiseState = _noiseState * 1664525u + 1013904223u;
            return (_noiseState & 0x00FFFFFF) / 16777215f;
        }
    }
}
