using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoAuthoringPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private bool loop;
        [SerializeField] private bool autoCollectChildren = true;

        public int Count => waypoints != null ? waypoints.Length : 0;
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        private void OnValidate()
        {
            if (autoCollectChildren)
            {
                CollectChildWaypoints();
            }
        }

        public void CollectChildWaypoints()
        {
            int childCount = transform.childCount;
            waypoints = new Transform[childCount];
            for (int index = 0; index < childCount; index++)
            {
                waypoints[index] = transform.GetChild(index);
            }
        }

        public bool TryGetWorldPoint(int index, out Vector3 worldPosition)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                worldPosition = default;
                return false;
            }

            Transform waypoint = waypoints[Mathf.Clamp(index, 0, waypoints.Length - 1)];
            if (waypoint == null)
            {
                worldPosition = default;
                return false;
            }

            worldPosition = waypoint.position;
            return true;
        }

        public bool TryEvaluateWorldPosition01(float normalized, out Vector3 worldPosition)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                worldPosition = default;
                return false;
            }

            if (waypoints.Length == 1)
            {
                return TryGetWorldPoint(0, out worldPosition);
            }

            float clamped = loop ? Mathf.Repeat(normalized, 1f) : Mathf.Clamp01(normalized);
            float scaled = loop ? clamped * waypoints.Length : clamped * (waypoints.Length - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, waypoints.Length - 1);
            int nextIndex = loop ? (index + 1) % waypoints.Length : Mathf.Min(index + 1, waypoints.Length - 1);
            float t = Mathf.Clamp01(scaled - index);

            Transform current = waypoints[index];
            Transform next = waypoints[nextIndex];
            if (current == null || next == null)
            {
                worldPosition = default;
                return false;
            }

            worldPosition = Vector3.Lerp(current.position, next.position, Smooth01(t));
            return true;
        }

        public bool TrySetWorldPoint(int index, Vector3 worldPosition)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length || waypoints[index] == null)
            {
                return false;
            }

            waypoints[index].position = worldPosition;
            return true;
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
