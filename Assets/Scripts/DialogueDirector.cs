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

    // ---- Easing into the freeze ----
    [Tooltip("Seconds for everyone to glide to a stop when a conversation starts.")]
    public float slowDownTime = 1f;

    float slowStart;
    int lastEndFrame = -100;
    Vector2 playerStartVelocity;
    bool playerWasMoving;
    bool playerPosed;
    string playerFaceState;
    NPCSpriteAnimator talkingNpcAnim;

    // 1 = normal speed, eases to 0 over slowDownTime once a conversation starts.
    public static float MoveScale { get; private set; } = 1f;

    // True while characters should be held (also for the frame or two between
    // back-to-back conversations, so nobody lurches forward in between).
    public static bool Freezing =>
        instance != null && (instance.running || Time.frameCount - instance.lastEndFrame <= 2);

    void FixedUpdate()
    {
        if (running && playerBody != null)
            playerBody.linearVelocity = playerStartVelocity * MoveScale;
    }

    void Update()
    {
        if (running)
        {
            if (DialogueManager.AnyDialogueActive)
            {
                float t = slowDownTime > 0f ? (Time.time - slowStart) / slowDownTime : 1f;
                MoveScale = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                AnimatePlayerFacing();
                KeepBoxShown();
                return;
            }
            ReleasePlayer();
            running = false;
            lastEndFrame = Time.frameCount;
        }
        else if (!Freezing)
        {
            MoveScale = 1f;
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

        bool chained = Time.frameCount - lastEndFrame <= 2;   // straight after another conversation

        DialogueManager.Instance.StartDialogue(lines);
        running = DialogueManager.AnyDialogueActive;
        if (!running)
            return;

        // Glide to a stop instead of freezing on the spot (already stopped if chained).
        if (chained)
        {
            slowStart = Time.time - slowDownTime;
            MoveScale = 0f;
            playerStartVelocity = Vector2.zero;
        }
        else
        {
            slowStart = Time.time;
            MoveScale = 1f;
            playerStartVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        }
        playerWasMoving = playerStartVelocity.sqrMagnitude > 0.01f;
        playerPosed = false;
        if (playerMove != null) playerMove.enabled = false;

        // Turn to face each other straight away, while still slowing down.
        playerFaceState = null;
        if (talkingNpcAnim != null) talkingNpcAnim.LookAt(null);
        talkingNpcAnim = null;
        if (player != null && r.npc != null)
        {
            Vector2 toNpc = (Vector2)r.npc.position - (Vector2)player.position;
            FacePlayer(toNpc);

            talkingNpcAnim = r.npc.GetComponentInChildren<NPCSpriteAnimator>(true);
            if (talkingNpcAnim != null)
                talkingNpcAnim.LookAt(player);
        }
    }

    // Player keeps "walking" (slowing) in the facing direction, then holds a clean pose.
    void AnimatePlayerFacing()
    {
        if (playerAnim == null || playerFaceState == null) return;

        if (playerWasMoving && MoveScale > 0.02f)
        {
            playerAnim.speed = MoveScale;
        }
        else if (!playerPosed)
        {
            playerAnim.Play(playerFaceState, 0, 0f);
            playerAnim.Update(0f);
            playerAnim.speed = 0f;
            playerPosed = true;
        }
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

    void ReleasePlayer()
    {
        if (playerBody != null) playerBody.linearVelocity = Vector2.zero;
        if (playerAnim != null) playerAnim.speed = 1f;
        if (playerMove != null) playerMove.enabled = true;
        if (talkingNpcAnim != null) talkingNpcAnim.LookAt(null);
        talkingNpcAnim = null;
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

        playerFaceState = state;
        // Hold this direction's walk flag on, or the animator drops straight back to Idle.
        playerAnim.SetBool(state, true);
        playerAnim.Play(state, 0, 0f);
        playerAnim.Update(0f);
        // Keep the walk cycle going (slowing with MoveScale) only if they were moving.
        playerAnim.speed = playerWasMoving ? 1f : 0f;
        playerPosed = !playerWasMoving;
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
