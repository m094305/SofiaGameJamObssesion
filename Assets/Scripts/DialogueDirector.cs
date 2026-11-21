using System;
using System.Collections.Generic;
using UnityEngine;

// Runs conversations one at a time on top of ttizuto's DialogueManager:
//  - queues them so a new one never cuts off the current one,
//  - freezes the player (and NPCGuard freezes NPCs) while a conversation is on screen,
//  - turns the player and the NPC to face each other,
//  - picks Narcissus' portrait from what he has stolen so far.
// It creates itself the first time something asks for it.
public class DialogueDirector : MonoBehaviour
{
    [Flags]
    public enum Item { None = 0, Scarf = 1, Cross = 2, Rings = 4 }

    static DialogueDirector instance;
    public static DialogueDirector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DialogueDirector>();
                if (instance == null)
                    instance = new GameObject("DialogueDirector (auto)").AddComponent<DialogueDirector>();
            }
            return instance;
        }
    }

    // What Narcissus is wearing so far (changes his portrait).
    public static Item Stolen { get; private set; }

    struct Request
    {
        public List<DialogueLine> lines;
        public Transform npc;
        public GameObject npcPortrait;
    }

    readonly Queue<Request> queue = new Queue<Request>();
    bool running;

    PlayerMovement playerMove;
    Rigidbody2D playerBody;
    Animator playerAnim;
    Transform player;

    public static void GiveItems(Item items)
    {
        Stolen |= items;
    }

    public void Play(List<DialogueLine> lines, Transform npc, GameObject npcPortrait)
    {
        if (lines == null || lines.Count == 0)
            return;
        queue.Enqueue(new Request { lines = lines, npc = npc, npcPortrait = npcPortrait });
    }

    public bool HasPending => queue.Count > 0;

    // Drop conversations for this NPC that haven't started yet.
    public void CancelPending(Transform npc)
    {
        if (queue.Count == 0) return;
        var keep = new List<Request>(queue);
        queue.Clear();
        foreach (Request r in keep)
            if (r.npc != npc) queue.Enqueue(r);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        Stolen = Item.None;
    }

    void Update()
    {
        if (running)
        {
            if (DialogueManager.AnyDialogueActive)
            {
                HoldPlayer();
                KeepBoxShown();
                return;
            }
            ReleasePlayer();
            running = false;
        }

        if (queue.Count > 0 && DialogueManager.Instance != null && !DialogueManager.AnyDialogueActive)
            Begin(queue.Dequeue());
    }

    void Begin(Request r)
    {
        FindPlayer();

        // Copy the lines so the portraits we pick don't get saved into the NPC's data.
        var lines = new List<DialogueLine>(r.lines.Count);
        foreach (DialogueLine src in r.lines)
        {
            var line = new DialogueLine
            {
                characterName = src.characterName,
                line = src.line,
                side = src.side,
                portraitOverride = src.portraitOverride
            };
            if (line.portraitOverride == null)
            {
                bool isNarcissus = (line.characterName ?? "").ToLowerInvariant().Contains("narcis");
                line.portraitOverride = isNarcissus ? NarcissusPortrait() : r.npcPortrait;
            }
            lines.Add(line);
        }

        DialogueManager.Instance.StartDialogue(lines);
        running = DialogueManager.AnyDialogueActive;
        if (!running)
            return;

        // Face each other.
        if (player != null && r.npc != null)
        {
            Vector2 toNpc = (Vector2)r.npc.position - (Vector2)player.position;
            FacePlayer(toNpc);

            NPCSpriteAnimator npcAnim = r.npc.GetComponentInChildren<NPCSpriteAnimator>(true);
            if (npcAnim != null)
                npcAnim.Face(-toNpc);
        }

        if (playerMove != null) playerMove.enabled = false;
        HoldPlayer();
    }

    // When one conversation ends and the next starts right away, the box's "hide" and
    // "show" land in the same frame and the "show" gets lost. Slide it back in from
    // wherever it is (no jump), mirroring how far the hide had gone.
    Animator boxAnim;
    void KeepBoxShown()
    {
        if (boxAnim == null)
        {
            GameObject box = GameObject.Find("DialogBox");
            if (box == null) return;
            boxAnim = box.GetComponent<Animator>();
            if (boxAnim == null) return;
        }

        AnimatorStateInfo st = boxAnim.GetCurrentAnimatorStateInfo(0);
        if (!st.IsName("hide"))
            return;

        float hidden = Mathf.Clamp01(st.normalizedTime);   // 0 = fully in, 1 = fully out
        boxAnim.Play("show", 0, 1f - hidden);
    }

    void FindPlayer()
    {
        if (player != null) return;
        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        if (ph == null) return;
        player = ph.transform;
        playerMove = ph.GetComponent<PlayerMovement>();
        playerBody = ph.GetComponent<Rigidbody2D>();
        playerAnim = ph.GetComponentInChildren<Animator>();
    }

    void HoldPlayer()
    {
        if (playerBody != null)
            playerBody.linearVelocity = Vector2.zero;
    }

    void ReleasePlayer()
    {
        if (playerAnim != null) playerAnim.speed = 1f;
        if (playerMove != null) playerMove.enabled = true;
    }

    // The player only has walk animations, so hold the first frame of the right one.
    void FacePlayer(Vector2 dir)
    {
        if (playerAnim == null || dir.sqrMagnitude < 0.0001f)
            return;

        string state = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? (dir.x > 0f ? "Walk Right" : "Walk Left")
            : (dir.y > 0f ? "Walk Up" : "Walk Down");

        foreach (string b in new[] { "Walk Up", "Walk Down", "Walk Left", "Walk Right" })
            playerAnim.SetBool(b, false);

        playerAnim.Play(state, 0, 0f);
        playerAnim.Update(0f);
        playerAnim.speed = 0f;
    }

    // Narcissus' portrait shows the pieces he has taken so far.
    static GameObject NarcissusPortrait()
    {
        string name;
        switch (Stolen)
        {
            case Item.Scarf: name = "Narcissus SCARF"; break;
            case Item.Cross: name = "Narcissus CROSS"; break;
            case Item.Rings: name = "Narcissus RINGS"; break;
            case Item.Cross | Item.Scarf: name = "Narcissus CROSS, SCARF"; break;
            case Item.Cross | Item.Rings: name = "Narcissus CROSS, RINGS"; break;
            case Item.Rings | Item.Scarf: name = "Narcissus RINGS, SCARF"; break;
            case Item.Scarf | Item.Cross | Item.Rings: name = "Narcissus ALL ITEMS"; break;
            default: return null; // DialogueManager falls back to the shadow portrait
        }

        GameObject portraits = GameObject.Find("Portraits");
        Transform t = portraits != null ? FindDeep(portraits.transform, name) : null;
        return t != null ? t.gameObject : null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
