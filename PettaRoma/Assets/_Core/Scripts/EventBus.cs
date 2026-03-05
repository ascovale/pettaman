using System;

/// <summary>
/// Central static event hub for decoupled communication.
/// All game systems fire events here; UI and other listeners subscribe.
/// </summary>
public static class EventBus
{
    // ── Player ──
    public static event Action<int> OnCoinChanged;
    public static event Action<float, float> OnHealthChanged; // current, max
    public static event Action OnPlayerDied;
    public static event Action OnPlayerRespawned;

    // ── Interaction ──
    public static event Action<string> OnInteractPromptShow;
    public static event Action OnInteractPromptHide;

    // ── Dialogue ──
    public static event Action<string> OnDialogueShow;
    public static event Action OnDialogueHide;

    // ── Enemy ──
    public static event Action<float, float> OnPlayerHealthChanged; // current, max
    public static event Action OnEnemyDied;

    // ── Game State ──
    public static event Action<int> OnCheckpointReached;

    // ── Fire helpers ──
    public static void CoinChanged(int total) => OnCoinChanged?.Invoke(total);
    public static void HealthChanged(float cur, float max) => OnHealthChanged?.Invoke(cur, max);
    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void PlayerRespawned() => OnPlayerRespawned?.Invoke();
    public static void InteractPromptShow(string text) => OnInteractPromptShow?.Invoke(text);
    public static void InteractPromptHide() => OnInteractPromptHide?.Invoke();
    public static void DialogueShow(string text) => OnDialogueShow?.Invoke(text);
    public static void DialogueHide() => OnDialogueHide?.Invoke();
    public static void EnemyDied() => OnEnemyDied?.Invoke();
    public static void PlayerHealthChanged(float cur, float max) => OnPlayerHealthChanged?.Invoke(cur, max);
    public static void CheckpointReached(int id) => OnCheckpointReached?.Invoke(id);
}
