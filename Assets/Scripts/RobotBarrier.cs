using UnityEngine;

public class RobotBarrier : MonoBehaviour
{
    // Static variable to easily share the collision state with pubtest.cs
    public static bool isCollisionActive = false;

    /// <summary>
    /// Checks whether any of the supplied world-space points violate this barrier.
    ///
    /// For every child Collider found on this GameObject (and its descendants):
    ///   - Tag "robot_barrier"          → violated when a point IS inside the collider.
    ///   - Tag "robot_barrier_inverted" → violated when a point is NOT inside the collider
    ///                                    (i.e. points must stay contained; leaving is a violation).
    ///
    /// Returns true if at least one violation is detected across all child colliders and all points.
    /// Also sets the static <see cref="isCollisionActive"/> flag and logs state changes.
    /// </summary>
    /// <param name="points">World-space positions to test (e.g. the trigger point transforms' positions).</param>
    public bool CheckPoints(Vector3[] points)
    {
        Collider[] childColliders = GetComponentsInChildren<Collider>();

        // Don't visually flag barriers while recording a bag or while active control is paused
        // (held both triggers) — the tracked points aren't meaningfully driving the arm in either case.
        bool barriersVisuallySuppressed = CommandSlotDashboard.IsRecording || !ROSPublishToggle.IsPublishingEnabled;

        bool anyViolation = false;
        string hitColliderName = "";
        string hitColliderTag = "";
        int hitPointIndex = -1;

        foreach (Collider col in childColliders)
        {
            if (col == null) continue;

            bool isInverted = col.gameObject.CompareTag("robot_barrier_inverted");
            bool isNormal   = col.gameObject.CompareTag("robot_barrier");

            if (!isInverted && !isNormal)
            {
                Debug.LogWarning("[RobotBarrier] No recognised tag on collider '" + col.name + "'. Skipping.");
                continue;
            }

            // Check whether any point violates this individual collider
            bool colViolated = false;
            for (int i = 0; i < points.Length; i++)
            {
                bool inside   = IsPointInside(points[i], col);
                bool violated = isInverted ? !inside : inside;

                if (violated)
                {
                    colViolated    = true;
                    hitColliderName = col.gameObject.name;
                    hitColliderTag  = col.gameObject.tag;
                    hitPointIndex   = i;
                    break;
                }
            }

            // Toggle this collider's MeshRenderer based on its own result
            MeshRenderer rend = col.GetComponent<MeshRenderer>();
            if (rend != null)
                rend.enabled = colViolated && !barriersVisuallySuppressed;

            if (colViolated)
                anyViolation = true;
        }

        // Update the shared flag and log only when the overall state changes
        if (anyViolation != isCollisionActive)
        {
            isCollisionActive = anyViolation;
            if (anyViolation)
                Debug.Log($"[RobotBarrier] VIOLATION DETECTED: Point[{hitPointIndex}] triggered '{hitColliderName}' (tag: {hitColliderTag}). ROS publishing BLOCKED.");
            else
                Debug.Log("[RobotBarrier] All points clear. ROS publishing RESUMED.");
        }

        return anyViolation;
    }

    // ── Helpers ──

    /// <summary>
    /// Returns true if <paramref name="worldPoint"/> is inside <paramref name="col"/>.
    /// SphereColliders use distance-to-radius math.
    /// All other collider types fall back to <c>bounds.Contains()</c>.
    /// </summary>
    private static bool IsPointInside(Vector3 worldPoint, Collider col)
    {
        if (col is SphereCollider sphere)
        {
            // Compute the world-space center (handles center offset + position)
            Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
            // Scale the radius by the largest lossy-scale axis to handle non-uniform scale
            float worldRadius = sphere.radius * Mathf.Max(
                sphere.transform.lossyScale.x,
                sphere.transform.lossyScale.y,
                sphere.transform.lossyScale.z);
            //bool inside = Vector3.Distance(worldPoint, worldCenter) <= sphere.radius;
            bool inside = Vector3.Distance(worldPoint, worldCenter) <= worldRadius;
            //Debug.Log($"[RobotBarrier] SphereCollider '{col.gameObject.name}', point inside: {inside}");
            //Debug.Log($"Radius: {sphere.radius}, distance: {Vector3.Distance(worldPoint, worldCenter)} point inside: {inside}");
            return inside;
        }
        else
        {
            // BoxCollider, CapsuleCollider, MeshCollider, etc. → use AABB bounds
            bool inside = col.bounds.Contains(worldPoint);
            //Debug.Log($"[RobotBarrier] Collider '{col.gameObject.name}' ({col.GetType().Name}), point inside: {inside}");
            return inside;
        }
    }
}
