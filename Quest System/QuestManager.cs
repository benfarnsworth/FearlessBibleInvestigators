using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Database")]
    [Tooltip("Drag all Quest ScriptableObjects here so the manager can track and instantiate runtime clones.")]
    public List<Quest> allGameQuests = new List<Quest>();

    [Header("Global UI & Icons")]
    [SerializeField] private Sprite defaultAvailableQuestSprite;
    [SerializeField] private Sprite defaultTurnInQuestSprite;

    [Header("Active State")]
    public List<Quest> activeQuests = new List<Quest>();
    public List<Quest> completedQuests = new List<Quest>();

    private readonly Dictionary<string, Quest> runtimeQuestMap = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);

    // --- EVENTS ---
    public static event Action OnQuestsUpdated;
    public static event Action OnQuestTimerTick;
    public static event Action<Quest> OnQuestAccepted;
    public static event Action<Quest> OnQuestReadyToTurnIn;
    public static event Action<Quest> OnQuestCompleted;
    public static event Action<Quest> OnQuestFailed;

    public static event Action<string> OnMissionStarted;
    public static event Action<string> OnMissionReset;

    // One-shot audio hook for playing sounds cleanly without pitch conflicts
    public static event Action<AudioClip> OnPlayQuestSFX;

    public Quest TrackedQuest { get; private set; }
    public static event Action<Quest> OnTrackedQuestChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeRuntimeQuests();
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueEventTriggered += HandleDialogueEvent;
        Inventory.OnInventoryChanged += SyncGatheringQuests;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEventTriggered -= HandleDialogueEvent;
        Inventory.OnInventoryChanged -= SyncGatheringQuests;
    }

    private void Update()
    {
        if (activeQuests == null || activeQuests.Count == 0) return;

        bool timerUpdatedThisFrame = false;

        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            Quest q = activeQuests[i];
            if (q == null) continue;

            if (q.isTimed && q.state == QuestState.Active && !q.IsCompleted())
            {
                if (q.timeRemaining > 0f)
                {
                    q.timeRemaining -= Time.unscaledDeltaTime;
                    timerUpdatedThisFrame = true;

                    if (q.timeRemaining <= 0f)
                    {
                        q.timeRemaining = 0f;
                        FailQuest(q);
                    }
                }
            }
        }

        if (timerUpdatedThisFrame)
        {
            OnQuestTimerTick?.Invoke();
        }
    }

