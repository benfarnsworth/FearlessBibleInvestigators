using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip transitionSound;

    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TransitionToScene(string targetSceneName, LocationID destinationID)
    {
        if (audioSource != null && transitionSound != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        StartCoroutine(PerformTransition(targetSceneName, destinationID));
    }

    private IEnumerator PerformTransition(string targetSceneName, LocationID destinationID)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // 1. Lock controls immediately so player can't walk during fade
        SetPlayerControls(player, false);

        // 2. Fade screen to black
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut();
        }

        // 3. Load the scene asynchronously in background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. Find the new scene's player and teleport to SpawnPoint using spawnPointID
        player = GameObject.FindGameObjectWithTag("Player");

        if (destinationID != null && player != null)
        {
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var point in spawnPoints)
            {
                if (point.spawnPointID == destinationID)
                {
                    // Calculate final position with offset
                    Vector3 targetSpawnPos = point.GetSpawnPosition();

                    // Reposition player
                    player.transform.position = targetSpawnPos;
                    if (player.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        rb.position = targetSpawnPos;
                        rb.linearVelocity = Vector2.zero;
                    }

                    // Force physics engine to immediately sync position before unpausing
                    Physics2D.SyncTransforms();

                    // Set facing direction
                    if (player.TryGetComponent<PlayerController>(out var controller))
                    {
                        controller.SetFacingDirection(point.spawnFacingDirection);
                    }

                    break;
                }
            }
        }

        // 5. Snap custom camera directly to player position while behind black screen
        SnapCamera();

        // 6. Fade screen back in
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        // 7. Restore player controls once fully visible
        SetPlayerControls(player, true);
    }

    private void SnapCamera()
    {
        DialogueCamera dialogueCam = FindFirstObjectByType<DialogueCamera>();
        if (dialogueCam != null)
        {
            dialogueCam.SnapToPlayer();
        }
    }

    private void SetPlayerControls(GameObject player, bool enable)
    {
        if (player == null) return;

        if (player.TryGetComponent<PlayerController>(out var controller))
        {
            // Disabling this component triggers OnDisable() automatically!
            controller.enabled = enable; 
        }
    }
}