
using UnityEngine;
using System.Collections;

public class PlayerInventory : MonoBehaviour
{
    [Header("Throwable Item")]
    public Transform throwableItem;

    [Header("Settings")]
    public float pickupRange = 1.5f;
    public float throwDistance = 3f;
    public float throwSpeed = 8f;
    public float itemRadius = 0.2f;

    public LayerMask wallLayer;

    private bool hasItem = false;
    private bool isFlying = false;
    private bool inventoryOpen = false;

    private Vector2 facingDirection = Vector2.down;
    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        // Remember which direction the player last moved.
        Vector2 movement = transform.position - previousPosition;

        if (movement.sqrMagnitude > 0.00001f)
        {
            facingDirection = movement.normalized;
        }

        previousPosition = transform.position;
    }

    // BUTTON A - PICK UP
    public void TryPickup()
    {
        if (hasItem)
        {
            Debug.Log("Inventory is full!");
            return;
        }

        if (throwableItem == null)
        {
            Debug.LogWarning("No throwable item assigned!");
            return;
        }

        if (isFlying)
            return;

        float distance = Vector2.Distance(
            transform.position,
            throwableItem.position
        );

        if (distance <= pickupRange)
        {
            hasItem = true;

            // Hide the item while it is in inventory.
            throwableItem.gameObject.SetActive(false);

            Debug.Log("Item picked up!");
        }
        else
        {
            Debug.Log("Move closer to the item!");
        }
    }

    // BUTTON B - THROW
    public void TryThrow()
    {
        if (!hasItem || isFlying)
        {
            Debug.Log("You have no item to throw!");
            return;
        }

        StartCoroutine(ThrowItem());
    }

    IEnumerator ThrowItem()
    {
        hasItem = false;
        isFlying = true;

        Vector2 startPosition = transform.position;

        // Throw in the player's last movement direction.
        Vector2 direction = facingDirection;

        // Check whether a wall is in the way.
        RaycastHit2D hit = Physics2D.CircleCast(
            startPosition,
            itemRadius,
            direction,
            throwDistance,
            wallLayer
        );

        float distance = throwDistance;

        if (hit.collider != null)
        {
            distance = Mathf.Max(0f, hit.distance - 0.05f);
        }

        Vector2 targetPosition =
            startPosition + direction * distance;

        // Make the item visible again.
        throwableItem.gameObject.SetActive(true);

        throwableItem.position = startPosition;

        // Move the item toward the target.
        while (Vector2.Distance(
            throwableItem.position,
            targetPosition) > 0.01f)
        {
            throwableItem.position = Vector2.MoveTowards(
                throwableItem.position,
                targetPosition,
                throwSpeed * Time.deltaTime
            );

            yield return null;
        }

        throwableItem.position = targetPosition;

        isFlying = false;

        Debug.Log("Item thrown! You can pick it up again.");
    }

    // BUTTON C - INVENTORY
    public void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;
    }

    void OnGUI()
    {
        if (!inventoryOpen)
            return;

        string inventoryText = hasItem
            ? "Inventory: Throwable Item x1"
            : "Inventory: Empty";

        GUI.Box(
            new Rect(20, 20, 280, 100),
            inventoryText
        );
    }
}
