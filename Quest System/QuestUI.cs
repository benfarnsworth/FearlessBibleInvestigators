using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class QuestUI : MonoBehaviour
{
    private class QuestEntryHolder
    {
        public GameObject gameObject;
        public TMP_Text titleText;
        public TMP_Text progressText;
        public Button button;
        public Image backgroundImage;
    }

    [Header("Mode Configuration")]
    [SerializeField] private bool isHUDTracker = false;

    [Header("Quest List Container")]
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private Transform entryContainer;

    [Header("UI Icons & Formatting")]
    [Tooltip("Drag your custom TMP Sprite Asset here to guarantee icons resolve.")]
    [SerializeField] private TMP_SpriteAsset customSpriteAsset;
    [Tooltip("Prefix applied when a quest is actively tracked.")]
    [SerializeField] private string trackedPrefix = "<sprite=122> ";
    [Tooltip("Prefix applied to the currently highlighted entry in menu mode.")]
    [SerializeField] private string selectedPrefix = "> ";
    [Tooltip("Suffix added when a quest is ready for turn-in.")]
    [SerializeField] private string readySuffix = " <color=#55FF55>[READY]</color>";
    [Tooltip("Color tint applied to completed quest text.")]
    [SerializeField] private Color completedTextColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [Tooltip("Icon or sprite tag for incomplete goals.")]
    [SerializeField] private string goalIncompleteIcon = "<color=#888888>[ ]</color>";
    [Tooltip("Icon or sprite tag for completed goals.")]
    [SerializeField] private string goalCompletedIcon = "<color=#55FF55>[X]</color>";

    [Header("Selected Quest Details Panel (Menu Mode Only)")]
    [SerializeField] private CanvasGroup detailsPanelGroup;
    [SerializeField] private TMP_Text selectedTitleText;
    [SerializeField] private TMP_Text selectedDescriptionText;
    [SerializeField] private TMP_Text selectedProgressText;

    [Header("Selection Highlighting (Menu Mode Only)")]
    [SerializeField] private Color defaultEntryColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    [SerializeField] private Color selectedEntryColor = new Color(0.25f, 0.45f, 0.75f, 0.9f);

    [Header("Action Controls (Menu Mode Only)")]
    [SerializeField] private Button trackQuestButton;
    [SerializeField] private TMP_Text trackButtonText;
    [SerializeField] private Button quitQuestButton;
    [SerializeField] private Button toggleCompletedButton;
    [SerializeField] private TMP_Text toggleCompletedButtonText;

    private readonly Dictionary<Quest, QuestEntryHolder> spawnedEntries = new Dictionary<Quest, QuestEntryHolder>();
    private readonly StringBuilder stringBuilder = new StringBuilder(256);
    private readonly List<Quest> keysToRemove = new List<Quest>();

    private Quest selectedQuest;
    private bool showCompletedQuests = true;
    private bool isDirty = false;

    private void OnEnable()
    {
        QuestManager.OnQuestsUpdated += RequestUIUpdate;
        QuestManager.OnQuestTimerTick += OnTimerTick;
        GoalArea.OnHoldTick += OnTimerTick;
        QuestManager.OnTrackedQuestChanged += RequestUIUpdate;

        if (trackQuestButton != null) trackQuestButton.onClick.AddListener(OnTrackButtonClicked);
        if (quitQuestButton != null) quitQuestButton.onClick.AddListener(OnQuitButtonClicked);
        if (toggleCompletedButton != null) toggleCompletedButton.onClick.AddListener(OnToggleCompletedClicked);

        RequestUIUpdate();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestsUpdated -= RequestUIUpdate;
        QuestManager.OnQuestTimerTick -= OnTimerTick;
        GoalArea.OnHoldTick -= OnTimerTick;
        QuestManager.OnTrackedQuestChanged -= RequestUIUpdate;

        if (trackQuestButton != null) trackQuestButton.onClick.RemoveListener(OnTrackButtonClicked);
        if (quitQuestButton != null) quitQuestButton.onClick.RemoveListener(OnQuitButtonClicked);
        if (toggleCompletedButton != null) toggleCompletedButton.onClick.RemoveListener(OnToggleCompletedClicked);
    }

    private void LateUpdate()
    {
        if (isDirty)
        {
            isDirty = false;
            PerformFullRefresh();
        }
    }

    public void RefreshAllUI()
    {
        RequestUIUpdate();
    }

    private void RequestUIUpdate()
    {
        isDirty = true;
    }

    private void RequestUIUpdate(Quest unused)
    {
        isDirty = true;
    }

    private void PerformFullRefresh()
    {
        UpdateQuestList();
        UpdateDetailsPanel();
    }

    private void OnTimerTick()
    {
        if (isHUDTracker)
        {
            Quest tracked = GetActiveHUDQuest();
            if (tracked != null && tracked.state == QuestState.Active)
            {
                if (spawnedEntries.TryGetValue(tracked, out QuestEntryHolder holder) && holder != null)
                {
                    BindEntryUI(holder, tracked);
                }
            }
        }
        else if (selectedQuest != null && selectedQuest.state == QuestState.Active)
        {
            if (selectedProgressText != null)
            {
                string newText = FormatGoalProgress(selectedQuest);
                if (selectedProgressText.text != newText)
                {
                    selectedProgressText.text = newText;
                }
            }

            if (spawnedEntries.TryGetValue(selectedQuest, out QuestEntryHolder holder) && holder != null)
            {
                BindEntryUI(holder, selectedQuest);
            }
        }
    }

    private void UpdateQuestList()
    {
        if (QuestManager.Instance == null || entryContainer == null || questEntryPrefab == null) return;

        List<Quest> questsToDisplay = GetQuestsToDisplay();
        bool hierarchyChanged = false;

        for (int i = 0; i < questsToDisplay.Count; i++)
        {
            Quest quest = questsToDisplay[i];
            if (quest == null) continue;

            if (!spawnedEntries.TryGetValue(quest, out QuestEntryHolder holder) || holder == null || holder.gameObject == null)
            {
                GameObject instance = Instantiate(questEntryPrefab, entryContainer);
                hierarchyChanged = true;
                
                holder = new QuestEntryHolder
                {
                    gameObject = instance,
                    backgroundImage = instance.GetComponentInChildren<Image>()
                };

                TMP_Text[] textComponents = instance.GetComponentsInChildren<TMP_Text>();
                if (textComponents.Length > 0) holder.titleText = textComponents[0];
                if (textComponents.Length > 1) holder.progressText = textComponents[1];

                if (holder.titleText != null)
                {
                    holder.titleText.richText = true;
                    if (customSpriteAsset != null) holder.titleText.spriteAsset = customSpriteAsset;
                }

                if (holder.progressText != null)
                {
                    holder.progressText.richText = true;
                    if (customSpriteAsset != null) holder.progressText.spriteAsset = customSpriteAsset;
                }

                holder.button = instance.GetComponent<Button>();
                Quest capturedQuest = quest;

                if (holder.button != null)
                {
                    holder.button.onClick.AddListener(() => SelectQuest(capturedQuest));
                }
                else if (!isHUDTracker)
                {
                    EventTrigger trigger = instance.GetComponent<EventTrigger>();
                    if (trigger == null) trigger = instance.AddComponent<EventTrigger>();

                    EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    clickEntry.callback.AddListener((_) => SelectQuest(capturedQuest));
                    trigger.triggers.Add(clickEntry);
                }

                spawnedEntries[quest] = holder;
            }

            // Only reorder if the sibling index actually changed
            if (holder.gameObject.transform.GetSiblingIndex() != i)
            {
                holder.gameObject.transform.SetSiblingIndex(i);
                hierarchyChanged = true;
            }

            BindEntryUI(holder, quest);
        }

        keysToRemove.Clear();
        foreach (var entry in spawnedEntries)
        {
            if (!questsToDisplay.Contains(entry.Key))
            {
                if (entry.Value != null && entry.Value.gameObject != null)
                {
                    Destroy(entry.Value.gameObject);
                    hierarchyChanged = true;
                }
                keysToRemove.Add(entry.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            spawnedEntries.Remove(keysToRemove[i]);
        }

        if (!isHUDTracker)
        {
            if (selectedQuest == null || !questsToDisplay.Contains(selectedQuest))
            {
                SelectQuest(questsToDisplay.Count > 0 ? questsToDisplay[0] : null);
            }
        }

        if (hierarchyChanged)
        {
            RebuildLayout(entryContainer);
        }
    }

    private List<Quest> GetQuestsToDisplay()
    {
        List<Quest> list = new List<Quest>();

        if (isHUDTracker)
        {
            Quest tracked = GetActiveHUDQuest();
            if (tracked != null && tracked.state == QuestState.Active)
            {
                list.Add(tracked);
            }
        }
        else
        {
            if (QuestManager.Instance != null)
            {
                list.AddRange(QuestManager.Instance.activeQuests);
                if (showCompletedQuests) list.AddRange(QuestManager.Instance.completedQuests);
            }

            list.Sort((a, b) =>
            {
                int GetPriority(Quest q)
                {
                    if (q == null) return 3;

                    bool isCompleted = q.state == QuestState.Completed || 
                                        (QuestManager.Instance != null && QuestManager.Instance.completedQuests.Contains(q));
                    if (isCompleted) return 2;

                    if (IsQuestReadyToTurnIn(q)) return 0;

                    return 1;
                }

                return GetPriority(a).CompareTo(GetPriority(b));
            });
        }

        return list;
    }

    private Quest GetActiveHUDQuest()
    {
        if (QuestManager.Instance == null) return null;

        Quest tracked = QuestManager.Instance.TrackedQuest;
        if (tracked == null && QuestManager.Instance.activeQuests.Count > 0)
        {
            tracked = QuestManager.Instance.activeQuests[0];
        }
        return tracked;
    }

    private bool IsQuestReadyToTurnIn(Quest quest)
    {
        if (quest == null) return false;

        if (StoryStateManager.GetFlag($"Quest_{quest.questName}_Ready")) return true;

        if (quest.state == QuestState.Active && quest.goals != null && quest.goals.Count > 0)
        {
            for (int i = 0; i < quest.goals.Count; i++)
            {
                if (quest.goals[i] != null && !quest.goals[i].isCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    private void BindEntryUI(QuestEntryHolder holder, Quest quest)
    {
        bool isCompleted = quest.state == QuestState.Completed || 
                           (QuestManager.Instance != null && QuestManager.Instance.completedQuests.Contains(quest));
        bool isReady = !isCompleted && IsQuestReadyToTurnIn(quest);
        bool isTracked = QuestManager.Instance != null && QuestManager.Instance.TrackedQuest == quest;
        bool isSelected = !isHUDTracker && quest == selectedQuest;

        string title = quest.DisplayTitle;
        string currentTrackPrefix = isTracked ? trackedPrefix : "";
        string currentSelectPrefix = isSelected ? selectedPrefix : "";

        if (!isHUDTracker && holder.backgroundImage != null)
        {
            Color targetColor = isSelected ? selectedEntryColor : defaultEntryColor;
            if (holder.backgroundImage.color != targetColor)
            {
                holder.backgroundImage.color = targetColor;
            }
        }

        if (isHUDTracker)
        {
            string goalSummary = FormatGoalProgress(quest);

            if (holder.progressText != null)
            {
                if (!holder.progressText.gameObject.activeSelf)
                    holder.progressText.gameObject.SetActive(true);

                string formattedTitle = currentTrackPrefix + title;
                if (holder.titleText != null && holder.titleText.text != formattedTitle)
                    holder.titleText.text = formattedTitle;

                if (holder.progressText.text != goalSummary)
                    holder.progressText.text = goalSummary;
            }
            else if (holder.titleText != null)
            {
                string combinedText = $"{currentTrackPrefix}{title}\n{goalSummary}";
                if (holder.titleText.text != combinedText)
                    holder.titleText.text = combinedText;
            }
            return;
        }

        if (holder.progressText != null && holder.progressText.gameObject.activeSelf)
        {
            holder.progressText.gameObject.SetActive(false);
        }

        if (holder.titleText != null)
        {
            string newTitleText;
            if (isCompleted)
            {
                string hexColor = ColorUtility.ToHtmlStringRGBA(completedTextColor);
                newTitleText = $"<color=#{hexColor}><s>[DONE] {title}</s></color>";
            }
            else if (isReady)
            {
                newTitleText = $"{currentSelectPrefix}{currentTrackPrefix}{title}{readySuffix}";
            }
            else
            {
                newTitleText = $"{currentSelectPrefix}{currentTrackPrefix}{title}";
            }

            if (holder.titleText.text != newTitleText)
            {
                holder.titleText.text = newTitleText;
            }
        }
    }

    public void SelectQuest(Quest quest)
    {
        selectedQuest = quest;
        UpdateDetailsPanel();

        if (!isHUDTracker)
        {
            foreach (var entry in spawnedEntries)
            {
                if (entry.Value != null)
                {
                    BindEntryUI(entry.Value, entry.Key);
                }
            }
        }
    }

    private void UpdateDetailsPanel()
    {
        if (isHUDTracker) return;

        if (selectedQuest == null)
        {
            if (detailsPanelGroup != null)
            {
                detailsPanelGroup.alpha = 0f;
                detailsPanelGroup.interactable = false;
                detailsPanelGroup.blocksRaycasts = false;
            }
            return;
        }

        if (detailsPanelGroup != null)
        {
            detailsPanelGroup.alpha = 1f;
            detailsPanelGroup.interactable = true;
            detailsPanelGroup.blocksRaycasts = true;
        }

        bool isCompleted = selectedQuest.state == QuestState.Completed || 
                           (QuestManager.Instance != null && QuestManager.Instance.completedQuests.Contains(selectedQuest));
        bool isTracked = QuestManager.Instance != null && QuestManager.Instance.TrackedQuest == selectedQuest;

        if (selectedTitleText != null && selectedTitleText.text != selectedQuest.DisplayTitle)
            selectedTitleText.text = selectedQuest.DisplayTitle;

        if (selectedDescriptionText != null && selectedDescriptionText.text != selectedQuest.description)
            selectedDescriptionText.text = selectedQuest.description;

        if (selectedProgressText != null)
        {
            selectedProgressText.richText = true;
            if (customSpriteAsset != null) selectedProgressText.spriteAsset = customSpriteAsset;
            string progressText = FormatGoalProgress(selectedQuest);
            if (selectedProgressText.text != progressText)
                selectedProgressText.text = progressText;
        }

        if (trackQuestButton != null)
        {
            trackQuestButton.interactable = !isCompleted && !isTracked;
            if (trackButtonText != null) 
                trackButtonText.text = isCompleted ? "Completed" : (isTracked ? "Currently Active" : "Track Quest");
        }

        if (quitQuestButton != null) 
            quitQuestButton.interactable = !isCompleted && selectedQuest.state == QuestState.Active;

        if (toggleCompletedButtonText != null) 
            toggleCompletedButtonText.text = showCompletedQuests ? "Hide Completed" : "Show Completed";
    }

    private void OnTrackButtonClicked()
    {
        if (selectedQuest == null || QuestManager.Instance == null) return;
        QuestManager.Instance.SetTrackedQuest(selectedQuest);
    }

    private void OnQuitButtonClicked()
    {
        if (selectedQuest == null || QuestManager.Instance == null) return;
        QuestManager.Instance.QuitQuest(selectedQuest);
        selectedQuest = null;
    }

    private void OnToggleCompletedClicked()
    {
        showCompletedQuests = !showCompletedQuests;
        RequestUIUpdate();
    }

    private string FormatGoalProgress(Quest quest)
    {
        if (quest == null) return string.Empty;

        stringBuilder.Clear();

        bool isReadyOrDone = quest.state == QuestState.Completed || IsQuestReadyToTurnIn(quest);

        // Suppress overall timer if quest is completed or ready to turn in
        if (!isReadyOrDone && quest.isTimed && quest.state == QuestState.Active)
        {
            int totalSecs = Mathf.Max(0, Mathf.CeilToInt(quest.timeRemaining));
            int mins = totalSecs / 60;
            int secs = totalSecs % 60;
            stringBuilder.Append("<color=#FF5555>⏱ Overall Time: ")
                         .Append(mins.ToString("D2"))
                         .Append(":")
                         .Append(secs.ToString("D2"))
                         .AppendLine("</color>");
        }

        // Suppress hold timers if quest is completed, ready to turn in, or goal already met
        if (!isReadyOrDone && GoalArea.ActiveAreas != null)
        {
            for (int i = 0; i < GoalArea.ActiveAreas.Count; i++)
            {
                var area = GoalArea.ActiveAreas[i];
                if (area != null && area.IsHolding && IsAreaActiveForUncompletedGoal(area, quest))
                {
                    int secondsLeft = Mathf.CeilToInt(area.RemainingHoldTime);
                    stringBuilder.Append("<color=#FFCC00>⏱ Hold Pen: ")
                                 .Append(secondsLeft)
                                 .AppendLine("s</color>");
                    break;
                }
            }
        }

        if (quest.goals != null)
        {
            for (int i = 0; i < quest.goals.Count; i++)
            {
                QuestGoal goal = quest.goals[i];
                if (goal != null)
                {
                    stringBuilder.AppendLine(goal.GetProgressText(goalIncompleteIcon, goalCompletedIcon));
                }
            }
        }

        return stringBuilder.ToString().TrimEnd('\r', '\n');
    }

    private bool IsAreaActiveForUncompletedGoal(GoalArea area, Quest quest)
    {
        if (area == null || quest == null) return false;

        if (quest.goals != null)
        {
            for (int i = 0; i < quest.goals.Count; i++)
            {
                var goal = quest.goals[i];
                // Only consider active if the goal matches AND is not already complete
                if (goal != null && !goal.isCompleted && string.Equals(goal.goalAreaID, area.GoalAreaID, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RebuildLayout(Transform container)
    {
        if (container == null) return;
        if (container is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}