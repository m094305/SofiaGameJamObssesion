using System.Collections.Generic;
using UnityEngine;

// Start menu: "Envy's Flower" over the darkened forest, "Press Space to start".
// The world is held still (like a cutscene) until a button is pressed, then the overlay fades away
// and the music moves from the opening song to the main theme.
// Added automatically when a level loads, so every scene gets it without setup.
public class TitleScreen : MonoBehaviour
{
    public string title = "Envy's Flower";
    // "Press [key] to start" (any key starts; this is the one shown).
    public string buttonLabel = KeyCap.Attack;
    public Color titleColor = new Color(0.93f, 0.86f, 0.62f);
    public float fadeOutTime = 1.2f;

    public static bool Showing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Showing = false; }

    // Before any Start() runs, so the music begins on the title song.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Install()
    {
        if (GameFlow.Retrying) return;                             // "try again" skips the title
        if (FindAnyObjectByType<PlayerHealth>() == null) return;   // not a level
        if (FindAnyObjectByType<TitleScreen>() != null) return;
        new GameObject("Title Screen").AddComponent<TitleScreen>();
        Showing = true;
    }

    float shownAt, startedAt = -1f;
    Font titleFont;
    GUIStyle titleStyle, promptStyle, smallStyle, buttonStyle;
    Texture2D vignette, circle, glow, star;

    struct Twinkle { public Vector2 pos; public float phase, speed, size; }
    readonly List<Twinkle> twinkles = new List<Twinkle>();

    void Start()
    {
        shownAt = Time.unscaledTime;
        DialogueDirector.HoldForCutscene(true);
        titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" }, 64);
        for (int i = 0; i < 14; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            twinkles.Add(new Twinkle
            {
                pos = new Vector2(Mathf.Cos(a) * Random.Range(0.22f, 0.36f), Mathf.Sin(a) * Random.Range(0.06f, 0.13f)),
                phase = Random.value * 10f,
                speed = Random.Range(0.6f, 1.3f),
                size = Random.Range(0.6f, 1.2f),
            });
        }
    }

    void Update()
    {
        if (startedAt < 0f)
        {
            // A short moment before a press counts, so a held button doesn't skip it.
            if (Time.unscaledTime - shownAt > 0.8f && GameFlow.AnyButtonPressed())
            {
                startedAt = Time.unscaledTime;
                AudioManager.BeginGameMusic();
            }
            return;
        }
        if (Time.unscaledTime - startedAt >= fadeOutTime)
        {
            Showing = false;
            DialogueDirector.HoldForCutscene(false);
            Destroy(gameObject);
        }
    }

    void OnGUI()
    {
        GUI.depth = -100;   // over the hearts and hints
        if (Event.current.type != EventType.Repaint) return;
        MakeAssets();

        float s = Screen.height / 720f;
        float t = Time.unscaledTime - shownAt;
        float fadeIn = Mathf.Clamp01(t / 1.0f);
        float fadeOut = startedAt < 0f ? 1f : 1f - Mathf.Clamp01((Time.unscaledTime - startedAt) / fadeOutTime);
        float a = fadeOut;
        Color old = GUI.color;

        // Darken the forest: solid at first, settling to a heavy vignette.
        GUI.color = new Color(0f, 0f, 0f, a * Mathf.Lerp(1f, 0.68f, fadeIn));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, a * 0.9f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignette);

        float cx = Screen.width * 0.5f, cy = Screen.height * 0.3f;
        float ta = a * Mathf.Clamp01((t - 0.3f) / 1.2f);

        // Soft glow behind the title.
        GUI.color = new Color(0.55f, 0.75f, 0.45f, 0.18f * ta * (0.85f + 0.15f * Mathf.Sin(t * 1.3f)));
        GUI.DrawTexture(new Rect(cx - 420f * s, cy - 130f * s, 840f * s, 260f * s), glow);

        // Twinkles around the title.
        foreach (Twinkle tw in twinkles)
        {
            float k = Mathf.Max(0f, Mathf.Sin(t * tw.speed * 2f + tw.phase));
            float size = 22f * s * tw.size * (0.4f + 0.6f * k);
            Vector2 p = new Vector2(cx + tw.pos.x * Screen.width, cy + tw.pos.y * Screen.height * 1.6f);
            GUI.color = new Color(1f, 0.95f, 0.75f, ta * k * 0.9f);
            GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), star);
        }

        // Title.
        titleStyle.fontSize = Mathf.RoundToInt(92f * s);
        Rect tr = new Rect(0, cy - 70f * s, Screen.width, 140f * s);
        titleStyle.normal.textColor = new Color(0.05f, 0.12f, 0.06f, 0.9f * ta);
        GUI.color = Color.white;
        GUI.Label(new Rect(tr.x + 4f * s, tr.y + 5f * s, tr.width, tr.height), title, titleStyle);
        titleStyle.normal.textColor = new Color(titleColor.r, titleColor.g, titleColor.b, ta);
        GUI.Label(tr, title, titleStyle);

        // A thin line with a flower-star in the middle.
        float lw = 260f * s;
        GUI.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.55f * ta);
        GUI.DrawTexture(new Rect(cx - lw - 16f * s, cy + 70f * s, lw, 1.5f * s), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + 16f * s, cy + 70f * s, lw, 1.5f * s), Texture2D.whiteTexture);
        float st = 22f * s * (0.9f + 0.1f * Mathf.Sin(t * 2f));
        GUI.color = new Color(1f, 0.92f, 0.7f, ta);
        GUI.DrawTexture(new Rect(cx - st * 0.5f, cy + 71f * s - st * 0.5f, st, st), star);

        // "Press Space to start", pulsing (steady once pressed).
        float pa = a * Mathf.Clamp01((t - 1.4f) / 0.6f);
        if (startedAt < 0f) pa *= 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(t * 3f));
        promptStyle.fontSize = Mathf.RoundToInt(30f * s);
        promptStyle.normal.textColor = new Color(0.95f, 0.92f, 0.85f, 1f);
        // "Press [Space] to start" - any key starts, Space is the one shown.
        KeyCap.DrawPrompt("Press", buttonLabel, "to start", cx, Screen.height * 0.72f + 25f * s,
                          promptStyle, s, s * 1.25f, pa);

        GUI.color = old;
    }

    void MakeAssets()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, wordWrap = false };
            if (titleFont != null) titleStyle.font = titleFont;
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            if (titleFont != null) promptStyle.font = titleFont;
            buttonStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        if (vignette == null)
        {
            const int n = 128;
            vignette = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            circle = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            glow = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                    float r = p.magnitude;
                    vignette.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.25f, r))));
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - r) * n * 0.5f)));
                    float g = Mathf.Clamp01(1f - r); glow.SetPixel(x, y, new Color(1f, 1f, 1f, g * g));
                }
            vignette.Apply();
            circle.Apply();
            glow.Apply();
            Sprite sp = GroundGlint.StarSprite();
            star = sp != null ? sp.texture : Texture2D.whiteTexture;
        }
    }
}
