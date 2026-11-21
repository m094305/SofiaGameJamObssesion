
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

    [Header("Collision")]
    public LayerMask wallLayer;

    public bool IsFlying { get; private set; }

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

        Debug.Log("Ranged weapon picked up!");
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

    public void Throw(Vector2 start, Vector2 direction)
    {
        if (throwableItem == null || IsFlying)
            return;

        StartCoroutine(ThrowRoutine(start, direction));
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

        if (wallLayer.value == 0)
            Debug.LogWarning("Assign the Walls layer!");

        while (traveled < throwDistance)
        {
            float step = Mathf.Min(
                Mathf.Max(throwSpeed, 0.01f) * Time.deltaTime,
                throwDistance - traveled
            );

            // Check for walls before moving.
            RaycastHit2D hit = Physics2D.CircleCast(
                position,
                itemRadius,
                direction,
                step + 0.02f,
                wallLayer
            );

            if (hit.collider != null)
            {
                float safeStep = Mathf.Clamp(
                    hit.distance - 0.02f,
                    0f,
                    step
                );

                position += direction * safeStep;

                throwableItem.position = new Vector3(
                    position.x,
                    position.y,
                    throwableItem.position.z
                );

                Debug.Log("Hit wall: " + hit.collider.name);
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

        Debug.Log("Throwable weapon landed!");
    }
}
