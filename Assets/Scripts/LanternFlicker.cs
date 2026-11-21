using UnityEngine;
using UnityEngine.Rendering.Universal;

// Gentle candle-like flicker for an NPC's lantern light.
[RequireComponent(typeof(Light2D))]
public class LanternFlicker : MonoBehaviour
{
    [Tooltip("How much the brightness wavers (0 = steady).")]
    [Range(0f, 1f)] public float amount = 0.15f;
    [Tooltip("How fast it wavers.")]
    public float speed = 3f;

    Light2D lantern;
    float baseIntensity;
    float baseRadius;
    float seed;

    void Awake()
    {
        lantern = GetComponent<Light2D>();
        baseIntensity = lantern.intensity;
        baseRadius = lantern.pointLightOuterRadius;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;   // -0.5..0.5
        lantern.intensity = baseIntensity * (1f + n * 2f * amount);
        lantern.pointLightOuterRadius = baseRadius * (1f + n * amount * 0.5f);
    }
}
