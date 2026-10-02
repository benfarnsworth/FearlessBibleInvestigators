using UnityEngine;

public class IntroCutsceneTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Unique flag name for StoryStateManager.")]
    [SerializeField] private string cutsceneFlagName = "IntroCutscene_FBIVillage";

    private void Start()
    {
        // Debug log to trace what's happening in Console
        bool alreadyPlayed = StoryStateManager.HasCutsceneBeenPlayed(cutsceneFlagName);
        Debug.Log($"[IntroTrigger] Checking '{cutsceneFlagName}' | Already Played? {alreadyPlayed}");

        if (!alreadyPlayed)
        {
            Debug.Log($"[IntroTrigger] Attempting to play cutscene: {gameObject.name}");
            
            // Pass the GameObject's actual name in the Hierarchy so GameObject.Find can locate it!
            CutsceneManager.PlayCutsceneOnce(gameObject.name);
        }
        else
        {
            Debug.Log($"[IntroTrigger] Skipping intro cutscene because it was already played.");
            gameObject.SetActive(false);
        }
    }
}