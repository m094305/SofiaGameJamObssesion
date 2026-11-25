using UnityEngine;

// Draws a keyboard key (a little 3D key-cap with its name on it) in OnGUI, for the on-screen hints:
// "E", "Space", "Tab"... The key widens to fit its label.
public static class KeyCap
{
    // The keys the game uses (see GameInput / ArcadeControlls.inputactions).
    public const string Attack = "Space";     // attack / throw / punch, next line, start
    public const string Pickup = "E";         // pick up / swap, look into the lake
    public const string Inventory = "Tab";

    static GUIStyle style;

    static void Make()
    {
        if (style != null) return;
        style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
        style.padding = new RectOffset(0, 0, 0, 0);
    }

    // Width the key will take for this label at scale s.
    public static float Width(string label, float s)
    {
        Make();
        style.fontSize = Mathf.RoundToInt(15f * s);
        float tw = style.CalcSize(new GUIContent(label)).x;
        return Mathf.Max(Height(s), tw + 16f * s);
    }

    public static float Height(float s) => 28f * s;

    // "before [key] after", centred on (centreX, centreY), with shadowed text in the given style.
    public static void DrawPrompt(string before, string key, string after, float centreX, float centreY,
                                  GUIStyle textStyle, float s, float keyScale, float alpha)
    {
        TextAnchor oldAlign = textStyle.alignment;
        textStyle.alignment = TextAnchor.MiddleLeft;
        float gap = 10f * s;
        float wb = textStyle.CalcSize(new GUIContent(before)).x;
        float wa = textStyle.CalcSize(new GUIContent(after)).x;
        float wk = Width(key, keyScale);
        float x = centreX - (wb + gap + wk + gap + wa) * 0.5f;
        float h = Mathf.Max(textStyle.CalcSize(new GUIContent("Ag")).y, Height(keyScale)) + 4f * s;

        Color old = GUI.color;
        GUI.color = Color.white;
        Color tc = textStyle.normal.textColor;
        Rect rb = new Rect(x, centreY - h * 0.5f, wb + 4f * s, h);
        Rect ra = new Rect(x + wb + gap + wk + gap, centreY - h * 0.5f, wa + 4f * s, h);
        textStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f * alpha);
        GUI.Label(new Rect(rb.x + 2f * s, rb.y + 2f * s, rb.width, rb.height), before, textStyle);
        GUI.Label(new Rect(ra.x + 2f * s, ra.y + 2f * s, ra.width, ra.height), after, textStyle);
        textStyle.normal.textColor = new Color(tc.r, tc.g, tc.b, alpha);
        GUI.Label(rb, before, textStyle);
        GUI.Label(ra, after, textStyle);
        Draw(x + wb + gap, centreY, key, keyScale, alpha);

        textStyle.normal.textColor = tc;
        textStyle.alignment = oldAlign;
        GUI.color = old;
    }

    // Draws the key with its left edge at x, centred vertically on centreY. Returns its width.
    public static float Draw(float x, float centreY, string label, float s, float alpha)
    {
        Make();
        float w = Width(label, s), h = Height(s);
        Rect r = new Rect(x, centreY - h * 0.5f, w, h);
        float rad = 5f * s;
        Texture t = Texture2D.whiteTexture;

        // shadow, key body (darker sides), top face, a thin highlight along its top edge
        GUI.DrawTexture(new Rect(r.x + 1.5f * s, r.y + 2.5f * s, w, h), t, ScaleMode.StretchToFill, true, 0f,
                        new Color(0f, 0f, 0f, 0.55f * alpha), 0f, rad);
        GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true, 0f, new Color(0.1f, 0.1f, 0.12f, alpha), 0f, rad);
        Rect face = new Rect(r.x + 2f * s, r.y + 1.5f * s, w - 4f * s, h - 5.5f * s);
        GUI.DrawTexture(face, t, ScaleMode.StretchToFill, true, 0f, new Color(0.3f, 0.3f, 0.34f, alpha), 0f, rad * 0.8f);
        GUI.DrawTexture(new Rect(face.x + rad * 0.6f, face.y + 1f * s, face.width - rad * 1.2f, 1.2f * s), t,
                        ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.18f * alpha), 0f, 0f);

        style.fontSize = Mathf.RoundToInt(15f * s);
        style.normal.textColor = new Color(0.97f, 0.95f, 0.9f, alpha);
        Color old = GUI.color;
        GUI.color = Color.white;
        GUI.Label(face, label, style);
        GUI.color = old;
        return w;
    }
}
