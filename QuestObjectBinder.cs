using UnityEngine;

public class QuestObjectBinder : MonoBehaviour
{
    [Header("Quest Connection")]
    [Tooltip("The quest that must be ACTIVE for this minigame/area to operate.")]
    [SerializeField] private Quest associatedQuest;

    [Header("Behavior When Quest Inactive / Finished")]
    [Tooltip("If true, disables scoring colliders in children. Ignores GoalAreaVisuals so visuals work properly.")]
    [SerializeField] private bool disableCollidersWhenInactive = true;

    [Tooltip("If true, deactivates this entire GameObject (and all children) when quest is inactive or completed.")]
    [SerializeField] private bool disableEntireObjectWhenInactive = false;

    private Collider2D[] childColliders2D;
    private Collider[] childColliders3D;

    private void Awake()
    {
        childColliders2D = GetComponentsInChildren<Collider2D>(true);
        childColliders3D = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        QuestManager.OnQuestsUpdated += EvaluateState;
        EvaluateState();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestsUpdated -= EvaluateState;
    }

    private void EvaluateState()
    {
        if (QuestManager.Instance == null || associatedQuest == null) return;

        Quest runtimeQuest = QuestManager.Instance.FindQuest(associatedQuest.questName);
        bool isQuestActive = (runtimeQuest != null && runtimeQuest.state == QuestState.Active);

        if (disableEntireObjectWhenInactive)
        {
            gameObject.SetActive(isQuestActive);
            return;
        }

        if (disableCollidersWhenInactive)
        {
            if (childColliders2D != null)
            {
                foreach (var col in childColliders2D)
                {
                    if (col == null) continue;

                    // DON'T disable colliders that belong to GoalAreaVisuals!
                    if (col.GetComponent<GoalAreaVisuals>() != null) continue;

                    col.enabled = isQuestActive;
                }
            }

            if (childColliders3D != null)
            {
                foreach (var col in childColliders3D)
                {
                    if (col == null) continue;

                    if (col.GetComponent<GoalAreaVisuals>() != null) continue;

                    col.enabled = isQuestActive;
                }
            }
        }
    }
}