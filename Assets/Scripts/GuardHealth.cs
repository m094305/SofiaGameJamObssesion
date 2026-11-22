using UnityEngine;

public class GuardHealth : MonoBehaviour
{
    public int maxHealth = 60;
    public int currentHealth;
    public bool isDead = false;   // from m094305's "possesions" commit

    private NPCGuard guard;
    private PlayerHealth playerHealth;

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
        TakeDamage(damage, false);
    }

    // fromThrow: the hit came from a thrown item (used for the knight's death cutscene).
    public void TakeDamage(int damage, bool fromThrow)
    {
        if (currentHealth <= 0 || damage <= 0)
            return;

        // A dead player can't hurt anyone. (NPCs stop "seeing" a dead player, so without this
        // a throw after game over would count as an unseen stealth kill.)
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
            return;

        // A player attack from outside the guard's CURRENT vision is lethal.
        bool stealthHit = guard != null && !guard.CanSeePlayerNow();

        NPCDialogue dialogue = guard != null
            ? guard.GetComponent<NPCDialogue>()
            : GetComponent<NPCDialogue>();

        currentHealth = stealthHit
            ? 0
            : Mathf.Max(0, currentHealth - damage);

        // An NPC with a death cutscene (knight, merchant) only dies by the kill that plays it
        // (unseen, with its weapon). Any other hit - e.g. while it's angry and sees you - leaves it on 1 HP.
        if (currentHealth <= 0 && dialogue != null && dialogue.HasDeathCutscene &&
            !dialogue.IsCutsceneKill(stealthHit, fromThrow))
        {
            currentHealth = 1;
            Debug.Log(name + ": can only be killed by its cutscene kill - left on 1 HP.");
        }

        Debug.Log(name + " HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log(name + (stealthHit ? ": Stealth takedown!" : ": Defeated!"));
            // Queue the death lines before the NPC is switched off.
            // An unseen kill with a thrown item can play this NPC's death cutscene.
            if (dialogue != null) dialogue.OnKilled(stealthHit, fromThrow);
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
