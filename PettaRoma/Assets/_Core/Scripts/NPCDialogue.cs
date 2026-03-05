using UnityEngine;

/// <summary>
/// Simple NPC that shows dialogue lines when the player interacts.
/// Cycles through lines on repeated interaction.
/// </summary>
public class NPCDialogue : Interactable
{
    [Header("Dialogue")]
    [SerializeField] private string npcName = "Romano";
    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "Ciao! Welcome to Roma!",
        "Have you tried Petta's pizza? Best in Prati!",
        "Watch out for the angry pizzas..."
    };

    [Header("Visual")]
    [SerializeField] private Transform visualRoot; // rotates to face player

    private int currentLine = 0;
    private bool dialogueActive = false;

    protected override void Start()
    {
        base.Start();
        promptText = $"Press E to talk to {npcName}";
    }

    protected override void OnInteract()
    {
        if (!dialogueActive)
        {
            // Start dialogue
            dialogueActive = true;
            currentLine = 0;
            ShowLine();
        }
        else
        {
            // Next line
            currentLine++;
            if (currentLine >= dialogueLines.Length)
            {
                // End dialogue
                dialogueActive = false;
                EventBus.InteractPromptShow(promptText);
                EventBus.DialogueHide();
                return;
            }
            ShowLine();
        }
    }

    void ShowLine()
    {
        string line = $"<b>{npcName}:</b> {dialogueLines[currentLine]}";
        EventBus.DialogueShow(line);

        // Face the player
        if (visualRoot != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 dir = player.transform.position - visualRoot.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    visualRoot.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        dialogueActive = false;
        EventBus.DialogueHide();
        EventBus.InteractPromptHide();
    }
}
