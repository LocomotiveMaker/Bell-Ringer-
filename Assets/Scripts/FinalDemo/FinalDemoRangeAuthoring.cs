using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoRangeAuthoring : MonoBehaviour
    {
        [SerializeField] private string label = "Sound + LED Range";
        [SerializeField] private float radiusMeters = 3.2f;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.9f);

        public string Label
        {
            get => label;
            set => label = value;
        }

        public float RadiusMeters
        {
            get => Mathf.Max(0.05f, radiusMeters);
            set => radiusMeters = Mathf.Max(0.05f, value);
        }

        public Color GizmoColor
        {
            get => gizmoColor;
            set => gizmoColor = value;
        }

        private void OnValidate()
        {
            radiusMeters = Mathf.Max(0.05f, radiusMeters);
        }

        public float EvaluateLinear01(Vector3 playerWorldPosition)
        {
            Vector2 player = new Vector2(playerWorldPosition.x, playerWorldPosition.z);
            Vector2 center = new Vector2(transform.position.x, transform.position.z);
            float distance = Vector2.Distance(player, center);
            return 1f - Mathf.Clamp01(distance / RadiusMeters);
        }

        private void OnDrawGizmos()
        {
            DrawRangeGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRangeGizmo(true);
        }

        private void DrawRangeGizmo(bool selected)
        {
            Color color = gizmoColor;
            color.a = selected ? 1f : 0.55f;
            Gizmos.color = color;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * RadiusMeters);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * RadiusMeters);

#if UNITY_EDITOR
            string text = string.IsNullOrWhiteSpace(label) ? name : label;
            Handles.color = color;
            Handles.DrawWireDisc(transform.position, Vector3.up, RadiusMeters);
            Handles.Label(transform.position + Vector3.up * 0.25f, $"{text}\nR {RadiusMeters:0.00}m");
#else
            Gizmos.DrawWireSphere(transform.position, RadiusMeters);
#endif
        }
    }
}
