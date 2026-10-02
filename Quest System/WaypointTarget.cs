using UnityEngine;

public enum WaypointType
{
    SpecificQuest, // World item, drop zone, or specific quest objective
    NPC,           // Points to an NPC (Giver or Turn-In)
    SceneDoor      // Points to a door/portal leading to another scene
}

public class WaypointTarget : MonoBehaviour
{
    [Header("Waypoint Category")]
    public WaypointType waypointType = WaypointType.SpecificQuest;

    [Header("1. Specific Quest (Used if WaypointType = SpecificQuest)")]
    [Tooltip("Leave assigned ONLY if this is a world item or objective tied to one quest.")]
    public Quest associatedQuest;

    [Header("2. NPC Target (Used if WaypointType = NPC)")]
    [Tooltip("The NPCData asset this object belongs to. If empty, checks parent DialogueInteractable automatically.")]
    public NPCData npcData;

    [Header("3. Door / Scene Target (Used if WaypointType = SceneDoor)")]
    [Tooltip("The scene this door leads into.")]
    public GameScene leadsToScene;

    [Header("Display Settings")]
    public Sprite customIcon;
    public bool isPrimaryObjective = false;
    public Vector3 positionOffset = Vector3.up * 1.5f;

    private void Awake()
    {
        if (waypointType == WaypointType.NPC && npcData == null)
        {
            var dialogue = GetComponentInParent<DialogueInteractable>();
            if (dialogue != null) npcData = dialogue.npcData;
        }
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position + positionOffset;
    }

    private void OnEnable()
    {
        WaypointIndicator.RegisterTarget(this);
    }

    private void OnDisable()
    {
        WaypointIndicator.UnregisterTarget(this);
    }

    /// <summary>
    /// Checks if this waypoint matches what the tracked quest currently needs.
    /// </summary>
    public bool IsValidForTrackedQuest(Quest trackedQuest)
    {
        if (trackedQuest == null) return false;

        switch (waypointType)
        {
            case WaypointType.SpecificQuest:
                return IsMatchingQuest(associatedQuest, trackedQuest);

            case WaypointType.NPC:
                string thisNPCName = npcData != null ? npcData.npcName : string.Empty;
                if (string.IsNullOrEmpty(thisNPCName)) return false;

                // Case A: Quest is available -> Match if this NPC is the Giver
                if (trackedQuest.state == QuestState.Unassigned)
                {
                    return string.Equals(thisNPCName, trackedQuest.GetGiverName(), System.StringComparison.OrdinalIgnoreCase);
                }

                // Case B: Quest goals met -> Match if this NPC is Turn-In
                if (trackedQuest.state == QuestState.Active && trackedQuest.IsCompleted())
                {
                    return string.Equals(thisNPCName, trackedQuest.GetTurnInName(), System.StringComparison.OrdinalIgnoreCase);
                }

                return false;

            case WaypointType.SceneDoor:
                // 1. Get the active goal for the tracked quest
                QuestGoal currentGoal = GetCurrentActiveGoal(trackedQuest);

                // 2. Check if the active goal points to the scene this door leads to
                if (currentGoal != null)
                {
                    return currentGoal.targetScene == leadsToScene;
                }

                // Fallback: If the goal has no scene specified, fall back to overall quest targetScene
                return trackedQuest.targetScene == leadsToScene;

            default:
                return false;
        }
    }

    private bool IsMatchingQuest(Quest q1, Quest q2)
    {
        if (q1 == null || q2 == null) return false;
        if (q1 == q2) return true;

        string name1 = string.IsNullOrEmpty(q1.questName) ? q1.name : q1.questName;
        string name2 = string.IsNullOrEmpty(q2.questName) ? q2.name : q2.questName;

        return string.Equals(name1.Replace("(Clone)", "").Trim(), name2.Replace("(Clone)", "").Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Helper to fetch the first uncompleted goal from the tracked quest.
    /// Adjust goal property names (e.g., isCompleted / completed / IsReached()) to match your QuestGoal script.
    /// </summary>
    private QuestGoal GetCurrentActiveGoal(Quest quest)
    {
        if (quest == null || quest.goals == null) return null;

        // Returns the first goal that isn't finished yet
        foreach (var goal in quest.goals)
        {
            if (!goal.IsReached()) // <--- Adjust 'isCompleted' if your variable has a different name
            {
                return goal;
            }
        }

        return null;
    }
}