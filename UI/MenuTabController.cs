using UnityEngine;
using UnityEngine.UI;

public class MenuTabController : MonoBehaviour
{
    [Header("Master Window Container")]
    [Tooltip("The main container GameObject that holds the entire UI menu screen.")]
    [SerializeField] private GameObject menuWindowContainer;

    [Header("Menu Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject saveLoadPanel;

    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button questTabButton;
    [SerializeField] private Button saveLoadTabButton;

    [Header("Tab Visuals")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Pause Game Setting")]
    [Tooltip("If checked, freezes game time when menu is open.")]
    [SerializeField] private bool pauseTimeOnOpen = true;

    private Image inventoryTabImg;
    private Image questTabImg;
    private Image saveLoadTabImg;

    private void Awake()
    {
        // 1. Cache tab images and assign button click listeners
        if (inventoryTabButton != null)
        {
            inventoryTabImg = inventoryTabButton.GetComponent<Image>();
            inventoryTabButton.onClick.AddListener(ShowInventoryTab);
        }

        if (questTabButton != null)
        {
            questTabImg = questTabButton.GetComponent<Image>();
            questTabButton.onClick.AddListener(ShowQuestTab);
        }

        if (saveLoadTabButton != null)
        {
            saveLoadTabImg = saveLoadTabButton.GetComponent<Image>();
            saveLoadTabButton.onClick.AddListener(ShowSaveLoadTab);
        }

        // 2. Initialize default panel states (Inventory open first)
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (questPanel != null) questPanel.SetActive(false);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

        // 3. Hide entire menu window on launch
        if (menuWindowContainer != null)
        {
            menuWindowContainer.SetActive(false);
        }
    }

    private void Update()
    {
        // Global toggle keybind (Tab or Escape)
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenuWindow();
        }
    }

    public void ToggleMenuWindow()
    {
        if (menuWindowContainer == null) return;

        bool isOpening = !menuWindowContainer.activeSelf;

        // Reset to Inventory tab every time the window is opened
        if (isOpening)
        {
            ShowInventoryTab();
        }

        menuWindowContainer.SetActive(isOpening);

        if (pauseTimeOnOpen)
        {
            Time.timeScale = isOpening ? 0f : 1f;
        }
    }

    public void ShowInventoryTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (questPanel != null) questPanel.SetActive(false);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

        SetTabVisuals(inventoryTabButton);
    }

    public void ShowQuestTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(true);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

        SetTabVisuals(questTabButton);
    }

    public void ShowSaveLoadTab()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(true);

        SetTabVisuals(saveLoadTabButton);        
    }

    /// <summary>
    /// Cleanly updates scale and tint for all 3 tabs dynamically.
    /// </summary>
    private void SetTabVisuals(Button activeBtn)
    {
        // Reset all tabs to inactive visual state
        ResetTab(inventoryTabButton, inventoryTabImg);
        ResetTab(questTabButton, questTabImg);
        ResetTab(saveLoadTabButton, saveLoadTabImg);

        // Highlight active tab
        if (activeBtn == inventoryTabButton) HighlightTab(inventoryTabButton, inventoryTabImg);
        else if (activeBtn == questTabButton) HighlightTab(questTabButton, questTabImg);
        else if (activeBtn == saveLoadTabButton) HighlightTab(saveLoadTabButton, saveLoadTabImg);
    }

    private void HighlightTab(Button btn, Image img)
    {
        if (img != null) img.color = activeTabColor;
        if (btn != null) btn.transform.localScale = Vector3.one * 1.05f;
    }

    private void ResetTab(Button btn, Image img)
    {
        if (img != null) img.color = inactiveTabColor;
        if (btn != null) btn.transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        if (pauseTimeOnOpen)
        {
            Time.timeScale = 1f;
        }
    }
}