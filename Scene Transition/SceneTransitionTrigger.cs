using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("The exact name of the scene as typed in Build Settings.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("The target SpawnPoint ID in the destination scene.")]
    [SerializeField] private LocationID destinationID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Save state before transitioning
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveCurrentState();
            }

            // Hand off control to the manager for smooth fading & spawn positioning
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(targetSceneName, destinationID);
            }
            else
            {
                Debug.LogError($"[SceneTransitionTrigger] No SceneTransitionManager found in scene!");
            }
        }
    }
}