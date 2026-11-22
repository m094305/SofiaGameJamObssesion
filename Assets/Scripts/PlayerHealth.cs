using UnityEngine;

// Narcissus has a few hearts (3 by default). Every hit takes exactly one heart, whatever hit him.
// After a hit he blinks for a moment and can't lose another heart, so one punch = one heart.
public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Number of hearts.")]
    public int maxHealth = 3;
    [Tooltip("Seconds after a hit before he can lose another heart (he blinks meanwhile).")]
    public float invulnerableTime = 1f;

    [Header("Hearts on screen")]
    public float heartSize = 44f;
    public Vector2 heartsPosition = new Vector2(16f, 14f);

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer[] sprites;
    private float invulnerableUntil;
    private float lostHeartTime = -10f;

    static Texture2D fullHeart, emptyHeart;

    void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        // Only the ones that are visible to begin with (so hidden helpers stay hidden).
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.enabled) list.Add(sr);
        sprites = list.ToArray();
    }

    // Every hit takes one heart (the damage number no longer matters).
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0 || Time.time < invulnerableUntil)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - 1);
        invulnerableUntil = Time.time + invulnerableTime;
        lostHeartTime = Time.time;
        AudioManager.PlayPunch();

        Debug.Log("Player hearts: " + CurrentHealth + "/" + maxHealth);

        if (CurrentHealth == 0)
        {
            Die();
        }
    }

    void Update()
    {
        // Blink while invulnerable.
        bool blinking = !IsDead && Time.time < invulnerableUntil;
        bool visible = !blinking || Mathf.Repeat(Time.time * 10f, 1f) < 0.5f;
        foreach (SpriteRenderer sr in sprites)
            if (sr != null && sr.enabled != visible)
                sr.enabled = visible;
    }

    void Die()
    {
        IsDead = true;

        Debug.Log("GAME OVER!");

        foreach (SpriteRenderer sr in sprites)
            if (sr != null) sr.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    void OnGUI()
    {
        MakeHeartTextures();

        float scale = Screen.height / 1080f;
        float size = heartSize * scale * 1.6f;
        for (int i = 0; i < maxHealth; i++)
        {
            bool full = i < CurrentHealth;
            float s = size;
            // The heart just lost pops and shrinks back.
            if (i == CurrentHealth && Time.time - lostHeartTime < 0.35f)
                s *= 1f + 0.35f * (1f - (Time.time - lostHeartTime) / 0.35f);
            float x = heartsPosition.x * scale + i * size * 1.1f + (size - s) * 0.5f;
            float y = heartsPosition.y * scale + (size - s) * 0.5f;
            GUI.DrawTexture(new Rect(x, y, s, s), full ? fullHeart : emptyHeart);
        }

        if (IsDead)
        {
            GUI.Box(
                new Rect(
                    Screen.width / 2f - 90,
                    Screen.height / 2f - 25,
                    180,
                    50
                ),
                "GAME OVER"
            );
        }
    }

    // Pixel-art hearts, made once in code: red with a highlight, or a dark empty outline.
    static void MakeHeartTextures()
    {
        if (fullHeart != null && emptyHeart != null) return;
        string[] shape =
        {
            "..XX...XX..",
            ".XHHX.XRRX.",
            "XHHRRXRRRRX",
            "XHRRRRRRRRX",
            "XRRRRRRRRRX",
            ".XRRRRRRRX.",
            "..XRRRRRX..",
            "...XRRRX...",
            "....XRX....",
            ".....X.....",
        };
        fullHeart = Build(shape, new Color(0.08f, 0.02f, 0.03f), new Color(0.86f, 0.13f, 0.2f), new Color(1f, 0.6f, 0.62f));
        emptyHeart = Build(shape, new Color(0.08f, 0.02f, 0.03f), new Color(0.22f, 0.18f, 0.2f, 0.85f), new Color(0.3f, 0.26f, 0.28f, 0.85f));
    }

    static Texture2D Build(string[] shape, Color outline, Color fill, Color shine)
    {
        int w = shape[0].Length, h = shape.Length, n = Mathf.Max(w, h);
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int row = y - (n - h) / 2;   // centre vertically
                char c = row >= 0 && row < h && x < w ? shape[row][x] : '.';
                Color col = c == 'X' ? outline : c == 'R' ? fill : c == 'H' ? shine : Color.clear;
                tex.SetPixel(x, n - 1 - y, col);   // texture rows go bottom-up
            }
        tex.Apply();
        return tex;
    }
}
