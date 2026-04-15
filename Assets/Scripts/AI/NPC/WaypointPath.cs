using UnityEngine;

namespace AI.NPC
{
    public class WaypointPath : MonoBehaviour
    {
        public Transform[] points;

        private void OnDrawGizmos()
        {
            if (points == null || points.Length < 1) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null) continue;

                // Draw sphere for each waypoint
                Gizmos.DrawSphere(points[i].position, 0.4f);

                // Draw line to next waypoint
                if (i < points.Length - 1)
                {
                    if (points[i+1] != null)
                        Gizmos.DrawLine(points[i].position, points[i+1].position);
                }
                else
                {
                    // Loop back to start
                    if (points[0] != null)
                        Gizmos.DrawLine(points[i].position, points[0].position);
                }
            }
        }
    }
}
