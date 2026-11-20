
using UnityEngine;
using System.Collections;

public class PlayerInventory : MonoBehaviour
{
    public Transform throwableItem;

    public float pickupRange = 1.5f;
    public float throwDistance = 3f;
    public float throwSpeed = 8f;
    public float itemRadius = 0.25f;

    private bool hasItem = false;
    private bool isFlying = false;
    private bool inventoryOpen = false;

    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;

    private int wallMask;

    void Start()
    {
        previousPosition = transform.position;

        // Automatically find the Walls layer
        wallMask = LayerMask.GetMask("Walls");

        if (wallMask == 0)
        {
            Debug.LogError(
                "Walls layer does not exist! Create it in Unity."
            );
        }
    }

    void LateUpdate()
    {
        // Remember the last movement direction
        Vector2 currentPosition = transform.position;
        Vector2 movement = currentPosition - previousPosition;

        if (movement.sqrMagnitude > 0.00001f)
        {
            facingDirection = movement.normalized;
        }

        previousPosition = currentPosition;
    }

    // BUTTON A - PICK UP
    public void TryPickup()
    {
        if (hasItem || isFlying || throwableItem == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            throwableItem.position
        );

        if (distance <= pickupRange)
        {
            hasItem = true;
            throwableItem.gameObject.SetActive(false);

            Debug.Log("Item picked up!");
        }
        else
        {
            Debug.Log("Move closer!");
        }
    }

    // BUTTON B - THROW
    public void TryThrow()
    {
        Debug.Log("Throw button pressed!");

        if (!hasItem || isFlying || throwableItem == null)
        {
            Debug.Log("Cannot throw - no item available!");
            return;
        }

        StartCoroutine(ThrowItem());
    }

    IEnumerator ThrowItem()
    {
        hasItem = false;
        isFlying = true;

        Vector2 start = transform.position;
        Vector2 direction = facingDirection.normalized;

        float distance = throwDistance;

        // Check the entire path before throwing
        RaycastHit2D hit = Physics2D.CircleCast(
            start,
            itemRadius,
            direction,
            throwDistance,
            wallMask
        );

        if (hit.collider != null)
        {
            // Stop before the wall
            distance = Mathf.Max(
                0f,
                hit.distance - 0.02f
            );

            Debug.Log(
                "WALL DETECTED: " + hit.collider.name
            );
        }
        else
        {
            Debug.Log(
                "NO WALL DETECTED in the throwing path."
            );
        }

        Vector2 target = start + direction * distance;

        // Make the item visible
        throwableItem.position = start;
        throwableItem.gameObject.SetActive(true);

        // Move to the stopping position
        while (Vector2.Distance(
            throwableItem.position,
            target
        ) > 0.01f)
        {
            throwableItem.position = Vector2.MoveTowards(
                throwableItem.position,
                target,
                throwSpeed * Time.deltaTime
            );

            yield return null;
        }

        throwableItem.position = target;

        isFlying = false;

        Debug.Log("Item landed!");
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

        string text = hasItem
            ? "Inventory: Throwable Item x1"
            : "Inventory: Empty";

        GUI.Box(new Rect(20, 20, 280, 100), text);
    }
}
