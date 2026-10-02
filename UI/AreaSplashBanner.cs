using System.Collections;
using TMPro;
using UnityEngine;

public class AreaSplashBanner : MonoBehaviour
{
    public static AreaSplashBanner Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Optional SFX")]
    [SerializeField] private AudioClip defaultAreaChime;

    private Coroutine bannerCoroutine;

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

        // Hide banner immediately on load
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void ShowBanner(string title, string subtitle = "", AudioClip chime = null)
    {
        // Cancel active animation if entering a new zone rapidly
        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
        }

        bannerCoroutine = StartCoroutine(BannerRoutine(title, subtitle, chime));
    }

    private IEnumerator BannerRoutine(string title, string subtitle, AudioClip chime)
    {
        // 1. Populate UI text
        if (titleText != null) titleText.text = title;

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }

        // 2. Play Area Arrival Chime
        AudioClip clipToPlay = chime != null ? chime : defaultAreaChime;
        if (clipToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clipToPlay, 1.0f, 1.0f);
        }

        // 3. Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 4. Hold
        yield return new WaitForSeconds(displayDuration);

        // 5. Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}