using UnityEngine;
using UnityEngine.UI;

// m094305's player side of the lake ending (from commit "npc", restored after the revert).
// Shows the interact button near the EndingObject; E / J / arcade button 2 / gamepad X uses it.
public class PlayerInteraction : MonoBehaviour
{
    [Header("Ending Object")]
    public InteractableObject endingObject;

    [Header("UI Button (optional)")]
    public Button interactButton;

    [Header("Interaction")]
    public float interactionDistance = 2.5f;

    void Start()
    {
        if (endingObject == null)
            Debug.LogError("Assign EndingObject!");

        if (interactButton != null)
        {
            interactButton.onClick.AddListener(Interact);
            interactButton.gameObject.SetActive(false);
        }
    }

    // True while standing at the lake with the ending ready (read by ArcadeButtons and ControlHints).
    public static bool AtLake { get; private set; }
    public Vector3 LakePosition => endingObject != null ? endingObject.transform.position : transform.position;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { AtLake = false; }

    void OnDisable() { AtLake = false; }

    void Update()
    {
        bool inRange = IsInRange() && !DialogueManager.InputBlocked && !DialogueDirector.Freezing &&
                       !TitleScreen.Showing;
        // The lake has something to show (the ending is ready): A uses it instead of attacking.
        AtLake = inRange && endingObject.EndingReady;

        // The on-screen "A  Look into the lake" prompt (ControlHints) replaces the old UI button.
        if (interactButton != null && interactButton.gameObject.activeSelf)
            interactButton.gameObject.SetActive(false);

        // The A button (arcade trigger / K / gamepad A), or E.
        if (AtLake && GameInput.LakePressed)
            Interact();
    }

    bool IsInRange()
    {
        if (endingObject == null ||
            !endingObject.isActiveAndEnabled)
            return false;

        return Vector2.Distance(
            transform.position,
            endingObject.transform.position
        ) <= interactionDistance;
    }

    public void Interact()
    {
        if (!IsInRange())
            return;

        endingObject.Interact();
    }

    void OnDestroy()
    {
        if (interactButton != null)
            interactButton.onClick.RemoveListener(Interact);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            interactionDistance
        );
    }
}
