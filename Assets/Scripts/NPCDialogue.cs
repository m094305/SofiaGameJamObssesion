using System.Collections.Generic;
using UnityEngine;

// An NPC's conversations, each played once:
//  Spotted  - Narcissus notices the NPC from a distance (what he sees on them).
//  Greeting - the NPC talks to you when you walk up, only if you haven't attacked it.
//  Attacked - the NPC was hit but survived (e.g. the knight left at 1 HP).
//  Death    - the NPC dies (its last words and Narcissus taking his prize).
[RequireComponent(typeof(NPCGuard))]
public class NPCDialogue : MonoBehaviour
{
    [Header("Portrait & loot")]
    [Tooltip("This NPC's splash art (under Canvas > Portraits).")]
    public GameObject portrait;
    [Tooltip("What Narcissus takes from this NPC when it dies (changes his portrait).")]
    public DialogueDirector.Item stolenItems;
    public enum KillWeapon { Thrown, Melee, Fists }

    [Tooltip("Optional: plays instead of the plain death text when killed without being seen, by the weapon below.")]
    public DeathCutscene deathCutscene;
    [Tooltip("Which kind of unseen kill plays the cutscene (knight: Thrown stone, merchant: Melee knife).")]
    public KillWeapon cutsceneWeapon = KillWeapon.Thrown;
    [Tooltip("Switched on when this NPC dies (e.g. the stone appears once the merchant is dead).")]
    public GameObject[] revealOnDeath;

    [Header("When it starts")]
    [Tooltip("Narcissus notices the NPC within this distance (needs a clear line of sight).")]
    public float spotRadius = 5f;
    [Tooltip("The NPC greets you within this distance, but only if it can see you.")]
    public float talkRadius = 1.6f;

    [Header("Lines")]
    public List<DialogueLine> spottedLines = new List<DialogueLine>();
    public List<DialogueLine> greetingLines = new List<DialogueLine>();
    public List<DialogueLine> attackedLines = new List<DialogueLine>();
    public List<DialogueLine> deathLines = new List<DialogueLine>();

    public bool WasAttacked { get; private set; }

    public bool HasDeathCutscene => deathCutscene != null;

    // Knight/merchant: unseen, with the weapon their cutscene is made for.
    // Grandma (Fists): a punch she sees, once the fight has brought Narcissus down to his last heart.
    public bool IsCutsceneKill(bool stealth, KillWeapon weapon)
    {
        if (deathCutscene == null || weapon != cutsceneWeapon) return false;
        if (weapon == KillWeapon.Fists)
        {
            // Only if Narcissus started it with a sneak punch; a fair fight can't be won.
            GuardHealth gh = GetComponent<GuardHealth>();
            if (gh == null || !gh.Ambushed) return false;
            PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
            return !stealth && ph != null && ph.CurrentHealth <= 1;
        }
        return stealth;
    }

    bool spotted, greeted, attackedPlayed, deathPlayed;
    NPCGuard guard;
    Transform player;
    PlayerHealth playerHealth;

    void Awake()
    {
        guard = GetComponent<NPCGuard>();
        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null) player = ph.transform;
    }

    void Update()
    {
        if (player == null || WasAttacked)
            return;

        // Nobody greets or eyes a dead man (GAME OVER stays clear).
        if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
            return;

        // Only start something new when nothing is on screen or waiting.
        if (DialogueManager.AnyDialogueActive || DialogueDirector.Freezing || DialogueDirector.Instance.HasPending)
            return;

        float d = Vector2.Distance(player.position, transform.position);

        if (!spotted && d <= spotRadius && ClearLine())
        {
            spotted = true;
            Play(spottedLines);
        }

        // The NPC only talks to you if it's close AND can actually see you.
        // (Not once it has turned on you.)
        if (!greeted && d <= talkRadius && guard != null && !guard.IsHostile && guard.CanSeePlayerNow())
        {
            greeted = true;
            spotted = true;
            Play(greetingLines);
        }
    }

    // Called by GuardHealth when a hit doesn't kill.
    public void OnAttacked()
    {
        WasAttacked = true;
        // A greeting/spotted line that hasn't played yet no longer makes sense.
        DialogueDirector.Instance.CancelPending(transform);
        if (attackedPlayed) return;
        attackedPlayed = true;
        Play(attackedLines);
    }

    // Called by GuardHealth right before the NPC is removed.
    // stealth: killed without being seen; fromThrow: by a thrown item (else melee).
    // An unseen kill with this NPC's cutscene weapon plays its death cutscene.
    public void OnKilled(bool stealth = false, KillWeapon weapon = KillWeapon.Melee)
    {
        WasAttacked = true;
        DialogueDirector.Instance.CancelPending(transform);
        if (deathPlayed) return;
        deathPlayed = true;
        DialogueDirector.GiveItems(stolenItems);

        if (revealOnDeath != null)
            foreach (GameObject go in revealOnDeath)
                if (go != null) go.SetActive(true);

        if (IsCutsceneKill(stealth, weapon))
        {
            deathCutscene.Play(deathLines);      // the shots, with the death lines over the last one
            AudioManager.PlayDeathSong(name);    // its fight song plays over its death
        }
        else
            Play(deathLines);
    }

    void Play(List<DialogueLine> lines)
    {
        DialogueDirector.Instance.Play(lines, transform, portrait);
    }

    bool ClearLine()
    {
        if (guard == null || guard.wallLayer.value == 0) return true;
        return Physics2D.Linecast(transform.position, player.position, guard.wallLayer).collider == null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spotRadius);
        Gizmos.color = new Color(0.3f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, talkRadius);
    }
}
