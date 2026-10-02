using UnityEngine;

public class AreaInfo : MonoBehaviour
{
    [Header("Area Identity")]
    [SerializeField] private string areaTitle = "The Whispering Woods";
    [SerializeField] private string areaSubtitle = "Danger Level: High";
    [SerializeField] private AudioClip areaChimeOverride;

    [Header("Trigger Mode")]
    [Tooltip("If true, shows the banner automatically as soon as the scene loads.")]
    [SerializeField] private bool showOnSceneStart = true;

    [Tooltip("If true, triggers when the Player walks into this 2D Collider.")]
    [SerializeField] private bool useAsTriggerZone = false;

    private static string currentAreaTitle = "";

    private void Start()
    {
        if (showOnSceneStart)
        {
            TriggerAreaBanner();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (useAsTriggerZone && collision.CompareTag("Player"))
        {
            // Avoid re-triggering if player is already in this zone
            if (currentAreaTitle == areaTitle) return;

            TriggerAreaBanner();
        }
    }

    public void TriggerAreaBanner()
    {
        currentAreaTitle = areaTitle;

        if (AreaSplashBanner.Instance != null)
        {
            AreaSplashBanner.Instance.ShowBanner(areaTitle, areaSubtitle, areaChimeOverride);
        }
    }
}