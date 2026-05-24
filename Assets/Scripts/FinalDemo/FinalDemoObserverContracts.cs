using UnityEngine;

namespace BellRinger.FinalDemo
{
    public readonly struct FinalDemoAudioCueSnapshot
    {
        public readonly FinalDemoCueId CueId;
        public readonly FinalDemoAudioBus Bus;
        public readonly Vector3 WorldPosition;
        public readonly float Volume01;
        public readonly bool IsLoop;
        public readonly float AgeSeconds;

        public FinalDemoAudioCueSnapshot(
            FinalDemoCueId cueId,
            FinalDemoAudioBus bus,
            Vector3 worldPosition,
            float volume01,
            bool isLoop,
            float ageSeconds)
        {
            CueId = cueId;
            Bus = bus;
            WorldPosition = worldPosition;
            Volume01 = Mathf.Clamp01(volume01);
            IsLoop = isLoop;
            AgeSeconds = Mathf.Max(0f, ageSeconds);
        }
    }
}
