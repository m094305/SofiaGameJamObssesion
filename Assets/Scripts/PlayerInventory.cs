
using UnityEngine;
using System.Collections;

public class PlayerInventory : MonoBehaviour
{
    public enum WeaponType
    {
        None,
        Ranged,
        Melee
    }

    [Header("Weapons")]
    public Transform throwableItem;
    public Transform meleeWeapon;

    [Header("Pickup Settings")]
    public float pickupRange = 1.5f;

    [Header("Throw Settings")]
    public float throwDistance = 3f;
    public float throwSpeed = 8f;
    public float itemRadius = 0.25f;

    [Header("Melee Settings")]
    public float meleeRange = 0.7f;
    public float swingDuration = 0.2f;

    private WeaponType equippedWeapon = WeaponType.None;

    private bool isFlying = false;
    private bool isSwinging = false;
    private bool inventoryOpen = false;

    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;

    private int wallMask;

    void Start()
    {
        previousPosition = transform.position;
        wallMask = LayerMask.GetMask("Walls");
    }

    void LateUpdate()
    {
        Vector2 currentPosition = transform.position;
        Vector2 movement = currentPosition - previousPosition;

        if (movement.sqrMagnitude > 0.00001f)
        {
            facingDirection = movement.normalized;
        }

        previousPosition = currentPosition;
    }

    // BUTTON A - PICK UP WEAPON

    public void TryPickup()
    {
        Debug.Log("A button pressed - trying to pick up weapon");

        if (isSwinging)
        {
            Debug.Log("Cannot pick up while swinging!");
            return;
        }

        if (meleeWeapon == null)
        {
            Debug.LogWarning("Melee weapon is NOT assigned!");
        }
        else
        {
            float distance = Vector2.Distance(
                transform.position,
                meleeWeapon.position
            );

            Debug.Log("Distance to melee: " + distance);
            Debug.Log("Pickup range: " + pickupRange);
            Debug.Log("Melee active: " +
                meleeWeapon.gameObject.activeInHierarchy);
        }

        WeaponType nearbyWeapon = FindNearbyWeapon();

        if (nearbyWeapon == WeaponType.None)
        {
            Debug.Log("No weapon in pickup range!");
            return;
        }

        if (equippedWeapon != WeaponType.None)
        {
            DropEquippedWeapon();
        }

        Transform item = GetWeaponObject(nearbyWeapon);

        equippedWeapon = nearbyWeapon;
        item.gameObject.SetActive(false);

        Debug.Log("Successfully picked up: " + equippedWeapon);
    }

    WeaponType FindNearbyWeapon()
    {
        WeaponType closest = WeaponType.None;
        float closestDistance = pickupRange;

        if (throwableItem != null &&
            throwableItem.gameObject.activeInHierarchy &&
            !isFlying)
        {
            float distance = Vector2.Distance(
                transform.position,
                throwableItem.position
            );

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = WeaponType.Ranged;
            }
        }

        if (meleeWeapon != null &&
            meleeWeapon.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(
                transform.position,
                meleeWeapon.position
            );

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = WeaponType.Melee;
            }
        }

        return closest;
    }

    Transform GetWeaponObject(WeaponType type)
    {
        if (type == WeaponType.Ranged)
            return throwableItem;

        if (type == WeaponType.Melee)
            return meleeWeapon;

        return null;
    }

    void DropEquippedWeapon()
    {
        Transform item = GetWeaponObject(equippedWeapon);

        if (item == null)
            return;

        Vector2 start = transform.position;
        Vector2 direction = -facingDirection;

        float distance = 0.7f;

        // Prevent dropping inside a wall.
        RaycastHit2D hit = Physics2D.CircleCast(
            start,
            itemRadius,
            direction,
            distance,
            wallMask
        );

        if (hit.collider != null)
        {
            distance = Mathf.Max(0f, hit.distance - 0.02f);
        }

        Vector2 dropPosition = start + direction * distance;

        item.position = new Vector3(
            dropPosition.x,
            dropPosition.y,
            0f
        );

        item.rotation = Quaternion.identity;
        item.gameObject.SetActive(true);

        Debug.Log("Dropped: " + equippedWeapon);

        equippedWeapon = WeaponType.None;
    }

    // BUTTON B - ATTACK OR THROW
    public void TryThrow()
    {
        if (equippedWeapon == WeaponType.Ranged)
        {
            if (!isFlying)
            {
                StartCoroutine(ThrowRangedWeapon());
            }
        }
        else if (equippedWeapon == WeaponType.Melee)
        {
            if (!isSwinging)
            {
                StartCoroutine(MeleeSwing());
            }
        }
        else
        {
            Debug.Log("No weapon equipped!");
        }
    }

    // RANGED WEAPON THROW
    IEnumerator ThrowRangedWeapon()
    {
        isFlying = true;
        equippedWeapon = WeaponType.None;

        Vector2 start = transform.position;
        Vector2 direction = facingDirection.normalized;

        float distance = throwDistance;

        // Stop the item when it reaches a wall.
        RaycastHit2D hit = Physics2D.CircleCast(
            start,
            itemRadius,
            direction,
            throwDistance,
            wallMask
        );

        if (hit.collider != null)
        {
            distance = Mathf.Max(0f, hit.distance - 0.02f);
        }

        Vector2 target = start + direction * distance;

        throwableItem.position = start;
        throwableItem.gameObject.SetActive(true);

        while (Vector2.Distance(
            throwableItem.position, target) > 0.01f)
        {
            throwableItem.position = Vector2.MoveTowards(
                throwableItem.position,
                target,
                Mathf.Max(throwSpeed, 0.01f) * Time.deltaTime
            );

            yield return null;
        }

        throwableItem.position = target;

        isFlying = false;

        Debug.Log("Ranged weapon landed!");
    }

    // MELEE WEAPON SWING
    IEnumerator MeleeSwing()
    {
        if (meleeWeapon == null)
            yield break;

        isSwinging = true;

        Debug.Log("Melee swing!");

        meleeWeapon.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, swingDuration);

        Vector2 forward = facingDirection.normalized;
        Vector2 side = new Vector2(-forward.y, forward.x);

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            float sideOffset = Mathf.Lerp(
                -0.4f,
                0.4f,
                progress
            );

            Vector2 position =
                (Vector2)transform.position +
                forward * meleeRange +
                side * sideOffset;

            meleeWeapon.position = new Vector3(
                position.x,
                position.y,
                0f
            );

            float angle =
                Mathf.Atan2(forward.y, forward.x) *
                Mathf.Rad2Deg - 90f;

            meleeWeapon.rotation = Quaternion.Euler(
                0f,
                0f,
                angle + Mathf.Lerp(-50f, 50f, progress)
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        meleeWeapon.gameObject.SetActive(false);
        meleeWeapon.rotation = Quaternion.identity;

        isSwinging = false;
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

        GUI.Box(
            new Rect(20, 20, 280, 100),
            "Inventory: " + equippedWeapon
        );
    }
}
