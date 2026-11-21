using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpeakerSide
{
    Left,
    Right
}

[Serializable]
public class DialogueLine
{
    public string characterName;

    [TextArea(2, 5)]
    public string line;

    public SpeakerSide side = SpeakerSide.Right;

    [Tooltip("Optional: assign a specific portrait GameObject (e.g., an ending Narcis image). Leave empty to auto-pick from characterName.")]
    public GameObject portraitOverride;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Lines")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = false;
    private bool hasTriggered = false;

    public void TriggerDialogue()
    {
        if (triggerOnce && hasTriggered) return;
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueManager.Instance is missing in the scene.");
            return;
        }

        hasTriggered = true;
        DialogueManager.Instance.StartDialogue(dialogueLines);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }
}