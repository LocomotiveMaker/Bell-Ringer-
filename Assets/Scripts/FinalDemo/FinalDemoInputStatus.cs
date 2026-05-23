using System.Text;
using BellRinger.Gameplay;
using BellRinger.Hardware;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoInputStatus : MonoBehaviour
    {
        [SerializeField] private HardwareBridge hardwareBridge;
        [SerializeField] private HeadImuReceiver headImuReceiver;
        [SerializeField] private HeadTiltInputProvider headTiltInputProvider;
        [SerializeField] private PadTrackingReceiver padTrackingReceiver;
        [SerializeField] private PadImuReceiver padImuReceiver;
        [SerializeField] private PadPoseProvider padPoseProvider;
        [SerializeField] private FinalDemoAudioRouter audioRouter;
        [SerializeField] private FinalDemoLightRouter lightRouter;
        [SerializeField] private FinalDemoHapticRouter hapticRouter;

        public HardwareBridge HardwareBridge => hardwareBridge;
        public HeadImuReceiver HeadImuReceiver => headImuReceiver;
        public HeadTiltInputProvider HeadTiltInputProvider => headTiltInputProvider;
        public PadTrackingReceiver PadTrackingReceiver => padTrackingReceiver;
        public PadImuReceiver PadImuReceiver => padImuReceiver;
        public PadPoseProvider PadPoseProvider => padPoseProvider;
        public FinalDemoAudioRouter AudioRouter => audioRouter;
        public FinalDemoLightRouter LightRouter => lightRouter;
        public FinalDemoHapticRouter HapticRouter => hapticRouter;
        public bool HardwareConnected => hardwareBridge != null && hardwareBridge.IsConnected;
        public bool HeadFresh => headTiltInputProvider != null && headTiltInputProvider.HasFreshSample;
        public bool PadCameraFresh => padTrackingReceiver != null && padTrackingReceiver.HasFreshDetection;
        public bool PadImuFresh => padImuReceiver != null && padImuReceiver.HasFreshSample;
        public bool PadPoseResolved => padPoseProvider != null && padPoseProvider.HasResolvedRotation;
        public bool HasGamepad => Gamepad.current != null || Gamepad.all.Count > 0;

        private void Awake()
        {
            RefreshReferences();
        }

        private void Update()
        {
            RefreshMissingReferences();
        }

        public void RefreshReferences()
        {
            hardwareBridge = HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>();
            headImuReceiver = FindFirstObjectByType<HeadImuReceiver>();
            headTiltInputProvider = FindFirstObjectByType<HeadTiltInputProvider>();
            padTrackingReceiver = FindFirstObjectByType<PadTrackingReceiver>();
            padImuReceiver = FindFirstObjectByType<PadImuReceiver>();
            padPoseProvider = FindFirstObjectByType<PadPoseProvider>();
            audioRouter = FindFirstObjectByType<FinalDemoAudioRouter>();
            lightRouter = FindFirstObjectByType<FinalDemoLightRouter>();
            hapticRouter = FindFirstObjectByType<FinalDemoHapticRouter>();
        }

        public string BuildStatusText(FinalDemoDirector director)
        {
            RefreshMissingReferences();

            StringBuilder builder = new StringBuilder(900);
            if (director != null)
            {
                builder.AppendLine($"Stage: {director.CurrentStage} ({director.StageIndex + 1}/{director.StageCount})");
                builder.AppendLine($"Elapsed: {director.StageElapsedSeconds:0.00}s  MovementLocked: {director.PlayerMovementLocked}");
                builder.AppendLine($"Assist: {director.AssistLevel}  HRTF preview: {(director.HrtfPreviewEnabled ? "On" : "Off")}");
            }

            builder.AppendLine($"LED/Hardware: {(HardwareConnected ? "Connected" : "Not connected")} {(hardwareBridge != null ? hardwareBridge.ActivePortName : "(none)")}");
            builder.AppendLine($"Head: connected={Bool(headImuReceiver != null && headImuReceiver.IsConnected)} fresh={Bool(HeadFresh)} port={(headImuReceiver != null ? headImuReceiver.ActivePortName : "(none)")}");
            if (headTiltInputProvider != null)
            {
                builder.AppendLine($"Head virtual yaw/pitch: {headTiltInputProvider.VirtualYawDegrees:0.0}, {headTiltInputProvider.VirtualPitchDegrees:0.0}");
                builder.AppendLine($"Head physical y/p/r: {headTiltInputProvider.PhysicalYawDegrees:0.0}, {headTiltInputProvider.PhysicalPitchDegrees:0.0}, {headTiltInputProvider.PhysicalRollDegrees:0.0}");
            }

            builder.AppendLine($"Pad camera: detected={Bool(PadCameraFresh)} ids={(padTrackingReceiver != null ? padTrackingReceiver.MarkerIds : string.Empty)} fps={(padTrackingReceiver != null ? padTrackingReceiver.FramesPerSecond : 0f):0.0}");
            if (padTrackingReceiver != null)
            {
                Vector3 padPosition = padTrackingReceiver.ApproximateCameraSpacePosition;
                builder.AppendLine($"Pad cam pos: {padPosition.x:0.000}, {padPosition.y:0.000}, {padPosition.z:0.000}m");
                builder.AppendLine($"Pad camera yaw: {Bool(padTrackingReceiver.HasFreshCameraYaw)} {padTrackingReceiver.CameraYawDegrees:0.0}");
            }

            builder.AppendLine($"Pad IMU: connected={Bool(padImuReceiver != null && padImuReceiver.IsConnected)} fresh={Bool(PadImuFresh)} port={(padImuReceiver != null ? padImuReceiver.ActivePortName : "(none)")}");
            if (padPoseProvider != null)
            {
                builder.AppendLine($"Pad resolved y/p/r: {padPoseProvider.ResolvedYawDegrees:0.0}, {padPoseProvider.ResolvedPitchDegrees:0.0}, {padPoseProvider.ResolvedRollDegrees:0.0}");
                builder.AppendLine($"Pad yaw source: cam={Bool(padPoseProvider.UsingCameraYaw)} imu={Bool(padPoseProvider.UsingImuYawFallback)} held={Bool(padPoseProvider.UsingHeldYaw)}");
            }

            builder.AppendLine($"Gamepad: {(HasGamepad ? DescribeGamepad() : "(none)")}");
            builder.AppendLine(audioRouter != null ? audioRouter.BuildStatusText() : "Audio router: missing");
            builder.AppendLine(lightRouter != null ? $"Light last: {lightRouter.LastAction} priority={lightRouter.HeldPriority}" : "Light router: missing");
            builder.AppendLine(hapticRouter != null ? $"Haptics active={Bool(hapticRouter.HapticsActive)} continuous={Bool(hapticRouter.Continuous)} last={hapticRouter.LastAction}" : "Haptic router: missing");
            return builder.ToString();
        }

        private void RefreshMissingReferences()
        {
            if (hardwareBridge == null || headImuReceiver == null || headTiltInputProvider == null ||
                padTrackingReceiver == null || padImuReceiver == null || padPoseProvider == null ||
                audioRouter == null || lightRouter == null || hapticRouter == null)
            {
                RefreshReferences();
            }
        }

        private static string DescribeGamepad()
        {
            Gamepad gamepad = Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null);
            return gamepad == null ? "(none)" : $"{gamepad.displayName} / {gamepad.description.interfaceName}";
        }

        private static string Bool(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
