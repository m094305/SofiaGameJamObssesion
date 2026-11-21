using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Text & Animator")]
    [SerializeField] private Animator dialogBoxAnimator;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogAreaText;

    [Header("Text Positions (Right vs. Left Speaker)")]
    [SerializeField] private RectTransform characterNameRect;
    [SerializeField] private RectTransform dialogAreaRect;
    [SerializeField] private Vector2 namePosForRightSpeaker;
    [SerializeField] private Vector2 dialogPosForRightSpeaker;
    [SerializeField] private Vector2 namePosForLeftSpeaker;
    [SerializeField] private Vector2 dialogPosForLeftSpeaker;

    [Header("Character Portrait Objects")]
    [SerializeField] private GameObject knightPortrait;
    [SerializeField] private GameObject vendorPortrait;
    [SerializeField] private GameObject babataPortrait;
    [SerializeField] private GameObject narcisShadowPortrait; // Main Narcis image for standard dialogs
    [SerializeField] private List<GameObject> allPortraitObjects = new List<GameObject>();

    [Header("Typing & Input Settings")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private string advanceBinding = "<Keyboard>/e";

    private readonly Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private InputAction advanceAction;
    private Coroutine typingCoroutine;
    private DialogueLine currentLine;
    private bool isDialogueActive;
    private bool isTyping;

    // Read-only, so other scripts (e.g. NPCGuard) can hide their prompts while someone is talking.
    public bool IsDialogueActive => isDialogueActive;
    public static bool AnyDialogueActive => Instance != null && Instance.isDialogueActive;

    // The button that closes the last line shouldn't also attack/interact that same frame.
    private static int lastEndedFrame = -10;
    public static bool InputBlocked =>
        AnyDialogueActive || UnityEngine.Time.frameCount - lastEndedFrame <= 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        advanceAction = new InputAction("AdvanceDialogue", InputActionType.Button, advanceBinding);
        // Also the arcade attack button, K/Space/Enter and the gamepad A button.
        foreach (string b in GameInput.AdvanceDialogueBindings)
            if (b != advanceBinding) advanceAction.AddBinding(b);
        advanceAction.performed += OnAdvancePerformed;
    }

    private void OnEnable()
    {
        advanceAction?.Enable();
    }

    private void OnDisable()
    {
        advanceAction?.Disable();
    }

    private void OnDestroy()
    {
        if (advanceAction != null)
        {
            advanceAction.performed -= OnAdvancePerformed;
            advanceAction.Dispose();
        }
    }

    private void Start()
    {
        HideAllPortraits();
    }

    private void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            // Complete current line immediately if still typing
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            if (dialogAreaText != null && currentLine != null)
            {
                dialogAreaText.text = currentLine.line;
            }
            isTyping = false;
        }
        else
        {
            DisplayNextLine();
        }
    }

    public void StartDialogue(IEnumerable<DialogueLine> lines)
    {
        if (lines == null) return;

        lineQueue.Clear();
        foreach (DialogueLine line in lines)
        {
            lineQueue.Enqueue(line);
        }

        isDialogueActive = true;

        if (dialogBoxAnimator != null)
        {
            dialogBoxAnimator.Play("show");
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        if (lineQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = lineQueue.Dequeue();

        if (characterNameText != null)
        {
            characterNameText.text = currentLine.characterName;
        }

        ApplyTextSideLayout(currentLine.side);
        ShowActivePortrait(currentLine);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeSentence(currentLine.line));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (dialogAreaText != null)
        {
            dialogAreaText.text = string.Empty;
            foreach (char letter in sentence)
            {
                dialogAreaText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        isTyping = false;
    }

    private void ApplyTextSideLayout(SpeakerSide side)
    {
        if (side == SpeakerSide.Right)
        {
            if (characterNameRect != null) characterNameRect.anchoredPosition = namePosForRightSpeaker;
            if (dialogAreaRect != null) dialogAreaRect.anchoredPosition = dialogPosForRightSpeaker;
        }
        else
        {
            if (characterNameRect != null) characterNameRect.anchoredPosition = namePosForLeftSpeaker;
            if (dialogAreaRect != null) dialogAreaRect.anchoredPosition = dialogPosForLeftSpeaker;
        }
    }

    private void ShowActivePortrait(DialogueLine line)
    {
        HideAllPortraits();

        GameObject targetPortrait = line.portraitOverride;

        if (targetPortrait == null)
        {
            string lowerName = (line.characterName ?? string.Empty).Trim().ToLowerInvariant();

            if (lowerName.Contains("knight"))
            {
                targetPortrait = knightPortrait;
            }
            else if (lowerName.Contains("vendor"))
            {
                targetPortrait = vendorPortrait;
            }
            else if (lowerName.Contains("babata"))
            {
                targetPortrait = babataPortrait;
            }
            else if (lowerName.Contains("narcis"))
            {
                targetPortrait = narcisShadowPortrait;
            }
        }

        if (targetPortrait != null)
        {
            targetPortrait.SetActive(true);
        }
    }

    private void HideAllPortraits()
    {
        foreach (GameObject portrait in allPortraitObjects)
        {
            if (portrait != null)
            {
                portrait.SetActive(false);
            }
        }
    }

    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;
        isDialogueActive = false;
        lastEndedFrame = Time.frameCount;
        HideAllPortraits();

        if (dialogBoxAnimator != null)
        {
            dialogBoxAnimator.Play("hide");
        }
    }
}