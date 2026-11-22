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

    [Header("Game over")]
    [Tooltip("Seconds after dying before \"Press A to try again\" appears and works.")]
    public float retryDelay = 1.5f;

    bool CanRetry => IsDead && Time.time - diedAt >= retryDelay;

    void Update()
    {
        if (CanRetry && GameFlow.AnyButtonPressed())
        {
            GameFlow.Restart();
            return;
        }

        // Blink while invulnerable.
        bool blinking = !IsDead && Time.time < invulnerableUntil;
        bool visible = !blinking || Mathf.Repeat(Time.time * 10f, 1f) < 0.5f;
        foreach (SpriteRenderer sr in sprites)
            if (sr != null && sr.enabled != visible)
                sr.enabled = visible;
    }

    float diedAt;
    GUIStyle gameOverStyle, retryStyle;
    static Texture2D buttonCircle;

    static Texture2D MakeCircle()
    {
        const int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
        t.Apply();
        return t;
    }

    void Die()
    {
        IsDead = true;
        diedAt = Time.time;

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
        if (TitleScreen.Showing || DeathCutscene.Playing) return;   // nothing over the title or cutscene pictures
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
            // Big red GAME OVER over a dark band, fading in.
            float a = Mathf.Clamp01((Time.time - diedAt) / 0.8f);
            float sc = Screen.height / 720f;
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * a);
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.5f - 85f * sc, Screen.width, 170f * sc), Texture2D.whiteTexture);
            if (gameOverStyle == null)
                gameOverStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            gameOverStyle.fontSize = Mathf.RoundToInt(96f * sc);
            Rect r = new Rect(0f, Screen.height * 0.5f - 70f * sc, Screen.width, 140f * sc);
            gameOverStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f * a);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 4f * sc, r.y + 4f * sc, r.width, r.height), "GAME OVER", gameOverStyle);
            gameOverStyle.normal.textColor = new Color(0.86f, 0.08f, 0.08f, a);
            GUI.Label(r, "GAME OVER", gameOverStyle);

            // "Press (A) to try again", pulsing, once it's allowed.
            if (CanRetry)
            {
                float pa = Mathf.Clamp01((Time.time - diedAt - retryDelay) / 0.5f) *
                           (0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f)));
                if (retryStyle == null)
                    retryStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                retryStyle.fontSize = Mathf.RoundToInt(26f * sc);
                string before = "Press", after = "to try again";
                float wb = retryStyle.CalcSize(new GUIContent(before)).x;
                float wa = retryStyle.CalcSize(new GUIContent(after)).x;
                float d = 32f * sc, gap = 10f * sc;
                float total = wb + gap + d + gap + wa;
                float x = Screen.width * 0.5f - total * 0.5f;
                float y = Screen.height * 0.5f + 115f * sc;
                Rect rb = new Rect(x, y - 16f * sc, wb, 32f * sc);
                Rect ra = new Rect(x + wb + gap + d + gap, y - 16f * sc, wa, 32f * sc);
                retryStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f * pa);
                GUI.Label(new Rect(rb.x + 2f * sc, rb.y + 2f * sc, rb.width, rb.height), before, retryStyle);
                GUI.Label(new Rect(ra.x + 2f * sc, ra.y + 2f * sc, ra.width, ra.height), after, retryStyle);
                retryStyle.normal.textColor = new Color(0.95f, 0.92f, 0.85f, pa);
                GUI.Label(rb, before, retryStyle);
                GUI.Label(ra, after, retryStyle);

                // The red A button.
                if (buttonCircle == null) buttonCircle = MakeCircle();
                Color bc = new Color(0.86f, 0.22f, 0.22f);
                Rect b = new Rect(x + wb + gap, y - d * 0.5f, d, d);
                GUI.color = new Color(0f, 0f, 0f, 0.6f * pa);
                GUI.DrawTexture(new Rect(b.x + 2f * sc, b.y + 2.5f * sc, d, d), buttonCircle);
                GUI.color = new Color(bc.r * 0.6f, bc.g * 0.6f, bc.b * 0.6f, pa);
                GUI.DrawTexture(b, buttonCircle);
                GUI.color = new Color(bc.r, bc.g, bc.b, pa);
                GUI.DrawTexture(new Rect(b.x + 2.5f * sc, b.y + 2f * sc, d - 5f * sc, d - 6f * sc), buttonCircle);
                GUI.color = Color.white;
                var bs = new GUIStyle(retryStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(19f * sc) };
                bs.normal.textColor = new Color(1f, 1f, 1f, pa);
                GUI.Label(new Rect(b.x, b.y - 1f * sc, d, d), "A", bs);
            }
            GUI.color = old;
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
