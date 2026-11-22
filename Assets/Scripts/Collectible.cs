using System.Collections.Generic;
using UnityEngine;

// Something the player can pick up with the pick-up button (arcade A / J) that just goes into
// the inventory - e.g. the hidden narcissus flower easter egg.
public class Collectible : MonoBehaviour
{
    [Tooltip("Shown in the inventory once picked up.")]
    public string inventoryText = "- A narcissus flower";

    static readonly List<Collectible> all = new List<Collectible>();
    static readonly List<string> collected = new List<string>();

    // Lines for the inventory screen, in pick-up order.
    public static IReadOnlyList<string> Collected => collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { all.Clear(); collected.Clear(); }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    // Picks up the nearest collectible in range. Returns true if one was picked up.
    public static bool TryCollectNear(Vector2 position, float range)
    {
        Collectible best = null;
        float bestDist = range;
        foreach (Collectible c in all)
        {
            float d = Vector2.Distance(position, c.transform.position);
            if (d <= bestDist) { best = c; bestDist = d; }
        }
        if (best == null) return false;

        collected.Add(best.inventoryText);
        best.gameObject.SetActive(false);
        return true;
    }
}
