using UnityEngine.InputSystem;

// One place for the buttons that scripts share, so keyboard, arcade stick and gamepad all work.
//
//   Arcade cabinet (DragonRise / generic USB encoder):
//     trigger (button 1) = Attack / throw  - also advances dialogue
//     button 2           = Pick up         - also "interact" (NPCs, the lake)
//     button 3           = Inventory
//   Keyboard: WASD move, J pick up / interact, K attack, I inventory, E interact + advance dialogue
//   Gamepad:  left stick / d-pad, X(West) pick up, A(South) attack, Y(North) inventory
//
// The pick up / attack / inventory buttons themselves live in ArcadeControlls.inputactions.
public static class GameInput
{
    public static readonly string[] AdvanceDialogueBindings =
    {
        "<Keyboard>/e", "<Keyboard>/k", "<Keyboard>/space", "<Keyboard>/enter",
        "<Joystick>/trigger", "<Gamepad>/buttonSouth",
    };

    public static readonly string[] InteractBindings =
    {
        "<Keyboard>/e", "<Keyboard>/j",
        "<Joystick>/button2", "<Gamepad>/buttonWest",
    };

    static InputAction interact;

    // The A button (arcade trigger / K / gamepad A) plus E: uses the lake.
    public static readonly string[] LakeBindings =
    {
        "<Joystick>/trigger", "<Keyboard>/k", "<Gamepad>/buttonSouth", "<Keyboard>/e",
    };
    static InputAction lake;

    public static bool LakePressed
    {
        get
        {
            if (lake == null)
            {
                lake = new InputAction("Lake", InputActionType.Button);
                foreach (string b in LakeBindings) lake.AddBinding(b);
                lake.Enable();
            }
            return lake.WasPressedThisFrame();
        }
    }

    // True on the frame any interact button is pressed.
    public static bool InteractPressed
    {
        get
        {
            if (interact == null)
            {
                interact = new InputAction("Interact", InputActionType.Button);
                foreach (string b in InteractBindings) interact.AddBinding(b);
                interact.Enable();
            }
            return interact.WasPressedThisFrame();
        }
    }
}
