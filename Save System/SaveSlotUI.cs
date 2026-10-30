using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Slot Configuration")]
    [SerializeField] private int slotIndex = 0; // 0, 1, or 2 in Inspector

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI slotInfoText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    private void Awake()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);

        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadClicked);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool hasSave = SaveSystem.HasSaveFile(slotIndex);

        if (hasSave)
        {
            SaveData data = SaveSystem.LoadGame(slotIndex);
            string location = data != null ? data.sceneName : "Saved Game";
            
            if (slotInfoText != null)
                slotInfoText.text = $"Slot {slotIndex + 1}: {location}";

            if (loadButton != null) loadButton.interactable = true;
            if (deleteButton != null) deleteButton.interactable = true;
        }
        else
        {
            if (slotInfoText != null)
                slotInfoText.text = $"Slot {slotIndex + 1}: Empty";

            if (loadButton != null) loadButton.interactable = false;
            if (deleteButton != null) deleteButton.interactable = false;
        }
    }

    private void OnSaveClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCurrentState(slotIndex);
        }
    }

    private void OnLoadClicked()
    {
        if (SaveManager.Instance != null && SaveSystem.HasSaveFile(slotIndex))
        {
            SaveManager.Instance.LoadSavedState(slotIndex);
        }
    }

    private void OnDeleteClicked()
    {
        if (ConfirmationModalUI.Instance == null)
        {
            SaveSystem.DeleteSaveFile(slotIndex);
            RefreshUI();
            return;
        }

        ConfirmationModalUI.Instance.Show(
            $"Are you sure you want to delete Slot {slotIndex + 1}? This cannot be undone.",
            () => 
            {
                SaveSystem.DeleteSaveFile(slotIndex);
                RefreshUI();
            }
        );
    }
}