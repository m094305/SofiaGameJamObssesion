using UnityEngine;

public class GuardHealth : MonoBehaviour
{
    public int maxHealth = 60;
    public int currentHealth;
    public bool isDead = false;   // from m094305's "possesions" commit

    // The fist fight was opened with a punch it didn't see (the grandma can only be beaten then).
    public bool Ambushed { get; private set; }
    bool fightDecided;

    [Tooltip("Body left behind on death (from Bodies.png), drawn lying with the head at the top.")]
    public Sprite corpseSprite;

    private NPCGuard guard;
    private PlayerHealth playerHealth;

    // Leaves the body where the NPC fell, head pointing the way it was facing.
    void SpawnCorpse()
    {
        if (corpseSprite == null) return;
        Transform root = guard != null ? guard.transform : transform;
        NPCSpriteAnimator anim = root.GetComponentInChildren<NPCSpriteAnimator>();
        SpriteRenderer npcSr = anim != null ? anim.GetComponent<SpriteRenderer>()
                                            : root.GetComponentInChildren<SpriteRenderer>();
        Vector2 facing = anim != null ? anim.FacingDir : Vector2.down;

        var go = new GameObject(root.name + " Corpse");
        Vector3 at = npcSr != null ? npcSr.transform.position : root.position;
        go.transform.position = at;
        // Head is drawn at the top (+Y): turn it to point along the facing direction.
        go.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, facing));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = corpseSprite;
        if (npcSr != null)
        {
            sr.sharedMaterial = npcSr.sharedMaterial;
            sr.sortingLayerID = npcSr.sortingLayerID;
            sr.sortingOrder = npcSr.sortingOrder - 1;   // on the ground: under living NPCs and objects
            // Same size as the living NPC.
            float npcH = npcSr.sprite != null ? npcSr.sprite.rect.height / npcSr.sprite.pixelsPerUnit * npcSr.transform.lossyScale.y : 1.4f;
            float bodyH = corpseSprite.rect.height / corpseSprite.pixelsPerUnit;
            float s = bodyH > 0f ? npcH / bodyH : 1f;
            go.transform.localScale = new Vector3(s, s, 1f);
        }
    }

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
        TakeDamage(damage, fromThrow ? NPCDialogue.KillWeapon.Thrown : NPCDialogue.KillWeapon.Melee);
    }

    // weapon: what hit it (thrown stone, knife, or Narcissus' bare fists).
    public void TakeDamage(int damage, NPCDialogue.KillWeapon weapon)
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

        // The first hit decides the fist fight: only a sneak punch (before it's angry) makes it winnable.
        if (!fightDecided)
        {
            fightDecided = true;
            Ambushed = weapon == NPCDialogue.KillWeapon.Fists && stealthHit &&
                       (guard == null || !guard.IsHostile);
        }

        NPCDialogue dialogue = guard != null
            ? guard.GetComponent<NPCDialogue>()
            : GetComponent<NPCDialogue>();

        currentHealth = stealthHit && weapon != NPCDialogue.KillWeapon.Fists   // a punch from behind is still just a punch
            ? 0
            : Mathf.Max(0, currentHealth - damage);

        // An NPC with a death cutscene (knight, merchant) only dies by the kill that plays it
        // (unseen, with its weapon). Any other hit - e.g. while it's angry and sees you - leaves it on 1 HP.
        if (currentHealth <= 0 && dialogue != null && dialogue.HasDeathCutscene &&
            !dialogue.IsCutsceneKill(stealthHit, weapon))
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
            if (dialogue != null) dialogue.OnKilled(stealthHit, weapon);
            SpawnCorpse();
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
