using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Full HUD — coin counter, health bar, interact prompt, dialogue panel.
/// Subscribes to EventBus for all updates.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Coin Counter")]
    [SerializeField] private Text coinText;
    [SerializeField] private string coinFormat = "× {0}";

    [Header("Health Bar")]
    [SerializeField] private Image healthFill;          // Image (Filled)
    [SerializeField] private GameObject healthBarRoot;   // enable/disable

    [Header("Interact Prompt")]
    [SerializeField] private Text interactText;
    [SerializeField] private GameObject interactPromptRoot;

    [Header("Dialogue Panel")]
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject dialoguePanelRoot;

    [Header("Notifications")]
    [SerializeField] private Text notificationText;
    [SerializeField] private GameObject notificationRoot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        EventBus.OnCoinChanged += UpdateCoinDisplay;
        EventBus.OnCheckpointReached += ShowCheckpointNotice;
        EventBus.OnPlayerDied += ShowDeathNotice;
        EventBus.OnInteractPromptShow += ShowInteractPrompt;
        EventBus.OnInteractPromptHide += HideInteractPrompt;
        EventBus.OnDialogueShow += ShowDialogue;
        EventBus.OnDialogueHide += HideDialogue;
        EventBus.OnPlayerHealthChanged += UpdateHealth;
    }

    void OnDisable()
    {
        EventBus.OnCoinChanged -= UpdateCoinDisplay;
        EventBus.OnCheckpointReached -= ShowCheckpointNotice;
        EventBus.OnPlayerDied -= ShowDeathNotice;
        EventBus.OnInteractPromptShow -= ShowInteractPrompt;
        EventBus.OnInteractPromptHide -= HideInteractPrompt;
        EventBus.OnDialogueShow -= ShowDialogue;
        EventBus.OnDialogueHide -= HideDialogue;
        EventBus.OnPlayerHealthChanged -= UpdateHealth;
    }

    void Start()
    {
        UpdateCoinDisplay(0);
        if (interactPromptRoot != null) interactPromptRoot.SetActive(false);
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(false);
        if (notificationRoot != null) notificationRoot.SetActive(false);
        // Health bar hidden until player takes damage
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
    }

    // ─── Coins ────────────────────────────────────────────

    void UpdateCoinDisplay(int total)
    {
        if (coinText != null)
            coinText.text = string.Format(coinFormat, total);
    }

    // ─── Health Bar ───────────────────────────────────────

    void UpdateHealth(float current, float max)
    {
        if (healthBarRoot != null)
            healthBarRoot.SetActive(current < max); // show only when hurt
        if (healthFill != null && max > 0)
            healthFill.fillAmount = current / max;
    }

    // ─── Interact Prompt ──────────────────────────────────

    void ShowInteractPrompt(string text)
    {
        if (interactPromptRoot != null) interactPromptRoot.SetActive(true);
        if (interactText != null) interactText.text = text;
    }

    void HideInteractPrompt()
    {
        if (interactPromptRoot != null) interactPromptRoot.SetActive(false);
    }

    // ─── Dialogue Panel ──────────────────────────────────

    void ShowDialogue(string text)
    {
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(true);
        if (dialogueText != null) dialogueText.text = text;
        // Hide interact prompt while dialogue is showing
        HideInteractPrompt();
    }

    void HideDialogue()
    {
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(false);
    }

    // ─── Checkpoint ───────────────────────────────────────

    void ShowCheckpointNotice(int id)
    {
        ShowNotification("Checkpoint!");
    }

    // ─── Death ────────────────────────────────────────────

    void ShowDeathNotice()
    {
        ShowNotification("You died!");
    }

    // ─── Notification flash ──────────────────────────────

    void ShowNotification(string msg)
    {
        if (notificationRoot == null || notificationText == null) return;
        notificationText.text = msg;
        notificationRoot.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideNotificationAfter(2f));
    }

    IEnumerator HideNotificationAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationRoot != null) notificationRoot.SetActive(false);
    }
}
