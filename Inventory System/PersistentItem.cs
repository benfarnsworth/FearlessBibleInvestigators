using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentItem : MonoBehaviour, ISerializationCallbackReceiver
{
    [Header("Persistence")]
    [Tooltip("Unique ID for saving. Do not edit unless fixing duplicates.")]
    [SerializeField] 
    private string uniqueID;
    public string UniqueID => uniqueID;

    [Header("Quest Reset Options")]
    public bool resetOnQuestFailed = false;
    public string associatedQuestName;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Start()
    {
        if (ItemPersistenceManager.Instance != null && ItemPersistenceManager.Instance.IsCollected(uniqueID))
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        QuestManager.OnQuestFailed += HandleQuestFailed;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestFailed -= HandleQuestFailed;
    }

    public void Collect()
    {
        if (ItemPersistenceManager.Instance != null)
        {
            ItemPersistenceManager.Instance.MarkAsCollected(uniqueID);
        }
        
        Destroy(gameObject);
    }

    private void HandleQuestFailed(Quest failedQuest)
    {
        if (!resetOnQuestFailed || string.IsNullOrEmpty(associatedQuestName)) return;

        if (failedQuest != null && failedQuest.questName == associatedQuestName)
        {
            ResetToStart();
        }
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

        if (string.IsNullOrEmpty(uniqueID) || HasDuplicateIDInScene())
        {
            GenerateUniqueID();
        }
#endif
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

        if (string.IsNullOrEmpty(uniqueID))
        {
            GenerateUniqueID();
        }
#endif
    }

    public void OnAfterDeserialize() { }

    private bool HasDuplicateIDInScene()
    {
#if UNITY_EDITOR
        PersistentItem[] items = FindObjectsByType<PersistentItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item != this && item.gameObject.scene == gameObject.scene && item.uniqueID == this.uniqueID)
            {
                return true;
            }
        }
#endif
        return false;
    }

    [ContextMenu("Force Regenerate New ID")]
    public void GenerateUniqueID()
    {
        uniqueID = Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}