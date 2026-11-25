using UnityEngine;

// After the ending's pictures: "Envy's Flower" and "The End" on black, then "Press Space" back to the title.
public class EndingScreen : MonoBehaviour
{
    public DeathCutscene cutscene;
    public string title = "Envy's Flower";
    public string subtitle = "The End";

    float finishedAt = -1f;
    Font font;
    GUIStyle titleStyle, subStyle, promptStyle, buttonStyle;
    Texture2D circle;

    public static void Begin(DeathCutscene cutscene)
    {
        var go = new GameObject("Ending Screen");
        go.AddComponent<EndingScreen>().cutscene = cutscene;
    }

    void Start()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" }, 64);
    }

    void Update()
    {
        if (cutscene == null) return;
        if (finishedAt < 0f && cutscene.Finished) finishedAt = Time.unscaledTime;
        if (finishedAt >= 0f && Time.unscaledTime - finishedAt > 4f && GameFlow.AnyButtonPressed())
            GameFlow.ReturnToTitle();
    }

    void OnGUI()
    {
        GUI.depth = -90;
        if (finishedAt < 0f || Event.current.type != EventType.Repaint) return;
        Make();
        float s = Screen.height / 720f;
        float t = Time.unscaledTime - finishedAt;
        Color old = GUI.color;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float ta = Mathf.Clamp01((t - 0.5f) / 1.5f);
        titleStyle.fontSize = Mathf.RoundToInt(80f * s);
        titleStyle.normal.textColor = new Color(0.93f, 0.86f, 0.62f, ta);
        GUI.Label(new Rect(0, Screen.height * 0.36f - 60f * s, Screen.width, 120f * s), title, titleStyle);

        float sa = Mathf.Clamp01((t - 1.8f) / 1.2f);
        subStyle.fontSize = Mathf.RoundToInt(34f * s);
        subStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, sa);
        GUI.Label(new Rect(0, Screen.height * 0.36f + 50f * s, Screen.width, 60f * s), subtitle, subStyle);

        if (t > 4f)
        {
            float pa = Mathf.Clamp01((t - 4f) / 0.6f) * (0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(t * 3f)));
            promptStyle.fontSize = Mathf.RoundToInt(24f * s);
            promptStyle.normal.textColor = new Color(0.9f, 0.88f, 0.82f, 1f);
            KeyCap.DrawPrompt("Press", KeyCap.Attack, "to return to the title",
                              Screen.width * 0.5f, Screen.height * 0.78f, promptStyle, s, s, pa);
        }
        GUI.color = old;
    }

    void Make()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
        subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
        buttonStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        if (font != null) { titleStyle.font = font; subStyle.font = font; promptStyle.font = font; }
        const int n = 64;
        circle = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
        circle.Apply();
    }
}
