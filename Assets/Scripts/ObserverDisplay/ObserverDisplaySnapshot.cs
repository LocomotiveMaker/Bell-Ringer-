using BellRinger.FinalDemo;
using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    public enum ObserverTrackingHealth
    {
        Missing = 0,
        Stale = 1,
        Fresh = 2,
    }

    public sealed class ObserverDisplaySnapshot
    {
        public FinalDemoStage stage;
        public string objectiveLabel;
        public float objectiveProgress01;
        public string activeSoundFocusLabel;
        public ObserverTrackingHealth headTracking;
        public ObserverTrackingHealth padCameraTracking;
        public ObserverTrackingHealth padImuTracking;
        public ObserverTrackingHealth ledTracking;
        public ObserverTrackingHealth hapticsTracking;
        public Vector3 playerWorldPosition;
        public bool hasPadPose;
        public Vector3 padWorldPosition;
        public Quaternion padWorldRotation = Quaternion.identity;
        public bool hasBell;
        public Vector3 bellWorldPosition;
        public bool hasTinnitus;
        public Vector3 tinnitusWorldPosition;
        public bool hasBoss;
        public Vector3 bossWorldPosition;
        public bool hasForestBell;
        public Vector3 forestBellWorldPosition;
    }
}
