using UnityEngine;
using UnityEngine.Playables;

public class AutoStartCutscene : MonoBehaviour
{
    private PlayableDirector director;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    private void Start()
    {
        // 1. Freeze the player so PlayerController doesn't fight the Spline
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.FreezePlayer();

        // 2. Turn off the regular gameplay camera, turn on Cinemachine
        if (CutsceneManager.Instance != null)
        {
            if (CutsceneManager.Instance.dialogueCam != null) 
                CutsceneManager.Instance.dialogueCam.enabled = false;

            if (CutsceneManager.Instance.cinemachineBrain != null) 
                CutsceneManager.Instance.cinemachineBrain.enabled = true;
        }

        // 3. Play the timeline if it hasn't already started
        if (director != null && director.state != PlayState.Playing)
        {
            director.Play();
        }
    }
}