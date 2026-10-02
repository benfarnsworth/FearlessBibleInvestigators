using UnityEngine;
using TMPro;

public class QuestTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void OnEnable()
    {
        QuestManager.OnQuestTimerTick += UpdateTimerUI;
        QuestManager.OnTrackedQuestChanged += HandleTrackedQuestChanged;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestTimerTick -= UpdateTimerUI;
        QuestManager.OnTrackedQuestChanged -= HandleTrackedQuestChanged;
    }

    private void Start()
    {
        UpdateTimerUI();
    }
    
    private void HandleTrackedQuestChanged(Quest q)
    {
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        Quest tracked = QuestManager.Instance != null ? QuestManager.Instance.TrackedQuest : null;

        if (tracked != null && tracked.isTimed && tracked.state == QuestState.Active)
        {
            if (timerText != null)
            {
                if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
                
                int minutes = Mathf.FloorToInt(tracked.timeRemaining / 60F);
                int seconds = Mathf.FloorToInt(tracked.timeRemaining % 60F);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
        else
        {
            if (timerText != null && timerText.gameObject.activeSelf)
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }
}