private void InitializeRuntimeQuests()
{
    foreach (var q in runtimeQuestMap.Values)
    {
        if (q != null) Destroy(q);
    }

    runtimeQuestMap.Clear();
    activeQuests.Clear();
    completedQuests.Clear();

    foreach (Quest original in allGameQuests)
    {
        if (original == null) continue;

        Quest runtimeClone = Instantiate(original);
        runtimeClone.state = QuestState.Unassigned;

        runtimeClone.goals = new List<QuestGoal>();
        if (original.goals != null)
        {
            foreach (var g in original.goals)
            {
                if (g != null)
                {
                    runtimeClone.goals.Add(g.Clone());
                }
            }
        }

        runtimeQuestMap[runtimeClone.questName] = runtimeClone;
    }
}

    public Sprite GetQuestIcon(QuestIconState state)
    {
        switch (state)
        {
            case QuestIconState.Available:
                return defaultAvailableQuestSprite;
            case QuestIconState.ReadyToTurnIn:
                return defaultTurnInQuestSprite;
            case QuestIconState.None:
            default:
                return null;
        }
    }

    public QuestIconState GetIconStateForNPC(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return QuestIconState.None;

        foreach (Quest q in activeQuests)
        {
            if (q != null && q.state == QuestState.Active && q.IsCompleted())
            {
                if (string.Equals(q.GetTurnInName(), npcName, StringComparison.OrdinalIgnoreCase))
                {
                    return QuestIconState.ReadyToTurnIn;
                }
            }
        }

        foreach (Quest q in runtimeQuestMap.Values)
        {
            if (q == null) continue;

            if (q.state == QuestState.Unassigned && ArePrerequisitesMet(q))
            {
                if (string.Equals(q.GetGiverName(), npcName, StringComparison.OrdinalIgnoreCase))
                {
                    return QuestIconState.Available;
                }
            }
        }

        return QuestIconState.None;
    }

    public QuestIconState GetIconStateForNPC(NPCData npc) => GetIconStateForNPC(npc != null ? npc.npcName : string.Empty);

    public List<Quest> GetAvailableQuestsForNPC(string npcName)
    {
        List<Quest> results = new List<Quest>();
        foreach (Quest q in runtimeQuestMap.Values)
        {
            if (q != null && q.state == QuestState.Unassigned && ArePrerequisitesMet(q))
            {
                if (string.Equals(q.GetGiverName(), npcName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(q);
                }
            }
        }
        return results;
    }

    public List<Quest> GetReadyTurnInQuestsForNPC(string npcName)
    {
        List<Quest> results = new List<Quest>();
        foreach (Quest q in activeQuests)
        {
            if (q != null && q.state == QuestState.Active && q.IsCompleted())
            {
                if (string.Equals(q.GetTurnInName(), npcName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(q);
                }
            }
        }
        return results;
    }

    public void AcceptQuest(Quest quest)
    {
        if (quest == null) return;

        Quest runtimeQuest = FindQuest(GetQuestIdentifier(quest));
        if (runtimeQuest != null) quest = runtimeQuest;

        if (quest.state != QuestState.Unassigned) return;
        if (!ArePrerequisitesMet(quest)) return;

        ResetQuestProgress(quest);
        quest.state = QuestState.Active;

        if (quest.isTimed) quest.timeRemaining = quest.timeLimit;
        if (!activeQuests.Contains(quest)) activeQuests.Add(quest);

        string qIdentifier = GetQuestIdentifier(quest);
        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Active", true);

        EvaluateRetroactiveGoals(quest);
        CheckQuestCompletion(quest);
        SetTrackedQuest(quest);

        if (quest.customAcceptSFX != null)
        {
            OnPlayQuestSFX?.Invoke(quest.customAcceptSFX);
        }

        OnQuestAccepted?.Invoke(quest);
        OnMissionStarted?.Invoke(qIdentifier);
        OnQuestsUpdated?.Invoke();
    }

    public void AcceptQuest(string identifier) => AcceptQuest(FindQuest(identifier));

    public void CompleteQuest(Quest quest)
    {
        if (quest == null) return;

        quest.Complete();
        if (activeQuests.Contains(quest)) activeQuests.Remove(quest);
        if (!completedQuests.Contains(quest)) completedQuests.Add(quest);

        string qIdentifier = GetQuestIdentifier(quest);

        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Active", false);
        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Ready", false);
        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Completed", true);

        GrantRewards(quest);

        if (TrackedQuest == quest)
        {
            SetTrackedQuest(activeQuests.Find(q => q != null && q.state == QuestState.Active));
        }

        if (quest.customCompleteSFX != null)
        {
            OnPlayQuestSFX?.Invoke(quest.customCompleteSFX);
        }

        OnQuestCompleted?.Invoke(quest);
        OnQuestsUpdated?.Invoke();
    }

    public void CompleteQuest(string identifier) => CompleteQuest(FindQuest(identifier));

    private void GrantRewards(Quest quest)
    {
        if (quest == null) return;

        if (quest.itemRewards != null && Inventory.Instance != null)
        {
            foreach (var item in quest.itemRewards)
            {
                if (item != null) Inventory.Instance.AddItem(item);
            }
        }

        // Connect your player XP/Currency managers here if applicable:
        // PlayerStats.Instance?.AddExp(quest.expReward);
        // PlayerWallet.Instance?.AddGold(quest.goldReward);
    }

    public void FailQuest(Quest quest) => CancelOrFailQuest(quest, isFailure: true);
    public void FailQuest(string identifier) => FailQuest(FindQuest(identifier));

    public void QuitQuest(Quest quest) => CancelOrFailQuest(quest, isFailure: false);
    public void QuitQuest(string identifier) => QuitQuest(FindQuest(identifier));

    private void CancelOrFailQuest(Quest quest, bool isFailure)
    {
        if (quest == null || quest.state != QuestState.Active) return;

        string qIdentifier = GetQuestIdentifier(quest);
        ResetQuestProgress(quest);

        if (activeQuests.Contains(quest)) activeQuests.Remove(quest);

        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Active", false);
        StoryStateManager.SetFlag($"Quest_{qIdentifier}_Ready", false);

        if (isFailure)
        {
            StoryStateManager.SetFlag($"Quest_{qIdentifier}_Failed", true);
            OnQuestFailed?.Invoke(quest);
        }

        if (TrackedQuest == quest)
        {
            SetTrackedQuest(activeQuests.Find(q => q != null && q.state == QuestState.Active));
        }

        OnMissionReset?.Invoke(qIdentifier);
        OnQuestsUpdated?.Invoke();
    }

    private void ResetQuestProgress(Quest quest)
    {
        quest.state = QuestState.Unassigned;
        quest.timeRemaining = quest.isTimed ? quest.timeLimit : 0f;

        if (quest.goals == null) return;

        foreach (QuestGoal goal in quest.goals)
        {
            if (goal != null) goal.currentAmount = 0;
        }
    }

    public void SetScore(string scoreID, int score) => ModifyScoreGoal(scoreID, score, isAdditive: false);
    public void AddScore(string scoreID, int points = 1) => ModifyScoreGoal(scoreID, points, isAdditive: true);

    private void ModifyScoreGoal(string scoreID, int amount, bool isAdditive)
    {
        if (string.IsNullOrEmpty(scoreID)) return;

        bool didUpdate = false;

        ForEachActiveGoal(goal =>
        {
            bool isMatchingType = goal.goalType == GoalType.ScorePoints || goal.goalType == GoalType.DeliverToArea;
            bool matchesID = string.Equals(goal.goalAreaID, scoreID, StringComparison.OrdinalIgnoreCase);

            if (isMatchingType && matchesID)
            {
                int targetVal = isAdditive ? goal.currentAmount + amount : amount;
                int newAmount = Mathf.Clamp(targetVal, 0, goal.requiredAmount);

                if (goal.currentAmount != newAmount)
                {
                    goal.currentAmount = newAmount;
                    didUpdate = true;
                }
            }
        });

        if (didUpdate)
        {
            OnQuestsUpdated?.Invoke();
        }
    }

    public void SyncGatheringQuests()
    {
        if (Inventory.Instance == null) return;

        foreach (Quest quest in activeQuests)
        {
            if (quest == null || quest.state != QuestState.Active || quest.goals == null) continue;

            foreach (QuestGoal goal in quest.goals)
            {
                if (goal != null && goal.goalType == GoalType.Gathering && goal.targetItem != null)
                {
                    int currentInBag = Inventory.Instance.GetItemCount(goal.targetItem);
                    goal.currentAmount = Mathf.Min(currentInBag, goal.requiredAmount);
                }
            }
            CheckQuestCompletion(quest);
        }

        OnQuestsUpdated?.Invoke();
    }

    public void TrackItemPickup(Item item)
    {
        if (item == null) return;
        ForEachActiveGoal(goal => goal.ItemCollected(item));
    }

    public void TrackNPCTalkedTo(NPCData npc)
    {
        if (npc == null) return;
        ForEachActiveGoal(goal => goal.NPCTalkedTo(npc));
    }

    public void TrackNPCTalkedTo(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        ForEachActiveGoal(goal => goal.NPCTalkedTo(npcName));
    }

    private void ForEachActiveGoal(Action<QuestGoal> action)
    {
        Quest[] activeSnapshot = activeQuests.ToArray();

        foreach (Quest quest in activeSnapshot)
        {
            if (quest == null || quest.state != QuestState.Active || quest.goals == null) continue;

            foreach (QuestGoal goal in quest.goals)
            {
                if (goal != null) action(goal);
            }

            CheckQuestCompletion(quest);
        }
    }

    private void EvaluateRetroactiveGoals(Quest quest)
    {
        if (quest.goals == null) return;

        foreach (QuestGoal goal in quest.goals)
        {
            if (goal == null) continue;

            if (goal.goalType == GoalType.Gathering && goal.targetItem != null && Inventory.Instance != null)
            {
                int existingCount = Inventory.Instance.GetItemCount(goal.targetItem);
                goal.currentAmount = Mathf.Min(existingCount, goal.requiredAmount);
            }
            /*else if (goal.goalType == GoalType.TalkToNPC)
            {
                bool alreadyMet = (goal.targetNPC != null && (StoryStateManager.HasMetNPC(goal.targetNPC) || StoryStateManager.CheckConditions($"met_{goal.targetNPC.npcName}"))) ||
                                  (!string.IsNullOrEmpty(goal.targetNPCName) && StoryStateManager.CheckConditions($"met_{goal.targetNPCName}"));

                goal.currentAmount = alreadyMet ? goal.requiredAmount : 0;
            }*/
        }
    }

    public void CheckQuestCompletion(Quest quest)
    {
        if (quest == null || quest.state != QuestState.Active) return;

        string qIdentifier = GetQuestIdentifier(quest);
        string readyFlag = $"Quest_{qIdentifier}_Ready";

        bool wasReady = StoryStateManager.CheckConditions(readyFlag);
        bool isReadyNow = quest.IsCompleted();

        StoryStateManager.SetFlag(readyFlag, isReadyNow);

        if (wasReady != isReadyNow)
        {
            if (isReadyNow) OnQuestReadyToTurnIn?.Invoke(quest);
            OnQuestsUpdated?.Invoke();
        }
    }

    private void HandleDialogueEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (eventName.StartsWith("AcceptQuest_"))
        {
            AcceptQuest(eventName.Replace("AcceptQuest_", "").Trim());
        }
        else if (eventName.StartsWith("CompleteQuest_"))
        {
            CompleteQuest(eventName.Replace("CompleteQuest_", "").Trim());
        }
        else if (eventName.StartsWith("FailQuest_"))
        {
            FailQuest(eventName.Replace("FailQuest_", "").Trim());
        }
    }

    [Serializable]
    public class QuestSaveEntry
    {
        public string questName;
        public QuestState state;
        public float timeRemaining;
        public List<int> goalAmounts = new List<int>();
    }

    public List<QuestSaveEntry> GetSaveData()
    {
        List<QuestSaveEntry> saveData = new List<QuestSaveEntry>();

        foreach (var kvp in runtimeQuestMap)
        {
            Quest q = kvp.Value;
            if (q == null || q.state == QuestState.Unassigned) continue;

            QuestSaveEntry entry = new QuestSaveEntry
            {
                questName = q.questName,
                state = q.state,
                timeRemaining = q.timeRemaining
            };

            if (q.goals != null)
            {
                foreach (var goal in q.goals)
                {
                    entry.goalAmounts.Add(goal != null ? goal.currentAmount : 0);
                }
            }

            saveData.Add(entry);
        }

        return saveData;
    }

    public void LoadSaveData(List<QuestSaveEntry> saveData)
    {
        InitializeRuntimeQuests();

        if (saveData == null) return;

        foreach (var entry in saveData)
        {
            Quest q = FindQuest(entry.questName);
            if (q == null) continue;

            q.state = entry.state;
            q.timeRemaining = entry.timeRemaining;

            if (q.goals != null && entry.goalAmounts != null)
            {
                for (int i = 0; i < Mathf.Min(q.goals.Count, entry.goalAmounts.Count); i++)
                {
                    if (q.goals[i] != null)
                    {
                        q.goals[i].currentAmount = entry.goalAmounts[i];
                    }
                }
            }

            if (q.state == QuestState.Active)
            {
                activeQuests.Add(q);
                CheckQuestCompletion(q);
            }
            else if (q.state == QuestState.Completed)
            {
                completedQuests.Add(q);
            }
        }

        if (activeQuests.Count > 0)
        {
            SetTrackedQuest(activeQuests[0]);
        }

        OnQuestsUpdated?.Invoke();
    }

    public Quest FindQuest(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return null;

        if (runtimeQuestMap.TryGetValue(identifier, out Quest found)) return found;

        string cleanIdentifier = identifier.Replace("(Clone)", "").Trim();
        foreach (var q in runtimeQuestMap.Values)
        {
            if (q == null) continue;

            string questNameClean = q.questName.Replace("(Clone)", "").Trim();
            string objNameClean = q.name.Replace("(Clone)", "").Trim();

            if (questNameClean.Equals(cleanIdentifier, StringComparison.OrdinalIgnoreCase) ||
                objNameClean.Equals(cleanIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return q;
            }
        }

        return null;
    }

    public void SetTrackedQuest(Quest quest)
    {
        if (quest != null && quest.state != QuestState.Active) return;

        if (TrackedQuest != quest)
        {
            TrackedQuest = quest;
            OnTrackedQuestChanged?.Invoke(TrackedQuest);
            OnQuestsUpdated?.Invoke();
        }
    }

    public bool ArePrerequisitesMet(Quest quest)
    {
        if (quest == null || quest.prerequisiteQuest == null) return true;

        Quest prereqRuntime = FindQuest(GetQuestIdentifier(quest.prerequisiteQuest));
        return prereqRuntime != null && prereqRuntime.state == QuestState.Completed;
    }

    public bool IsQuestActive(string identifier)
    {
        Quest q = FindQuest(identifier);
        return q != null && q.state == QuestState.Active && !q.IsCompleted();
    }

    public bool IsQuestReady(string identifier)
    {
        Quest q = FindQuest(identifier);
        if (q != null && q.state == QuestState.Active)
        {
            return q.IsCompleted();
        }

        return StoryStateManager.CheckConditions($"Quest_{identifier}_Ready");
    }

    public bool IsQuestCompleted(string identifier)
    {
        Quest q = FindQuest(identifier);
        if (q != null)
        {
            return q.state == QuestState.Completed;
        }

        return StoryStateManager.CheckConditions($"Quest_{identifier}_Completed");
    }

    public string GetQuestIdentifier(Quest quest)
    {
        if (quest == null) return string.Empty;

        if (!string.IsNullOrWhiteSpace(quest.questName))
            return quest.questName.Trim();

        return quest.name.Replace("(Clone)", "").Trim();
    }
}