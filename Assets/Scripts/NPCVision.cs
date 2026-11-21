
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class NPCVision : MonoBehaviour
{

    private NPCGuard guard;
    private PolygonCollider2D visionCollider;

    void Awake()
    {
        guard = GetComponentInParent<NPCGuard>();
        visionCollider = GetComponent<PolygonCollider2D>();

        if (guard == null)
        {
            Debug.LogError("NPCVision needs an NPCGuard parent!");
            return;
        }

        visionCollider.isTrigger = true;

        CreateVisionCone();
    }

    void CreateVisionCone()
    {
        int segments = 24;

        Vector2[] points = new Vector2[segments + 2];

        // The first point is the guard's position.
        points[0] = Vector2.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle =
                -guard.visionAngle / 2f +
                guard.visionAngle * i / segments;

            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) *
                      guard.visionDistance;

            float y = Mathf.Cos(radians) *
                      guard.visionDistance;

            points[i + 1] = new Vector2(x, y);
        }
        // Make the polygon into a cone.
        visionCollider.pathCount = 1;
        visionCollider.SetPath(0, points);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (guard != null)
            guard.TrySpot(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (guard != null)
            guard.TrySpot(other);
    }

    // Show the viewing area in Scene view.
    void OnDrawGizmosSelected()
    {
        NPCGuard npc = GetComponentInParent<NPCGuard>();

        if (npc == null)
            return;

        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position;
        Vector3 previous = origin;

        int segments = 24;

        for (int i = 0; i <= segments; i++)
        {
            float angle =
                -npc.visionAngle / 2f +
                npc.visionAngle * i / segments;

            float radians = angle * Mathf.Deg2Rad;

            Vector3 localPoint = new Vector3(
                Mathf.Sin(radians) * npc.visionDistance,
                Mathf.Cos(radians) * npc.visionDistance,
                0f
            );

            Vector3 worldPoint =
                transform.TransformPoint(localPoint);

            Gizmos.DrawLine(previous, worldPoint);
            previous = worldPoint;
        }

        Gizmos.DrawLine(previous, origin);
    }
}
