using UnityEngine.InputSystem;

// One place for the buttons that scripts share, so keyboard, arcade stick and gamepad all work.
//
//   Arcade cabinet (DragonRise / generic USB encoder):
//     trigger (button 1) = Attack / throw  - also advances dialogue
//     button 2           = Pick up         - also "interact" (NPCs, the lake)
//     button 3           = Inventory
//   Keyboard: WASD / arrows move, E pick up / interact (lake, NPCs), Space attack + next line,
//             Tab or I inventory, Enter also advances dialogue
//   Gamepad:  left stick / d-pad, X(West) pick up, A(South) attack, Y(North) inventory
//
// The pick up / attack / inventory buttons themselves live in ArcadeControlls.inputactions.
public static class GameInput
{
    public static readonly string[] AdvanceDialogueBindings =
    {
        "<Keyboard>/e", "<Keyboard>/space", "<Keyboard>/enter",
        "<Joystick>/trigger", "<Gamepad>/buttonSouth",
    };

    public static readonly string[] InteractBindings =
    {
        "<Keyboard>/e",
        "<Joystick>/button2", "<Gamepad>/buttonWest",
    };

    static InputAction interact;

    // Uses the lake: E or Space on the keyboard, the arcade trigger, gamepad A.
    public static readonly string[] LakeBindings =
    {
        "<Joystick>/trigger", "<Keyboard>/space", "<Gamepad>/buttonSouth", "<Keyboard>/e",
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
