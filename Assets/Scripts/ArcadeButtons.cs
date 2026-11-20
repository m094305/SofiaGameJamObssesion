
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeButtons : MonoBehaviour
{
    public InputActionReference buttonA;
    public InputActionReference buttonB;
    public InputActionReference buttonC;

    private PlayerInventory inventory;

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
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

        // A - Pick up
        if (buttonA != null &&
            buttonA.action.WasPressedThisFrame())
        {
            inventory.TryPickup();
        }

        // B - Throw
        if (buttonB != null &&
            buttonB.action.WasPressedThisFrame())
        {
            inventory.TryThrow();
        }

        // C - Inventory
        if (buttonC != null &&
            buttonC.action.WasPressedThisFrame())
        {
            inventory.ToggleInventory();
        }
    }
}
