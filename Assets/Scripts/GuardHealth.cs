using UnityEngine;

public class GuardHealth : MonoBehaviour
{
    public int maxHealth = 60;
    public int currentHealth;
    public bool isDead = false;   // from m094305's "possesions" commit

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

        NPCDialogue dialogue = guard != null
            ? guard.GetComponent<NPCDialogue>()
            : GetComponent<NPCDialogue>();

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log(name + (stealthHit ? ": Stealth takedown!" : ": Defeated!"));
            // Queue the death lines before the NPC is switched off.
            if (dialogue != null) dialogue.OnKilled();
            // Disable the NPC root even when GuardHealth is attached to a child.
            (guard != null ? guard.gameObject : gameObject).SetActive(false);
        }
        else if (guard != null)
        {
            if (dialogue != null) dialogue.OnAttacked();
            guard.BecomeHostile();
        }
    }
}
