using UnityEngine;

namespace BellRinger.ObserverDisplay
{
    [DisallowMultipleComponent]
    public sealed class ObserverDisplayLayout : MonoBehaviour
    {
        [SerializeField, Range(0.18f, 0.45f)] private float bottomBandHeight01 = 0.31f;
        [SerializeField, Range(0.18f, 0.4f)] private float leftPanelWidth01 = 0.32f;
        [SerializeField, Range(0.18f, 0.4f)] private float rightPanelWidth01 = 0.32f;
        [SerializeField] private float panelMarginPixels = 18f;
        [SerializeField] private float panelGapPixels = 18f;
        [SerializeField] private float worldViewportTopInsetPixels = 14f;

        public float BottomBandHeight01 => bottomBandHeight01;
        public float LeftPanelWidth01 => leftPanelWidth01;
        public float RightPanelWidth01 => rightPanelWidth01;
        public float PanelMarginPixels => Mathf.Max(0f, panelMarginPixels);
        public float PanelGapPixels => Mathf.Max(0f, panelGapPixels);
        public float WorldViewportTopInsetPixels => Mathf.Max(0f, worldViewportTopInsetPixels);
    }
}
