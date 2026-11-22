using UnityEngine;
using UnityEngine.Playables;

// m094305's lake ending (from commit "npc", restored after the revert).
// Attach ONLY to the EndingObject at the lake. Picks the ending cutscene:
//   all three NPCs dead -> cutscene1 (Ending 1), anyone alive -> cutscene2 (Ending 2).
// The cutscenes are Timeline assets on PlayableDirectors - not made yet.
// (File renamed from Interactable.cs so Unity can attach it: file and class names must match.)
public class InteractableObject : MonoBehaviour
{
    [Header("NPC health references")]
    public GuardHealth npc1; // Vendor
    public GuardHealth npc2; // Knight
    public GuardHealth npc3; // Baba

    [Header("Ending cutscenes")]
    public PlayableDirector cutscene1; // All three are dead
    public PlayableDirector cutscene2; // At least one is alive
    [Tooltip("Ending 1 made of still pictures (all three dead). Used instead of Cutscene 1 when set.")]
    public DeathCutscene ending1Pictures;

    [Tooltip("Ending 2 made of still pictures (not all three dead: he gives himself to the water). " +
             "Used instead of Cutscene 2 when set. Its first picture is swapped for the reflection below.")]
    public DeathCutscene ending2Pictures;

    [System.Serializable]
    public class Reflection
    {
        [Tooltip("What he is wearing in this picture (the items taken from the NPCs he killed).")]
        public DialogueDirector.Item items;
        public Sprite picture;
    }
    [Tooltip("Ending 2's reflection, one per set of stolen items. The closest match is used when there's no exact one.")]
    public Reflection[] ending2Reflections;

    bool AllDead => npc1 != null && npc2 != null && npc3 != null &&
                    npc1.currentHealth <= 0 && npc2.currentHealth <= 0 && npc3.currentHealth <= 0;

    // Using the lake would play something right now.
    public bool EndingReady =>
        !DeathCutscene.Playing &&
        (AllDead ? (ending1Pictures != null || (cutscene1 != null && cutscene1.playableAsset != null))
                 : (ending2Pictures != null || (cutscene2 != null && cutscene2.playableAsset != null)));

    // The reflection that best matches what he's wearing: most items in common, then fewest he isn't wearing.
    Sprite PickReflection(DialogueDirector.Item worn)
    {
        Sprite best = null;
        int bestScore = int.MinValue;
        if (ending2Reflections == null) return null;
        foreach (Reflection r in ending2Reflections)
        {
            if (r == null || r.picture == null) continue;
            int common = Bits((int)(r.items & worn));
            int extra = Bits((int)(r.items & ~worn));
            int missing = Bits((int)(worn & ~r.items));
            int score = common * 10 - extra * 3 - missing;
            if (score > bestScore) { bestScore = score; best = r.picture; }
        }
        return best;
    }

    static int Bits(int v) { int n = 0; while (v != 0) { n += v & 1; v >>= 1; } return n; }

    public void Interact()
    {
        if (npc1 == null || npc2 == null || npc3 == null)
        {
            Debug.LogError(name + ": Assign GuardHealth for all three NPCs!");
            return;
        }

        bool allDead = npc1.currentHealth <= 0 &&
                       npc2.currentHealth <= 0 &&
                       npc3.currentHealth <= 0;

        // Ending 1 as still pictures (killed at the lake): plays instead of the Timeline when set.
        if (allDead && ending1Pictures != null)
        {
            if (DeathCutscene.Playing) return;
            AudioManager.QuietForEnding();   // the ending song starts with the stab (set on that picture)
            ending1Pictures.Play(null);
            EndingScreen.Begin(ending1Pictures);
            Debug.Log(name + ": Ending 1 (killed at the lake).");
            return;
        }

        // Ending 2 as still pictures (not enough - he gives himself to the water).
        if (!allDead && ending2Pictures != null)
        {
            if (DeathCutscene.Playing) return;
            Sprite reflection = PickReflection(DialogueDirector.Stolen);
            if (reflection != null && ending2Pictures.shots != null && ending2Pictures.shots.Length > 0)
                ending2Pictures.shots[0].image = reflection;
            AudioManager.QuietForEnding();   // its own music starts on the first picture
            ending2Pictures.Play(null);
            EndingScreen.Begin(ending2Pictures);
            Debug.Log(name + ": Ending 2 (not enough), wearing " + DialogueDirector.Stolen + ".");
            return;
        }

        PlayableDirector selected = allDead ? cutscene1 : cutscene2;
        PlayableDirector other = allDead ? cutscene2 : cutscene1;

        if (selected == null || selected.playableAsset == null)
        {
            Debug.LogWarning(name + ": " + (allDead ? "Ending 1" : "Ending 2") +
                " - no cutscene assigned yet (needs a PlayableDirector with a Timeline asset).");
            return;
        }

        if (other != null && other != selected)
            other.Stop();

        selected.Stop();
        selected.time = 0;
        selected.Play();

        Debug.Log(name + (allDead ? ": Playing Cutscene1" : ": Playing Cutscene2"));
    }
}
