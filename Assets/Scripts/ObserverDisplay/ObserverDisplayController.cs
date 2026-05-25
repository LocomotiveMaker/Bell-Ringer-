using System.Collections.Generic;
using BellRinger.FinalDemo;
using BellRinger.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverDisplayController : MonoBehaviour
    {
        private const int ObserverWorldLayer = 0;

        private static readonly string[] HiddenGameplayPlaceholderNames =
        {
            "FinalDemo_BellPlaceholder",
            "FinalDemo_TinnitusA",
            "FinalDemo_TinnitusB",
            "FinalDemo_BossTinnitus",
            "FinalDemo_RainFloor",
            "FinalDemo_WallNoisePlane",
        };

        [SerializeField] private FinalDemoDirector director;
        [SerializeField] private FinalDemoInputStatus inputStatus;
        [SerializeField] private FinalDemoLightRouter lightRouter;
        [SerializeField] private FinalDemoAudioRouter audioRouter;
        [SerializeField] private ObserverDisplayLayout layout;
        [SerializeField] private Color bottomPanelColor = new Color(0.06f, 0.06f, 0.09f, 0.94f);
        [SerializeField] private Color centerSafeColor = new Color(0.015f, 0.015f, 0.02f, 0.98f);
        [SerializeField] private Color worldBackgroundColor = new Color(0.015f, 0.015f, 0.03f, 1f);
        [SerializeField] private Vector3 observerCameraOffset = new Vector3(0f, 3.7f, -5.0f);
        [SerializeField] private Vector3 observerLookOffset = new Vector3(0f, 0.65f, 2.45f);
        [SerializeField] private float observerCameraFollowLerp = 6f;

        private readonly ObserverDisplaySnapshot _snapshot = new ObserverDisplaySnapshot();
        private readonly List<FinalDemoAudioCueSnapshot> _audioCueSnapshots = new List<FinalDemoAudioCueSnapshot>(12);

        private Camera _observerCamera;
        private Camera _playerViewCamera;
        private Light _observerKeyLight;
        private Canvas _canvas;
        private ObserverStagePanel _stagePanel;
        private ObserverLedMatrixPreview _ledPreview;
        private ObserverPadView _padView;
        private ObserverBellView _bellView;
        private ObserverRainView _rainView;
        private ObserverTinnitusView _tinnitusView;
        private ObserverBossTinnitusView _bossView;
        private ObserverForestView _forestView;
        private Transform _worldVisualRoot;
        private Transform _worldEnvironmentRoot;
        private Renderer _environmentFloorRenderer;
        private Renderer _environmentWallRenderer;
        private RectTransform _leftPanelRect;
        private RectTransform _centerPanelRect;
        private RectTransform _rightPanelRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (!IsAutoBootstrapEnabled())
            {
                return;
            }

            if (FindFirstObjectByType<FinalDemoDirector>() == null || FindFirstObjectByType<ObserverDisplayController>() != null)
            {
                return;
            }

            GameObject root = new GameObject("ObserverDisplayRoot");
            root.AddComponent<ObserverDisplayLayout>();
            root.AddComponent<ObserverDisplayController>();
        }

        private static bool IsAutoBootstrapEnabled()
        {
            return false;
        }

        private void Awake()
        {
            EnsureReferences();
            EnsureRuntimeObjects();
        }

        private void LateUpdate()
        {
            EnsureReferences();
            EnsureRuntimeObjects();
            UpdateObserverCamera();
            BuildSnapshot();
            _stagePanel?.ApplySnapshot(_snapshot);
            _ledPreview?.Bind(lightRouter);
            _padView?.ApplySnapshot(_snapshot, inputStatus, _snapshot.hasBell, _snapshot.bellWorldPosition);
            _bellView?.ApplySnapshot(_snapshot, director, _audioCueSnapshots, inputStatus != null && inputStatus.PadImuReceiver != null ? inputStatus.PadImuReceiver.MotionIntensity01 : 0f);
            _rainView?.ApplySnapshot(_snapshot, director);
            _tinnitusView?.ApplySnapshot(_snapshot, director);
            _bossView?.ApplySnapshot(_snapshot, director);
            _forestView?.ApplySnapshot(_snapshot, director);
            UpdateWorldEnvironment();
            ApplyObserverWorldLayer();
            HideGameplayPlaceholderRenderers();
        }

        private void EnsureReferences()
        {
            director ??= FindFirstObjectByType<FinalDemoDirector>();
            inputStatus ??= FindFirstObjectByType<FinalDemoInputStatus>();
            lightRouter ??= FindFirstObjectByType<FinalDemoLightRouter>();
            audioRouter ??= FindFirstObjectByType<FinalDemoAudioRouter>();
            layout ??= GetComponent<ObserverDisplayLayout>() ?? gameObject.AddComponent<ObserverDisplayLayout>();

            if (_playerViewCamera == null && director != null && director.PlayerRig != null)
            {
                Camera[] cameras = director.PlayerRig.GetComponentsInChildren<Camera>(true);
                foreach (Camera candidate in cameras)
                {
                    if (candidate != null)
                    {
                        _playerViewCamera = candidate;
                        break;
                    }
                }
            }
        }

        private void EnsureRuntimeObjects()
        {
            EnsureObserverCamera();
            EnsureObserverLight();
            EnsureCanvas();
            EnsureWorldViews();
            EnsureWorldEnvironment();
            LayoutPanels();
        }

        private void EnsureObserverCamera()
        {
            if (_observerCamera != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("ObserverWorldCamera");
            cameraObject.transform.SetParent(transform, false);
            _observerCamera = cameraObject.AddComponent<Camera>();
            _observerCamera.clearFlags = CameraClearFlags.SolidColor;
            _observerCamera.backgroundColor = worldBackgroundColor;
            _observerCamera.depth = 10f;
            _observerCamera.fieldOfView = 48f;
            _observerCamera.nearClipPlane = 0.03f;
            _observerCamera.farClipPlane = 100f;
            _observerCamera.useOcclusionCulling = false;
            _observerCamera.cullingMask = 1 << ObserverWorldLayer;
        }

        private void EnsureObserverLight()
        {
            if (_observerKeyLight != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("ObserverKeyLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            _observerKeyLight = lightObject.AddComponent<Light>();
            _observerKeyLight.type = LightType.Directional;
            _observerKeyLight.color = new Color(0.94f, 0.97f, 1f);
            _observerKeyLight.intensity = 1.35f;
            _observerKeyLight.shadows = LightShadows.None;
            _observerKeyLight.cullingMask = 1 << ObserverWorldLayer;
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("ObserverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1200f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            _leftPanelRect = CreatePanel(canvasRect, "ObserverBottomLeft", bottomPanelColor);
            _centerPanelRect = CreatePanel(canvasRect, "ObserverBottomCenterSafe", centerSafeColor);
            _rightPanelRect = CreatePanel(canvasRect, "ObserverBottomRight", bottomPanelColor);

            _stagePanel = _leftPanelRect.gameObject.AddComponent<ObserverStagePanel>();
            _ledPreview = _rightPanelRect.gameObject.AddComponent<ObserverLedMatrixPreview>();

            GameObject centerLabelObject = new GameObject("CenterSafeLabel", typeof(RectTransform));
            centerLabelObject.transform.SetParent(_centerPanelRect, false);
            Text centerLabel = centerLabelObject.AddComponent<Text>();
            centerLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            centerLabel.fontSize = 18;
            centerLabel.fontStyle = FontStyle.Bold;
            centerLabel.color = new Color(0.42f, 0.45f, 0.52f, 1f);
            centerLabel.alignment = TextAnchor.UpperCenter;
            centerLabel.text = "TRACKING SAFE AREA";
            RectTransform centerLabelRect = centerLabelObject.GetComponent<RectTransform>();
            centerLabelRect.anchorMin = new Vector2(0f, 1f);
            centerLabelRect.anchorMax = new Vector2(1f, 1f);
            centerLabelRect.pivot = new Vector2(0.5f, 1f);
            centerLabelRect.offsetMin = new Vector2(14f, -36f);
            centerLabelRect.offsetMax = new Vector2(-14f, -10f);
        }

        private void EnsureWorldViews()
        {
            if (_padView != null && _bellView != null && _rainView != null && _tinnitusView != null && _bossView != null && _forestView != null)
            {
                return;
            }

            if (_worldVisualRoot == null)
            {
                GameObject worldVisualRoot = new GameObject("ObserverWorldViews");
                worldVisualRoot.transform.SetParent(transform, false);
                _worldVisualRoot = worldVisualRoot.transform;
            }

            _padView ??= _worldVisualRoot.gameObject.AddComponent<ObserverPadView>();
            _bellView ??= _worldVisualRoot.gameObject.AddComponent<ObserverBellView>();
            _rainView ??= _worldVisualRoot.gameObject.AddComponent<ObserverRainView>();
            _tinnitusView ??= _worldVisualRoot.gameObject.AddComponent<ObserverTinnitusView>();
            _bossView ??= _worldVisualRoot.gameObject.AddComponent<ObserverBossTinnitusView>();
            _forestView ??= _worldVisualRoot.gameObject.AddComponent<ObserverForestView>();
        }

        private void EnsureWorldEnvironment()
        {
            if (_worldEnvironmentRoot != null)
            {
                return;
            }

            _worldEnvironmentRoot = new GameObject("ObserverWorldEnvironment").transform;
            _worldEnvironmentRoot.SetParent(transform, false);

            _environmentFloorRenderer = CreateEnvironmentPart(
                _worldEnvironmentRoot,
                "ObserverFloor",
                PrimitiveType.Cube,
                new Vector3(0f, -0.03f, 3.1f),
                new Vector3(18f, 0.04f, 16f),
                new Color(0.28f, 0.29f, 0.31f));

            _environmentWallRenderer = CreateEnvironmentPart(
                _worldEnvironmentRoot,
                "ObserverWall",
                PrimitiveType.Cube,
                new Vector3(0f, 2.1f, 8f),
                new Vector3(18f, 4.4f, 0.08f),
                new Color(0.90f, 0.92f, 0.94f));
        }

        private void LayoutPanels()
        {
            if (_observerCamera == null || _canvas == null || layout == null)
            {
                return;
            }

            float bottomHeight = Mathf.Clamp(layout.BottomBandHeight01, 0.18f, 0.45f);
            _observerCamera.rect = new Rect(0f, bottomHeight, 1f, 1f - bottomHeight);

            float margin = layout.PanelMarginPixels;
            float gap = layout.PanelGapPixels;
            float width = Screen.width;
            float height = Screen.height;
            float bottomPixels = height * bottomHeight;
            float usableWidth = Mathf.Max(100f, width - margin * 2f - gap * 2f);
            float leftWidth = usableWidth * layout.LeftPanelWidth01;
            float rightWidth = usableWidth * layout.RightPanelWidth01;
            float centerWidth = Mathf.Max(80f, usableWidth - leftWidth - rightWidth);

            SetPanelRect(_leftPanelRect, margin, margin, leftWidth, bottomPixels - margin * 2f);
            SetPanelRect(_centerPanelRect, margin + leftWidth + gap, margin, centerWidth - gap, bottomPixels - margin * 2f);
            SetPanelRect(_rightPanelRect, width - margin - rightWidth, margin, rightWidth, bottomPixels - margin * 2f);
        }

        private void UpdateObserverCamera()
        {
            if (_observerCamera == null || director == null || director.PlayerRig == null)
            {
                return;
            }

            Vector3 targetAnchor = director.PlayerRig.position;
            Vector3 desiredPosition = targetAnchor + observerCameraOffset;
            float lerp01 = 1f - Mathf.Exp(-Mathf.Max(0f, observerCameraFollowLerp) * Time.unscaledDeltaTime);
            _observerCamera.transform.position = Vector3.Lerp(_observerCamera.transform.position, desiredPosition, lerp01);
            _observerCamera.transform.LookAt(targetAnchor + observerLookOffset);
        }

        private void UpdateWorldEnvironment()
        {
            if (_worldEnvironmentRoot == null)
            {
                return;
            }

            Vector3 anchor = director != null && director.PlayerRig != null
                ? director.PlayerRig.position
                : Vector3.zero;
            _worldEnvironmentRoot.position = new Vector3(anchor.x, 0f, anchor.z);

            if (_environmentFloorRenderer != null)
            {
                Color floorColor = _snapshot.stage == FinalDemoStage.ForestEnding || _snapshot.stage == FinalDemoStage.Complete
                    ? new Color(0.18f, 0.21f, 0.19f)
                    : _snapshot.stage == FinalDemoStage.BellFollowRain
                        ? new Color(0.16f, 0.18f, 0.20f)
                        : new Color(0.28f, 0.29f, 0.31f);
                _environmentFloorRenderer.material.color = floorColor;
            }

            if (_environmentWallRenderer != null)
            {
                Color wallColor = _snapshot.stage == FinalDemoStage.ForestEnding || _snapshot.stage == FinalDemoStage.Complete
                    ? new Color(0.22f, 0.28f, 0.24f)
                    : new Color(0.90f, 0.92f, 0.94f);
                _environmentWallRenderer.material.color = wallColor;
            }
        }

        private void ApplyObserverWorldLayer()
        {
            if (_worldVisualRoot != null)
            {
                SetLayerRecursively(_worldVisualRoot.gameObject, ObserverWorldLayer);
            }

            if (_worldEnvironmentRoot != null)
            {
                SetLayerRecursively(_worldEnvironmentRoot.gameObject, ObserverWorldLayer);
            }
        }

        private static void HideGameplayPlaceholderRenderers()
        {
            for (int nameIndex = 0; nameIndex < HiddenGameplayPlaceholderNames.Length; nameIndex++)
            {
                GameObject placeholder = GameObject.Find(HiddenGameplayPlaceholderNames[nameIndex]);
                if (placeholder == null)
                {
                    continue;
                }

                Renderer[] renderers = placeholder.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    if (renderers[rendererIndex] != null)
                    {
                        renderers[rendererIndex].enabled = false;
                    }
                }
            }
        }

        private void BuildSnapshot()
        {
            if (director == null)
            {
                return;
            }

            _snapshot.stage = director.CurrentStage;
            _snapshot.objectiveLabel = director.CurrentObjectiveLabel;
            _snapshot.objectiveProgress01 = director.CurrentObjectiveProgress01;
            _snapshot.activeSoundFocusLabel = ResolveFocusLabel();
            _snapshot.rainIntensity01 = director.CurrentRainIntensity;
            _snapshot.bellGazeProgress01 = director.BellGazeProgress01;
            _snapshot.tinnitusProgress01 = director.GeneralTinnitusProgress01;
            _snapshot.tinnitusMatch01 = director.GeneralTinnitusMatch01;
            _snapshot.bossPatternProgress01 = director.CurrentBossPatternProgress01;
            _snapshot.bossPatternMatch01 = director.CurrentBossPatternTargetMatch01;
            _snapshot.playerWorldPosition = director.PlayerRig != null ? director.PlayerRig.position : Vector3.zero;
            _snapshot.headTracking = ResolveHeadHealth();
            _snapshot.padCameraTracking = ResolvePadCameraHealth();
            _snapshot.padImuTracking = ResolvePadImuHealth();
            _snapshot.ledTracking = ResolveLedHealth();
            _snapshot.hapticsTracking = ResolveHapticsHealth();

            PadPoseProvider padPoseProvider = inputStatus != null ? inputStatus.PadPoseProvider : null;
            _snapshot.hasPadPose = false;
            if (padPoseProvider != null && padPoseProvider.HasPose && _playerViewCamera != null)
            {
                _snapshot.hasPadPose = true;
                _snapshot.padWorldPosition = _playerViewCamera.transform.TransformPoint(padPoseProvider.CameraSpacePosition);
                _snapshot.padWorldRotation = _playerViewCamera.transform.rotation * padPoseProvider.RelativeRotation;
            }

            _snapshot.hasBell = director.TryGetBellWorldPosition(out _snapshot.bellWorldPosition);
            _snapshot.hasTinnitus = director.TryGetCurrentTinnitusWorldPosition(out _snapshot.tinnitusWorldPosition);
            _snapshot.hasBoss = director.TryGetBossWorldPosition(out _snapshot.bossWorldPosition);
            _snapshot.hasForestBell = director.CurrentStage == FinalDemoStage.ForestEnding && _snapshot.hasBell;
            if (_snapshot.hasForestBell)
            {
                _snapshot.forestBellWorldPosition = _snapshot.bellWorldPosition;
            }
        }

        private string ResolveFocusLabel()
        {
            if (audioRouter != null)
            {
                audioRouter.FillActiveCueSnapshots(_audioCueSnapshots);
            }
            else
            {
                _audioCueSnapshots.Clear();
            }

            if (_audioCueSnapshots.Count > 0)
            {
                for (int i = _audioCueSnapshots.Count - 1; i >= 0; i--)
                {
                    FinalDemoAudioCueSnapshot cue = _audioCueSnapshots[i];
                    if (cue.CueId == FinalDemoCueId.BossBasePulse || cue.CueId == FinalDemoCueId.BossGlitchBurst || cue.CueId == FinalDemoCueId.BossWeakpointMove || cue.CueId == FinalDemoCueId.BossHit)
                    {
                        return "BOSS TINNITUS";
                    }

                    if (cue.CueId == FinalDemoCueId.TinnitusLongGlitch || cue.CueId == FinalDemoCueId.TinnitusBurst || cue.CueId == FinalDemoCueId.TinnitusResolve)
                    {
                        return "TINNITUS";
                    }

                    if (cue.CueId == FinalDemoCueId.ForestBell || cue.CueId == FinalDemoCueId.ForestBed)
                    {
                        return "FOREST BELL";
                    }

                    if (cue.CueId == FinalDemoCueId.BellDistantCall || cue.CueId == FinalDemoCueId.BellOpeningOrbit || cue.CueId == FinalDemoCueId.BellMovementTexture)
                    {
                        return "LIVING BELL";
                    }

                    if (cue.CueId == FinalDemoCueId.RainLightBed || cue.CueId == FinalDemoCueId.RainStrongBed || cue.CueId == FinalDemoCueId.RainCloseDrops)
                    {
                        return "RAIN / WIND";
                    }
                }
            }

            return director.CurrentStage switch
            {
                FinalDemoStage.GeneralTinnitusOne => "TINNITUS",
                FinalDemoStage.GeneralTinnitusTwo => "TINNITUS",
                FinalDemoStage.BossApproach => "BOSS TINNITUS",
                FinalDemoStage.BossPatternOne => "BOSS TINNITUS",
                FinalDemoStage.BossPatternTwo => "BOSS TINNITUS",
                FinalDemoStage.BossPatternThree => "BOSS TINNITUS",
                FinalDemoStage.BossDefeat => "RELEASE",
                FinalDemoStage.ForestEnding => "FOREST BELL",
                FinalDemoStage.BellFollowRain => "LIVING BELL",
                _ => "LIVING BELL",
            };
        }

        private ObserverTrackingHealth ResolveHeadHealth()
        {
            if (inputStatus == null || inputStatus.HeadImuReceiver == null)
            {
                return ObserverTrackingHealth.Missing;
            }

            return inputStatus.HeadFresh ? ObserverTrackingHealth.Fresh : ObserverTrackingHealth.Stale;
        }

        private ObserverTrackingHealth ResolvePadCameraHealth()
        {
            if (inputStatus == null || inputStatus.PadTrackingReceiver == null)
            {
                return ObserverTrackingHealth.Missing;
            }

            return inputStatus.PadCameraFresh ? ObserverTrackingHealth.Fresh : ObserverTrackingHealth.Stale;
        }

        private ObserverTrackingHealth ResolvePadImuHealth()
        {
            if (inputStatus == null || inputStatus.PadImuReceiver == null)
            {
                return ObserverTrackingHealth.Missing;
            }

            return inputStatus.PadImuFresh ? ObserverTrackingHealth.Fresh : ObserverTrackingHealth.Stale;
        }

        private ObserverTrackingHealth ResolveLedHealth()
        {
            if (inputStatus == null || inputStatus.HardwareBridge == null)
            {
                return ObserverTrackingHealth.Missing;
            }

            return inputStatus.HardwareConnected ? ObserverTrackingHealth.Fresh : ObserverTrackingHealth.Stale;
        }

        private ObserverTrackingHealth ResolveHapticsHealth()
        {
            if (inputStatus == null)
            {
                return ObserverTrackingHealth.Missing;
            }

            return inputStatus.HasGamepad ? ObserverTrackingHealth.Fresh : ObserverTrackingHealth.Missing;
        }

        private static RectTransform CreatePanel(RectTransform parent, string objectName, Color color)
        {
            GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            return panelObject.GetComponent<RectTransform>();
        }

        private static Renderer CreateEnvironmentPart(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            Shader shader = Shader.Find("Unlit/Color") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Standard");
            renderer.material = new Material(shader)
            {
                color = color,
            };
            return renderer;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            Transform rootTransform = root.transform;
            for (int index = 0; index < rootTransform.childCount; index++)
            {
                SetLayerRecursively(rootTransform.GetChild(index).gameObject, layer);
            }
        }

        private static void SetPanelRect(RectTransform rectTransform, float x, float y, float width, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}
