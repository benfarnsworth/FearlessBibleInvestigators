using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Quick Test Controls")]
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;

    // Events for UI popups ("Game Saved!", etc.)
    public static event Action<int> OnSaveCompleted;
    public static event Action<int> OnLoadCompleted;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (Input.GetKeyDown(saveKey)) SaveCurrentState(0);
        if (Input.GetKeyDown(loadKey)) LoadSavedState(0);
    }

    public void ClearCurrentSession()
    {
        StoryStateManager.LoadFlags(new List<string>());

        if (Inventory.Instance != null)
        {
            Inventory.Instance.LoadSaveData(null);
        }

        if (ItemPersistenceManager.Instance != null)
        {
            ItemPersistenceManager.Instance.LoadState(new SaveData());
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadSaveData(null);
        }
    }

    public void SaveCurrentState(int slotIndex = 0)
    {
        if (isTransitioning) return;

        SaveData data = new SaveData();

        data.sceneName = SceneManager.GetActiveScene().name;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
        }

        data.activeStoryFlags = StoryStateManager.GetActiveFlags();

        if (Inventory.Instance != null)
        {
            data.inventoryData = Inventory.Instance.GetSaveData();
        }

        if (ItemPersistenceManager.Instance != null)
        {
            ItemPersistenceManager.Instance.SaveState(data);
        }

        if (QuestManager.Instance != null)
        {
            data.questData = QuestManager.Instance.GetSaveData();
        }

        SaveSystem.SaveGame(data, slotIndex);
        OnSaveCompleted?.Invoke(slotIndex);
    }

    public void LoadSavedState(int slotIndex = 0)
    {
        if (isTransitioning) return;

        SaveData data = SaveSystem.LoadGame(slotIndex);
        if (data == null) return;

        Time.timeScale = 1f;

        // Pre-load global persistence data into RAM before scene reload
        if (ItemPersistenceManager.Instance != null)
        {
            ItemPersistenceManager.Instance.LoadState(data);
        }

        StoryStateManager.LoadFlags(data.activeStoryFlags);

        StartCoroutine(LoadSceneAndApplyState(data, slotIndex));
    }

    private IEnumerator LoadSceneAndApplyState(SaveData data, int slotIndex)
    {
        isTransitioning = true;

        // 1. Fade out screen
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut();
        }

        // 2. Load target scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. Allow 1 frame for Awake/Start on newly instantiated objects
        yield return null;

        // 4. Apply inventory, quest, item, and player positioning data
        ApplyState(data);

        // 5. Fade back in smoothly
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn();
        }

        isTransitioning = false;
        OnLoadCompleted?.Invoke(slotIndex);
    }

    private void ApplyState(SaveData data)
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.LoadSaveData(data.inventoryData);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadSaveData(data.questData);
        }

        if (ItemPersistenceManager.Instance != null)
        {
            ItemPersistenceManager.Instance.RestoreDroppedItems(data);
        }

        // Safely relocate player without physics/controller overrides
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SetPlayerPositionSafely(player, data.playerPosition);
        }
    }

    private void SetPlayerPositionSafely(GameObject player, Vector3 targetPosition)
    {
        // Handle CharacterController if present
        if (player.TryGetComponent<CharacterController>(out var controller))
        {
            controller.enabled = false;
            player.transform.position = targetPosition;
            controller.enabled = true;
            return;
        }

        // Handle Rigidbody2D if present
        if (player.TryGetComponent<Rigidbody2D>(out var rb2d))
        {
            rb2d.position = targetPosition;
            rb2d.linearVelocity = Vector2.zero;
        }

        // Handle Rigidbody (3D) if present
        if (player.TryGetComponent<Rigidbody>(out var rb3d))
        {
            rb3d.position = targetPosition;
            rb3d.linearVelocity = Vector3.zero;
        }

        player.transform.position = targetPosition;
    }
}