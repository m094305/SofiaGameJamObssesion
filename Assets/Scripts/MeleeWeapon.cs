
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapons : MonoBehaviour
{
    [Header("Weapon")]
    public Transform meleeWeapon;

    [Header("Attack Settings")]
    public float meleeRange = 0.7f;
    public float swingDuration = 0.2f;
    public float attackCooldown = 0.35f;

    [Header("Damage")]
    public int meleeDamage = 20;
    public float hitRadius = 0.5f;

    [Header("Collision")]
    public LayerMask wallLayer;

    [Header("Single use")]
    [Tooltip("Gone for good after it kills someone (the knife).")]
    public bool singleUse = true;

    public bool IsSwinging { get; private set; }
    // True once a single-use weapon has made its kill.
    public bool Used { get; private set; }

    void SetGlint(bool on)
    {
        GroundGlint g = meleeWeapon != null ? meleeWeapon.GetComponent<GroundGlint>() : null;
        if (g != null) g.enabled = on;
    }

    private float nextAttackTime = 0f;

    void Awake()
    {
        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask("Walls");
    }

    public bool CanPickup(Vector2 playerPosition, float range)
    {
        if (meleeWeapon == null || IsSwinging || Used)
            return false;

        if (!meleeWeapon.gameObject.activeInHierarchy)
            return false;

        return Vector2.Distance(
            playerPosition,
            meleeWeapon.position
        ) <= range;
    }

    public float DistanceTo(Vector2 position)
    {
        if (meleeWeapon == null)
            return Mathf.Infinity;

        return Vector2.Distance(
            position,
            meleeWeapon.position
        );
    }

    public void Pickup()
    {
        if (meleeWeapon == null)
            return;

        meleeWeapon.gameObject.SetActive(false);
        SetGlint(false);

        Debug.Log("Melee picked up!");
    }

    public void Drop(Vector2 position)
    {
        if (meleeWeapon == null || Used)
            return;

        meleeWeapon.position = new Vector3(
            position.x,
            position.y,
            meleeWeapon.position.z
        );

        meleeWeapon.rotation = Quaternion.identity;
        meleeWeapon.gameObject.SetActive(true);
        SetGlint(true);

        Debug.Log("Melee dropped!");
    }

    public void Attack(Transform player, Vector2 direction)
    {
        if (meleeWeapon == null || player == null)
            return;

        if (IsSwinging || Used || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        StartCoroutine(SwingRoutine(player, direction));
    }

    IEnumerator SwingRoutine(Transform player, Vector2 direction)
    {
        IsSwinging = true;

        direction = direction.normalized;

        if (direction == Vector2.zero)
            direction = Vector2.down;

        Vector2 side = new Vector2(
            -direction.y,
            direction.x
        );

        HashSet<GuardHealth> damagedGuards =
            new HashSet<GuardHealth>();

        meleeWeapon.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, swingDuration);

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            float offset = Mathf.Lerp(
                -0.4f,
                0.4f,
                progress
            );

            Vector2 attackPosition =
                (Vector2)player.position +
                direction * meleeRange +
                side * offset;

            meleeWeapon.position = new Vector3(
                attackPosition.x,
                attackPosition.y,
                meleeWeapon.position.z
            );

            float angle =
                Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg - 90f;

            meleeWeapon.rotation = Quaternion.Euler(
                0f,
                0f,
                angle + Mathf.Lerp(-50f, 50f, progress)
            );

            // Check which guards the swing touches.
            CheckMeleeHits(
                player.position,
                attackPosition,
                damagedGuards
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        meleeWeapon.gameObject.SetActive(false);
        meleeWeapon.rotation = Quaternion.identity;

        IsSwinging = false;
    }

    void CheckMeleeHits(
        Vector2 playerPosition,
        Vector2 attackPosition,
        HashSet<GuardHealth> damagedGuards)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition,
            hitRadius
        );

        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger)
                continue;

            GuardHealth guard =
                hit.GetComponentInParent<GuardHealth>();

            if (guard == null || damagedGuards.Contains(guard))
                continue;

            // Don't hit guards through walls.
            RaycastHit2D wall = Physics2D.Linecast(
                playerPosition,
                hit.bounds.center,
                wallLayer
            );

            if (wall.collider != null)
                continue;

            damagedGuards.Add(guard);
            guard.TakeDamage(meleeDamage, false);

            // The knife stays in the body: used up.
            if (singleUse && guard.isDead)
                Used = true;

            Debug.Log("Melee hit NPC!");
        }
    }
}
