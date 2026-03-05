using UnityEngine;

/// <summary>
/// Base class for objects the player can interact with (press E/F).
/// Subclass and override OnInteract().
/// Attach to a GameObject with a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string promptText = "Press E to interact";
    [SerializeField] protected float interactRadius = 3f;

    private bool playerInRange;

    protected virtual void Start()
    {
        // Ensure collider is trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        EventBus.InteractPromptShow(promptText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        EventBus.InteractPromptHide();
    }

    void Update()
    {
        if (playerInRange && InputManager.Instance != null && InputManager.Instance.ConsumeInteract())
        {
            OnInteract();
        }
    }

    /// <summary>Override this to define interaction behavior.</summary>
    protected abstract void OnInteract();
}
