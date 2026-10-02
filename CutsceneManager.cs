using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine; // Unity 6 / Cinemachine 3.x
using Yarn.Unity;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    [Header("Camera Control")]
    [SerializeField] public DialogueCamera dialogueCam;
    [SerializeField] public CinemachineBrain cinemachineBrain;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [YarnCommand("play_cutscene")]
    public static void PlayCutscene(string cutsceneObjectName)
    {
        // Default behavior: allow re-playing unless specified otherwise, 
        // or check StoryStateManager if you want strict one-time usage.
        PlayCutsceneInternal(cutsceneObjectName, false);
    }

    // Overload if you want to pass a boolean from Yarn Spinner, e.g. <<play_once "MyCutscene">>
    [YarnCommand("play_cutscene_once")]
    public static void PlayCutsceneOnce(string cutsceneObjectName)
    {
        if (StoryStateManager.HasCutsceneBeenPlayed(cutsceneObjectName))
        {
            Debug.Log($"[CutsceneManager] Skipping cutscene '{cutsceneObjectName}' because it was already played.");
            return;
        }

        PlayCutsceneInternal(cutsceneObjectName, true);
    }

    private static void PlayCutsceneInternal(string cutsceneObjectName, bool markAsPlayed)
    {
        if (Instance == null) return;

        GameObject cutsceneObj = GameObject.Find(cutsceneObjectName);
        if (cutsceneObj != null && cutsceneObj.TryGetComponent<PlayableDirector>(out var director))
        {
            // 1. Freeze Player and grab references
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) 
            {
                player.FreezePlayer();

                // Start the SplineAnimate component if it lives on the player!
                if (player.TryGetComponent<UnityEngine.Splines.SplineAnimate>(out var splineAnim))
                {
                    splineAnim.Restart(true); // Resets to 0% and plays!
                }
            }

            // 2. Camera Hand-off
            if (Instance.dialogueCam != null) Instance.dialogueCam.enabled = false;
            if (Instance.cinemachineBrain != null) Instance.cinemachineBrain.enabled = true;

            // 3. Mark as played in StoryStateManager
            if (markAsPlayed)
            {
                StoryStateManager.MarkCutsceneAsPlayed(cutsceneObjectName);
            }

            // 4. Roll Film!
            director.Play();
        }
        else
        {
            Debug.LogWarning($"[CutsceneManager] Could not find cutscene GameObject '{cutsceneObjectName}' with a PlayableDirector!");
        }
    }

    [YarnCommand("stop_cutscene")]
    public static void StopCutscene()
    {
        if (Instance == null) return;

        // 1. Give control back to the player
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.UnfreezePlayer();

        // 2. Snap camera back to player
        if (Instance.cinemachineBrain != null) Instance.cinemachineBrain.enabled = false;
        if (Instance.dialogueCam != null)
        {
            Instance.dialogueCam.enabled = true;
            Instance.dialogueCam.SnapToPlayer();
        }
    }
}