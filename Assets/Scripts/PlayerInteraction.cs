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

    void Update()
    {
        bool inRange = IsInRange() && !DialogueManager.InputBlocked && !DialogueDirector.Freezing;

        if (interactButton != null &&
            interactButton.gameObject.activeSelf != inRange)
        {
            interactButton.gameObject.SetActive(inRange);
        }

        // Was keyboard E only; now also J, arcade button 2 and gamepad X.
        if (inRange && GameInput.InteractPressed)
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
