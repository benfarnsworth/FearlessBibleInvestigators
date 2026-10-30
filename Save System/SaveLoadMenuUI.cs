using System.Collections;
using TMPro;
using UnityEngine;

public class SaveLoadMenuUI : MonoBehaviour
{
    public static SaveLoadMenuUI Instance { get; private set; }

    [Header("Menu Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Modular Slot Management")]
    [SerializeField] private SaveSlotUI[] slotContainers;

    [Header("Notification Feedback")]
    [SerializeField] private GameObject notificationToast;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float notificationDuration = 2.0f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip saveSound;
    [SerializeField] private AudioClip loadSound;

    private Coroutine notificationCoroutine;
    public bool IsMenuOpen { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (notificationToast != null) notificationToast.SetActive(false);
        
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        SaveManager.OnSaveCompleted += HandleSaveCompleted;
        SaveManager.OnLoadCompleted += HandleLoadCompleted;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveCompleted -= HandleSaveCompleted;
        SaveManager.OnLoadCompleted -= HandleLoadCompleted;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        // Block menu toggle during active dialogue
        if (DialogueManager.IsDialogueActive) return;

        // 1. If Confirmation Modal is open, close it first
        if (ConfirmationModalUI.Instance != null && ConfirmationModalUI.Instance.IsOpen)
        {
            ConfirmationModalUI.Instance.Hide();
            return;
        }

        // 2. Otherwise toggle menu state
        if (IsMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        IsMenuOpen = true;
        if (menuPanel != null) menuPanel.SetActive(true);

        Time.timeScale = 0f;
        RefreshAllSlots();
    }

    public void CloseMenu()
    {
        IsMenuOpen = false;
        if (menuPanel != null) menuPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void RefreshAllSlots()
    {
        if (slotContainers == null) return;

        foreach (var slot in slotContainers)
        {
            if (slot != null)
            {
                slot.RefreshUI();
            }
        }
    }

    private void HandleSaveCompleted(int slotIndex)
    {
        if (uiAudioSource != null && saveSound != null)
        {
            uiAudioSource.PlayOneShot(saveSound);
        }

        RefreshAllSlots();
        ShowToast($"Progress saved to Slot {slotIndex + 1}!");
    }

    private void HandleLoadCompleted(int slotIndex)
    {
        if (uiAudioSource != null && loadSound != null)
        {
            uiAudioSource.PlayOneShot(loadSound);
        }

        CloseMenu();
    }

    public void ShowToast(string message)
    {
        if (notificationToast == null || notificationText == null) return;

        notificationText.text = message;
        
        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
        notificationCoroutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        notificationToast.SetActive(true);
        yield return new WaitForSecondsRealtime(notificationDuration);
        notificationToast.SetActive(false);
        notificationCoroutine = null;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}