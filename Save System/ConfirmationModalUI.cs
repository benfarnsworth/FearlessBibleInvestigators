using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationModalUI : MonoBehaviour
{
    public static ConfirmationModalUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmCallback;

    public bool IsOpen => modalPanel != null && modalPanel.activeSelf;

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

        if (modalPanel != null) modalPanel.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void Show(string message, Action onConfirm)
    {
        if (messageText != null) messageText.text = message;
        onConfirmCallback = onConfirm;

        if (modalPanel != null) modalPanel.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke();
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }

    public void Hide()
    {
        onConfirmCallback = null;
        if (modalPanel != null) modalPanel.SetActive(false);
    }
}