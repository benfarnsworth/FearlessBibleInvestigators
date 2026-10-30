
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinWander : MonoBehaviour
{
    private enum PenguinState
    {
        Idle,
        Walking,
        Grooming,
        Stumbling
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 0.8f;
    [SerializeField] private float minWalkDuration = 1.5f;
    [SerializeField] private float maxWalkDuration = 3.5f;

    [Header("Idle Behavior")]
    [SerializeField] private float minIdleDuration = 2f;
    [SerializeField] private float maxIdleDuration = 5f;

    [Range(0f, 1f)]
    [SerializeField] private float groomingChance = 0.3f;

    [Header("Walking Behavior")]
    [Range(0f, 1f)]
    [SerializeField] private float stumbleChance = 0.18f;

    [Header("Animation State Names")]
    [SerializeField] private string idleAnimation = "Idle";
    [SerializeField] private string walkAnimation = "Walk";
    [SerializeField] private string groomAnimation = "Groom";
    [SerializeField] private string stumbleAnimation = "Stumble";

    private Rigidbody2D rb;
    private Transform visual;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private PenguinState currentState;
    private Vector2 walkDirection;
    private float stateTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the Visual child automatically.
        visual = transform.Find("Visual");

        if (visual == null)
        {
            Debug.LogError(
                "PenguinWander: Could not find a direct child named 'Visual'.",
                this
            );

            enabled = false;
            return;
        }

        animator = visual.GetComponent<Animator>();
        spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            Debug.LogError(
                "PenguinWander: Visual needs an Animator component.",
                this
            );

            enabled = false;
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "PenguinWander: Visual needs a SpriteRenderer component.",
                this
            );

            enabled = false;
            return;
        }

        // Suitable defaults for a top-down wandering animal.
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case PenguinState.Idle:
                UpdateIdle();
                break;

            case PenguinState.Walking:
                UpdateWalking();
                break;

            case PenguinState.Grooming:
            case PenguinState.Stumbling:
                UpdateOneShotAnimation();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState != PenguinState.Walking)
            return;

        Vector2 movement =
            walkDirection * walkSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);
    }

    private void UpdateIdle()
    {
        if (stateTimer > 0f)
            return;

        // Roll once per idle period: groom or start walking.
        if (Random.value < groomingChance)
        {
            EnterGrooming();
        }
        else
        {
            StartWalking();
        }
    }

    private void UpdateWalking()
    {
        if (stateTimer > 0f)
            return;

        // Occasionally stumble at the end of a walk.
        if (Random.value < stumbleChance)
        {
            EnterStumbling();
        }
        else
        {
            EnterIdle();
        }
    }

    private void UpdateOneShotAnimation()
    {
        AnimatorStateInfo stateInfo =
            animator.GetCurrentAnimatorStateInfo(0);

        // Wait until Unity has entered the intended animation state.
        if (stateInfo.IsName(GetCurrentAnimationName()) &&
            stateInfo.normalizedTime >= 1f &&
            !animator.IsInTransition(0))
        {
            EnterIdle();
        }
    }

    private string GetCurrentAnimationName()
    {
        return currentState switch
        {
            PenguinState.Grooming => groomAnimation,
            PenguinState.Stumbling => stumbleAnimation,
            _ => idleAnimation
        };
    }

    private void EnterIdle()
    {
        currentState = PenguinState.Idle;
        stateTimer = Random.Range(minIdleDuration, maxIdleDuration);

        StopMovement();
        animator.Play(idleAnimation, 0, 0f);
    }

    private void StartWalking()
    {
        currentState = PenguinState.Walking;
        stateTimer = Random.Range(minWalkDuration, maxWalkDuration);

        // Wander left or right.
        walkDirection = Random.value < 0.5f
            ? Vector2.left
            : Vector2.right;

        spriteRenderer.flipX = walkDirection.x < 0f;

        animator.Play(walkAnimation, 0, 0f);
    }

    private void EnterGrooming()
    {
        currentState = PenguinState.Grooming;

        StopMovement();
        animator.Play(groomAnimation, 0, 0f);
    }

    private void EnterStumbling()
    {
        currentState = PenguinState.Stumbling;

        StopMovement();
        animator.Play(stumbleAnimation, 0, 0f);
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != PenguinState.Walking)
            return;

        // Turn around when the penguin bumps into something.
        walkDirection = -walkDirection;
        spriteRenderer.flipX = walkDirection.x < 0f;
    }
}
