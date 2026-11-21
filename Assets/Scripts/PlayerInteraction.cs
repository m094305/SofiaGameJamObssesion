
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Ending Object")]
    public InteractableObject endingObject;

    [Header("UI Button")]
    public Button interactButton;

    [Header("Interaction")]
    public float interactionDistance = 2.5f;

    void Start()
    {
        if (endingObject == null)
            Debug.LogError("Assign EndingObject!");

        if (interactButton == null)
        {
            Debug.LogError("Assign InteractButton!");
            return;
        }

        interactButton.onClick.AddListener(Interact);
        interactButton.gameObject.SetActive(false);
    }

    void Update()
    {
        bool inRange = IsInRange();

        if (interactButton != null &&
            interactButton.gameObject.activeSelf != inRange)
        {
            interactButton.gameObject.SetActive(inRange);
        }

        if (inRange &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
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
