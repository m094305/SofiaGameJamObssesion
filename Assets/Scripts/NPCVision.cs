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
        visionCollider.isTrigger = true;

        if (guard == null)
            Debug.LogError(name + ": NPCVision needs an NPCGuard in its parent hierarchy.");
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
}
