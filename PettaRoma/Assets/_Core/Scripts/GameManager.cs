using UnityEngine;

/// <summary>
/// Singleton that persists across scenes.
/// Tracks coins, checkpoints, and global game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private int coins;
    private int lastCheckpointId = -1;
    private Vector3 lastCheckpointPos;
    private Vector3 defaultSpawn = new Vector3(35f, 0f, -2f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        lastCheckpointPos = defaultSpawn;
    }

    // ── Coins ──
    public void AddCoins(int amount)
    {
        coins += amount;
        EventBus.CoinChanged(coins);
        if (showDebugInfo) Debug.Log($"[GameManager] Coins: {coins}");
    }

    public int GetCoins() => coins;

    // ── Checkpoints ──
    public void SetCheckpoint(int id, Vector3 pos)
    {
        if (id > lastCheckpointId)
        {
            lastCheckpointId = id;
            lastCheckpointPos = pos;
            if (showDebugInfo) Debug.Log($"[GameManager] Checkpoint {id} set at {pos}");
        }
    }

    public Vector3 GetSpawnPosition() => lastCheckpointPos;
    public int GetLastCheckpointId() => lastCheckpointId;

    // ── Reset ──
    public void ResetProgress()
    {
        coins = 0;
        lastCheckpointId = -1;
        lastCheckpointPos = defaultSpawn;
        EventBus.CoinChanged(coins);
    }
}
