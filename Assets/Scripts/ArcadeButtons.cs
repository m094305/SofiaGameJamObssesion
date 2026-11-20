
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeButtons : MonoBehaviour
{
    public InputActionReference buttonA;
    public InputActionReference buttonB;
    public InputActionReference buttonC;

    private string message = "";

    void OnEnable()
    {
        buttonA.action.Enable();
        buttonB.action.Enable();
        buttonC.action.Enable();
    }

    void OnDisable()
    {
        buttonA.action.Disable();
        buttonB.action.Disable();
        buttonC.action.Disable();
    }

    void Update()
    {
        if (buttonA.action.WasPressedThisFrame())
        {
            message = "A - Interact / Pick up";
            Debug.Log(message);
        }

        if (buttonB.action.WasPressedThisFrame())
        {
            message = "B - Attack / Throw";
            Debug.Log(message);
        }

        if (buttonC.action.WasPressedThisFrame())
        {
            message = "C - Inventory";
            Debug.Log(message);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 30;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(20, 20, 500, 60),
            message,
            style
        );
    }
}
