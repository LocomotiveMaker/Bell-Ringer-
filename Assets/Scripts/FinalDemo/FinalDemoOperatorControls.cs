using UnityEngine;
using BellRinger.ObserverDisplay;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoOperatorControls : MonoBehaviour
    {
        [SerializeField] private FinalDemoDirector director;
        [SerializeField] private FinalDemoInputStatus inputStatus;
        [SerializeField] private bool showOperatorControls = true;
        [SerializeField] private bool showInputStatus = true;
        [SerializeField] private Vector2 preflightPanelSize = new Vector2(920f, 640f);
        [SerializeField] private Vector2 runtimePanelSize = new Vector2(560f, 300f);
        [SerializeField] private Vector2 ledPanelSize = new Vector2(430f, 300f);
        [SerializeField] private float hudMarginPixels = 16f;
        [SerializeField] private float hudGapPixels = 18f;
        [SerializeField, Range(1f, 10f)] private float ledPreviewBrightness = 5f;

        private Rect _preflightRect = new Rect(0f, 0f, 920f, 640f);
        private Rect _runtimeRect = new Rect(16f, 16f, 560f, 300f);
        private Rect _ledRect = new Rect(0f, 16f, 430f, 300f);
        private Vector2 _operatorScroll;
        private Vector2 _runtimeScroll;
        private Color[] _ledScratch;

        public float RuntimeHudTopPixelsFromBottom
        {
            get
            {
                float margin = Mathf.Max(8f, hudMarginPixels);
                float runtimeHeight = Mathf.Min(runtimePanelSize.y, Screen.height * 0.28f);
                return runtimeHeight + margin;
            }
        }

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

            bool preflight = director.CurrentStage == FinalDemoStage.Preflight;
            if (preflight)
            {
                if (showOperatorControls)
                {
                    LayoutPreflightWindow();
                    _preflightRect = GUI.Window(7101, _preflightRect, DrawPreflightWindow, "Final Demo Preflight");
                }
                return;
            }

            LayoutRuntimeWindows();
            if (showOperatorControls || showInputStatus)
            {
                _runtimeRect = GUI.Window(7101, _runtimeRect, DrawRuntimeWindow, "Final Demo Runtime");
            }

            _ledRect = GUI.Window(7103, _ledRect, DrawLedWindow, "16x8 LED Preview");
        }

        private void LayoutPreflightWindow()
        {
            float width = Mathf.Min(preflightPanelSize.x, Screen.width - hudMarginPixels * 2f);
            float height = Mathf.Min(preflightPanelSize.y, Screen.height - hudMarginPixels * 2f);
            _preflightRect.width = width;
            _preflightRect.height = height;
            _preflightRect.x = (Screen.width - width) * 0.5f;
            _preflightRect.y = (Screen.height - height) * 0.5f;
        }

        private void LayoutRuntimeWindows()
        {
            float margin = Mathf.Max(8f, hudMarginPixels);
            float gap = Mathf.Max(8f, hudGapPixels);
            float runtimeWidth = Mathf.Min(runtimePanelSize.x, Screen.width * 0.33f);
            float runtimeHeight = Mathf.Min(runtimePanelSize.y, Screen.height * 0.28f);
            float ledWidth = Mathf.Min(ledPanelSize.x, Screen.width * 0.26f);
            float ledHeight = runtimeHeight;
            _runtimeRect = new Rect(margin, Screen.height - runtimeHeight - margin, runtimeWidth, runtimeHeight);
            _ledRect = new Rect(Screen.width - ledWidth - margin, Screen.height - ledHeight - margin, ledWidth, ledHeight);
        }

        private void DrawPreflightWindow(int windowId)
        {
            _operatorScroll = GUILayout.BeginScrollView(_operatorScroll);
            GUILayout.Label(director.BuildStageSummary());
            GUILayout.Label($"Elapsed {director.StageElapsedSeconds:0.00}s");
            GUILayout.Space(6f);

            if (GUILayout.Button("Start Demo", GUILayout.Height(38f)))
            {
                director.StartDemo();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Next"))
            {
                director.ForceNextStage();
            }

            if (GUILayout.Button("Force Complete Current"))
            {
                director.ForceCompleteCurrentObjective();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Stage"))
            {
                director.ResetCurrentStage();
            }

            if (GUILayout.Button("Reset To Preflight"))
            {
                director.ResetToPreflight();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(director.PlayerMovementLocked ? "Unlock Movement" : "Lock Movement"))
            {
                director.ToggleMovementLock();
            }

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

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Global Glitch: {(director.GlobalGlitchEnabled ? "On" : "Off")}"))
            {
                director.ToggleGlobalGlitch();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Stop All Outputs"))
            {
                director.StopAllOutputs();
            }

            GUILayout.Space(8f);
            DrawObserverVisualCheckSection();
            GUILayout.Space(8f);
            DrawAudioTestSection();
            DrawLightHapticTestSection();

            if (showInputStatus)
            {
                GUILayout.Space(10f);
                GUILayout.Label("Preflight Status");
                GUILayout.TextArea(BuildRuntimeStatusText(), GUILayout.MinHeight(170f));
            }

            GUILayout.EndScrollView();
        }

        private void DrawRuntimeWindow(int windowId)
        {
            _runtimeScroll = GUILayout.BeginScrollView(_runtimeScroll);
            GUILayout.Label(director.BuildStageSummary());
            GUILayout.Label(director.CurrentObjectiveLabel);
            DrawProgressBar(director.CurrentObjectiveProgress01);
            DrawTinnitusPoseProgressIfNeeded();
            GUILayout.Space(6f);
            GUILayout.Label(BuildTrackingSummary());
            if (showInputStatus)
            {
                GUILayout.TextArea(BuildRuntimeStatusText(), GUILayout.ExpandHeight(true));
            }

            GUILayout.Space(6f);
            DrawObserverVisualCheckSection();
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Next"))
            {
                director.ForceNextStage();
            }

            if (GUILayout.Button("Reset Stage"))
            {
                director.ResetCurrentStage();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preflight"))
            {
                director.ResetToPreflight();
            }

            if (GUILayout.Button(director.PlayerMovementLocked ? "Unlock Move" : "Lock Move"))
            {
                director.ToggleMovementLock();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Head Center"))
            {
                director.RecenterHead();
            }

            if (GUILayout.Button("Pad Center"))
            {
                director.RecenterPad();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Assist: {director.AssistLevel}"))
            {
                director.CycleAssistLevel();
            }

            if (GUILayout.Button($"Glitch: {(director.GlobalGlitchEnabled ? "On" : "Off")}"))
            {
                director.ToggleGlobalGlitch();
            }

            if (GUILayout.Button("Stop Outputs"))
            {
                director.StopAllOutputs();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void DrawObserverVisualCheckSection()
        {
            GUILayout.Label("Observer Visual Checks");
            GUILayout.BeginHorizontal();
            DrawStageButton("Bell Orbit", FinalDemoStage.BellOrbit);
            DrawStageButton("Bell Follow", FinalDemoStage.BellFollowOne);
            DrawStageButton("Bell Gaze", FinalDemoStage.BellGaze);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawStageButton("Pad/Bell", FinalDemoStage.BellGaze);
            DrawStageButton("Rain", FinalDemoStage.BellFollowRain);
            DrawStageButton("Tinnitus", FinalDemoStage.GeneralTinnitusOne);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawStageButton("Boss", FinalDemoStage.BossPatternOne);
            DrawStageButton("Forest", FinalDemoStage.ForestEnding);
            DrawStageButton("Complete", FinalDemoStage.Complete);
            GUILayout.EndHorizontal();
        }

        private void DrawStageButton(string label, FinalDemoStage stage)
        {
            if (GUILayout.Button(label))
            {
                director.ForceStage(stage);
            }
        }

        private void DrawLedWindow(int windowId)
        {
            FinalDemoLightRouter router = ResolveLightRouter();
            GUILayout.Label($"Preview brightness x{ledPreviewBrightness:0.0}");
            Rect gridRect = GUILayoutUtility.GetRect(10f, 220f, GUILayout.ExpandWidth(true));
            DrawLedPreviewGrid(gridRect, router);
            if (router != null)
            {
                GUILayout.Label(router.LastAction);
                GUILayout.Label(router.LastRainDebug);
            }

            GUILayout.Label("Bottom row here matches the physical board bottom row.");
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

        private void DrawProgressBar(float progress01)
        {
            Rect rect = GUILayoutUtility.GetRect(12f, 18f, GUILayout.ExpandWidth(true));
            Color previous = GUI.color;
            GUI.color = new Color(0.11f, 0.12f, 0.14f, 1f);
            GUI.Box(rect, GUIContent.none);
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(progress01), rect.height - 4f);
            GUI.color = new Color(0.18f, 0.82f, 0.38f, 1f);
            GUI.Box(fillRect, GUIContent.none);
            GUI.color = previous;
        }

        private void DrawTinnitusPoseProgressIfNeeded()
        {
            if (director.CurrentStage != FinalDemoStage.GeneralTinnitusOne &&
                director.CurrentStage != FinalDemoStage.GeneralTinnitusTwo &&
                director.CurrentStage != FinalDemoStage.BossPatternOne &&
                director.CurrentStage != FinalDemoStage.BossPatternTwo &&
                director.CurrentStage != FinalDemoStage.BossPatternThree)
            {
                return;
            }

            GUILayout.Space(4f);
            if (director.CurrentStage == FinalDemoStage.BossPatternOne ||
                director.CurrentStage == FinalDemoStage.BossPatternTwo ||
                director.CurrentStage == FinalDemoStage.BossPatternThree)
            {
                GUILayout.Label($"Boss pattern {director.BossPatternFailureCount} misses  target match {director.CurrentBossPatternTargetMatch01:0.00}");
                GUILayout.Label($"Boss cleanse {director.CurrentBossPatternProgress01:0.00}");
                DrawProgressBar(director.CurrentBossPatternProgress01);
                GUILayout.Label($"Boss target match {director.CurrentBossPatternTargetMatch01:0.00}");
                DrawProgressBar(director.CurrentBossPatternTargetMatch01);
                return;
            }

            GUILayout.Label(
                $"Pad target posErr {director.GeneralTinnitusPositionErrorMeters:0.000}m  " +
                $"rotErr Y/P/R {director.GeneralTinnitusYawErrorDegrees:0}/{director.GeneralTinnitusPitchErrorDegrees:0}/{director.GeneralTinnitusRollErrorDegrees:0}  " +
                $"inside {director.GeneralTinnitusInsideTolerance}");
            GUILayout.Label($"Position match {director.GeneralTinnitusPositionMatch01:0.00}");
            DrawProgressBar(director.GeneralTinnitusPositionMatch01);
            GUILayout.Label($"Rotation match {director.GeneralTinnitusRotationMatch01:0.00}");
            DrawProgressBar(director.GeneralTinnitusRotationMatch01);
            GUILayout.Label($"Cleanse {director.GeneralTinnitusProgress01:0.00}");
            DrawProgressBar(director.GeneralTinnitusProgress01);
        }

        private string BuildTrackingSummary()
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            if (inputStatus == null)
            {
                return "HEAD OFF   ARUCO OFF   PAD IMU OFF   LED OFF   VIB OFF";
            }

            return
                $"HEAD {BuildFreshLabel(inputStatus.HeadFresh)}   " +
                $"ARUCO {BuildFreshLabel(inputStatus.PadCameraFresh)}   " +
                $"PAD IMU {BuildFreshLabel(inputStatus.PadImuFresh)}   " +
                $"LED {BuildFreshLabel(inputStatus.HardwareConnected)}   " +
                $"VIB {BuildFreshLabel(inputStatus.HasGamepad)}";
        }

        private string BuildRuntimeStatusText()
        {
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            return inputStatus != null ? inputStatus.BuildStatusText(director) : "No FinalDemoInputStatus found.";
        }

        private FinalDemoLightRouter ResolveLightRouter()
        {
            if (director != null && director.LightRouter != null)
            {
                return director.LightRouter;
            }

            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            return inputStatus != null ? inputStatus.LightRouter : null;
        }

        private void DrawLedPreviewGrid(Rect rect, FinalDemoLightRouter router)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.07f, 1f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previous;

            if (router == null)
            {
                GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 24f), "Light router missing.");
                return;
            }

            _ledScratch ??= new Color[16 * 8];
            if (_ledScratch.Length != router.LogicalFrameWidth * router.LogicalFrameHeight)
            {
                _ledScratch = new Color[router.LogicalFrameWidth * router.LogicalFrameHeight];
            }

            router.CopyLogicalLedFrame(_ledScratch);
            float padding = 14f;
            float gap = 3f;
            float gridWidth = rect.width - padding * 2f;
            float gridHeight = rect.height - padding * 2f;
            float cellWidth = (gridWidth - gap * 15f) / 16f;
            float cellHeight = (gridHeight - gap * 7f) / 8f;
            float cellSize = Mathf.Max(4f, Mathf.Min(cellWidth, cellHeight));
            float usedWidth = cellSize * 16f + gap * 15f;
            float usedHeight = cellSize * 8f + gap * 7f;
            float startX = rect.x + (rect.width - usedWidth) * 0.5f;
            float startY = rect.y + (rect.height - usedHeight) * 0.5f;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int index = y * 16 + x;
                    Color cellColor = index < _ledScratch.Length ? _ledScratch[index] : Color.black;
                    if (cellColor.maxColorComponent > 0.0001f)
                    {
                        cellColor *= ledPreviewBrightness;
                        cellColor.r = Mathf.Clamp01(cellColor.r);
                        cellColor.g = Mathf.Clamp01(cellColor.g);
                        cellColor.b = Mathf.Clamp01(cellColor.b);
                        cellColor.a = 1f;
                    }
                    else
                    {
                        cellColor = new Color(0.02f, 0.02f, 0.025f, 1f);
                    }

                    float drawX = startX + x * (cellSize + gap);
                    float drawY = startY + (7 - y) * (cellSize + gap);
                    Rect cellRect = new Rect(drawX, drawY, cellSize, cellSize);
                    GUI.color = cellColor;
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                }
            }

            GUI.color = new Color(0.26f, 0.28f, 0.34f, 0.75f);
            float dividerX = startX + 8f * cellSize + 7.5f * gap;
            GUI.DrawTexture(new Rect(dividerX, startY, 1f, usedHeight), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static string BuildFreshLabel(bool value)
        {
            return value ? "OK" : "OFF";
        }
    }
}
