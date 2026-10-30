
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SquirrelWander : MonoBehaviour
{
    private enum SquirrelState
    {
        Idle,
        Walking,
        Digging,
        EatingNut
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 0.9f;
    [SerializeField] private float minWalkDuration = 1.5f;
    [SerializeField] private float maxWalkDuration = 3.5f;

    [Header("Idle Behavior")]
    [SerializeField] private float minIdleDuration = 1.5f;
    [SerializeField] private float maxIdleDuration = 3.5f;

    [Header("Behavior Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float diggingChance = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float eatingNutChance = 0.25f;

    [Header("Digging")]
    [SerializeField] private float minDigDuration = 0.4f;
    [SerializeField] private float maxDigDuration = 1.2f;

    [Header("Eating Nut")]
    [SerializeField] private float minEatingDuration = 1.5f;
    [SerializeField] private float maxEatingDuration = 3f;

    [Header("Animation State Names")]
    [SerializeField] private string idleAAnimation = "IdleA";
    [SerializeField] private string idleBAnimation = "IdleB";
    [SerializeField] private string walkAnimation = "Walk";
    [SerializeField] private string digAnimation = "Dig";
    [SerializeField] private string eatNutAnimation = "EatNut";

    private Rigidbody2D rb;
    private Transform visual;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Cached animation hashes.
    private int idleAHash;
    private int idleBHash;
    private int walkHash;
    private int digHash;
    private int eatNutHash;

    private SquirrelState currentState;
    private Vector2 walkDirection;
    private float stateTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the child named Visual, even if it is nested.
        visual = FindVisualChild(transform);

        if (visual == null)
        {
            Debug.LogError(
                "SquirrelWander: Could not find a child named Visual.",
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
                "SquirrelWander: Visual needs an Animator component.",
                this
            );
            enabled = false;
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "SquirrelWander: Visual needs a SpriteRenderer component.",
                this
            );
            enabled = false;
            return;
        }

        // Cache hashes once instead of looking up strings repeatedly.
        idleAHash = Animator.StringToHash(idleAAnimation);
        idleBHash = Animator.StringToHash(idleBAnimation);
        walkHash = Animator.StringToHash(walkAnimation);
        digHash = Animator.StringToHash(digAnimation);
        eatNutHash = Animator.StringToHash(eatNutAnimation);

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
            case SquirrelState.Idle:
                UpdateIdle();
                break;

            case SquirrelState.Walking:
                UpdateWalking();
                break;

            case SquirrelState.Digging:
                UpdateDigging();
                break;

            case SquirrelState.EatingNut:
                UpdateEatingNut();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState != SquirrelState.Walking)
            return;

        Vector2 movement =
            walkDirection * walkSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);
    }

    private void UpdateIdle()
    {
        if (stateTimer > 0f)
            return;

        float roll = Random.value;

        if (roll < diggingChance)
        {
            EnterDigging();
        }
        else if (roll < diggingChance + eatingNutChance)
        {
            EnterEatingNut();
        }
        else
        {
            StartWalking();
        }
    }

    private void UpdateWalking()
    {
        if (stateTimer <= 0f)
        {
            EnterIdle();
        }
    }

    private void UpdateDigging()
    {
        // Wait until the Dig animation has actually finished.
        AnimatorStateInfo info =
            animator.GetCurrentAnimatorStateInfo(0);

        if (info.shortNameHash == digHash &&
            info.normalizedTime >= 1f &&
            !animator.IsInTransition(0))
        {
            EnterIdle();
        }
        else if (stateTimer <= 0f)
        {
            // Fallback if the animation state fails to finish normally.
            EnterIdle();
        }
    }

    private void UpdateEatingNut()
    {
        if (stateTimer <= 0f)
        {
            EnterIdle();
        }
    }

    private void EnterIdle()
    {
        currentState = SquirrelState.Idle;
        stateTimer = Random.Range(minIdleDuration, maxIdleDuration);

        StopMovement();
        animator.speed = 1f;

        // Randomly choose one of the two idle animations.
        int idleHash = Random.value < 0.5f
            ? idleAHash
            : idleBHash;

        animator.Play(idleHash, 0, 0f);
    }

    private void StartWalking()
    {
        currentState = SquirrelState.Walking;
        stateTimer = Random.Range(minWalkDuration, maxWalkDuration);

        // Choose a random direction, including diagonals.
        walkDirection = Random.insideUnitCircle.normalized;

        if (walkDirection.sqrMagnitude < 0.01f)
            walkDirection = Vector2.right;

        spriteRenderer.flipX = walkDirection.x < 0f;

        StopMovement();
        animator.speed = 1f;
        animator.Play(walkHash, 0, 0f);
    }

    private void EnterDigging()
    {
        currentState = SquirrelState.Digging;
        stateTimer = Random.Range(minDigDuration, maxDigDuration);

        StopMovement();
        animator.speed = 1f;
        animator.Play(digHash, 0, 0f);
    }

    private void EnterEatingNut()
    {
        currentState = SquirrelState.EatingNut;
        stateTimer = Random.Range(
            minEatingDuration,
            maxEatingDuration
        );

        StopMovement();
        animator.speed = 1f;
        animator.Play(eatNutHash, 0, 0f);
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != SquirrelState.Walking)
            return;

        // Bounce away from the surface we hit.
        if (collision.contactCount > 0)
        {
            Vector2 normal = collision.GetContact(0).normal;
            Vector2 reflected = Vector2.Reflect(
                walkDirection,
                normal
            );

            if (reflected.sqrMagnitude > 0.01f)
                walkDirection = reflected.normalized;
            else
                walkDirection = -walkDirection;

            spriteRenderer.flipX = walkDirection.x < 0f;
        }
        else
        {
            walkDirection = -walkDirection;
            spriteRenderer.flipX = walkDirection.x < 0f;
        }
    }

    private Transform FindVisualChild(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == "Visual")
                return child;

            Transform result = FindVisualChild(child);

            if (result != null)
                return result;
        }

        return null;
    }
}
