using UnityEngine;

public class GoalJuice : MonoBehaviour
{
    [Header("Goal SFX")]
    [Tooltip("Happy sound when item enters or points are scored.")]
    [SerializeField] private AudioClip goalSFX;

    [Tooltip("Sad sound when item leaves the pen or score decreases.")]
    [SerializeField] private AudioClip scoreLostSFX;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    public void TriggerGoalEffects(Vector3 spawnPosition, bool isPositive = true)
    {
        // 1. Screen Shake (only shake on positive goals)
        if (isPositive && CameraJuice.Instance != null)
        {
            CameraJuice.Instance.TriggerScreenShake(shakeMagnitude, shakeDuration);
        }

        // 2. Play Sound Effect via dedicated Stinger channel
        AudioClip clipToPlay = isPositive ? goalSFX : scoreLostSFX;

        if (clipToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStinger(clipToPlay);
        }
    }
}