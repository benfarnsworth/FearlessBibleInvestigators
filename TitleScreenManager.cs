using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    [Header("Scene Setup")]
    [Tooltip("The name of the first gameplay scene to load when starting a New Game.")]
    [SerializeField] private string firstSceneName = "SampleScene";

    [Header("UI References")]
    [SerializeField] private Button loadGameButton;
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    private void Start()
    {
        // Ensure time scale is normal on the title screen
        Time.timeScale = 1f;

        CheckForExistingSaves();
    }

    /// <summary>
    /// Starts a fresh game session and loads the primary gameplay scene.
    /// </summary>
    public void NewGame()
    {
        PlaySound();

        Time.timeScale = 1f;

        // Ensure active save session is cleared before entering a new game
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearCurrentSession();
        }

        SceneManager.LoadScene(firstSceneName);
    }

    /// <summary>
    /// Opens the Save/Load menu overlay so the player can pick a slot.
    /// </summary>
    public void OpenLoadMenu()
    {
        PlaySound();

        if (saveLoadMenuUI != null)
        {
            saveLoadMenuUI.OpenMenu();
        }
        else
        {
            Debug.LogWarning("SaveLoadMenuUI reference is missing on TitleScreenManager!");
        }
    }

    /// <summary>
    /// Checks if any save files exist on disk and disables the Load button if none are found.
    /// </summary>
    public void CheckForExistingSaves()
    {
        if (loadGameButton == null) return;

        bool hasAnySave = false;

        // Check common slot indices (0, 1, 2)
        for (int i = 0; i < 3; i++)
        {
            if (SaveSystem.HasSaveFile(i))
            {
                hasAnySave = true;
                break;
            }
        }

        loadGameButton.interactable = hasAnySave;
    }

    /// <summary>
    /// Quits the application or stops playing in the Unity Editor.
    /// </summary>
    public void QuitGame()
    {
        PlaySound();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void PlaySound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}