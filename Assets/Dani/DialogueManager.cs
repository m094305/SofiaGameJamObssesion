using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    [Header("Settings")]
    public bool isDialogueActive = false;
    public float typingSpeed = 0.03f;
    public int maxLinesPerPage = 2;
    public KeyCode advanceKey = KeyCode.A;
    public Animator animator;

    private Queue<DialogueLine> lines;
    private Queue<string> currentLinePages;
    private bool isTyping = false;
    private bool skipTypingRequested = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        lines = new Queue<DialogueLine>();
        currentLinePages = new Queue<string>();
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetKeyDown(advanceKey))
        {
            if (isTyping)
            {
                // Finish typing the current 2-line page immediately
                skipTypingRequested = true;
            }
            else
            {
                // Move to the next 2-line page or next DialogueLine
                DisplayNextPageOrLine();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;

        if (animator != null)
        {
            animator.Play("show");
        }

        lines.Clear();
        currentLinePages.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextPageOrLine();
    }

    public void DisplayNextPageOrLine()
    {
        // If the current character's line still has 2-line chunks left, show the next chunk
        if (currentLinePages.Count > 0)
        {
            StopAllCoroutines();
            StartCoroutine(TypePage(currentLinePages.Dequeue()));
            return;
        }

        // Otherwise, dequeue the next DialogueLine from the queue
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        if (characterIcon != null)
        {
            characterIcon.sprite = currentLine.character.icon;
        }

        if (characterName != null)
        {
            characterName.text = currentLine.character.name;
        }

        BuildPagesForLine(currentLine.line);

        StopAllCoroutines();
        if (currentLinePages.Count > 0)
        {
            StartCoroutine(TypePage(currentLinePages.Dequeue()));
        }
        else
        {
            DisplayNextPageOrLine();
        }
    }

    private void BuildPagesForLine(string fullLine)
    {
        currentLinePages.Clear();

        if (string.IsNullOrWhiteSpace(fullLine) || dialogueArea == null)
        {
            currentLinePages.Enqueue(fullLine ?? "");
            return;
        }

        string originalText = dialogueArea.text;
        int originalMaxChars = dialogueArea.maxVisibleCharacters;

        // Ensure all characters are visible while measuring line wrapping
        dialogueArea.maxVisibleCharacters = int.MaxValue;

        string[] words = fullLine.Split(' ');
        string currentPage = "";

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            string testPage = string.IsNullOrEmpty(currentPage) ? word : currentPage + " " + word;

            dialogueArea.text = testPage;
            dialogueArea.ForceMeshUpdate();

            if (dialogueArea.textInfo.lineCount > maxLinesPerPage && !string.IsNullOrEmpty(currentPage))
            {
                currentLinePages.Enqueue(currentPage);
                currentPage = word;
            }
            else
            {
                currentPage = testPage;
            }
        }

        if (!string.IsNullOrEmpty(currentPage))
        {
            currentLinePages.Enqueue(currentPage);
        }

        dialogueArea.text = originalText;
        dialogueArea.maxVisibleCharacters = originalMaxChars;
    }

    IEnumerator TypePage(string pageText)
    {
        isTyping = true;
        skipTypingRequested = false;

        dialogueArea.text = pageText;
        dialogueArea.ForceMeshUpdate();

        int totalChars = dialogueArea.textInfo.characterCount;
        dialogueArea.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalChars; i++)
        {
            if (skipTypingRequested)
            {
                dialogueArea.maxVisibleCharacters = totalChars;
                break;
            }

            dialogueArea.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        currentLinePages.Clear();

        if (animator != null)
        {
            animator.Play("hide");
        }
    }
}