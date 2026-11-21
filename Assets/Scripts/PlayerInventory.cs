
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public enum WeaponType
    {
        None,
        Ranged,
        Melee
    }

    [Header("Player")]
    public Transform player;

    [Header("Weapon Systems")]
    public ThrowableWeapons throwableWeapons;
    public MeleeWeapons meleeWeapons;

    [Header("Settings")]
    public float pickupRange = 2f;
    public float dropDistance = 0.7f;

    private WeaponType equippedWeapon = WeaponType.None;

    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;

    private bool inventoryOpen = false;

    // Read by InventoryView (the on-screen inventory panel).
    public bool IsOpen => inventoryOpen;
    public WeaponType Equipped => equippedWeapon;

    void Awake()
    {
        // Inventory should be directly under Player.
        if (player == null)
            player = transform.parent;

        if (throwableWeapons == null)
            throwableWeapons =
                GetComponentInChildren<ThrowableWeapons>(true);

        if (meleeWeapons == null)
            meleeWeapons =
                GetComponentInChildren<MeleeWeapons>(true);

        if (player != null)
            previousPosition = player.position;
        else
            Debug.LogError("Player is not assigned!");
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector2 currentPosition = player.position;
        Vector2 movement = currentPosition - previousPosition;

        if (movement.sqrMagnitude > 0.00001f)
            facingDirection = movement.normalized;

        previousPosition = currentPosition;
    }

    // A - PICK UP A WEAPON
    public void TryPickup()
    {
        if (player == null)
        {
            Debug.LogError("Player missing!");
            return;
        }

        if (meleeWeapons != null && meleeWeapons.IsSwinging)
            return;

        Vector2 position = player.position;

        bool canPickupRanged =
            throwableWeapons != null &&
            throwableWeapons.CanPickup(position, pickupRange);

        bool canPickupMelee =
            meleeWeapons != null &&
            meleeWeapons.CanPickup(position, pickupRange);

        if (!canPickupRanged && !canPickupMelee)
        {
            Debug.Log("No weapon in pickup range!");
            return;
        }

        WeaponType selected;

        // Find the closest available weapon.
        if (canPickupRanged && canPickupMelee)
        {
            float rangedDistance =
                throwableWeapons.DistanceTo(position);

            float meleeDistance =
                meleeWeapons.DistanceTo(position);

            selected = meleeDistance <= rangedDistance
                ? WeaponType.Melee
                : WeaponType.Ranged;
        }
        else
        {
            selected = canPickupMelee
                ? WeaponType.Melee
                : WeaponType.Ranged;
        }

        // Drop the old weapon before picking up the new one.
        if (equippedWeapon != WeaponType.None)
        {
            DropEquippedWeapon();
        }

        if (selected == WeaponType.Ranged)
        {
            throwableWeapons.Pickup();
        }
        else
        {
            meleeWeapons.Pickup();
        }

        equippedWeapon = selected;

        Debug.Log("EQUIPPED: " + equippedWeapon);
    }

    // DROP EQUIPPED WEAPON
    void DropEquippedWeapon()
    {
        if (player == null)
            return;

        Vector2 direction = -facingDirection;
        float distance = dropDistance;

        int wallMask = LayerMask.GetMask("Walls");

        if (wallMask != 0)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                player.position,
                0.25f,
                direction,
                dropDistance,
                wallMask
            );

            if (hit.collider != null)
            {
                distance = Mathf.Max(
                    0f,
                    hit.distance - 0.02f
                );
            }
        }

        Vector2 position =
            (Vector2)player.position + direction * distance;

        if (equippedWeapon == WeaponType.Ranged)
        {
            throwableWeapons.Drop(position);
        }
        else if (equippedWeapon == WeaponType.Melee)
        {
            meleeWeapons.Drop(position);
        }

        equippedWeapon = WeaponType.None;
    }

    // B - THROW OR SWING
    public void TryThrow()
    {
        if (player == null)
            return;

        if (equippedWeapon == WeaponType.Ranged)
        {
            if (throwableWeapons != null &&
                throwableWeapons.Throw(
                    player.position,
                    facingDirection
                ))
            {
                // Weapon has left the inventory.
                equippedWeapon = WeaponType.None;
            }
        }
        else if (equippedWeapon == WeaponType.Melee)
        {
            if (meleeWeapons != null)
            {
                meleeWeapons.Attack(
                    player,
                    facingDirection
                );
            }
        }
        else
        {
            Debug.Log("Inventory is empty!");
        }
    }

    // C - OPEN / CLOSE INVENTORY
    public void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;
    }

    void OnGUI()
    {
        // The InventoryView panel replaces this debug box when it's in the scene.
        if (!inventoryOpen || InventoryView.Exists)
            return;

        GUI.Box(
            new Rect(20, 20, 280, 100),
            "Inventory: " + equippedWeapon
        );
    }
}
