using System.Collections.Generic;
using UnityEngine;

// The player is hidden while standing inside one of the individual bushes.
// NPCs can't see a hidden player. Added to the player automatically by ForestSetup.
public class PlayerHiding : MonoBehaviour
{
    [Tooltip("How see-through the player gets while hidden.")]
    [Range(0f, 1f)] public float hiddenAlpha = 0.45f;

    public bool IsHidden { get; private set; }

    struct Bush { public Vector2 center; public float radius; }
    static readonly List<Bush> bushes = new List<Bush>();

    Collider2D body;
    SpriteRenderer[] renderers;
    float[] normalAlpha;

    public static void RegisterBush(Vector2 center, float radius)
    {
        bushes.Add(new Bush { center = center, radius = radius });
    }

    public static void ClearBushes()
    {
        bushes.Clear();
    }

    // True if this player (or anything with PlayerHiding in its parents) is in a bush.
    public static bool IsPlayerHidden(Component player)
    {
        if (player == null) return false;
        PlayerHiding h = player.GetComponentInParent<PlayerHiding>();
        return h != null && h.IsHidden;
    }

    void Awake()
    {
        body = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        normalAlpha = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            normalAlpha[i] = renderers[i].color.a;
    }

    void Update()
    {
        Vector2 pos = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

        bool hidden = false;
        foreach (Bush b in bushes)
        {
            if ((pos - b.center).sqrMagnitude <= b.radius * b.radius)
            {
                hidden = true;
                break;
            }
        }

        if (hidden != IsHidden)
        {
            IsHidden = hidden;
            SetAlpha(hidden);
        }
    }

    void SetAlpha(bool hidden)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = hidden ? normalAlpha[i] * hiddenAlpha : normalAlpha[i];
            renderers[i].color = c;
        }
    }

    void OnDisable()
    {
        if (IsHidden)
        {
            IsHidden = false;
            SetAlpha(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.6f);
        foreach (Bush b in bushes)
            Gizmos.DrawWireSphere(b.center, b.radius);
    }
}
