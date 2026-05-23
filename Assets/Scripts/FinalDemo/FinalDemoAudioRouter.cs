using System.Collections.Generic;
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

        [Header("Optional AudioMixer Groups")]
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

        public string LastAction => _lastAction;
        public int ActiveLoopCount => _loops.Count;
        public bool IsNarrationDucking => Time.realtimeSinceStartup < _duckUntilRealtime;
        public bool SpatializerEnabled => spatializerEnabled;

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
                    loop.source.spatialize = spatializerEnabled && loop.cue.Spatialized;
                }
            }

            _lastAction = $"Spatializer preview {(enabled ? "enabled" : "disabled")}.";
        }

        public AudioSource PlayOneShot(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale = 1f)
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
            source.volume = ResolveVolume(cue, volumeScale);
            source.PlayOneShot(clip, 1f);
            Destroy(sourceObject, Mathf.Max(0.25f, clip.length / Mathf.Max(0.1f, cue.Pitch)) + 0.5f);

            if (cue.Bus == FinalDemoAudioBus.Narration)
            {
                BeginNarrationDuck(clip.length);
            }

            _lastAction = $"One-shot {cueId} on {cue.Bus}.";
            return source;
        }

        public void StartLoop(FinalDemoCueId cueId, Vector3 worldPosition, float volumeScale = 1f)
        {
            if (!TryResolveCue(cueId, out FinalDemoCueEntry cue, out AudioClip clip))
            {
                _lastAction = $"No loop clip assigned for {cueId}.";
                return;
            }

            if (_loops.TryGetValue(cueId, out ActiveLoop existingLoop) && existingLoop.source != null)
            {
                existingLoop.source.transform.position = worldPosition;
                existingLoop.targetScale = Mathf.Clamp01(volumeScale);
                existingLoop.stopping = false;
                _lastAction = $"Updated loop {cueId}.";
                return;
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
            _lastAction = "Stopped all audio cues.";
        }

        public string BuildStatusText()
        {
            return $"Audio loops={ActiveLoopCount} ducking={IsNarrationDucking} hrtf={SpatializerEnabled} last={LastAction}";
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
