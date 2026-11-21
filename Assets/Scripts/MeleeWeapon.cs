
using UnityEngine;
using System.Collections;

public class MeleeWeapons : MonoBehaviour
{
    [Header("Weapon Object")]
    public Transform meleeWeapon;

    [Header("Attack Settings")]
    public float meleeRange = 0.7f;
    public float swingDuration = 0.2f;

    public bool IsSwinging { get; private set; }

    public bool CanPickup(Vector2 playerPosition, float range)
    {
        if (meleeWeapon == null || IsSwinging)
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

    // Hide melee weapon when picked up.
    public void Pickup()
    {
        if (meleeWeapon == null)
            return;

        meleeWeapon.gameObject.SetActive(false);

        Debug.Log("Melee weapon picked up!");
    }

    // Show melee weapon when dropped.
    public void Drop(Vector2 position)
    {
        if (meleeWeapon == null)
            return;

        meleeWeapon.position = new Vector3(
            position.x,
            position.y,
            meleeWeapon.position.z
        );

        meleeWeapon.rotation = Quaternion.identity;
        meleeWeapon.gameObject.SetActive(true);

        Debug.Log("Melee weapon dropped!");
    }

    public void Attack(Transform player, Vector2 direction)
    {
        if (meleeWeapon == null || player == null || IsSwinging)
            return;

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

        // Show weapon while swinging.
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

            Vector2 position =
                (Vector2)player.position +
                direction * meleeRange +
                side * offset;

            meleeWeapon.position = new Vector3(
                position.x,
                position.y,
                meleeWeapon.position.z
            );

            float angle = Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg - 90f;

            meleeWeapon.rotation = Quaternion.Euler(
                0f,
                0f,
                angle + Mathf.Lerp(-50f, 50f, progress)
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Weapon returns to inventory after the swing.
        meleeWeapon.gameObject.SetActive(false);
        meleeWeapon.rotation = Quaternion.identity;

        IsSwinging = false;

        Debug.Log("Melee swing finished!");
    }
}
