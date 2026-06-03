using System;
using System.Collections.Generic;
using BellRinger.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoAudioRouter : MonoBehaviour
    {
        private sealed class ActiveLoop
        {
            public FinalDemoCueEntry cue;
            public AudioSource source;
            public float baseVolume;
            public float targetScale = 1f;
            public bool stopping;
        }

        [SerializeField] private FinalDemoCueLibrary cueLibrary;
        [SerializeField] private Transform listenerTransform;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.82f;
        [SerializeField, Range(0f, 2f)] private float bellBusGain = 1f;
        [SerializeField, Range(0f, 2f)] private float rainWindBusGain = 0.72f;
        [SerializeField, Range(0f, 2f)] private float tinnitusBusGain = 0.72f;
        [SerializeField, Range(0f, 2f)] private float bossTinnitusBusGain = 0.78f;
        [SerializeField, Range(0f, 2f)] private float ambienceBusGain = 0.65f;
        [SerializeField, Range(0f, 2f)] private float interactionBusGain = 0.9f;
        [SerializeField, Range(0f, 2f)] private float narrationBusGain = 0.9f;
        [SerializeField, Range(0f, 1f)] private float narrationDuckMultiplier = 0.52f;
        [SerializeField] private float defaultNarrationDuckSeconds = 2.5f;
        [SerializeField] private bool spatializerEnabled;
        [SerializeField, Range(0f, 1f)] private float softwareBinauralStrength = 1f;
        [Header("오디오 / Bell Distance Reverb")]
        [SerializeField] private bool bellDistanceReverbEnabled = true;
        [SerializeField, Range(0f, 1f)] private float bellReverbStartDistance01 = 0.35f;
        [SerializeField, Range(0f, 1f)] private float bellReverbMaxWet01 = 0.28f;

        [Header("오디오 / Optional AudioMixer Groups")]
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup bellGroup;
        [SerializeField] private AudioMixerGroup rainWindGroup;
        [SerializeField] private AudioMixerGroup tinnitusGroup;
        [SerializeField] private AudioMixerGroup bossTinnitusGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private AudioMixerGroup interactionGroup;
        [SerializeField] private AudioMixerGroup narrationGroup;

        private readonly Dictionary<FinalDemoCueId, ActiveLoop> _loops = new Dictionary<FinalDemoCueId, ActiveLoop>();
        private float _duckUntilRealtime;
        private string _lastAction = "(idle)";
        private FinalDemoCueSnapshotRecord _recentOneShot;

        private struct FinalDemoCueSnapshotRecord
        {
            public bool valid;
            public FinalDemoCueId cueId;
            public FinalDemoAudioBus bus;
            public Vector3 worldPosition;
            public float volume01;
            public float startedAtRealtime;
        }

        public string LastAction => _lastAction;
        public int ActiveLoopCount => _loops.Count;
        public bool IsNarrationDucking => Time.realtimeSinceStartup < _duckUntilRealtime;
        public bool SpatializerEnabled => spatializerEnabled;
        public bool HasNativeSpatializer => !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());
        public bool UsingSoftwareBinaural => spatializerEnabled && !HasNativeSpatializer;

        private void Update()
        {
            UpdateLoopVolumes();
        }

        public void Initialize(FinalDemoCueLibrary library, Transform listener)
        {
            cueLibrary = library;
            listenerTransform = listener;
        }

        public void SetCueLibrary(FinalDemoCueLibrary library)
        {
            cueLibrary = library;
        }

        public void SetListenerTransform(Transform listener)
        {
            listenerTransform = listener;
        }

        public void SetSpatializerEnabled(bool enabled)
        {
            spatializerEnabled = enabled;
            foreach (ActiveLoop loop in _loops.Values)
            {
                if (loop.source != null && loop.cue != null)
                {
                    ConfigureSourceSpatialization(loop.source, loop.cue);
                }
            }

            _lastAction = enabled
                ? $"HRTF preview enabled ({(UsingSoftwareBinaural ? "software binaural" : AudioSettings.GetSpatializerPluginName())})."
                : "HRTF preview disabled.";
        }

        public AudioSource PlayOneShot(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (!TryResolveCue(cueId, out FinalDemoCueEntry cue, out AudioClip clip))
            {
                _lastAction = $"No clip assigned for {cueId}.";
                return null;
            }

            GameObject sourceObject = new GameObject($"FinalDemoOneShot_{cueId}");
            sourceObject.transform.SetParent(transform);
            sourceObject.transform.position = worldPosition;

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, worldPosition);
            source.pitch = cue.Pitch * Mathf.Max(0.1f, pitchScale);
            source.volume = ResolveVolume(cue, volumeScale);
            source.clip = clip;
            source.loop = false;
            source.Play();
            Destroy(sourceObject, Mathf.Max(0.25f, clip.length / Mathf.Max(0.1f, source.pitch)) + 0.5f);

            if (cue.Bus == FinalDemoAudioBus.Narration)
            {
                BeginNarrationDuck(clip.length);
            }

            _recentOneShot = new FinalDemoCueSnapshotRecord
            {
                valid = true,
                cueId = cueId,
                bus = cue.Bus,
                worldPosition = worldPosition,
                volume01 = source.volume,
                startedAtRealtime = Time.realtimeSinceStartup,
            };

            _lastAction = $"One-shot {cueId} on {cue.Bus}.";
            return source;
        }

        public AudioSource PlayOneShotAttached(FinalDemoCueId cueId, Transform followTarget, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (followTarget == null)
            {
                Vector3 fallbackPosition = listenerTransform != null ? listenerTransform.position : transform.position;
                return PlayOneShot(cueId, fallbackPosition, volumeScale, pitchScale);
            }

            AudioSource source = PlayOneShot(cueId, followTarget.position, volumeScale, pitchScale);
            if (source != null)
            {
                source.transform.SetParent(followTarget, true);
            }

            return source;
        }

        public AudioSource StartLoop(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale = 1f)
        {
            if (!TryResolveCue(cueId, out FinalDemoCueEntry cue, out AudioClip clip))
            {
                _lastAction = $"No loop clip assigned for {cueId}.";
                return null;
            }

            if (_loops.TryGetValue(cueId, out ActiveLoop existingLoop) && existingLoop.source != null)
            {
                existingLoop.source.transform.position = worldPosition;
                existingLoop.targetScale = Mathf.Clamp01(volumeScale);
                existingLoop.stopping = false;
                _lastAction = $"Updated loop {cueId}.";
                return existingLoop.source;
            }

            GameObject sourceObject = new GameObject($"FinalDemoLoop_{cueId}");
            sourceObject.transform.SetParent(transform);
            sourceObject.transform.position = worldPosition;

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            ConfigureSource(source, cue, worldPosition);
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.Play();

            _loops[cueId] = new ActiveLoop
            {
                cue = cue,
                source = source,
                baseVolume = cue.DefaultVolume,
                targetScale = Mathf.Clamp01(volumeScale),
            };

            _lastAction = $"Started loop {cueId} on {cue.Bus}.";
            return source;
        }

        public void StopLoop(FinalDemoCueId cueId)
        {
            if (_loops.TryGetValue(cueId, out ActiveLoop activeLoop))
            {
                activeLoop.stopping = true;
                activeLoop.targetScale = 0f;
                _lastAction = $"Stopping loop {cueId}.";
            }
        }

        public void SetLoopVolumeScale(FinalDemoCueId cueId, float volumeScale)
        {
            if (_loops.TryGetValue(cueId, out ActiveLoop activeLoop))
            {
                activeLoop.targetScale = Mathf.Clamp01(volumeScale);
                activeLoop.stopping = false;
            }
        }

        public void StopAllCues()
        {
            foreach (ActiveLoop activeLoop in _loops.Values)
            {
                if (activeLoop.source != null)
                {
                    Destroy(activeLoop.source.gameObject);
                }
            }

            _loops.Clear();
            _recentOneShot.valid = false;
            _lastAction = "Stopped all audio cues.";
        }

        public void StopAllCuesExcept(params FinalDemoCueId[] preservedLoopIds)
        {
            HashSet<FinalDemoCueId> preserved = new HashSet<FinalDemoCueId>(preservedLoopIds ?? Array.Empty<FinalDemoCueId>());
            List<FinalDemoCueId> removeIds = new List<FinalDemoCueId>();
            foreach (KeyValuePair<FinalDemoCueId, ActiveLoop> pair in _loops)
            {
                if (preserved.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value.source != null)
                {
                    Destroy(pair.Value.source.gameObject);
                }

                removeIds.Add(pair.Key);
            }

            foreach (FinalDemoCueId cueId in removeIds)
            {
                _loops.Remove(cueId);
            }

            _recentOneShot.valid = false;
            _lastAction = preserved.Count > 0 ? "Stopped stage cues and preserved ambience." : "Stopped all audio cues.";
        }

        public string BuildStatusText()
        {
            string hrtfMode = SpatializerEnabled ? (UsingSoftwareBinaural ? "software" : "native") : "off";
            return $"Audio loops={ActiveLoopCount} ducking={IsNarrationDucking} hrtf={hrtfMode} last={LastAction}";
        }

        public float GetCueLengthSeconds(FinalDemoCueId cueId)
        {
            return TryResolveCue(cueId, out FinalDemoCueEntry cue, out AudioClip clip) && clip != null
                ? clip.length / Mathf.Max(0.1f, cue.Pitch)
                : 0f;
        }

        public void FillActiveCueSnapshots(List<FinalDemoAudioCueSnapshot> destination, float recentOneShotLifetimeSeconds = 1.25f)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            foreach (KeyValuePair<FinalDemoCueId, ActiveLoop> pair in _loops)
            {
                ActiveLoop activeLoop = pair.Value;
                if (activeLoop.source == null || activeLoop.cue == null)
                {
                    continue;
                }

                destination.Add(new FinalDemoAudioCueSnapshot(
                    pair.Key,
                    activeLoop.cue.Bus,
                    activeLoop.source.transform.position,
                    activeLoop.source.volume,
                    true,
                    0f));
            }

            if (_recentOneShot.valid)
            {
                float ageSeconds = Time.realtimeSinceStartup - _recentOneShot.startedAtRealtime;
                if (ageSeconds <= Mathf.Max(0.05f, recentOneShotLifetimeSeconds))
                {
                    destination.Add(new FinalDemoAudioCueSnapshot(
                        _recentOneShot.cueId,
                        _recentOneShot.bus,
                        _recentOneShot.worldPosition,
                        _recentOneShot.volume01,
                        false,
                        ageSeconds));
                }
            }
        }

        private void UpdateLoopVolumes()
        {
            if (_loops.Count == 0)
            {
                return;
            }

            List<FinalDemoCueId> finished = null;
            foreach (KeyValuePair<FinalDemoCueId, ActiveLoop> pair in _loops)
            {
                ActiveLoop activeLoop = pair.Value;
                if (activeLoop.source == null)
                {
                    finished ??= new List<FinalDemoCueId>();
                    finished.Add(pair.Key);
                    continue;
                }

                float targetVolume = activeLoop.stopping
                    ? 0f
                    : ResolveVolume(activeLoop.cue, activeLoop.targetScale);
                activeLoop.source.volume = Mathf.MoveTowards(activeLoop.source.volume, targetVolume, Time.unscaledDeltaTime * 1.8f);

                if (activeLoop.stopping && activeLoop.source.volume <= 0.001f)
                {
                    Destroy(activeLoop.source.gameObject);
                    finished ??= new List<FinalDemoCueId>();
                    finished.Add(pair.Key);
                }
            }

            if (finished == null)
            {
                return;
            }

            foreach (FinalDemoCueId cueId in finished)
            {
                _loops.Remove(cueId);
            }
        }

        private bool TryResolveCue(FinalDemoCueId cueId, out FinalDemoCueEntry cue, out AudioClip clip)
        {
            cue = null;
            clip = null;
            if (cueLibrary == null || !cueLibrary.TryGetCue(cueId, out cue))
            {
                return false;
            }

            clip = cue.AudioClip;
            if (clip == null && cue.AlternateClips != null)
            {
                foreach (AudioClip alternateClip in cue.AlternateClips)
                {
                    if (alternateClip != null)
                    {
                        clip = alternateClip;
                        break;
                    }
                }
            }

            return clip != null;
        }

        private void ConfigureSource(AudioSource source, FinalDemoCueEntry cue, Vector3 worldPosition)
        {
            source.transform.position = worldPosition;
            source.playOnAwake = false;
            source.spatialBlend = cue.Spatialized ? 1f : 0f;
            source.spatialize = spatializerEnabled && cue.Spatialized;
            source.spatializePostEffects = false;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.pitch = cue.Pitch;
            source.outputAudioMixerGroup = ResolveMixerGroup(cue.Bus);
            ConfigureSourceSpatialization(source, cue);

            if (cue.LowPassCutoff < 21999f)
            {
                AudioLowPassFilter lowPassFilter = source.gameObject.AddComponent<AudioLowPassFilter>();
                lowPassFilter.cutoffFrequency = cue.LowPassCutoff;
            }

            if (cue.HighPassCutoff > 10.1f)
            {
                AudioHighPassFilter highPassFilter = source.gameObject.AddComponent<AudioHighPassFilter>();
                highPassFilter.cutoffFrequency = cue.HighPassCutoff;
            }

            ApplyBellDistanceReverb(source, cue, worldPosition);
        }

        private void ApplyBellDistanceReverb(AudioSource source, FinalDemoCueEntry cue, Vector3 worldPosition)
        {
            if (!bellDistanceReverbEnabled || cue.Bus != FinalDemoAudioBus.Bell || listenerTransform == null)
            {
                return;
            }

            float distance = Vector3.Distance(listenerTransform.position, worldPosition);
            float distance01 = Mathf.InverseLerp(cue.MinDistance, cue.MaxDistance, distance);
            float wet01 = Mathf.InverseLerp(bellReverbStartDistance01, 1f, distance01) * bellReverbMaxWet01;
            if (wet01 <= 0.001f)
            {
                return;
            }

            AudioReverbFilter reverb = source.gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.dryLevel = 0f;
            reverb.room = Mathf.Lerp(-10000f, -4200f, wet01);
            reverb.roomHF = -1400f;
            reverb.decayTime = Mathf.Lerp(0.75f, 1.45f, wet01);
            reverb.decayHFRatio = 0.45f;
            reverb.reflectionsLevel = Mathf.Lerp(-10000f, -5200f, wet01);
            reverb.reflectionsDelay = 0.026f;
            reverb.reverbLevel = Mathf.Lerp(-10000f, -3800f, wet01);
            reverb.reverbDelay = 0.052f;
            reverb.diffusion = 72f;
            reverb.density = 68f;
        }

        private void ConfigureSourceSpatialization(AudioSource source, FinalDemoCueEntry cue)
        {
            if (source == null || cue == null)
            {
                return;
            }

            bool eligible = cue.Spatialized && IsBinauralPreviewBus(cue.Bus);
            BellRingerBinauralSpatializer binaural = source.GetComponent<BellRingerBinauralSpatializer>();
            if (spatializerEnabled && eligible && UsingSoftwareBinaural)
            {
                source.spatialBlend = 0f;
                source.spatialize = false;
                if (binaural == null)
                {
                    binaural = source.gameObject.AddComponent<BellRingerBinauralSpatializer>();
                }

                binaural.Configure(source, listenerTransform, true, softwareBinauralStrength);
                return;
            }

            if (binaural != null)
            {
                binaural.Configure(source, listenerTransform, false, softwareBinauralStrength);
            }

            source.spatialBlend = cue.Spatialized ? 1f : 0f;
            source.spatialize = spatializerEnabled && eligible && HasNativeSpatializer;
            source.spatializePostEffects = false;
        }

        private static bool IsBinauralPreviewBus(FinalDemoAudioBus bus)
        {
            return bus == FinalDemoAudioBus.Bell ||
                   bus == FinalDemoAudioBus.Tinnitus ||
                   bus == FinalDemoAudioBus.BossTinnitus;
        }

        private AudioMixerGroup ResolveMixerGroup(FinalDemoAudioBus bus)
        {
            return bus switch
            {
                FinalDemoAudioBus.Bell => bellGroup != null ? bellGroup : masterGroup,
                FinalDemoAudioBus.RainWind => rainWindGroup != null ? rainWindGroup : masterGroup,
                FinalDemoAudioBus.Tinnitus => tinnitusGroup != null ? tinnitusGroup : masterGroup,
                FinalDemoAudioBus.BossTinnitus => bossTinnitusGroup != null ? bossTinnitusGroup : masterGroup,
                FinalDemoAudioBus.Ambience => ambienceGroup != null ? ambienceGroup : masterGroup,
                FinalDemoAudioBus.Interaction => interactionGroup != null ? interactionGroup : masterGroup,
                FinalDemoAudioBus.Narration => narrationGroup != null ? narrationGroup : masterGroup,
                _ => masterGroup,
            };
        }

        private float ResolveVolume(FinalDemoCueEntry cue, float volumeScale)
        {
            float volume = cue.DefaultVolume * Mathf.Clamp01(volumeScale) * masterVolume * ResolveBusGain(cue.Bus);
            if (IsNarrationDucking && (cue.Bus == FinalDemoAudioBus.RainWind || cue.Bus == FinalDemoAudioBus.Tinnitus || cue.Bus == FinalDemoAudioBus.BossTinnitus))
            {
                volume *= narrationDuckMultiplier;
            }

            return Mathf.Clamp01(volume);
        }

        private float ResolveBusGain(FinalDemoAudioBus bus)
        {
            return bus switch
            {
                FinalDemoAudioBus.Bell => bellBusGain,
                FinalDemoAudioBus.RainWind => rainWindBusGain,
                FinalDemoAudioBus.Tinnitus => tinnitusBusGain,
                FinalDemoAudioBus.BossTinnitus => bossTinnitusBusGain,
                FinalDemoAudioBus.Ambience => ambienceBusGain,
                FinalDemoAudioBus.Interaction => interactionBusGain,
                FinalDemoAudioBus.Narration => narrationBusGain,
                _ => 1f,
            };
        }

        private void BeginNarrationDuck(float clipLength)
        {
            _duckUntilRealtime = Mathf.Max(_duckUntilRealtime, Time.realtimeSinceStartup + Mathf.Max(defaultNarrationDuckSeconds, clipLength + 0.1f));
        }
    }
}
