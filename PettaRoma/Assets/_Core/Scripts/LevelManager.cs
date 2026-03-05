using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene loading by level ID.
/// Supports future level expansion.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [System.Serializable]
    public class LevelEntry
    {
        public int id;
        public string sceneName;
    }

    [SerializeField] private LevelEntry[] levels = new LevelEntry[]
    {
        new LevelEntry { id = 1, sceneName = "Level1_Rome" }
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void LoadLevel(int id)
    {
        foreach (var level in levels)
        {
            if (level.id == id)
            {
                SceneManager.LoadScene(level.sceneName);
                return;
            }
        }
        Debug.LogWarning($"[LevelManager] Level ID {id} not found!");
    }

    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
