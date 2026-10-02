using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class GoalArea : MonoBehaviour
{
    public static List<GoalArea> ActiveAreas = new List<GoalArea>();

    [Header("Waypoint / Identity")]
    [SerializeField] private WaypointTarget waypointTarget;
    [Tooltip("Unique ID for this zone (MUST match Goal Area ID in Quest SO)")]
    [SerializeField] private string goalAreaID = "Kitchen";

    [Header("Target Settings")]
    [Tooltip("Optional: Assign target item. Leave NULL to count ANY object entering zone.")]
    [SerializeField] private Item targetItem;
    [SerializeField] private int requiredCount = 5;

    [Header("Delivery Settings")]
    [Tooltip("If TRUE, items are permanently counted once delivered.")]
    [SerializeField] private bool permanentDelivery = true;
    [Tooltip("If TRUE, destroys delivered object (e.g. food eaten). Requires permanentDelivery = true.")]
    [SerializeField] private bool destroyOnDelivery = false;

    [Header("Optional Hold Timer")]
    [Tooltip("Time (seconds) items must stay inside before quest completes. 0 = instant.")]
    [SerializeField] private float requiredHoldTime = 0f;
    private float currentHoldTimer = 0f;

    [Header("Visual Feedback")]
    [SerializeField] private GoalAreaVisuals visualHandler;

    [Header("Quest Integration")]
    public UnityEvent onGoalReached;
    [Tooltip("Quest ScriptableObjects linked to this zone.")]
    [SerializeField] private List<Quest> associatedQuests = new List<Quest>();

    private HashSet<GameObject> objectsInPen = new HashSet<GameObject>();
    private HashSet<GameObject> permanentlyDelivered = new HashSet<GameObject>();
    private Collider2D zoneCollider;
    private bool hasTriggeredComplete = false;
    private bool isEvaluating = false;

    public static event System.Action OnHoldTick;

    public string GoalAreaID => goalAreaID;
    public float RemainingHoldTime => Mathf.Max(0f, requiredHoldTime - currentHoldTimer);
    public float RequiredHoldTime => requiredHoldTime;
    public bool HasTriggeredComplete => hasTriggeredComplete;
    public bool IsHolding => objectsInPen.Count >= requiredCount && requiredHoldTime > 0f && !hasTriggeredComplete;
    public bool IsTimerActive => requiredHoldTime > 0f && IsHolding;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        if (visualHandler == null) visualHandler = GetComponentInChildren<GoalAreaVisuals>(true);
    }

    private void OnEnable()
    {
        if (!ActiveAreas.Contains(this)) ActiveAreas.Add(this);
        QuestManager.OnQuestsUpdated += EvaluateQuestState;
        QuestManager.OnMissionReset += HandleMissionReset;
    }

    private void OnDisable()
    {
        if (ActiveAreas.Contains(this)) ActiveAreas.Remove(this);
        QuestManager.OnQuestsUpdated -= EvaluateQuestState;
        QuestManager.OnMissionReset -= HandleMissionReset;
    }

    private void Start()
    {
        EvaluateQuestState();
    }

    private void Update()
    {
        if (requiredHoldTime <= 0f || permanentDelivery || hasTriggeredComplete) return;

        if (objectsInPen.Count >= requiredCount)
        {
            currentHoldTimer += Time.deltaTime;

            if (currentHoldTimer >= requiredHoldTime)
            {
                currentHoldTimer = requiredHoldTime;
                hasTriggeredComplete = true;

                // 1. Update score in QuestManager
                if (QuestManager.Instance != null && !string.IsNullOrEmpty(goalAreaID))
                {
                    QuestManager.Instance.SetScore(goalAreaID, requiredCount);
                }

                // 2. Fire completion events FIRST so QuestManager updates fully
                onGoalReached?.Invoke();
                OnHoldTick?.Invoke();

                // 3. Trigger visual state LAST after all event cascades settle
                if (visualHandler != null) 
                {
                    visualHandler.SetState(GoalState.MissionComplete);
                }
            }
            else
            {
                OnHoldTick?.Invoke();
            }
        }
        else
        {
            if (currentHoldTimer > 0f)
            {
                currentHoldTimer = 0f;
                if (QuestManager.Instance != null && !string.IsNullOrEmpty(goalAreaID))
                {
                    QuestManager.Instance.SetScore(goalAreaID, objectsInPen.Count);
                }
                OnHoldTick?.Invoke();
            }
        }
    }

    private void HandleMissionReset(string questName)
    {
        if (associatedQuests == null || associatedQuests.Count == 0) return;

        foreach (Quest q in associatedQuests)
        {
            if (q != null && q.questName.Equals(questName, System.StringComparison.OrdinalIgnoreCase))
            {
                ResetGoalArea();
                break;
            }
        }
    }

    public void ResetGoalArea()
    {
        objectsInPen.Clear();
        permanentlyDelivered.Clear();
        hasTriggeredComplete = false;
        currentHoldTimer = 0f;

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(goalAreaID))
        {
            QuestManager.Instance.SetScore(goalAreaID, 0);
        }

        OnHoldTick?.Invoke();
        EvaluateQuestState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggeredComplete) return;

        GameObject rootObject = GetValidRootObject(other);
        if (rootObject == null) return;

        if (permanentlyDelivered.Contains(rootObject) || objectsInPen.Contains(rootObject)) return;

        if (MatchesTarget(rootObject))
        {
            objectsInPen.Add(rootObject);

            if (permanentDelivery)
            {
                permanentlyDelivered.Add(rootObject);
            }

            int currentCount = permanentDelivery ? permanentlyDelivered.Count : objectsInPen.Count;

            if (currentCount >= requiredCount && requiredHoldTime <= 0f)
            {
                hasTriggeredComplete = true; // Lock completion state immediately
                if (visualHandler != null) visualHandler.SetState(GoalState.MissionComplete);
            }
            else
            {
                if (visualHandler != null) visualHandler.SetState(GoalState.Scored);
            }

            OnItemDelivered(currentCount);

            if (destroyOnDelivery && permanentDelivery)
            {
                objectsInPen.Remove(rootObject);
                Destroy(rootObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (permanentDelivery || hasTriggeredComplete) return;

        GameObject rootObject = GetValidRootObject(other);
        if (rootObject == null) return;

        if (objectsInPen.Contains(rootObject))
        {
            objectsInPen.Remove(rootObject);

            if (visualHandler != null) visualHandler.SetState(GoalState.Lost);

            if (QuestManager.Instance != null)
            {
                int reportedScore = (objectsInPen.Count >= requiredCount && requiredHoldTime > 0f) 
                    ? requiredCount - 1 
                    : objectsInPen.Count;
                
                QuestManager.Instance.SetScore(goalAreaID, reportedScore);
            }
        }
    }

    private GameObject GetValidRootObject(Collider2D col)
    {
        if (col == null) return null;

        ItemHolder holder = col.GetComponentInParent<ItemHolder>();
        if (holder != null) return holder.gameObject;

        Rigidbody2D rb = col.attachedRigidbody;
        if (rb != null) return rb.gameObject;

        return col.transform.root.gameObject;
    }

    private bool MatchesTarget(GameObject obj)
    {
        if (targetItem == null) return true;

        ItemHolder holder = obj.GetComponent<ItemHolder>();
        if (holder == null) holder = obj.GetComponentInChildren<ItemHolder>();

        if (holder == null || holder.itemData == null) return false;

        Item item = holder.itemData;
        bool matchesID = !string.IsNullOrEmpty(item.itemID) && string.Equals(targetItem.itemID, item.itemID, System.StringComparison.OrdinalIgnoreCase);
        bool matchesRef = targetItem == item || targetItem.name.Equals(item.name, System.StringComparison.OrdinalIgnoreCase);

        return matchesID || matchesRef;
    }

    private void OnItemDelivered(int currentCount)
    {
        if (QuestManager.Instance == null || string.IsNullOrEmpty(goalAreaID)) return;

        if (requiredHoldTime > 0f)
        {
            int reportedScore = (currentCount >= requiredCount) ? requiredCount - 1 : currentCount;
            QuestManager.Instance.SetScore(goalAreaID, reportedScore);
            return;
        }

        if (permanentDelivery)
            QuestManager.Instance.AddScore(goalAreaID);
        else
            QuestManager.Instance.SetScore(goalAreaID, currentCount);

        if (currentCount >= requiredCount)
        {
            onGoalReached?.Invoke();
        }
    }

    private void EvaluateQuestState()
    {
        if (isEvaluating) return;
        isEvaluating = true;

        try
        {
            if (associatedQuests == null || associatedQuests.Count == 0)
            {
                SetAreaActive(true, GoalState.Active);
                return;
            }

            if (QuestManager.Instance == null)
            {
                SetAreaActive(false, GoalState.Inactive);
                return;
            }

            bool isAnyActive = false;
            bool isAnyComplete = false;

            foreach (Quest questSO in associatedQuests)
            {
                if (questSO == null) continue;
                Quest runtimeQuest = QuestManager.Instance.FindQuest(questSO.questName);
                if (runtimeQuest != null)
                {
                    if (runtimeQuest.state == QuestState.Active) isAnyActive = true;
                    if (runtimeQuest.IsCompleted()) isAnyComplete = true;
                }
            }

            if (!isAnyActive && !isAnyComplete)
            {
                hasTriggeredComplete = false;
                SetAreaActive(false, GoalState.Inactive);
                return;
            }

            if (isAnyComplete)
            {
                if (requiredHoldTime > 0f && !hasTriggeredComplete)
                {
                    SetAreaActive(true, GoalState.Active);
                }
                else
                {
                    hasTriggeredComplete = true;
                    SetAreaActive(false, GoalState.MissionComplete);
                }
            }
            else
            {
                SetAreaActive(true, GoalState.Active);
            }
        }
        finally
        {
            isEvaluating = false;
        }
    }

    private void SetAreaActive(bool colliderState, GoalState visualState)
    {
        if (zoneCollider != null) zoneCollider.enabled = colliderState;

        if (visualHandler != null)
        {
            if (visualState != GoalState.Inactive && !visualHandler.gameObject.activeSelf)
            {
                visualHandler.gameObject.SetActive(true);
            }

            if (visualState != GoalState.MissionComplete)
            {
                if (visualHandler.CurrentState != GoalState.Scored && visualHandler.CurrentState != GoalState.Lost)
                {
                    visualHandler.SetState(visualState);
                }
            }
            else
            {
                visualHandler.SetState(GoalState.MissionComplete);
            }
        }
    }
}