
using UnityEngine;
using System.Collections;

public class ThrowableWeapons : MonoBehaviour
{
    [Header("Weapon")]
    public Transform throwableItem;

    [Header("Throw Settings")]
    public float throwDistance = 3f;
    public float throwSpeed = 8f;
    public float itemRadius = 0.25f;

    [Header("Damage")]
    public int throwDamage = 40;

    [Header("Collision")]
    public LayerMask wallLayer;

    public bool IsFlying { get; private set; }

    void Awake()
    {
        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask("Walls");
    }

    public bool CanPickup(Vector2 playerPosition, float range)
    {
        if (throwableItem == null || IsFlying)
            return false;

        if (!throwableItem.gameObject.activeInHierarchy)
            return false;

        return Vector2.Distance(
            playerPosition,
            throwableItem.position
        ) <= range;
    }

    public float DistanceTo(Vector2 position)
    {
        if (throwableItem == null)
            return Mathf.Infinity;

        return Vector2.Distance(
            position,
            throwableItem.position
        );
    }

    public void Pickup()
    {
        if (throwableItem == null)
            return;

        throwableItem.gameObject.SetActive(false);

        Debug.Log("Throwable picked up!");
    }

    public void Drop(Vector2 position)
    {
        if (throwableItem == null)
            return;

        throwableItem.position = new Vector3(
            position.x,
            position.y,
            throwableItem.position.z
        );

        throwableItem.rotation = Quaternion.identity;
        throwableItem.gameObject.SetActive(true);
    }

    public bool Throw(Vector2 start, Vector2 direction)
    {
        if (throwableItem == null || IsFlying)
            return false;

        StartCoroutine(ThrowRoutine(start, direction));
        return true;
    }

    IEnumerator ThrowRoutine(Vector2 start, Vector2 direction)
    {
        IsFlying = true;

        direction = direction.normalized;

        if (direction == Vector2.zero)
            direction = Vector2.down;

        Vector2 position = start;
        float traveled = 0f;

        Drop(start);

        while (traveled < throwDistance)
        {
            float step = Mathf.Min(
                Mathf.Max(throwSpeed, 0.01f) * Time.deltaTime,
                throwDistance - traveled
            );

            if (step <= 0f)
            {
                yield return null;
                continue;
            }

            // Look for every collider along the next step.
            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                position,
                itemRadius,
                direction,
                step + 0.02f
            );

            float closestDistance = Mathf.Infinity;
            GuardHealth hitGuard = null;
            Collider2D hitWall = null;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                    continue;

                // Ignore vision and punch triggers.
                if (hit.collider.isTrigger)
                    continue;

                bool isWall =
                    (wallLayer.value &
                    (1 << hit.collider.gameObject.layer)) != 0;

                GuardHealth guard =
                    hit.collider.GetComponentInParent<GuardHealth>();

                // Ignore player and unrelated objects.
                if (!isWall && guard == null)
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitGuard = isWall ? null : guard;
                    hitWall = isWall ? hit.collider : null;
                }
            }

            // Hit a wall or NPC.
            if (closestDistance < Mathf.Infinity)
            {
                float safeStep = Mathf.Clamp(
                    closestDistance - 0.02f,
                    0f,
                    step
                );

                position += direction * safeStep;

                throwableItem.position = new Vector3(
                    position.x,
                    position.y,
                    throwableItem.position.z
                );

                if (hitGuard != null)
                {
                    hitGuard.TakeDamage(throwDamage, true);
                    Debug.Log("Throwable hit NPC!");
                }
                else if (hitWall != null)
                {
                    Debug.Log("Throwable hit wall!");
                }

                break;
            }

            position += direction * step;
            traveled += step;

            throwableItem.position = new Vector3(
                position.x,
                position.y,
                throwableItem.position.z
            );

            yield return null;
        }

        IsFlying = false;

        Debug.Log("Throwable landed. It can be picked up again!");
    }
}
