
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

    [Header("Fists (B with empty hands)")]
    public int fistDamage = 25;
    public float fistReach = 0.75f;
    public float fistRadius = 0.55f;
    public float fistCooldown = 0.45f;

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

        // A single-use knife that made its kill is gone from the hand.
        if (equippedWeapon == WeaponType.Melee && meleeWeapons != null && meleeWeapons.Used)
            equippedWeapon = WeaponType.None;

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

        // Little things (the flower) go straight into the inventory, hands stay as they are.
        if (Collectible.TryCollectNear(position, pickupRange * 0.6f))
        {
            AudioManager.PlayPickup();
            return;
        }

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

        AudioManager.PlayPickup();
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
            // Empty hands: punch.
            Punch();
        }
    }

    float nextPunchTime;
    static Sprite impactSprite;

    // A bare-handed jab in front of Narcissus. Weak - only the grandma can be beaten this way
    // (her choke cutscene); the knight and merchant can't be killed by fists.
    void Punch()
    {
        if (Time.time < nextPunchTime) return;
        nextPunchTime = Time.time + fistCooldown;

        Vector2 dir = facingDirection.sqrMagnitude > 0.01f ? facingDirection.normalized : Vector2.down;
        // Snap to the 4 directions the animation has.
        dir = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? new Vector2(Mathf.Sign(dir.x), 0f) : new Vector2(0f, Mathf.Sign(dir.y));

        PlayerPunchAnimation anim = player.GetComponent<PlayerPunchAnimation>();
        if (anim != null)
        {
            anim.Play(dir);
            StartCoroutine(LandPunchAfter(anim.TimeToImpact, dir));   // the hit lands on the strike frame
        }
        else LandPunch(dir);
    }

    System.Collections.IEnumerator LandPunchAfter(float delay, Vector2 dir)
    {
        yield return new WaitForSeconds(delay);
        LandPunch(dir);
    }

    void LandPunch(Vector2 dir)
    {
        Vector2 from = player.position;
        Vector2 at = from + dir * fistReach;
        LayerMask walls = meleeWeapons != null ? meleeWeapons.wallLayer : (LayerMask)LayerMask.GetMask("Walls");

        GuardHealth target = null;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(at, fistRadius))
        {
            if (hit.isTrigger) continue;
            GuardHealth g = hit.GetComponentInParent<GuardHealth>();
            if (g == null || g.currentHealth <= 0) continue;
            if (walls.value != 0 && Physics2D.Linecast(from, hit.bounds.center, walls).collider != null) continue;
            target = g;
            break;
        }

        ShowImpact(target != null ? (Vector2)target.transform.position + Vector2.up * 0.3f - dir * 0.2f : at, target != null);
        if (target == null) { Debug.Log("Punch - missed."); return; }

        AudioManager.PlayPunch();
        target.TakeDamage(fistDamage, NPCDialogue.KillWeapon.Fists);
        Debug.Log("Punch hit " + target.name + "!");
    }

    void ShowImpact(Vector2 pos, bool landed)
    {
        if (impactSprite == null) impactSprite = GroundGlint.StarSprite();
        var go = new GameObject("Punch Impact");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = impactSprite;
        sr.sortingOrder = 50;
        sr.color = landed ? new Color(1f, 0.95f, 0.8f, 1f) : new Color(1f, 1f, 1f, 0.45f);
        StartCoroutine(FadeImpact(go.transform, sr, landed ? 0.9f : 0.5f));
    }

    System.Collections.IEnumerator FadeImpact(Transform t, SpriteRenderer sr, float size)
    {
        const float life = 0.18f;
        Color c = sr.color;
        for (float f = 0f; f < life; f += Time.deltaTime)
        {
            float k = f / life;
            t.localScale = Vector3.one * size * (0.6f + 0.6f * k);
            sr.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
            yield return null;
        }
        Destroy(t.gameObject);
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
