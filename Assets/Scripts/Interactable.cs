using UnityEngine;
using UnityEngine.Playables;

// Attach ONLY to the EndingObject. No collision handling.
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
            Debug.LogError(name + ": The selected cutscene needs a PlayableDirector and Timeline asset!");
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
