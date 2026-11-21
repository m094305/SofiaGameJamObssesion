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
