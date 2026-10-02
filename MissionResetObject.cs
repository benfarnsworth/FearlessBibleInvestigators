using UnityEngine;

public class MissionResetObject : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("Matches either the ScriptableObject asset name or questName field in QuestManager.")]
    public string targetQuestName = "YahMoBocce";

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb3D;
    private Rigidbody2D rb2D;

    private void Awake()
    {
        // Cache the spawn transform before any gameplay interactions or physics act on it
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rb3D = GetComponent<Rigidbody>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        // Unsubscribe first to avoid duplicate handler calls if OnEnable fires multiple times
        Unsubscribe();

        QuestManager.OnMissionStarted += HandleReset;
        QuestManager.OnMissionReset += HandleReset;
    }

    private void Unsubscribe()
    {
        QuestManager.OnMissionStarted -= HandleReset;
        QuestManager.OnMissionReset -= HandleReset;
    }

    private void HandleReset(string incomingQuestName)
    {
        if (string.IsNullOrWhiteSpace(incomingQuestName)) return;

        // Trims outer whitespaces and performs a case-insensitive check
        if (incomingQuestName.Trim().Equals(targetQuestName.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            ResetToInitialState();
        }
    }

    public void ResetToInitialState()
    {
        // 1. Reset Transform
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // 2. Halt 3D Physics Movement
        if (rb3D != null)
        {
            rb3D.linearVelocity = Vector3.zero;
            rb3D.angularVelocity = Vector3.zero;
            rb3D.Sleep(); // Stops physics simulation on this body until acted on again
        }

        // 3. Halt 2D Physics Movement (if used)
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.Sleep();
        }
    }
}