using System.Collections;
using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(Animator))]
public class NPCFacing : MonoBehaviour
{
    public enum FacingDirection { Down, Up, Left, Right }

    [Header("Initial Facing")]
    [SerializeField] private FacingDirection defaultFacing = FacingDirection.Down;

    [Header("Idle Behavior (Optional)")]
    [Tooltip("If true, the NPC will periodically look around while standing still.")]
    [SerializeField] private bool randomFacingWhenIdle = false;
    [SerializeField] private float minTurnInterval = 3f;
    [SerializeField] private float maxTurnInterval = 8f;

    [Header("Dialogue Auto-Face")]
    [Tooltip("If true, turns to look at the player when nearby dialogue triggers.")]
    [SerializeField] private bool autoFacePlayerOnDialogue = true;
    [SerializeField] private float dialogueFaceRange = 3.0f;

    private Animator animator;
    private Transform cachedPlayer;
    private Vector2 lastDirection = Vector2.down;
    private Vector2 currentMovement = Vector2.zero;
    private float idleTurnTimer;

    private readonly Vector2[] cardinalDirections = new Vector2[]
    {
        Vector2.down, Vector2.up, Vector2.left, Vector2.right
    };

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ApplyFacingEnum(defaultFacing);
    }

    private void Start()
    {
        ResetIdleTimer();
        UpdateAnimator();

        // 🟢 Cache player reference to avoid runtime hierarchy searches
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            cachedPlayer = playerObj.transform;
        }

        DialogueManager.OnDialogueOpened += HandleDialogueOpened;
    }

    private void OnDestroy()
    {
        DialogueManager.OnDialogueOpened -= HandleDialogueOpened;
    }

    private void Update()
    {
        if (randomFacingWhenIdle && currentMovement == Vector2.zero)
        {
            idleTurnTimer -= Time.deltaTime;
            if (idleTurnTimer <= 0)
            {
                TurnToRandomDirection();
                ResetIdleTimer();
            }
        }

        UpdateAnimator();
    }

    private void HandleDialogueOpened()
    {
        if (!autoFacePlayerOnDialogue || cachedPlayer == null) return;

        float dist = Vector2.Distance(transform.position, cachedPlayer.position);
        if (dist <= dialogueFaceRange)
        {
            LookAt(cachedPlayer);
        }
    }

    public void SetMovement(Vector2 movementVector)
    {
        currentMovement = movementVector;

        if (movementVector.sqrMagnitude > 0.01f)
        {
            lastDirection = SnapToCardinal(movementVector);
            ResetIdleTimer();
        }
    }

    public void SetFacing(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            lastDirection = SnapToCardinal(direction);
            currentMovement = Vector2.zero;
            ResetIdleTimer();
            UpdateAnimator();
        }
    }

    public void LookAt(Transform target)
    {
        if (target == null) return;
        Vector2 dir = (target.position - transform.position);
        SetFacing(dir);
    }

    private Vector2 SnapToCardinal(Vector2 dir)
    {
        if (dir == Vector2.zero) return lastDirection;

        // Snaps diagonal vectors to the dominant axis for clean 4-way animator blend trees
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return dir.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    private void TurnToRandomDirection()
    {
        int index = Random.Range(0, cardinalDirections.Length);
        lastDirection = cardinalDirections[index];
    }

    private void ResetIdleTimer()
    {
        idleTurnTimer = Random.Range(minTurnInterval, maxTurnInterval);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", lastDirection.x);
        animator.SetFloat("MoveY", lastDirection.y);
        animator.SetFloat("Speed", currentMovement.sqrMagnitude);
    }

    private void ApplyFacingEnum(FacingDirection direction)
    {
        lastDirection = direction switch
        {
            FacingDirection.Up => Vector2.up,
            FacingDirection.Down => Vector2.down,
            FacingDirection.Left => Vector2.left,
            FacingDirection.Right => Vector2.right,
            _ => Vector2.down
        };
    }

    // ========================================================================
    // YARN SPINNER COMMANDS (Instance-Bound)
    // Usage in Yarn: <<face NPC_GameObject_Name "up">>
    // Usage in Yarn: <<look_at NPC_GameObject_Name "Player">>
    // ========================================================================

    [YarnCommand("face")]
    public void FaceCommand(string directionStr)
    {
        Vector2 dir = directionStr.ToLower() switch
        {
            "up" => Vector2.up,
            "down" => Vector2.down,
            "left" => Vector2.left,
            "right" => Vector2.right,
            _ => Vector2.down
        };
        SetFacing(dir);
    }

    [YarnCommand("look_at")]
    public void LookAtCommand(string targetName)
    {
        GameObject target = GameObject.Find(targetName);
        if (target != null)
        {
            LookAt(target.transform);
        }
    }
}