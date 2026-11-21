
using UnityEngine;
using System.Collections;

public class ThrowableWeapons : MonoBehaviour
{
    [Header("Weapon Object")]
    public Transform throwableItem;

    [Header("Throw Settings")]
    public float throwDistance = 3f;
    public float throwSpeed = 8f;
    public float itemRadius = 0.25f;

    [Header("Walls")]
    public LayerMask wallLayer;

    public bool IsFlying { get; private set; }

    void Awake()
    {
        // Automatically find the Walls layer if not assigned.
        if (wallLayer.value == 0)
        {
            wallLayer = LayerMask.GetMask("Walls");
        }
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

    // Hide the item when collected.
    public void Pickup()
    {
        if (throwableItem == null)
            return;

        throwableItem.gameObject.SetActive(false);

        Debug.Log("Throwable picked up!");
    }

    // Make the item visible on the ground.
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

    // Throw the equipped weapon.
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

        // Show the weapon at the player's position.
        Drop(position);

        // Move until maximum distance or wall collision.
        while (traveled < throwDistance)
        {
            float step = Mathf.Min(
                Mathf.Max(throwSpeed, 0.01f) * Time.deltaTime,
                throwDistance - traveled
            );

            // Check whether the next movement hits a wall.
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

                Debug.Log("Throwable hit: " + hit.collider.name);
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

        // The item remains visible where it landed.
        IsFlying = false;

        Debug.Log("Throwable landed! Pick it up with A.");
    }
}
