using System;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [CreateAssetMenu(menuName = "Bell Ringer/Final Demo Cue Library", fileName = "FinalDemoCueLibrary")]
    public sealed class FinalDemoCueLibrary : ScriptableObject
    {
        [SerializeField] private FinalDemoCueEntry[] cues =
        {
            new FinalDemoCueEntry(FinalDemoCueId.OpeningAmbience, "Opening ambience"),
            new FinalDemoCueEntry(FinalDemoCueId.OpeningCloseBell, "Close bell"),
            new FinalDemoCueEntry(FinalDemoCueId.BellOrbit, "Bell orbit"),
            new FinalDemoCueEntry(FinalDemoCueId.BellFollowBell, "Bell follow cue"),
            new FinalDemoCueEntry(FinalDemoCueId.RainWindMasking, "Rain and wind masking"),
            new FinalDemoCueEntry(FinalDemoCueId.BellGazeLoop, "Bell gaze loop"),
            new FinalDemoCueEntry(FinalDemoCueId.BellAcquisition, "Bell acquisition"),
            new FinalDemoCueEntry(FinalDemoCueId.GeneralTinnitus, "General tinnitus"),
            new FinalDemoCueEntry(FinalDemoCueId.TinnitusResolve, "Tinnitus resolve"),
            new FinalDemoCueEntry(FinalDemoCueId.BossTinnitus, "Boss tinnitus"),
            new FinalDemoCueEntry(FinalDemoCueId.BossResolve, "Boss resolve"),
            new FinalDemoCueEntry(FinalDemoCueId.ForestEnding, "Forest ending"),
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
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.55f;
        [SerializeField] private bool loop;
        [SerializeField] private bool stopOnStageExit = true;

        public FinalDemoCueEntry(FinalDemoCueId id, string operatorLabel)
        {
            this.id = id;
            this.operatorLabel = operatorLabel;
        }

        public FinalDemoCueId Id => id;
        public string OperatorLabel => string.IsNullOrWhiteSpace(operatorLabel) ? id.ToString() : operatorLabel;
        public AudioClip AudioClip => audioClip;
        public float DefaultVolume => defaultVolume;
        public bool Loop => loop;
        public bool StopOnStageExit => stopOnStageExit;
    }
}
