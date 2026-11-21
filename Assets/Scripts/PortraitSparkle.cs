using UnityEngine;
using UnityEngine.UI;

// Makes a "SPARKLE" overlay image flicker on top of a dialogue portrait,
// e.g. the glint on the knight's silver cross that Narcissus wants.
[RequireComponent(typeof(Image))]
public class PortraitSparkle : MonoBehaviour
{
    [Tooltip("Flickers per second.")]
    public float speed = 2.5f;
    [Range(0f, 1f)] public float minAlpha = 0.15f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    Image image;
    float seed;

    void Awake()
    {
        image = GetComponent<Image>();
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Uneven shimmer rather than a smooth pulse.
        float n = Mathf.PerlinNoise(seed, Time.unscaledTime * speed);
        Color c = image.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, n * n);
        image.color = c;
    }
}
