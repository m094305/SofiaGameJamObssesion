using System.Collections.Generic;
using UnityEngine;

// A few lines he says on his own, to tell the player what the game is about:
//   - the opening, once the title screen is gone (not on "try again"),
//   - after the third kill, pointing him to the lake.
// Each waits until no conversation or cutscene is on screen. Added to the player automatically.
public class StoryLines : MonoBehaviour
{
    [TextArea(2, 4)]
    public string openingLine = "Every morning the lake shows Me the finest face in the world... and every morning it is bare.";
    [TextArea(2, 4)]
    public string allTakenLine = "All of it, Mine. Now — the lake must see Me.";
    [Tooltip("Pause before each line once the world is free.")]
    public float delay = 0.8f;

    public static void Install()
    {
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        if (player != null && player.GetComponent<StoryLines>() == null)
            player.gameObject.AddComponent<StoryLines>();
    }

    bool openingDone, allTakenDone;
    float freeSince = -1f;
    InteractableObject lake;

    void Start()
    {
        openingDone = GameFlow.Retrying;   // a retry goes straight back into the game
        lake = FindAnyObjectByType<InteractableObject>(FindObjectsInactive.Include);
    }

    bool AllDead =>
        lake != null && lake.npc1 != null && lake.npc2 != null && lake.npc3 != null &&
        lake.npc1.currentHealth <= 0 && lake.npc2.currentHealth <= 0 && lake.npc3.currentHealth <= 0;

    void Update()
    {
        bool free = !TitleScreen.Showing && !DeathCutscene.Playing && !DialogueDirector.Freezing &&
                    !DialogueManager.AnyDialogueActive && !DialogueDirector.Instance.HasPending;
        PlayerHealth h = GetComponent<PlayerHealth>();
        if (h != null && h.IsDead) free = false;

        if (!free) { freeSince = -1f; return; }
        if (freeSince < 0f) freeSince = Time.time;
        if (Time.time - freeSince < delay) return;

        if (!openingDone)
        {
            openingDone = true;
            Say(openingLine);
        }
        else if (!allTakenDone && AllDead)
        {
            allTakenDone = true;
            Say(allTakenLine);
        }
    }

    static void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var lines = new List<DialogueLine>
        {
            new DialogueLine { characterName = "Narcissus", line = text, side = SpeakerSide.Right },
        };
        DialogueDirector.Instance.Play(lines, null, null);
    }
}
