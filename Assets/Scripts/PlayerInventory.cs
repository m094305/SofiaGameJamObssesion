
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

    [Header("Inventory Children")]
    public ThrowableWeapons throwableWeapons;
    public MeleeWeapons meleeWeapons;

    [Header("Settings")]
    public float pickupRange = 1.5f;
    public float dropDistance = 0.6f;

    private WeaponType equippedWeapon = WeaponType.None;

    private Vector2 facingDirection = Vector2.down;
    private Vector2 previousPosition;

    private bool inventoryOpen = false;

    void Awake()
    {
        if (player == null)
            player = transform.parent;

        if (throwableWeapons == null)
            throwableWeapons = GetComponentInChildren<ThrowableWeapons>();

        if (meleeWeapons == null)
            meleeWeapons = GetComponentInChildren<MeleeWeapons>();

        if (player != null)
            previousPosition = player.position;
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

    // BUTTON A - PICK UP / SWITCH

    public void TryPickup()
    {
        Debug.Log("A pressed - Checking weapons");

        if (player == null)
        {
            Debug.LogError("Player is not assigned!");
            return;
        }

        if (throwableWeapons == null)
        {
            Debug.LogError("ThrowableWeapons is not assigned!");
        }

        if (meleeWeapons == null)
        {
            Debug.LogError("MeleeWeapons is not assigned!");
        }

        if (meleeWeapons != null && meleeWeapons.IsSwinging)
            return;

        Vector2 position = player.position;

        bool rangedAvailable = false;
        bool meleeAvailable = false;

        // CHECK THROWABLE WEAPON
        if (throwableWeapons != null)
        {
            if (throwableWeapons.throwableItem == null)
            {
                Debug.LogWarning("ThrowableItem is not assigned!");
            }
            else
            {
                Debug.Log("Ranged distance: " +
                    throwableWeapons.DistanceTo(position));

                Debug.Log("Ranged active: " +
                    throwableWeapons.throwableItem.gameObject.activeInHierarchy);

                rangedAvailable = throwableWeapons.CanPickup(
                    position, pickupRange
                );
            }
        }

        // CHECK MELEE WEAPON
        if (meleeWeapons != null)
        {
            if (meleeWeapons.meleeWeapon == null)
            {
                Debug.LogWarning("MeleeWeapon is not assigned!");
            }
            else
            {
                Debug.Log("Melee distance: " +
                    meleeWeapons.DistanceTo(position));

                Debug.Log("Melee active: " +
                    meleeWeapons.meleeWeapon.gameObject.activeInHierarchy);

                meleeAvailable = meleeWeapons.CanPickup(
                    position, pickupRange
                );
            }
        }

        if (!rangedAvailable && !meleeAvailable)
        {
            Debug.Log("No weapon in pickup range!");
            return;
        }

        WeaponType selected;

        // Pick the closest available weapon.
        if (rangedAvailable && meleeAvailable)
        {
            selected =
                meleeWeapons.DistanceTo(position) <
                throwableWeapons.DistanceTo(position)
                ? WeaponType.Melee
                : WeaponType.Ranged;
        }
        else
        {
            selected = meleeAvailable
                ? WeaponType.Melee
                : WeaponType.Ranged;
        }

        // Drop the current weapon before picking up another.
        if (equippedWeapon != WeaponType.None)
        {
            DropEquippedWeapon();
        }

        // Pick up the new weapon.
        if (selected == WeaponType.Melee)
        {
            meleeWeapons.Pickup();
        }
        else
        {
            throwableWeapons.Pickup();
        }

        equippedWeapon = selected;

        Debug.Log("SUCCESSFULLY PICKED UP: " + equippedWeapon);
    }


    void DropEquippedWeapon()
    {
        Vector2 dropPosition =
            (Vector2)player.position -
            facingDirection * dropDistance;

        // The old weapon is put back on the ground.
        if (equippedWeapon == WeaponType.Ranged)
        {
            throwableWeapons.Drop(dropPosition);
        }
        else if (equippedWeapon == WeaponType.Melee)
        {
            meleeWeapons.Drop(dropPosition);
        }

        equippedWeapon = WeaponType.None;
    }

    // BUTTON B - THROW OR MELEE ATTACK
    public void TryThrow()
    {
        if (player == null)
            return;

        if (equippedWeapon == WeaponType.Ranged)
        {
            if (throwableWeapons == null ||
                throwableWeapons.IsFlying)
                return;

            equippedWeapon = WeaponType.None;

            throwableWeapons.Throw(
                player.position,
                facingDirection
            );
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
            Debug.Log("No weapon equipped!");
        }
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
