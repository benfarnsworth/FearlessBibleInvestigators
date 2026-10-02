using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Sort & Action Buttons")]
    [SerializeField] private Button sortByNameButton;
    [SerializeField] private Button sortByQuantityButton;
    [SerializeField] private Button dropButton;

    [Header("UI Containers")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Item Details Display (Optional)")]
    [SerializeField] private TMP_Text selectedItemNameText;
    [SerializeField] private TMP_Text selectedItemDescriptionText;
    [SerializeField] private Image selectedItemDetailsImage; // <--- Separate image exclusively for the details panel

    [Header("Empty State Feedback (Optional)")]
    [SerializeField] private GameObject emptyInventoryMessage;

    // Track active slot UI instances for reuse (Object Pooling)
    private readonly List<InventorySlotUI> spawnedSlotUIs = new List<InventorySlotUI>();

    // Selection tracking
    private Item selectedItem;
    private InventorySlotUI selectedSlotUI;

    private void Awake()
    {
        if (sortByNameButton != null)
        {
            sortByNameButton.onClick.AddListener(() => Inventory.Instance?.SortInventory(SortOption.Name));
        }

        if (sortByQuantityButton != null)
        {
            sortByQuantityButton.onClick.AddListener(() => Inventory.Instance?.SortInventory(SortOption.Quantity));
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(OnDropButtonClicked);
        }
    }

    private void OnEnable()
    {
        Inventory.OnInventoryChanged += RedrawUI;
        RedrawUI();
    }

    private void OnDisable()
    {
        Inventory.OnInventoryChanged -= RedrawUI;
    }

    public void SelectSlot(InventorySlotUI slotUI, Item item)
    {
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
        }

        selectedSlotUI = slotUI;
        selectedItem = item;

        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(true);
        }

        UpdateDetailsPanel();
        UpdateDropButtonState();
    }

    public void OnDropButtonClicked()
    {
        if (selectedItem == null || Inventory.Instance == null) return;

        if (selectedItem.isQuestItem || !selectedItem.canBeDropped)
        {
            Debug.LogWarning($"[INVENTORY UI] Cannot drop '{selectedItem.itemName}' (Quest or Key item).");
            return;
        }

        Item itemToDrop = selectedItem;
        ClearSelection();
        Inventory.Instance.DropItem(itemToDrop);
    }

    public void RedrawUI()
    {
        if (slotContainer == null || Inventory.Instance == null) return;

        ClearSelection();

        List<Inventory.InventorySlot> activeSlots = Inventory.Instance.GetSlots();
        bool hasItems = activeSlots != null && activeSlots.Count > 0;

        if (emptyInventoryMessage != null)
        {
            emptyInventoryMessage.SetActive(!hasItems);
        }

        int activeIndex = 0;

        if (hasItems)
        {
            for (int i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                if (slot.item == null || slot.count <= 0) continue;

                InventorySlotUI slotUI = GetOrCreateSlotUI(activeIndex);
                slotUI.gameObject.SetActive(true);
                slotUI.transform.SetAsLastSibling();
                slotUI.Setup(slot.item, slot.count);

                if (slotUI.TryGetComponent<Button>(out var button))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SelectSlot(slotUI, slot.item));
                }

                activeIndex++;
            }
        }

        for (int i = activeIndex; i < spawnedSlotUIs.Count; i++)
        {
            spawnedSlotUIs[i].gameObject.SetActive(false);
        }

        Canvas.ForceUpdateCanvases();
        if (slotContainer is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private InventorySlotUI GetOrCreateSlotUI(int index)
    {
        if (index < spawnedSlotUIs.Count)
        {
            return spawnedSlotUIs[index];
        }

        GameObject newSlotObj = Instantiate(slotPrefab, slotContainer, false);
        if (newSlotObj.TryGetComponent<InventorySlotUI>(out var slotUI))
        {
            spawnedSlotUIs.Add(slotUI);
            return slotUI;
        }

        Debug.LogError("[INVENTORY UI] Slot prefab is missing InventorySlotUI component!", newSlotObj);
        return null;
    }

    private void ClearSelection()
    {
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetSelected(false);
        }

        selectedItem = null;
        selectedSlotUI = null;

        UpdateDetailsPanel();
        UpdateDropButtonState();
    }

    private Coroutine detailsAnimationRoutine;

    private void UpdateDetailsPanel()
    {
        if (selectedItemNameText != null)
        {
            selectedItemNameText.text = selectedItem != null ? selectedItem.itemName : string.Empty;
        }

        if (selectedItemDescriptionText != null)
        {
            selectedItemDescriptionText.text = selectedItem != null ? selectedItem.description : string.Empty;
        }

        // Pulls the world prefab sprite specifically for the details panel
        if (selectedItemDetailsImage != null)
        {
            Sprite worldSprite = GetSpriteFromWorldPrefab(selectedItem);

            if (worldSprite != null)
            {
                selectedItemDetailsImage.sprite = worldSprite;
                selectedItemDetailsImage.gameObject.SetActive(true);

                // Trigger Juice Animation
                if (detailsAnimationRoutine != null)
                {
                    StopCoroutine(detailsAnimationRoutine);
                }
                detailsAnimationRoutine = StartCoroutine(DetailsImageJuiceRoutine(selectedItemDetailsImage.transform));
            }
            else
            {
                if (detailsAnimationRoutine != null)
                {
                    StopCoroutine(detailsAnimationRoutine);
                }
                selectedItemDetailsImage.sprite = null;
                selectedItemDetailsImage.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator DetailsImageJuiceRoutine(Transform imageTransform)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        
        Vector3 startScale = new Vector3(0.2f, 0.2f, 1f); // Starts tiny
        Vector3 maxScale = new Vector3(1.25f, 0.85f, 1f);   // Squashed wide on impact
        Vector3 targetScale = Vector3.one;

        imageTransform.localScale = targetScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Custom curve: quick snap out with an elastic bounce
            float bounce = Mathf.Sin(t * Mathf.PI * 1.5f) * Mathf.Exp(-t * 2.5f);
            
            if (t < 0.5f)
            {
                imageTransform.localScale = Vector3.Lerp(startScale, maxScale, t * 2f);
            }
            else
            {
                imageTransform.localScale = Vector3.Lerp(maxScale, targetScale, (t - 0.5f) * 2f) + (Vector3.one * bounce * 0.3f);
            }

            yield return null;
        }

        imageTransform.localScale = targetScale;
        imageTransform.localRotation = Quaternion.identity;

        // phase 2: infinite float/breathe/shimmer
        float floatSpeed = 3f;
        float floatAmount = 0.04f;
        float timeOffset = UnityEngine.Random.Range(0f, 100f);

        while (true)
        {
            float sine = Mathf.Sin((Time.unscaledTime + timeOffset) * floatSpeed);
            imageTransform.localScale = Vector3.one + (Vector3.up * sine * floatAmount) + (Vector3.right * sine * floatAmount);
            yield return null;
        }
    }

    private Sprite GetSpriteFromWorldPrefab(Item item)
    {
        if (item == null || item.worldPrefab == null) return null;

        if (item.worldPrefab.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            return spriteRenderer.sprite;
        }

        SpriteRenderer nestedRenderer = item.worldPrefab.GetComponentInChildren<SpriteRenderer>();
        if (nestedRenderer != null)
        {
            return nestedRenderer.sprite;
        }

        return null;
    }

    private void UpdateDropButtonState()
    {
        if (dropButton == null) return;

        bool canDrop = selectedItem != null && !selectedItem.isQuestItem && selectedItem.canBeDropped;
        dropButton.interactable = canDrop;
    }
}