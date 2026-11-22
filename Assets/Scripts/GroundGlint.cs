using UnityEngine;
using UnityEngine.Rendering.Universal;

// A weapon lying on the ground catches the light now and then so the player can spot it in the
// dark: a small glow, and every few seconds a light flash with a white star twinkling on it.
// Put it on the item; it adds its own light and sparkle.
public class GroundGlint : MonoBehaviour
{
    public Color color = new Color(1f, 0.95f, 0.8f);
    [Tooltip("Seconds between glints.")]
    public float interval = 1.8f;
    [Tooltip("How long one glint lasts.")]
    public float glintTime = 0.45f;
    public float glintIntensity = 2.6f;
    [Tooltip("Faint glow between glints, so it's never fully lost in the dark.")]
    public float restIntensity = 0.45f;
    public float radius = 1.3f;

    [Header("Sparkle")]
    [Tooltip("Where the star twinkles, relative to the item (e.g. on the blade).")]
    public Vector2 sparkleOffset = new Vector2(0f, 0.15f);
    [Tooltip("Size of the star at its brightest, in world units.")]
    public float sparkleSize = 0.55f;
    [Tooltip("Optional unlit material so the star stays bright at night.")]
    public Material sparkleMaterial;

    [Header("Halo")]
    [Tooltip("Soft light patch under the item (world units across). 0 = none.")]
    public float haloSize = 1.2f;
    public Color haloColor = new Color(1f, 0.93f, 0.75f, 1f);
    [Tooltip("Halo opacity: resting (slow breathing between these) and at the glint.")]
    public Vector2 haloRestAlpha = new Vector2(0.18f, 0.32f);
    public float haloGlintAlpha = 0.6f;

    Light2D glow;
    Transform sparkle;
    SpriteRenderer halo;
    float offset;

    static Sprite starSprite;
    static Sprite haloSprite;

    void Awake()
    {
        Transform child = transform.Find("Glint Light");
        if (child == null)
        {
            child = new GameObject("Glint Light").transform;
            child.SetParent(transform, false);
        }
        glow = child.GetComponent<Light2D>();
        if (glow == null) glow = child.gameObject.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = color;
        glow.pointLightOuterRadius = radius;
        glow.pointLightInnerRadius = 0f;
        glow.falloffIntensity = 0.7f;

        sparkle = transform.Find("Sparkle");
        if (sparkle == null)
        {
            sparkle = new GameObject("Sparkle").transform;
            sparkle.SetParent(transform, false);
            var sr = sparkle.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = StarSprite();
            SpriteRenderer item = GetComponent<SpriteRenderer>();
            if (item != null)
            {
                sr.sortingLayerID = item.sortingLayerID;
                sr.sortingOrder = item.sortingOrder + 1;
                sr.sharedMaterial = item.sharedMaterial;
            }
            if (sparkleMaterial != null) sr.sharedMaterial = sparkleMaterial;
        }
        sparkle.localPosition = sparkleOffset;
        sparkle.localScale = Vector3.zero;

        Transform h = transform.Find("Halo");
        if (h == null && haloSize > 0f)
        {
            h = new GameObject("Halo").transform;
            h.SetParent(transform, false);
            halo = h.gameObject.AddComponent<SpriteRenderer>();
            halo.sprite = HaloSprite();
            SpriteRenderer item = GetComponent<SpriteRenderer>();
            if (item != null)
            {
                halo.sortingLayerID = item.sortingLayerID;
                halo.sortingOrder = item.sortingOrder - 1;
                halo.sharedMaterial = item.sharedMaterial;
            }
            if (sparkleMaterial != null) halo.sharedMaterial = sparkleMaterial;
        }
        else if (h != null) halo = h.GetComponent<SpriteRenderer>();

        offset = Random.value * interval;
    }

    void OnDisable()
    {
        if (glow != null) glow.intensity = 0f;
        if (sparkle != null) sparkle.localScale = Vector3.zero;
        if (halo != null) halo.enabled = false;
    }

    void OnEnable()
    {
        if (halo != null) halo.enabled = true;
    }

    void Update()
    {
        float t = (Time.time + offset) % interval;
        float k = t < glintTime ? Mathf.Sin(t / glintTime * Mathf.PI) : 0f;   // 0 -> 1 -> 0
        glow.intensity = Mathf.Lerp(restIntensity, glintIntensity, k);

        // The star pops in, turns a little and shrinks away (undoing the item's own scale and tilt).
        Vector3 ls = transform.lossyScale;
        float s = sparkleSize * k;
        sparkle.localScale = new Vector3(s / Mathf.Max(0.01f, ls.x), s / Mathf.Max(0.01f, ls.y), 1f);
        sparkle.rotation = Quaternion.Euler(0f, 0f, Mathf.Min(t, glintTime) / glintTime * 45f);

        if (halo != null)
        {
            float breathe = 0.5f + 0.5f * Mathf.Sin((Time.time + offset) * 2.2f);
            float a = Mathf.Lerp(Mathf.Lerp(haloRestAlpha.x, haloRestAlpha.y, breathe), haloGlintAlpha, k);
            halo.color = new Color(haloColor.r, haloColor.g, haloColor.b, a);
            Transform ht = halo.transform;
            ht.rotation = Quaternion.identity;
            ht.position = transform.position;
            ht.localScale = new Vector3(haloSize / Mathf.Max(0.01f, ls.x), haloSize * 0.6f / Mathf.Max(0.01f, ls.y), 1f);
        }
    }

    // A soft round patch of light, fading out to the edge (flattened into an oval under the item).
    public static Sprite HaloSprite()
    {
        if (haloSprite != null) return haloSprite;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        haloSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return haloSprite;
    }

    // A small four-pointed star, made once in code (crisp pixels).
    public static Sprite StarSprite()
    {
        if (starSprite != null) return starSprite;
        const int n = 17;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        int c = n / 2;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
                float a = 0f;
                if (dy == 0) a = 1f - dx / (float)(c + 1);                 // horizontal arm
                if (dx == 0) a = Mathf.Max(a, 1f - dy / (float)(c + 1));  // vertical arm
                if (dx + dy <= 1) a = 1f;                                   // bright core
                else if (dx == 1 && dy == 1) a = Mathf.Max(a, 0.7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 0.92f, Mathf.Clamp01(a)));
            }
        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return starSprite;
    }
}
