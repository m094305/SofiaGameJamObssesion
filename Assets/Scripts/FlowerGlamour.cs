using UnityEngine;
using UnityEngine.Rendering.Universal;

// Makes the hidden narcissus flower look magical: a soft golden glow that breathes,
// a gentle sway, and little stars that pop up around it and drift upwards.
public class FlowerGlamour : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.85f, 0.45f);
    public float glowRadius = 2.2f;
    public Vector2 glowIntensity = new Vector2(0.8f, 1.6f);
    [Tooltip("Soft light patch under the flower.")]
    public Color haloColor = new Color(1f, 0.9f, 0.55f, 1f);
    public float haloSize = 1.1f;
    public Vector2 haloAlpha = new Vector2(0.2f, 0.4f);

    [Header("Sway")]
    public float swayDegrees = 5f;
    public float swaySpeed = 1.6f;

    [Header("Sparkles")]
    public int sparkleCount = 7;
    [Tooltip("How far around the flower they appear.")]
    public float sparkleRadius = 0.55f;
    public float sparkleSize = 0.4f;
    public float sparkleLife = 1.3f;
    public float sparkleRise = 0.35f;
    public Color sparkleColorA = new Color(1f, 0.97f, 0.75f);
    public Color sparkleColorB = new Color(1f, 0.8f, 0.95f);
    [Tooltip("Optional unlit material so it shines at night.")]
    public Material unlitMaterial;

    Light2D glow;
    SpriteRenderer halo;
    Transform[] sparkles;
    SpriteRenderer[] sparkleRenderers;
    Vector2[] sparkleStart;
    float[] sparkleBorn;
    SpriteRenderer flower;
    float seed;

    void Awake()
    {
        flower = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;

        var lightGo = new GameObject("Glamour Light");
        lightGo.transform.SetParent(transform, false);
        glow = lightGo.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = glowColor;
        glow.pointLightOuterRadius = glowRadius;
        glow.pointLightInnerRadius = 0f;
        glow.falloffIntensity = 0.75f;

        // The halo and sparkles live on a sibling-free holder so the sway doesn't tilt them.
        halo = NewSprite("Glamour Halo", GroundGlint.HaloSprite(), -1);
        halo.transform.localScale = new Vector3(haloSize, haloSize * 0.6f, 1f);

        sparkles = new Transform[sparkleCount];
        sparkleRenderers = new SpriteRenderer[sparkleCount];
        sparkleStart = new Vector2[sparkleCount];
        sparkleBorn = new float[sparkleCount];
        for (int i = 0; i < sparkleCount; i++)
        {
            sparkleRenderers[i] = NewSprite("Glamour Sparkle " + (i + 1), GroundGlint.StarSprite(), 1);
            sparkles[i] = sparkleRenderers[i].transform;
            Respawn(i, Time.time - sparkleLife * i / sparkleCount);   // staggered
        }
    }

    SpriteRenderer NewSprite(string n, Sprite s, int orderOffset)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        if (flower != null)
        {
            sr.sortingLayerID = flower.sortingLayerID;
            sr.sortingOrder = flower.sortingOrder + orderOffset;
            sr.sharedMaterial = flower.sharedMaterial;
        }
        if (unlitMaterial != null) sr.sharedMaterial = unlitMaterial;
        return sr;
    }

    void Respawn(int i, float born)
    {
        Vector2 r = Random.insideUnitCircle * sparkleRadius;
        sparkleStart[i] = new Vector2(r.x, r.y * 0.7f + 0.15f);
        sparkleBorn[i] = born;
        sparkleRenderers[i].color = Color.Lerp(sparkleColorA, sparkleColorB, Random.value);
    }

    void Update()
    {
        float t = Time.time + seed;
        float breathe = 0.5f + 0.5f * Mathf.Sin(t * 1.8f);

        glow.intensity = Mathf.Lerp(glowIntensity.x, glowIntensity.y, breathe);
        Color hc = haloColor; hc.a = Mathf.Lerp(haloAlpha.x, haloAlpha.y, breathe);
        halo.color = hc;

        // Sway the flower itself (children are counter-rotated below).
        float sway = Mathf.Sin(t * swaySpeed) * swayDegrees;
        transform.localRotation = Quaternion.Euler(0f, 0f, sway);

        Vector3 ls = transform.lossyScale;
        Vector3 unscale = new Vector3(1f / Mathf.Max(0.01f, ls.x), 1f / Mathf.Max(0.01f, ls.y), 1f);
        halo.transform.rotation = Quaternion.identity;
        halo.transform.position = transform.position;
        halo.transform.localScale = Vector3.Scale(new Vector3(haloSize, haloSize * 0.6f, 1f), unscale);

        for (int i = 0; i < sparkles.Length; i++)
        {
            float age = (Time.time - sparkleBorn[i]) / sparkleLife;
            if (age >= 1f) { Respawn(i, Time.time); age = 0f; }
            float k = Mathf.Sin(age * Mathf.PI);   // 0 -> 1 -> 0
            Transform s = sparkles[i];
            s.rotation = Quaternion.Euler(0f, 0f, age * 90f);
            s.position = transform.position + (Vector3)(sparkleStart[i] + new Vector2(0f, sparkleRise * age));
            s.localScale = Vector3.Scale(Vector3.one * sparkleSize * k, unscale);
        }
    }
}
