using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    public Sprite icon;

    [TextArea(3, 6)]
    public string description;

    [Header("World Setup")]
    [Tooltip("The physical prefab spawned in the world when dropped from inventory.")]
    public GameObject worldPrefab;

    [Tooltip("Can the player put this in their inventory? Set false for kickable/physics objects like cheese wheels!")]
    public bool canBePickedUp = true;

    [Header("Quest & Restrictions")]
    [Tooltip("Is this item required for a quest?")]
    public bool isQuestItem = false;

    [Tooltip("Can the player drop this item from their inventory? Automatically enforced as false for quest items if desired.")]
    public bool canBeDropped = true;

    private void OnValidate()
    {
        // Auto-populate itemID or itemName in Inspector if empty
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = name;
        }
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = name;
        }
    }
}