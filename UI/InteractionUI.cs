using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject promptContainer; // Background Panel / Container
    [SerializeField] private TMP_Text promptText;         // The TextMeshPro element

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

        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        if (promptContainer != null && !promptContainer.activeSelf)
        {
            promptContainer.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = $"[E] {message}";
        }
    }

    public void HidePrompt()
    {
        if (promptContainer != null)
        {
            promptContainer.SetActive(false);
        }
    }
}