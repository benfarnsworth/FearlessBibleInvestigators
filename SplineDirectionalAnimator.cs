using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineAnimate))]
public class SplineDirectionalAnimator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How quickly speed drops to 0 when movement stops.")]
    [SerializeField] private float speedDamping = 10f;
    [Tooltip("Minimum movement magnitude to consider as walking.")]
    [SerializeField] private float speedThreshold = 0.05f;

    [Header("Completion Options")]
    [Tooltip("Automatically re-enable PlayerController when the spline path finishes?")]
    [SerializeField] private bool restoreControlOnComplete = true;

    private Animator animator;
    private PlayerController playerController;
    private SplineAnimate splineAnimate;
    private Vector3 lastPos;
    private float currentSpeedFloat = 0f;
    private bool isFinished = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        splineAnimate = GetComponent<SplineAnimate>();
    }

    private void OnEnable()
    {
        lastPos = transform.position;
        currentSpeedFloat = 0f;
        isFinished = false;

        // Take away player control during cutscene
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void OnDisable()
    {
        RestorePlayerState();
    }

    private void Update()
    {
        if (isFinished || animator == null) return;

        if (!animator.enabled) animator.enabled = true;

        Vector3 currentPos = transform.position;
        Vector3 moveDelta = currentPos - lastPos;
        lastPos = currentPos;

        float rawDistance = moveDelta.magnitude;
        float rawSpeed = Time.deltaTime > 0 ? (rawDistance / Time.deltaTime) : 0f;
        float targetSpeed = rawSpeed > speedThreshold ? 1.0f : 0.0f;

        currentSpeedFloat = Mathf.MoveTowards(currentSpeedFloat, targetSpeed, speedDamping * Time.deltaTime);

        if (rawSpeed > speedThreshold)
        {
            Vector2 dir = moveDelta.normalized;
            animator.SetFloat("MoveX", dir.x);
            animator.SetFloat("MoveY", dir.y);
        }

        animator.SetFloat("Speed", currentSpeedFloat);

        // Check if SplineAnimate has completed its path
        if (splineAnimate != null && (!splineAnimate.IsPlaying || splineAnimate.NormalizedTime >= 1.0f))
        {
            // Ensure she has actually stopped moving before handing control back
            if (rawSpeed <= speedThreshold)
            {
                OnCutsceneComplete();
            }
        }
    }

    private void OnCutsceneComplete()
    {
        if (isFinished) return;
        isFinished = true;

        // Reset speed parameter so she returns to Idle stance
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }

        RestorePlayerState();
        RestoreCamera();

        // Self-disable this animator helper component since the cutscene is over
        enabled = false;
    }

    private void RestorePlayerState()
    {
        if (restoreControlOnComplete && playerController != null)
        {
            playerController.enabled = true;
            playerController.UnfreezePlayer();
        }
    }

    private void RestoreCamera()
    {
        if (CutsceneManager.Instance != null)
        {
            // Swap back to gameplay camera setup
            if (CutsceneManager.Instance.dialogueCam != null) 
                CutsceneManager.Instance.dialogueCam.enabled = true;

            if (CutsceneManager.Instance.cinemachineBrain != null) 
                CutsceneManager.Instance.cinemachineBrain.enabled = false;
        }
    }
}