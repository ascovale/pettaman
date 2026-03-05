using UnityEngine;

/// <summary>
/// Invisible trigger. When the player enters, stores this as the new spawn point.
/// Attach to a GameObject with a BoxCollider (isTrigger = true).
/// The RomeBlockoutBuilder places these automatically.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Unique id — higher id = further in the level.")]
    [SerializeField] private int checkpointId;

    [Tooltip("Visual feedback: briefly scale the flag/object on activation.")]
    [SerializeField] private Transform visual;

    private bool _activated;

    public int Id => checkpointId;

    void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;

        // Push spawn data to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.SetCheckpoint(checkpointId, transform.position + Vector3.up * 0.5f);

        EventBus.CheckpointReached(checkpointId);

        // Simple visual pulse (scale up then back)
        if (visual != null)
            StartCoroutine(Pulse(visual));
    }

    private System.Collections.IEnumerator Pulse(Transform t)
    {
        Vector3 orig = t.localScale;
        Vector3 big = orig * 1.4f;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            t.localScale = Vector3.Lerp(orig, big, Mathf.PingPong(elapsed / duration * 2f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = orig;
    }
}
