using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [Header("Track Configuration")]
    [SerializeField] private AudioClip sceneBGM;
    [SerializeField] private bool loop = true;
    [SerializeField] private float fadeDuration = 1.0f;

    private void Start()
    {
        PlayTrack();
    }

    public void PlayTrack()
    {
        if (sceneBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(sceneBGM, loop, fadeDuration);
        }
    }
}