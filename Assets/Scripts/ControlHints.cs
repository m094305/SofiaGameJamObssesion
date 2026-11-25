using System.Collections.Generic;
using UnityEngine;

// On-screen help for the arcade buttons:
//   - A "pick up" prompt over the item you're standing at (always).
//   - First-time hints, once per playthrough: what the knife / stone / fists do, the inventory,
//     moving on in a conversation, and hiding after being spotted.
// Added to the player automatically, so it works in every scene without setup.
public class ControlHints : MonoBehaviour
{
    [Header("Keys (as drawn on screen)")]
    public string attackLabel = KeyCap.Attack;       // Space: attack / throw / punch, next line
    public string pickupLabel = KeyCap.Pickup;       // E: pick up / swap, the lake
    public string inventoryLabel = KeyCap.Inventory; // Tab
    public Color attackColor = new Color(0.86f, 0.22f, 0.22f);
    public Color pickupColor = new Color(0.25f, 0.5f, 0.92f);
    public Color inventoryColor = new Color(0.95f, 0.78f, 0.2f);

    [Header("Hints")]
    public float hintTime = 5f;
    [Tooltip("An NPC this close while you're empty-handed brings up the punch hint.")]
    public float punchHintDistance = 4.5f;

    enum Btn { Attack, Pickup, Inventory, None }
    struct Row { public Btn btn; public string text; }
    class Hint { public List<Row> rows = new List<Row>(); public float shownAt = -1f; }

    static readonly HashSet<string> shown = new HashSet<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { shown.Clear(); }

    // Put one on the player in every scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Install()
    {
        ArcadeButtons buttons = FindAnyObjectByType<ArcadeButtons>();
        if (buttons != null && buttons.GetComponent<ControlHints>() == null)
            buttons.gameObject.AddComponent<ControlHints>();
    }

    readonly Queue<Hint> queue = new Queue<Hint>();
    Hint current;
    PlayerInventory inventory;
    PlayerHealth health;
    NPCGuard[] guards;
    Camera cam;
    PlayerInventory.WeaponType lastEquipped = PlayerInventory.WeaponType.None;
    int lastCollected;

    // Prompt over an item this frame.
    bool promptOn;
    Vector3 promptWorld;
    string promptText;
    Btn promptBtn = Btn.Pickup;

    static Texture2D circle, white;
    GUIStyle textStyle, labelStyle;

