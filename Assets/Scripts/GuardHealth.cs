using UnityEngine;

public class GuardHealth : MonoBehaviour
{
    public int maxHealth = 60;
    public int currentHealth;

    private NPCGuard guard;

    void Awake()
    {
        currentHealth = maxHealth;
        guard = GetComponent<NPCGuard>();
        if (guard == null)
            guard = GetComponentInParent<NPCGuard>();
    }

    // Keep the same method signature: existing melee and throwable attacks
    // can continue calling TakeDamage(damage).
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || damage <= 0)
            return;

        // A player attack from outside the guard's CURRENT vision is lethal.
        bool stealthHit = guard != null && !guard.CanSeePlayerNow();

        currentHealth = stealthHit
            ? 0
            : Mathf.Max(0, currentHealth - damage);

        Debug.Log(name + " HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log(name + (stealthHit ? ": Stealth takedown!" : ": Defeated!"));
            // Disable the NPC root even when GuardHealth is attached to a child.
            (guard != null ? guard.gameObject : gameObject).SetActive(false);
        }
        else if (guard != null)
        {
            guard.BecomeHostile();
        }
    }
}
