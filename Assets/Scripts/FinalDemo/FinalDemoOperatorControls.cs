using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoOperatorControls : MonoBehaviour
    {
        [SerializeField] private FinalDemoDirector director;
        [SerializeField] private FinalDemoInputStatus inputStatus;
        [SerializeField] private bool showOperatorControls = true;
        [SerializeField] private bool showInputStatus = true;
        [SerializeField] private Vector2 operatorPanelPosition = new Vector2(10f, 10f);
        [SerializeField] private Vector2 statusPanelSize = new Vector2(455f, 430f);

        private Rect _operatorRect = new Rect(10f, 10f, 520f, 720f);
        private Rect _statusRect = new Rect(0f, 10f, 455f, 430f);
        private Vector2 _statusScroll;

        public void Initialize(FinalDemoDirector newDirector, FinalDemoInputStatus newInputStatus)
        {
            director = newDirector;
            inputStatus = newInputStatus;
        }

        private void Awake()
        {
            director ??= GetComponent<FinalDemoDirector>() ?? FindFirstObjectByType<FinalDemoDirector>();
            inputStatus ??= GetComponent<FinalDemoInputStatus>() ?? FindFirstObjectByType<FinalDemoInputStatus>();
        }

        private void OnGUI()
        {
            if (director == null)
            {
                return;
            }

            _operatorRect.x = operatorPanelPosition.x;
            _operatorRect.y = operatorPanelPosition.y;
            if (showOperatorControls)
            {
                _operatorRect = GUI.Window(7101, _operatorRect, DrawOperatorWindow, "Final Demo Operator");
            }

            if (showInputStatus)
            {
                _statusRect.width = statusPanelSize.x;
                _statusRect.height = statusPanelSize.y;
                _statusRect.x = Mathf.Max(10f, Screen.width - _statusRect.width - 10f);
                _statusRect.y = 10f;
                _statusRect = GUI.Window(7102, _statusRect, DrawStatusWindow, "Final Demo Input Status");
            }
        }

        private void DrawOperatorWindow(int windowId)
        {
            GUILayout.Label(director.BuildStageSummary());
            GUILayout.Label($"Elapsed {director.StageElapsedSeconds:0.00}s");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Demo"))
            {
                director.StartDemo();
            }

            if (GUILayout.Button("Force Next"))
            {
                director.ForceNextStage();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Complete Current"))
            {
                director.ForceCompleteCurrentObjective();
            }

            if (GUILayout.Button("Reset Stage"))
            {
                director.ResetCurrentStage();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset To Preflight"))
            {
                director.ResetToPreflight();
            }

            if (GUILayout.Button(director.PlayerMovementLocked ? "Unlock Movement" : "Lock Movement"))
            {
                director.ToggleMovementLock();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recenter Head"))
            {
                director.RecenterHead();
            }

            if (GUILayout.Button("Recenter Pad"))
            {
                director.RecenterPad();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Assist: {director.AssistLevel}"))
            {
                director.CycleAssistLevel();
            }

            if (GUILayout.Button($"HRTF Preview: {(director.HrtfPreviewEnabled ? "On" : "Off")}"))
            {
                director.ToggleHrtfPreview();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Stop All Outputs"))
            {
                director.StopAllOutputs();
            }

            GUILayout.Space(8f);
            DrawAudioTestSection();
            DrawLightHapticTestSection();

            GUI.DragWindow();
        }

        private void DrawAudioTestSection()
        {
            GUILayout.Label("Bundle 2 Audio Tests");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell OneShot"))
            {
                director.AudioRouter?.PlayOneShot(FinalDemoCueId.BellDistantCall, ResolveFrontPosition(2.2f), 1f);
            }

            if (GUILayout.Button("Narr Duck"))
            {
                director.AudioRouter?.PlayOneShot(FinalDemoCueId.NarrFollowBell, ResolveFrontPosition(1.1f), 1f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Rain Loop"))
            {
                director.AudioRouter?.StartLoop(FinalDemoCueId.RainLightBed, ResolveFrontPosition(4f), 0.65f);
            }

            if (GUILayout.Button("Stop Rain Loop"))
            {
                director.AudioRouter?.StopLoop(FinalDemoCueId.RainLightBed);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Tinnitus Loop"))
            {
                director.AudioRouter?.StartLoop(FinalDemoCueId.TinnitusLongGlitch, ResolveFrontPosition(2.6f), 0.55f);
            }

            if (GUILayout.Button("Stop Tinnitus Loop"))
            {
                director.AudioRouter?.StopLoop(FinalDemoCueId.TinnitusLongGlitch);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Stop Audio"))
            {
                director.AudioRouter?.StopAllCues();
            }
        }

        private void DrawLightHapticTestSection()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Bundle 3 Light / Haptic Tests");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell LED"))
            {
                director.LightRouter?.ShowBellPoint(ResolveFrontPosition(2f), 1f);
            }

            if (GUILayout.Button("Rain LED"))
            {
                director.LightRouter?.ShowRainFloorBand(0.45f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tinnitus LED"))
            {
                director.LightRouter?.ShowTinnitusPoint(ResolveFrontPosition(2.4f), 0.75f);
            }

            if (GUILayout.Button("Boss LED"))
            {
                director.LightRouter?.ShowTinnitusPoint(ResolveFrontPosition(3f), 0.9f, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rain Then Bell Priority"))
            {
                director.LightRouter?.ShowRainFloorBand(0.55f);
                director.LightRouter?.ShowBellPoint(ResolveFrontPosition(2f), 1f);
                director.LightRouter?.ShowRainFloorBand(0.55f);
            }

            if (GUILayout.Button("Clear LED"))
            {
                director.LightRouter?.Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bell Assist Vib"))
            {
                director.HapticRouter?.TriggerBellAssistPulse();
            }

            if (GUILayout.Button("Tinnitus Lock Vib"))
            {
                director.HapticRouter?.TriggerTinnitusLockPulse();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cleanse Hum"))
            {
                director.HapticRouter?.StartTinnitusCleanseHum();
            }

            if (GUILayout.Button("Boss Hit Vib"))
            {
                director.HapticRouter?.TriggerBossHitPulse();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Boss Failure Vib"))
            {
                director.HapticRouter?.TriggerBossFailurePulse();
            }

            if (GUILayout.Button("Stop Haptics"))
            {
                director.HapticRouter?.StopAllHaptics();
            }
            GUILayout.EndHorizontal();
        }

        private Vector3 ResolveFrontPosition(float distance)
        {
            Transform playerRig = director.PlayerRig;
            if (playerRig == null)
            {
                return new Vector3(0f, 1.5f, distance);
            }

            return playerRig.position + playerRig.forward * distance;
        }

        private void DrawStatusWindow(int windowId)
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            string statusText = inputStatus != null ? inputStatus.BuildStatusText(director) : "No FinalDemoInputStatus found.";
            _statusScroll = GUILayout.BeginScrollView(_statusScroll);
            GUILayout.TextArea(statusText, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
}