    void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null) inventory = GetComponentInChildren<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        guards = FindObjectsByType<NPCGuard>(FindObjectsInactive.Include);
        lastCollected = Collectible.Collected.Count;
    }

    bool Busy => DeathCutscene.Playing || DialogueDirector.Freezing ||
                 (health != null && health.IsDead);

    void Update()
    {
        if (inventory == null || inventory.player == null) return;
        if (cam == null) cam = Camera.main;

        UpdatePrompt();
        CheckFirstTimes();

        // Next hint in line.
        if (current != null && Time.unscaledTime - current.shownAt > hintTime) current = null;
        if (current == null && queue.Count > 0 && !DeathCutscene.Playing && !TitleScreen.Showing)
        {
            current = queue.Dequeue();
            current.shownAt = Time.unscaledTime;
        }
    }

    // ---------- "Pick up" prompt ----------

    void UpdatePrompt()
    {
        promptOn = false;
        if (Busy || DialogueManager.AnyDialogueActive) return;

        Vector2 pos = inventory.player.position;
        float range = inventory.pickupRange;
        float best = float.MaxValue;

        // At the lake with the ending ready: A looks into the water.
        if (PlayerInteraction.AtLake)
        {
            PlayerInteraction lake = inventory.player.GetComponent<PlayerInteraction>();
            promptOn = true;
            promptWorld = lake != null ? lake.LakePosition : (Vector3)pos;
            promptText = "Look into the lake";
            promptBtn = Btn.Pickup;   // E
            return;
        }

        Collectible c = Collectible.NearestTo(pos, range * 0.6f);
        if (c != null)
        {
            Offer(c.transform.position, "Pick up the " + c.name.ToLowerInvariant(), 0f, ref best);
            return;   // a collectible in range is what the button takes first
        }

        bool holding = inventory.Equipped != PlayerInventory.WeaponType.None;
        ThrowableWeapons t = inventory.throwableWeapons;
        if (t != null && t.throwableItem != null && t.CanPickup(pos, range))
            Offer(t.throwableItem.position, (holding ? "Swap for the " : "Pick up the ") + "stone", t.DistanceTo(pos), ref best);
        MeleeWeapons m = inventory.meleeWeapons;
        if (m != null && m.meleeWeapon != null && m.CanPickup(pos, range))
            Offer(m.meleeWeapon.position, (holding ? "Swap for the " : "Pick up the ") + "knife", m.DistanceTo(pos), ref best);
    }

    void Offer(Vector3 at, string text, float dist, ref float best)
    {
        if (dist > best) return;
        best = dist;
        promptOn = true;
        promptWorld = at;
        promptText = text;
        promptBtn = Btn.Pickup;
    }

    // ---------- First-time hints ----------

    void CheckFirstTimes()
    {
        var eq = inventory.Equipped;
        if (eq != lastEquipped)
        {
            if (eq == PlayerInventory.WeaponType.Melee)
                Once("knife", R(Btn.Attack, "Stab - it only kills if they don't see you"),
                              R(Btn.None, "The knife breaks after one kill"));
            else if (eq == PlayerInventory.WeaponType.Ranged)
                Once("stone", R(Btn.Attack, "Throw - an unseen hit kills"),
                              R(Btn.None, "A miss makes noise - they'll come looking"));
            if (eq != PlayerInventory.WeaponType.None) InventoryHint();
            lastEquipped = eq;
        }
        if (Collectible.Collected.Count != lastCollected)
        {
            lastCollected = Collectible.Collected.Count;
            InventoryHint();
        }

        // Talking: the first conversation.
        if (DialogueManager.AnyDialogueActive && !DeathCutscene.Playing)
            Once("talk", R(Btn.Attack, "Next line"));

        // Spotted for the first time: sneaking.
        if (!shown.Contains("hide") && guards != null)
            foreach (NPCGuard g in guards)
                if (g != null && g.Suspicion > 0)
                {
                    Once("hide", R(Btn.None, "You were spotted!"),
                                 R(Btn.None, "Hide in the bushes - nobody can see you there"),
                                 R(Btn.None, "Spotted too often and they'll attack"));
                    break;
                }

        // Empty hands near someone: punching.
        if (!shown.Contains("punch") && eq == PlayerInventory.WeaponType.None && guards != null && !Busy)
            foreach (NPCGuard g in guards)
                if (g != null && g.isActiveAndEnabled &&
                    Vector2.Distance(g.transform.position, inventory.player.position) < punchHintDistance)
                {
                    Once("punch", R(Btn.Attack, "Punch (with empty hands)"),
                                  R(Btn.None, "Strike first, from behind - a fair fight can't be won"));
                    break;
                }
    }

    void InventoryHint()
    {
        Once("inventory", R(Btn.Pickup, "Pick up / swap items"), R(Btn.Inventory, "Open the inventory"));
    }

    static Row R(Btn b, string text) => new Row { btn = b, text = text };

    void Once(string key, params Row[] rows)
    {
        if (!shown.Add(key)) return;
        var h = new Hint();
        h.rows.AddRange(rows);
        queue.Enqueue(h);
    }

    // ---------- Drawing ----------

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || TitleScreen.Showing) return;
        Styles();
        float s = Screen.height / 720f;

        if (promptOn && cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(promptWorld + Vector3.up * 0.55f);
            if (sp.z > 0f)
            {
                Vector2 p = new Vector2(sp.x, Screen.height - sp.y);
                DrawRow(R(promptBtn, promptText), p, s, true, 1f);
            }
        }

        if (current != null)
        {
            float age = Time.unscaledTime - current.shownAt;
            float a = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((hintTime - age) / 0.6f);
            float rowH = 34f * s;
            float w = 0f;
            foreach (Row r in current.rows) w = Mathf.Max(w, RowWidth(r, s));
            float h = current.rows.Count * rowH + 16f * s;
            Rect box = new Rect((Screen.width - w) * 0.5f - 18f * s, 70f * s, w + 36f * s, h);
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.82f * a);
            GUI.DrawTexture(box, white);
            GUI.color = new Color(0.85f, 0.75f, 0.45f, 0.6f * a);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f * s), white);
            for (int i = 0; i < current.rows.Count; i++)
                DrawRow(current.rows[i], new Vector2(Screen.width * 0.5f, box.y + 8f * s + rowH * (i + 0.5f)), s, true, a);
            GUI.color = Color.white;
        }
    }

    float RowWidth(Row r, float s)
    {
        textStyle.fontSize = Mathf.RoundToInt(19f * s);
        float tw = textStyle.CalcSize(new GUIContent(r.text)).x;
        return tw + (r.btn == Btn.None ? 0f : KeyCap.Width(LabelOf(r.btn), s) + 9f * s);
    }

    string LabelOf(Btn b) => b == Btn.Attack ? attackLabel : b == Btn.Pickup ? pickupLabel : inventoryLabel;

    // centre = middle of the row.
    void DrawRow(Row r, Vector2 centre, float s, bool centred, float alpha)
    {
        textStyle.fontSize = Mathf.RoundToInt(19f * s);
        labelStyle.fontSize = Mathf.RoundToInt(16f * s);
        float w = RowWidth(r, s);
        float x = centre.x - w * 0.5f;
        if (r.btn != Btn.None)
        {
            // The keyboard key to press, drawn as a key-cap.
            x += KeyCap.Draw(x, centre.y, LabelOf(r.btn), s, alpha) + 9f * s;
        }

        Rect tr = new Rect(x, centre.y - 14f * s, w, 28f * s);
        GUI.color = new Color(0f, 0f, 0f, 0.85f * alpha);
        GUI.Label(new Rect(tr.x + 1.5f * s, tr.y + 1.5f * s, tr.width, tr.height), r.text, textStyle);
        GUI.color = new Color(0.96f, 0.93f, 0.85f, alpha);
        GUI.Label(tr, r.text, textStyle);
        GUI.color = Color.white;
    }

    void Styles()
    {
        if (circle == null)
        {
            const int n = 64;
            circle = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - dist)));
                }
            circle.Apply();
            white = Texture2D.whiteTexture;
        }
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, wordWrap = false };
            textStyle.normal.textColor = Color.white;
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;
        }
    }
}
