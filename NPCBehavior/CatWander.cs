
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CatWander : MonoBehaviour
{
    private enum CatState
    {
        Idle,
        Walking,
        Zoomies,
        Prancing,
        Sleeping,
        Pouncing,
        Startled
    }

    [Header("Automatic Discovery")]
    [SerializeField] private string playerTag = "Player";

    [Header("Idle Timing")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("Idle Animation Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float interestedChance = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float lickAChance = 0.20f;

    [Range(0f, 1f)]
    [SerializeField] private float lickBChance = 0.20f;

    [Header("Normal Walking")]
    [SerializeField] private float walkSpeed = 0.8f;
    [SerializeField] private float minWalkTime = 1f;
    [SerializeField] private float maxWalkTime = 3f;

    [Header("Zoomies")]
    [SerializeField] private float zoomiesSpeed = 3f;
    [SerializeField] private float minZoomiesTime = 1f;
    [SerializeField] private float maxZoomiesTime = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float zoomiesChance = 0.08f;

    [Header("Prancing")]
    [SerializeField] private float pranceSpeed = 1.2f;
    [SerializeField] private float minPranceTime = 1f;
    [SerializeField] private float maxPranceTime = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float pranceChance = 0.15f;

    [Header("Sleeping")]
    [SerializeField] private float minSleepTime = 5f;
    [SerializeField] private float maxSleepTime = 12f;
    [Range(0f, 1f)]
    [SerializeField] private float sleepChance = 0.08f;

    [Header("Pouncing")]
    [SerializeField] private float pounceSpeed = 2.5f;
    [SerializeField] private float pounceDuration = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float pounceChance = 0.12f;

    [Header("Hackles Up")]
    [SerializeField] private float minStartledTime = 1f;
    [SerializeField] private float maxStartledTime = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float startledChance = 0.08f;

    [Header("Facing")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private Rigidbody2D rb;
    private Transform player;
    private Transform visual;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private CatState currentState = CatState.Idle;

    private Vector2 movementDirection;
    private float stateTimer;
    private float pounceTimer;
    private bool pounceHasStarted;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("IsWalking");

    private static readonly int IsZoomiesHash =
        Animator.StringToHash("IsZoomies");

    private static readonly int IsPrancingHash =
        Animator.StringToHash("IsPrancing");

    private static readonly int IsSleepingHash =
        Animator.StringToHash("IsSleeping");

    private static readonly int InterestedHash =
        Animator.StringToHash("Interested");

    private static readonly int LickAHash =
        Animator.StringToHash("LickA");

    private static readonly int LickBHash =
        Animator.StringToHash("LickB");

    private static readonly int PounceHash =
        Animator.StringToHash("Pounce");

    private static readonly int HacklesUpHash =
        Animator.StringToHash("HacklesUp");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the player automatically.
        // The player is not used as a trigger in this behavior design yet.
        FindPlayer();

        // Find the Visual child automatically.
        visual = transform.Find("Visual");

        if (visual == null)
        {
            Animator childAnimator = GetComponentInChildren<Animator>();

            if (childAnimator != null)
                visual = childAnimator.transform;
        }

        if (visual == null)
        {
            Debug.LogError(
                "CatWander: Could not find a Visual child or child Animator.",
                this);

            enabled = false;
            return;
        }

        animator = visual.GetComponent<Animator>();
        spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (animator == null || spriteRenderer == null)
        {
            Debug.LogError(
                "CatWander: Visual needs an Animator and SpriteRenderer.",
                this);

            enabled = false;
            return;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
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
            case CatState.Idle:
                if (stateTimer <= 0f)
                    ChooseNextAction();
                break;

            case CatState.Walking:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case CatState.Zoomies:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case CatState.Prancing:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case CatState.Sleeping:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case CatState.Pouncing:
                UpdatePounce();
                break;

            case CatState.Startled:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case CatState.Walking:
                MoveAtSpeed(walkSpeed);
                break;

            case CatState.Zoomies:
                MoveAtSpeed(zoomiesSpeed);
                break;

            case CatState.Prancing:
                MoveAtSpeed(pranceSpeed);
                break;

            case CatState.Pouncing:
                if (pounceHasStarted)
                    MoveAtSpeed(pounceSpeed);
                break;

            default:
                // The cat stays still during idle, sleep,
                // and the hackles-up reaction.
                break;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning(
                $"CatWander: No GameObject found with tag '{playerTag}'.",
                this);
        }
    }

    private void ChooseNextAction()
    {
        float roll = Random.value;

        // Each chance is checked in order.
        // If none succeeds, the cat walks normally.
        if (roll < sleepChance)
        {
            BeginSleeping();
        }
        else if (roll < sleepChance + startledChance)
        {
            BeginStartled();
        }
        else if (roll < sleepChance + startledChance + pounceChance)
        {
            BeginPounce();
        }
        else if (roll < sleepChance + startledChance
                 + pounceChance + zoomiesChance)
        {
            BeginZoomies();
        }
        else if (roll < sleepChance + startledChance
                 + pounceChance + zoomiesChance + pranceChance)
        {
            BeginPrancing();
        }
        else
        {
            BeginWalking();
        }
    }

    private void EnterIdle()
    {
        currentState = CatState.Idle;
        movementDirection = Vector2.zero;

        SetLocomotionBools(false, false, false);
        animator.SetBool(IsSleepingHash, false);

        // Reset any leftover special-action triggers.
        animator.ResetTrigger(PounceHash);
        animator.ResetTrigger(HacklesUpHash);

        PlayRandomIdleAnimation();

        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void PlayRandomIdleAnimation()
    {
        float totalChance =
            interestedChance + lickAChance + lickBChance;

        // The remaining probability uses the Bored animation.
        float roll = Random.value * (1f + totalChance);

        animator.ResetTrigger(InterestedHash);
        animator.ResetTrigger(LickAHash);
        animator.ResetTrigger(LickBHash);

        if (roll < interestedChance)
        {
            animator.SetTrigger(InterestedHash);
        }
        else if (roll < interestedChance + lickAChance)
        {
            animator.SetTrigger(LickAHash);
        }
        else if (roll < interestedChance + lickAChance + lickBChance)
        {
            animator.SetTrigger(LickBHash);
        }
        // Otherwise, the Animator stays in or returns to Bored.
    }

    private void BeginWalking()
    {
        currentState = CatState.Walking;
        ChooseMovementDirection();

        SetLocomotionBools(true, false, false);

        stateTimer = Random.Range(minWalkTime, maxWalkTime);
    }

    private void BeginZoomies()
    {
        currentState = CatState.Zoomies;
        ChooseMovementDirection();

        SetLocomotionBools(false, true, false);

        stateTimer = Random.Range(minZoomiesTime, maxZoomiesTime);
    }

    private void BeginPrancing()
    {
        currentState = CatState.Prancing;
        ChooseMovementDirection();

        SetLocomotionBools(false, false, true);

        stateTimer = Random.Range(minPranceTime, maxPranceTime);
    }

    private void BeginSleeping()
    {
        currentState = CatState.Sleeping;
        movementDirection = Vector2.zero;

        SetLocomotionBools(false, false, false);
        animator.SetBool(IsSleepingHash, true);

        stateTimer = Random.Range(minSleepTime, maxSleepTime);
    }

    private void BeginStartled()
    {
        currentState = CatState.Startled;
        movementDirection = Vector2.zero;

        SetLocomotionBools(false, false, false);
        animator.SetBool(IsSleepingHash, false);

        animator.ResetTrigger(HacklesUpHash);
        animator.SetTrigger(HacklesUpHash);

        stateTimer = Random.Range(minStartledTime, maxStartledTime);
    }

    private void BeginPounce()
    {
        currentState = CatState.Pouncing;
        ChooseMovementDirection();

        SetLocomotionBools(false, false, false);

        animator.ResetTrigger(PounceHash);
        animator.SetTrigger(PounceHash);

        pounceTimer = 0f;
        pounceHasStarted = false;
    }

    private void UpdatePounce()
    {
        pounceTimer += Time.deltaTime;

        // Brief pause to let the pounce animation begin.
        // Movement starts after this short wind-up.
        if (!pounceHasStarted && pounceTimer >= 0.12f)
        {
            pounceHasStarted = true;
        }

        if (pounceTimer >= pounceDuration)
        {
            pounceHasStarted = false;
            EnterIdle();
        }
    }

    private void ChooseMovementDirection()
    {
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized,
            new Vector2(1f, -1f).normalized
        };

        movementDirection =
            directions[Random.Range(0, directions.Length)];

        FaceDirection(movementDirection.x);
    }

    private void MoveAtSpeed(float speed)
    {
        rb.MovePosition(
            rb.position +
            movementDirection * speed * Time.fixedDeltaTime);
    }

    private void SetLocomotionBools(
        bool walking,
        bool zoomies,
        bool prancing)
    {
        animator.SetBool(IsWalkingHash, walking);
        animator.SetBool(IsZoomiesHash, zoomies);
        animator.SetBool(IsPrancingHash, prancing);
    }

    private void FaceDirection(float horizontalDirection)
    {
        if (horizontalDirection > 0.01f)
        {
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        }
        else if (horizontalDirection < -0.01f)
        {
            spriteRenderer.flipX = spriteFacesRightByDefault;
        }
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
