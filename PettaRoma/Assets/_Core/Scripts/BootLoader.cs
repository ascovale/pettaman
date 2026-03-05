using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal boot loader. Placed in Boot scene (build index 0).
/// Initializes managers then loads the first level.
/// </summary>
public class BootLoader : MonoBehaviour
{
    [SerializeField] private string firstScene = "Level1_Rome";

    void Start()
    {
        // GameManager and LevelManager should be on this same GO
        // or on a "Managers" prefab in Boot scene.
        // DontDestroyOnLoad keeps them alive.
        SceneManager.LoadScene(firstScene);
    }
}
