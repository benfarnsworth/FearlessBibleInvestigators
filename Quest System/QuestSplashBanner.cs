using System.Collections;
using UnityEngine;
using TMPro;

public class QuestSplashBanner : MonoBehaviour
{
    public static QuestSplashBanner Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject bannerContainer;
    [SerializeField] private TMP_Text headerText;     // e.g. "QUEST ACCEPTED" or "OBJECTIVE COMPLETE!"
    [SerializeField] private TMP_Text questTitleText; // e.g. "Pete Craves Apples!"
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip questAcceptedSFX;
    [SerializeField] private AudioClip questReadySFX;    // Plays when objectives are complete
    [SerializeField] private AudioClip questCompletedSFX;
    [SerializeField] private AudioClip questFailedSFX;   // NEW: Plays when quest fails

    [Header("Particle FX")]
    [SerializeField] private ParticleSystem confettiParticles;

    [Header("Timing & Animation")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeSpeed = 4f;

    private Coroutine bannerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (bannerContainer != null) bannerContainer.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        // Listen to QuestManager events
        QuestManager.OnQuestAccepted += ShowQuestAccepted;
        QuestManager.OnQuestReadyToTurnIn += ShowQuestReady;
        QuestManager.OnQuestCompleted += ShowQuestCompleted;
        QuestManager.OnQuestFailed += ShowQuestFailed; // NEW: Hook into failure event
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        QuestManager.OnQuestAccepted -= ShowQuestAccepted;
        QuestManager.OnQuestReadyToTurnIn -= ShowQuestReady;
        QuestManager.OnQuestCompleted -= ShowQuestCompleted;
        QuestManager.OnQuestFailed -= ShowQuestFailed;
    }

    /// <summary>
    /// Triggered automatically via QuestManager.OnQuestAccepted
    /// </summary>
    public void ShowQuestAccepted(Quest quest)
    {
        if (quest == null) return;
        TriggerBanner("QUEST ACCEPTED!", quest.DisplayTitle, new Color(1f, 0.85f, 0f), questAcceptedSFX, false);
    }

    /// <summary>
    /// Triggered automatically when objectives are done and quest is ready to turn in
    /// </summary>
    public void ShowQuestReady(Quest quest)
    {
        if (quest == null) return;
        // Uses Cyan / Light Blue color to distinguish from Accepted (Yellow) and Completed (Green)
        TriggerBanner("OBJECTIVE COMPLETE!", quest.DisplayTitle, new Color(0.2f, 0.85f, 1f), questReadySFX, false);
    }

    /// <summary>
    /// Triggered automatically via QuestManager.OnQuestCompleted
    /// </summary>
    public void ShowQuestCompleted(Quest quest)
    {
        if (quest == null) return;
        TriggerBanner("QUEST COMPLETED!", quest.DisplayTitle, new Color(0.3f, 1f, 0.4f), questCompletedSFX, true);
    }

    /// <summary>
    /// Triggered automatically via QuestManager.OnQuestFailed
    /// </summary>
    public void ShowQuestFailed(Quest quest)
    {
        if (quest == null) return;
        // Uses Bright Red color for failure
        TriggerBanner("QUEST FAILED!", quest.DisplayTitle, new Color(1f, 0.25f, 0.25f), questFailedSFX, false);
    }

    private void TriggerBanner(string header, string title, Color headerColor, AudioClip sfx, bool triggerConfetti)
    {
        if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
        bannerCoroutine = StartCoroutine(BannerRoutine(header, title, headerColor, sfx, triggerConfetti));
    }

    private IEnumerator BannerRoutine(string header, string title, Color headerColor, AudioClip sfx, bool triggerConfetti)
    {
        if (bannerContainer == null || canvasGroup == null) yield break;

        // Set Text & Styling
        if (headerText != null)
        {
            headerText.text = header;
            headerText.color = headerColor;
        }

        if (questTitleText != null)
        {
            questTitleText.text = title;
        }

        bannerContainer.SetActive(true);

        // Play SFX
        if (sfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfx);
        }

        // Play Confetti
        if (triggerConfetti && confettiParticles != null)
        {
            confettiParticles.Stop();
            confettiParticles.Play();
        }

        // Fade In
        float time = 0f;
        while (time < 1f)
        {
            time += Time.unscaledDeltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time);
            yield return null;
        }

        // Hold display
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade Out
        time = 0f;
        while (time < 1f)
        {
            time += Time.unscaledDeltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time);
            yield return null;
        }

        bannerContainer.SetActive(false);
    }
}