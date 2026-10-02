using UnityEngine;

[RequireComponent(typeof(PersistentItem))]
public class CollectibleItem : MonoBehaviour, IInteractable
{
    [Header("Item Configuration")]
    [SerializeField] private Item itemData;
    [SerializeField] private int quantity = 1;
    [SerializeField] private GameObject pickupTextPrefab;

    [Header("Pickup SFX")]
    [SerializeField] private AudioClip pickupSound;

    private PersistentItem persistentItem;
    private bool hasBeenPickedUp = false;

    private void Awake()
    {
        persistentItem = GetComponent<PersistentItem>();
    }

    public void Interact()
    {
        if (hasBeenPickedUp || !CanBePickedUp()) return;
        Pickup();
    }

    public string GetInteractionPrompt()
    {
        if (hasBeenPickedUp || !CanBePickedUp()) return string.Empty;

        string itemName = itemData != null ? itemData.itemName : "Item";
        return quantity > 1 ? $"Pick up {itemName} (x{quantity})" : $"Pick up {itemName}";
    }

    private bool CanBePickedUp()
    {
        return itemData != null && itemData.canBePickedUp;
    }

    private void Pickup()
    {
        if (itemData == null)
        {
            Debug.LogError($"[COLLECTIBLE] '{gameObject.name}' is missing an Item asset!", this);
            return;
        }

        hasBeenPickedUp = true;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(itemData, Mathf.Max(1, quantity));
        }

        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSound, 1.15f, 1.35f);
        }

        TriggerFeedback();
        persistentItem.Collect();
    }

    private void TriggerFeedback()
    {
        if (pickupTextPrefab == null) return;

        Vector3 spawnPos = transform.position + new Vector3(0, 0.5f, 0);
        GameObject textObj = Instantiate(pickupTextPrefab, spawnPos, Quaternion.identity);

        if (textObj.TryGetComponent<SelfDestructText>(out var popUpText))
        {
            string displayName = itemData != null ? itemData.itemName : "Item";
            int count = Mathf.Max(1, quantity);
            popUpText.SetText(count > 1 ? $"+{count} {displayName}" : $"+1 {displayName}");
        }
    }
}