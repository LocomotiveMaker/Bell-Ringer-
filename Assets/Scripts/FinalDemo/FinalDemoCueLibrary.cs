using System;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [CreateAssetMenu(menuName = "Bell Ringer/Final Demo Cue Library", fileName = "FinalDemoCueLibrary")]
    public sealed class FinalDemoCueLibrary : ScriptableObject
    {
        [SerializeField] private FinalDemoCueEntry[] cues =
        {
            new FinalDemoCueEntry(FinalDemoCueId.BellOpeningOrbit, "Bell opening/orbit", FinalDemoAudioBus.Bell, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellMovementTexture, "Bell movement texture", FinalDemoAudioBus.Bell, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellDistantCall, "Bell distant call", FinalDemoAudioBus.Bell, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellStrongAssist, "Bell strong assist", FinalDemoAudioBus.Bell, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellPadShakeResponse, "Bell pad shake response", FinalDemoAudioBus.Bell, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellGazeSuccess, "Bell gaze success", FinalDemoAudioBus.Bell, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BellAcquisition, "Bell acquisition", FinalDemoAudioBus.Interaction, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.RainLightBed, "Rain light bed", FinalDemoAudioBus.RainWind, true, false),
            new FinalDemoCueEntry(FinalDemoCueId.RainCloseDrops, "Rain close drops", FinalDemoAudioBus.RainWind, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.RainStrongBed, "Rain strong bed", FinalDemoAudioBus.RainWind, true, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrFollowBell, "Narration follow bell", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrLookBell, "Narration look bell", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusLongGlitch, "Tinnitus long glitch", FinalDemoAudioBus.Tinnitus, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusBurst, "Tinnitus burst", FinalDemoAudioBus.Tinnitus, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusPoseLock, "Tinnitus pose lock", FinalDemoAudioBus.Interaction, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusPoseLost, "Tinnitus pose lost", FinalDemoAudioBus.Interaction, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusHealingLoop, "Tinnitus healing loop", FinalDemoAudioBus.Tinnitus, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusResolve, "Tinnitus resolve", FinalDemoAudioBus.Interaction, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BossBasePulse, "Boss base pulse", FinalDemoAudioBus.BossTinnitus, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.BossGlitchBurst, "Boss glitch burst", FinalDemoAudioBus.BossTinnitus, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BossWeakpointMove, "Boss weakpoint move", FinalDemoAudioBus.BossTinnitus, true, true),
            new FinalDemoCueEntry(FinalDemoCueId.BossHit, "Boss hit", FinalDemoAudioBus.Interaction, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.BossDefeatRise, "Boss defeat rise", FinalDemoAudioBus.BossTinnitus, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.BossDefeatAir, "Boss defeat air", FinalDemoAudioBus.Ambience, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.TransitionSoftCut, "Transition soft cut", FinalDemoAudioBus.Interaction, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.ForestBed, "Forest bed", FinalDemoAudioBus.Ambience, true, false),
            new FinalDemoCueEntry(FinalDemoCueId.ForestBell, "Forest bell", FinalDemoAudioBus.Bell, false, true),
            new FinalDemoCueEntry(FinalDemoCueId.NarrPadShakeAssist, "Narration pad shake assist", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrFindTinnitusPose, "Narration find tinnitus pose", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrHoldPose, "Narration hold pose", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrBossTrack, "Narration boss track", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrFaceForwardWait, "Narration face forward wait", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrRainFocusBell, "Narration rain focus bell", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrBellInHand, "Narration bell in hand", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrApproachTinnitus, "Narration approach tinnitus", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrFindSoundOrigin, "Narration find sound origin", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.OpeningAmbienceBed, "Opening ambience bed", FinalDemoAudioBus.Ambience, true, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrBellEscaped, "Narration bell escaped", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrNoiseStillExists, "Narration noise still exists", FinalDemoAudioBus.Narration, false, false),
            new FinalDemoCueEntry(FinalDemoCueId.NarrBossAhead, "Narration boss ahead", FinalDemoAudioBus.Narration, false, false),
        };

        public FinalDemoCueEntry[] Cues => cues;

        public bool TryGetCue(FinalDemoCueId id, out FinalDemoCueEntry cue)
        {
            if (cues != null)
            {
                foreach (FinalDemoCueEntry candidate in cues)
                {
                    if (candidate != null && candidate.Id == id)
                    {
                        cue = candidate;
                        return true;
                    }
                }
            }

            cue = null;
            return false;
        }

        public AudioClip ResolveAudioClip(FinalDemoCueId id)
        {
            return TryGetCue(id, out FinalDemoCueEntry cue) ? cue.AudioClip : null;
        }
    }

    [Serializable]
    public sealed class FinalDemoCueEntry
    {
        [SerializeField] private FinalDemoCueId id;
        [SerializeField] private string operatorLabel;
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private AudioClip[] alternateClips;
        [SerializeField] private FinalDemoAudioBus bus = FinalDemoAudioBus.Master;
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.55f;
        [SerializeField] private bool loop;
        [SerializeField] private bool spatialized = true;
        [SerializeField] private float pitch = 1f;
        [SerializeField] private float minDistance = 0.6f;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float lowPassCutoff = 22000f;
        [SerializeField] private float highPassCutoff = 10f;
        [SerializeField] private FinalDemoLightBinding lightBinding;
        [SerializeField] private FinalDemoHapticBinding hapticBinding;
        [SerializeField] private bool stopOnStageExit = true;

        public FinalDemoCueEntry(FinalDemoCueId id, string operatorLabel)
            : this(id, operatorLabel, FinalDemoAudioBus.Master, false, true)
        {
        }

        public FinalDemoCueEntry(FinalDemoCueId id, string operatorLabel, FinalDemoAudioBus bus, bool loop, bool spatialized)
        {
            this.id = id;
            this.operatorLabel = operatorLabel;
            this.bus = bus;
            this.loop = loop;
            this.spatialized = spatialized;
        }

        public FinalDemoCueId Id => id;
        public string OperatorLabel => string.IsNullOrWhiteSpace(operatorLabel) ? id.ToString() : operatorLabel;
        public AudioClip AudioClip => audioClip;
        public AudioClip[] AlternateClips => alternateClips;
        public FinalDemoAudioBus Bus => bus;
        public float DefaultVolume => defaultVolume;
        public bool Loop => loop;
        public bool Spatialized => spatialized;
        public float Pitch => Mathf.Clamp(pitch, 0.1f, 3f);
        public float MinDistance => Mathf.Max(0.01f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance + 0.01f, maxDistance);
        public float LowPassCutoff => Mathf.Clamp(lowPassCutoff, 10f, 22000f);
        public float HighPassCutoff => Mathf.Clamp(highPassCutoff, 10f, 22000f);
        public FinalDemoLightBinding LightBinding => lightBinding;
        public FinalDemoHapticBinding HapticBinding => hapticBinding;
        public bool StopOnStageExit => stopOnStageExit;
    }
}
