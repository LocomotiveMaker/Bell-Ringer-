using BellRinger.Gameplay;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoSceneReferences : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform hardwareRoot;
        [SerializeField] private Transform systemsRoot;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Transform padRoot;
        [SerializeField] private Transform bellRoot;
        [SerializeField] private Transform rainRoot;
        [SerializeField] private Transform tinnitusRoot;
        [SerializeField] private Transform bossRoot;
        [SerializeField] private Transform forestRoot;
        [SerializeField] private Transform uiRoot;

        [Header("Gameplay Visuals")]
        [SerializeField] private Transform padVisual;
        [SerializeField] private Transform bellVisual;
        [SerializeField] private Transform tinnitusOneVisual;
        [SerializeField] private Transform tinnitusTwoVisual;
        [SerializeField] private Transform bossVisual;
        [SerializeField] private Transform rainVolume;
        [SerializeField] private Transform forestSet;

        [Header("Authoring Paths")]
        [SerializeField] private Transform bellOrbitPath;
        [SerializeField] private Transform bellFollowPath;
        [SerializeField] private Transform bellGazePath;
        [SerializeField] private Transform bossWeakpointPathOne;
        [SerializeField] private Transform bossWeakpointPathTwo;
        [SerializeField] private Transform bossWeakpointPathThree;

        [Header("Pose Markers")]
        [SerializeField] private Transform tinnitusOneHealPose;
        [SerializeField] private Transform tinnitusTwoHealPose;
        [SerializeField] private Transform bossPoseOne;
        [SerializeField] private Transform bossPoseTwo;
        [SerializeField] private Transform bossPoseThree;

        [Header("Input")]
        [SerializeField] private PadPoseProvider padPoseProvider;

        public Transform PlayerRoot => playerRoot;
        public Camera PlayerCamera => playerCamera;
        public Transform HardwareRoot => hardwareRoot;
        public Transform SystemsRoot => systemsRoot;
        public Transform WorldRoot => worldRoot;
        public Transform PadRoot => padRoot;
        public Transform BellRoot => bellRoot;
        public Transform RainRoot => rainRoot;
        public Transform TinnitusRoot => tinnitusRoot;
        public Transform BossRoot => bossRoot;
        public Transform ForestRoot => forestRoot;
        public Transform UiRoot => uiRoot;
        public Transform PadVisual => padVisual;
        public Transform BellVisual => bellVisual;
        public Transform TinnitusOneVisual => tinnitusOneVisual;
        public Transform TinnitusTwoVisual => tinnitusTwoVisual;
        public Transform BossVisual => bossVisual;
        public Transform RainVolume => rainVolume;
        public Transform ForestSet => forestSet;
        public Transform BellOrbitPath => bellOrbitPath;
        public Transform BellFollowPath => bellFollowPath;
        public Transform BellGazePath => bellGazePath;
        public Transform BossWeakpointPathOne => bossWeakpointPathOne;
        public Transform BossWeakpointPathTwo => bossWeakpointPathTwo;
        public Transform BossWeakpointPathThree => bossWeakpointPathThree;
        public Transform TinnitusOneHealPose => tinnitusOneHealPose;
        public Transform TinnitusTwoHealPose => tinnitusTwoHealPose;
        public Transform BossPoseOne => bossPoseOne;
        public Transform BossPoseTwo => bossPoseTwo;
        public Transform BossPoseThree => bossPoseThree;
        public PadPoseProvider PadPoseProvider => padPoseProvider;
        public FinalDemoAuthoringPath BellOrbitAuthoringPath => bellOrbitPath != null ? bellOrbitPath.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoAuthoringPath BellFollowAuthoringPath => bellFollowPath != null ? bellFollowPath.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoAuthoringPath BellGazeAuthoringPath => bellGazePath != null ? bellGazePath.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoAuthoringPath BossWeakpointPathOneAuthoring => bossWeakpointPathOne != null ? bossWeakpointPathOne.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoAuthoringPath BossWeakpointPathTwoAuthoring => bossWeakpointPathTwo != null ? bossWeakpointPathTwo.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoAuthoringPath BossWeakpointPathThreeAuthoring => bossWeakpointPathThree != null ? bossWeakpointPathThree.GetComponent<FinalDemoAuthoringPath>() : null;
        public FinalDemoPoseAuthoringMarker TinnitusOneHealMarker => tinnitusOneHealPose != null ? tinnitusOneHealPose.GetComponent<FinalDemoPoseAuthoringMarker>() : null;
        public FinalDemoPoseAuthoringMarker TinnitusTwoHealMarker => tinnitusTwoHealPose != null ? tinnitusTwoHealPose.GetComponent<FinalDemoPoseAuthoringMarker>() : null;
        public FinalDemoPoseAuthoringMarker BossPoseOneMarker => bossPoseOne != null ? bossPoseOne.GetComponent<FinalDemoPoseAuthoringMarker>() : null;
        public FinalDemoPoseAuthoringMarker BossPoseTwoMarker => bossPoseTwo != null ? bossPoseTwo.GetComponent<FinalDemoPoseAuthoringMarker>() : null;
        public FinalDemoPoseAuthoringMarker BossPoseThreeMarker => bossPoseThree != null ? bossPoseThree.GetComponent<FinalDemoPoseAuthoringMarker>() : null;
    }
}
