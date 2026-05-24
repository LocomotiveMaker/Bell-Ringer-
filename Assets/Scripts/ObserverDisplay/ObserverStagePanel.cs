using UnityEngine;
using UnityEngine.UI;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ObserverStagePanel : MonoBehaviour
    {
        [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        [SerializeField] private Color titleColor = new Color(0.93f, 0.93f, 0.95f, 1f);
        [SerializeField] private Color bodyColor = new Color(0.73f, 0.76f, 0.82f, 1f);
        [SerializeField] private Color accentColor = new Color(0.26f, 0.9f, 0.44f, 1f);
        [SerializeField] private Color staleColor = new Color(0.92f, 0.72f, 0.22f, 1f);
        [SerializeField] private Color errorColor = new Color(0.85f, 0.24f, 0.28f, 1f);

        private Image _backgroundImage;
        private Text _stageText;
        private Text _objectiveText;
        private Text _focusText;
        private Text _trackingText;
        private Image _progressBack;
        private Image _progressFill;

        private void Awake()
        {
            EnsureUi();
        }

        public void ApplySnapshot(ObserverDisplaySnapshot snapshot)
        {
            EnsureUi();
            if (snapshot == null)
            {
                return;
            }

            _backgroundImage.color = panelColor;
            _stageText.text = $"STAGE  {snapshot.stage}";
            _objectiveText.text = snapshot.objectiveLabel;
            _focusText.text = $"FOCUS  {snapshot.activeSoundFocusLabel}";
            _trackingText.text =
                $"HEAD {BuildHealthLabel(snapshot.headTracking)}   " +
                $"ARUCO {BuildHealthLabel(snapshot.padCameraTracking)}   " +
                $"PAD IMU {BuildHealthLabel(snapshot.padImuTracking)}   " +
                $"LED {BuildHealthLabel(snapshot.ledTracking)}   " +
                $"VIB {BuildHealthLabel(snapshot.hapticsTracking)}";

            _progressFill.fillAmount = Mathf.Clamp01(snapshot.objectiveProgress01);
            _progressFill.color = accentColor;
            _trackingText.color = ResolveTrackingColor(snapshot);
        }

        private void EnsureUi()
        {
            if (_backgroundImage != null)
            {
                return;
            }

            RectTransform root = GetComponent<RectTransform>();
            _backgroundImage = gameObject.GetComponent<Image>();
            if (_backgroundImage == null)
            {
                _backgroundImage = gameObject.AddComponent<Image>();
            }

            _backgroundImage.color = panelColor;

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _stageText = CreateText(root, "StageText", new Vector2(14f, -14f), new Vector2(-14f, -46f), font, 24, titleColor, FontStyle.Bold);
            _objectiveText = CreateText(root, "ObjectiveText", new Vector2(14f, -56f), new Vector2(-14f, -118f), font, 18, bodyColor, FontStyle.Normal);
            _focusText = CreateText(root, "FocusText", new Vector2(14f, -126f), new Vector2(-14f, -156f), font, 17, accentColor, FontStyle.Bold);
            _trackingText = CreateText(root, "TrackingText", new Vector2(14f, -164f), new Vector2(-14f, -210f), font, 14, bodyColor, FontStyle.Normal);

            RectTransform progressRoot = CreateRect(root, "ProgressRoot", new Vector2(14f, -222f), new Vector2(-14f, -248f));
            _progressBack = progressRoot.gameObject.AddComponent<Image>();
            _progressBack.color = new Color(0.16f, 0.18f, 0.24f, 1f);

            RectTransform fillRoot = CreateRect(progressRoot, "ProgressFill", Vector2.zero, Vector2.zero);
            fillRoot.anchorMin = Vector2.zero;
            fillRoot.anchorMax = Vector2.one;
            fillRoot.offsetMin = Vector2.zero;
            fillRoot.offsetMax = Vector2.zero;
            _progressFill = fillRoot.gameObject.AddComponent<Image>();
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = 0;
            _progressFill.fillAmount = 0f;
            _progressFill.color = accentColor;
        }

        private Text CreateText(RectTransform parent, string objectName, Vector2 offsetMin, Vector2 offsetMax, Font font, int fontSize, Color color, FontStyle fontStyle)
        {
            RectTransform rect = CreateRect(parent, objectName, offsetMin, offsetMax);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateRect(RectTransform parent, string objectName, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject childObject = new GameObject(objectName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            RectTransform rect = childObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
            rect.offsetMax = new Vector2(offsetMax.x, offsetMin.y);
            return rect;
        }

        private string BuildHealthLabel(ObserverTrackingHealth health)
        {
            return health switch
            {
                ObserverTrackingHealth.Fresh => "OK",
                ObserverTrackingHealth.Stale => "WARN",
                _ => "OFF",
            };
        }

        private Color ResolveTrackingColor(ObserverDisplaySnapshot snapshot)
        {
            if (snapshot.headTracking == ObserverTrackingHealth.Missing ||
                snapshot.padCameraTracking == ObserverTrackingHealth.Missing ||
                snapshot.padImuTracking == ObserverTrackingHealth.Missing)
            {
                return errorColor;
            }

            if (snapshot.headTracking == ObserverTrackingHealth.Stale ||
                snapshot.padCameraTracking == ObserverTrackingHealth.Stale ||
                snapshot.padImuTracking == ObserverTrackingHealth.Stale)
            {
                return staleColor;
            }

            return bodyColor;
        }
    }
}
