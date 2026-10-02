using UnityEngine;

public class DoorTrigger : MonoBehaviour, IInteractable
{
    [Header("Destination Settings")]
    [SerializeField] private GameScene targetScene;
    [SerializeField] private LocationID targetLocationID;

    [Header("UI Prompt")]
    [SerializeField] private string promptText = "Enter";

    [Header("Waypoint / Navigation")]
    [Tooltip("Optional. Auto-fetches WaypointTarget from this GameObject if left unassigned.")]
    [SerializeField] private WaypointTarget waypointTarget;

    private bool hasTriggered = false;

    private void Awake()
    {
        if (waypointTarget == null)
        {
            waypointTarget = GetComponent<WaypointTarget>();
        }
    }

    private void OnEnable()
    {
        hasTriggered = false;
        QuestManager.OnQuestsUpdated += RefreshDoorWaypoint;
        RefreshDoorWaypoint();
    }

    private void OnDisable()
    {
        hasTriggered = false;
        QuestManager.OnQuestsUpdated -= RefreshDoorWaypoint;
    }

    /// <summary>
    /// Checks if any active quest objective takes place in this door's target scene.
    /// If yes, enables the WaypointTarget on this door.
    /// </summary>
    public void RefreshDoorWaypoint()
    {
        if (waypointTarget == null || QuestManager.Instance == null) return;

        bool hasObjectiveInTargetScene = false;

        foreach (var q in QuestManager.Instance.activeQuests)
        {
            if (q == null) continue;

            // Direct enum comparison — fast and typo-proof!
            if (q.targetScene == targetScene)
            {
                hasObjectiveInTargetScene = true;
                break;
            }
        }

        waypointTarget.enabled = hasObjectiveInTargetScene;
        waypointTarget.isPrimaryObjective = hasObjectiveInTargetScene;
    }

    public void Interact()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        string locationName = targetLocationID != null ? targetLocationID.name : "NULL / UNASSIGNED";
        Debug.Log($"[DOOR] Object '<b>{gameObject.name}</b>' is firing! Target Scene: <b>{targetScene}</b> | LocationID: <b>{locationName}</b>", this);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetScene.ToString(), targetLocationID);
        }
        else
        {
            Debug.LogError("[DOOR] SceneTransitionManager missing from scene!", this);
        }
    }

    public string GetInteractionPrompt()
    {
        return promptText;
    }
}