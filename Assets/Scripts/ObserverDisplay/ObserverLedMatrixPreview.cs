using BellRinger.FinalDemo;
using UnityEngine;
using UnityEngine.UI;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ObserverLedMatrixPreview : MonoBehaviour
    {
        [SerializeField] private FinalDemoLightRouter lightRouter;
        [SerializeField, Range(1f, 10f)] private float brightnessMultiplier = 4f;
        [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        [SerializeField] private Color gridBackColor = new Color(0.03f, 0.03f, 0.05f, 1f);
        [SerializeField] private Color cellOffColor = new Color(0.02f, 0.02f, 0.025f, 1f);

        private Image _backgroundImage;
        private Text _titleText;
        private Text _footerText;
        private RectTransform _gridRoot;
        private GridLayoutGroup _gridLayoutGroup;
        private Image[] _cells;
        private Color[] _scratchFrame;
        private int _lastFrameVersion = -1;

        private void Awake()
        {
            EnsureUi();
        }

        private void LateUpdate()
        {
            RefreshFromRouter();
            UpdateGridCellSize();
        }

        public void Bind(FinalDemoLightRouter router)
        {
            lightRouter = router;
            _lastFrameVersion = -1;
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
            _titleText = CreateText(root, "LedTitle", new Vector2(14f, -14f), new Vector2(-14f, -42f), font, 20, Color.white, FontStyle.Bold);
            _titleText.text = "16x8 LED PREVIEW";
            _footerText = CreateText(root, "LedFooter", new Vector2(14f, -240f), new Vector2(-14f, -268f), font, 14, new Color(0.64f, 0.68f, 0.76f, 1f), FontStyle.Normal);
            _footerText.text = "Bottom row matches the board bottom row.";

            _gridRoot = CreateRect(root, "LedGrid", new Vector2(14f, -52f), new Vector2(-14f, -232f));
            Image gridBack = _gridRoot.gameObject.AddComponent<Image>();
            gridBack.color = gridBackColor;

            _gridLayoutGroup = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = 16;
            _gridLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
            _gridLayoutGroup.spacing = new Vector2(2f, 2f);
            _gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayoutGroup.startCorner = GridLayoutGroup.Corner.LowerLeft;

            _cells = new Image[16 * 8];
            for (int i = 0; i < _cells.Length; i++)
            {
                GameObject cellObject = new GameObject($"Cell_{i:000}", typeof(RectTransform));
                cellObject.transform.SetParent(_gridRoot, false);
                Image cellImage = cellObject.AddComponent<Image>();
                cellImage.color = cellOffColor;
                _cells[i] = cellImage;
            }

            _scratchFrame = new Color[_cells.Length];
        }

        private void RefreshFromRouter()
        {
            EnsureUi();
            lightRouter ??= FindFirstObjectByType<FinalDemoLightRouter>();
            if (lightRouter == null)
            {
                return;
            }

            if (_scratchFrame == null || _scratchFrame.Length != lightRouter.LogicalFrameWidth * lightRouter.LogicalFrameHeight)
            {
                _scratchFrame = new Color[lightRouter.LogicalFrameWidth * lightRouter.LogicalFrameHeight];
            }

            if (_lastFrameVersion == lightRouter.LogicalFrameVersion)
            {
                return;
            }

            lightRouter.CopyLogicalLedFrame(_scratchFrame);
            for (int i = 0; i < _cells.Length && i < _scratchFrame.Length; i++)
            {
                Color source = _scratchFrame[i];
                if (source.maxColorComponent <= 0.0001f)
                {
                    _cells[i].color = cellOffColor;
                    continue;
                }

                Color boosted = source * brightnessMultiplier;
                boosted.r = Mathf.Clamp01(boosted.r);
                boosted.g = Mathf.Clamp01(boosted.g);
                boosted.b = Mathf.Clamp01(boosted.b);
                boosted.a = 1f;
                _cells[i].color = boosted;
            }

            _lastFrameVersion = lightRouter.LogicalFrameVersion;
        }

        private void UpdateGridCellSize()
        {
            if (_gridRoot == null || _gridLayoutGroup == null)
            {
                return;
            }

            Rect rect = _gridRoot.rect;
            float availableWidth = rect.width - _gridLayoutGroup.padding.left - _gridLayoutGroup.padding.right - (_gridLayoutGroup.spacing.x * 15f);
            float availableHeight = rect.height - _gridLayoutGroup.padding.top - _gridLayoutGroup.padding.bottom - (_gridLayoutGroup.spacing.y * 7f);
            float cellWidth = Mathf.Max(2f, availableWidth / 16f);
            float cellHeight = Mathf.Max(2f, availableHeight / 8f);
            float cellSize = Mathf.Min(cellWidth, cellHeight);
            _gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
        }

        private static Text CreateText(RectTransform parent, string objectName, Vector2 offsetMin, Vector2 offsetMax, Font font, int fontSize, Color color, FontStyle fontStyle)
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
    }
}
