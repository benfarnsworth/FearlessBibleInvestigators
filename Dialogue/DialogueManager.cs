using System;
using UnityEngine;
using Yarn.Unity;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Yarn References")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public static event Action OnDialogueOpened;
    public static event Action OnDialogueClosed;
    public static event Action<string> OnDialogueEventTriggered;

    public static bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure root manager persists across scenes
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Fallback lookup if not assigned manually in Inspector
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.AddListener(OnYarnDialogueStarted);
            dialogueRunner.onDialogueComplete.AddListener(OnYarnDialogueEnded);
        }
    }

    private void OnDestroy()
    {
        // Clear dead static reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }

        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(OnYarnDialogueStarted);
            dialogueRunner.onDialogueComplete.RemoveListener(OnYarnDialogueEnded);
        }
    }

    private void OnYarnDialogueStarted()
    {
        IsDialogueActive = true;
        OnDialogueOpened?.Invoke();
    }

    private void OnYarnDialogueEnded()
    {
        IsDialogueActive = false;
        OnDialogueClosed?.Invoke();
    }

    public static void TriggerEvent(string eventName)
    {
        OnDialogueEventTriggered?.Invoke(eventName);
    }
}