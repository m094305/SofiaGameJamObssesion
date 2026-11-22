
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeButtons : MonoBehaviour
{
    public InputActionReference buttonA;
    public InputActionReference buttonB;
    public InputActionReference buttonC;

    private PlayerInventory inventory;
    private PlayerHealth health;

    void Awake()
    {
        inventory = GetComponentInChildren<PlayerInventory>(true);

        if (inventory == null)
            Debug.LogError("PlayerInventory child not found!");
    }

    void OnEnable()
    {
        if (buttonA != null) buttonA.action.Enable();
        if (buttonB != null) buttonB.action.Enable();
        if (buttonC != null) buttonC.action.Enable();
    }

    void OnDisable()
    {
        if (buttonA != null) buttonA.action.Disable();
        if (buttonB != null) buttonB.action.Disable();
        if (buttonC != null) buttonC.action.Disable();
    }

    void Update()
    {
        if (inventory == null)
            return;

        // Frozen while a conversation is on screen: no picking up, attacking or inventory.
        if (DialogueManager.InputBlocked || DialogueDirector.Freezing)
            return;

        // Nothing to do once he's dead (game over).
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (health == null) health = FindAnyObjectByType<PlayerHealth>();
        if (health != null && health.IsDead)
            return;

        // A - PICK UP / SWITCH
        if (buttonA != null &&
            buttonA.action.WasPressedThisFrame())
        {
            inventory.TryPickup();
        }

        // B - THROW / MELEE ATTACK
        if (buttonB != null &&
            buttonB.action.WasPressedThisFrame())
        {
            inventory.TryThrow();
        }

        // C - INVENTORY
        if (buttonC != null &&
            buttonC.action.WasPressedThisFrame())
        {
            inventory.ToggleInventory();
        }
    }
}
