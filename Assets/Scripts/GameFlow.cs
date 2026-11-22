using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

// Restarting the level after GAME OVER ("Press A to try again").
// Reloads the scene and re-runs the things that normally only set themselves up at game launch
// (forest colliders/bushes, button hints, title screen). A retry skips the title screen.
public static class GameFlow
{
    // True after "try again": straight into the game, main theme, no title.
    public static bool Retrying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Retrying = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void Restart()
    {
        Retrying = true;
        Collectible.ResetForNewRun();
        Scene s = SceneManager.GetActiveScene();
        if (s.buildIndex >= 0) SceneManager.LoadScene(s.buildIndex);
        else SceneManager.LoadScene(s.path);   // a scene not in Build Settings (e.g. Copy in the editor)
    }

    // After the ending: back to the title screen with a fresh level.
    public static void ReturnToTitle()
    {
        Restart();
        Retrying = false;   // set after Restart() so the reloaded level shows the title
    }

    // Runs for every scene load (the installers skip anything already there).
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ForestSetup.AutoCreate();
        TitleScreen.Install();
        ControlHints.Install();
        StoryLines.Install();
    }

    // Any button on the arcade stick, a gamepad or the keyboard.
    public static bool AnyButtonPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        foreach (InputDevice d in InputSystem.devices)
        {
            if (!(d is Gamepad) && !(d is Joystick)) continue;
            foreach (InputControl c in d.allControls)
                if (c is ButtonControl b && !b.synthetic && b.wasPressedThisFrame) return true;
        }
        return false;
    }
}
