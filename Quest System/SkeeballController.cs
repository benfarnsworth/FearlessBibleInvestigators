using System.Collections.Generic;
using UnityEngine;

public class SkeeballController : MonoBehaviour
{
    [Header("Quest Integration")]
    [Tooltip("Drag and drop the associated Quest SO. Leaves area disabled if quest is not active.")]
    [SerializeField] private Quest associatedQuest;

    [Tooltip("MUST match the 'Goal Area ID' in your Quest Goal ScriptableObject!")]
    [SerializeField] private string scoreID = "RiverCurling";
    [SerializeField] private Item targetItem;

    private Dictionary<GameObject, int> activeBallScores = new Dictionary<GameObject, int>();
    private ScoreZone[] childScoreZones;
    private Collider2D[] childColliders;
    private bool hasTriggeredComplete = false;

    public int CurrentScore { get; private set; }
    public bool IsGameLocked { get; private set; }

    private void Awake()
    {
        childScoreZones = GetComponentsInChildren<ScoreZone>(true);
        childColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void OnEnable()
    {
        QuestManager.OnQuestsUpdated += EvaluateQuestState;
        QuestManager.OnMissionReset += HandleMissionReset;
        EvaluateQuestState();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestsUpdated -= EvaluateQuestState;
        QuestManager.OnMissionReset -= HandleMissionReset;
    }

    private void HandleMissionReset(string questName)
    {
        if (associatedQuest != null && associatedQuest.questName.Equals(questName, System.StringComparison.OrdinalIgnoreCase))
        {
            ResetGame();
        }
    }

    public void EvaluateQuestState()
    {
        if (associatedQuest == null || QuestManager.Instance == null)
        {
            IsGameLocked = true;
            SetMinigameState(false, GoalState.Inactive);
            return;
        }

        Quest runtimeQuest = QuestManager.Instance.FindQuest(associatedQuest.questName);

        if (runtimeQuest == null || runtimeQuest.state == QuestState.Unassigned || runtimeQuest.state == QuestState.Completed)
        {
            IsGameLocked = true;
            hasTriggeredComplete = false;
            SetMinigameState(false, GoalState.Inactive);
            return;
        }

        if (runtimeQuest.state == QuestState.Active)
        {
            if (runtimeQuest.IsCompleted())
            {
                IsGameLocked = true; 
                SetCollidersEnabled(false);

                if (!hasTriggeredComplete)
                {
                    hasTriggeredComplete = true;
                    SetVisualStates(GoalState.MissionComplete);
                }
            }
            else
            {
                IsGameLocked = false;
                SetMinigameState(true, GoalState.Active);
            }
        }
    }

    private void SetMinigameState(bool triggersEnabled, GoalState visualState)
    {
        SetCollidersEnabled(triggersEnabled);
        SetVisualStates(visualState);
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        if (childColliders != null)
        {
            foreach (var col in childColliders)
            {
                if (col != null) col.enabled = enabledState;
            }
        }
    }

    private void SetVisualStates(GoalState state)
    {
        if (childScoreZones != null)
        {
            foreach (var zone in childScoreZones)
            {
                if (zone != null)
                {
                    GoalAreaVisuals v = zone.GetVisualHandler();
                    if (v != null) v.SetState(state);
                }
            }
        }
    }

    public void AddPoints(int points, GameObject scoredObject, ScoreZone.ScoreBehavior behavior)
    {
        if (scoredObject == null || IsGameLocked) return;

        int currentBallValue = activeBallScores.ContainsKey(scoredObject) ? activeBallScores[scoredObject] : 0;

        if (points > currentBallValue)
        {
            int pointDifference = points - currentBallValue;
            activeBallScores[scoredObject] = points;
            
            CurrentScore += pointDifference;
            Debug.Log($"[{scoreID}] Score upgraded to {points} Pts (+{pointDifference})! Total Score: {CurrentScore}");

            UpdateQuestProgress();
            ApplyScoreBehavior(scoredObject, behavior);
        }
    }

    public void RemovePoints(int points, GameObject exitedObject)
    {
        if (exitedObject == null || IsGameLocked || !activeBallScores.ContainsKey(exitedObject)) return;

        int currentBallValue = activeBallScores[exitedObject];
        
        if (points >= currentBallValue)
        {
            CurrentScore = Mathf.Max(0, CurrentScore - currentBallValue);
            activeBallScores.Remove(exitedObject);
            Debug.Log($"[{scoreID}] Score reduced (-{currentBallValue} Pts)! Total Score: {CurrentScore}");
        }

        UpdateQuestProgress();
    }

    private void UpdateQuestProgress()
    {
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(scoreID))
        {
            QuestManager.Instance.SetScore(scoreID, CurrentScore);
        }
    }

    private void ApplyScoreBehavior(GameObject obj, ScoreZone.ScoreBehavior behavior)
    {
        switch (behavior)
        {
            case ScoreZone.ScoreBehavior.DestroyObject:
                Destroy(obj, 0.3f);
                break;

            case ScoreZone.ScoreBehavior.DisableColliderOnly:
                Collider2D col = obj.GetComponentInChildren<Collider2D>();
                if (col != null) col.enabled = false;
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;
                break;

            case ScoreZone.ScoreBehavior.KeepInPlay:
                break;
        }
    }

    public void ResetGame()
    {
        CurrentScore = 0;
        IsGameLocked = false;
        hasTriggeredComplete = false;
        activeBallScores.Clear();
        
        UpdateQuestProgress();
        EvaluateQuestState();
    }
}