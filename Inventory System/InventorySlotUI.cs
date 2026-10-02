using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private GameObject selectionBorder;
    [SerializeField] private GameObject questItemIndicator;

    [Header("Juice / Animation")]
    [SerializeField] private bool animateOnSpawn = true;
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField] private float popScaleAmount = 0.15f;

    private Item currentItem;
    private InventoryUI inventoryUI;

    public Item CurrentItem => currentItem;

    public void Setup(Item item, int count)
    {
        if (item == null) return;

        currentItem = item;
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInParent<InventoryUI>();
        }

        // 1. Icon Setup
        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = (item.icon != null);
        }

        // 2. Name Display
        if (nameText != null)
        {
            string displayName = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
            nameText.text = displayName;
        }

        // 3. Stack Quantity
        if (quantityText != null)
        {
            quantityText.text = count > 1 ? $"x{count}" : string.Empty;
            quantityText.gameObject.SetActive(count > 1);
        }

        // 4. Quest Indicator Badge
        if (questItemIndicator != null)
        {
            questItemIndicator.SetActive(item.isQuestItem);
        }

        // 5. Default to unselected on spawn
        SetSelected(false);

        // 6. Scale Pop Animation (Guard against running when object is inactive)
        if (animateOnSpawn && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(PopAnimationRoutine());
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInParent<InventoryUI>();
        }

        inventoryUI?.SelectSlot(this, currentItem);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    private IEnumerator PopAnimationRoutine()
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / popDuration;

            float scaleModifier = 1f + (Mathf.Sin(t * Mathf.PI) * popScaleAmount);
            transform.localScale = Vector3.one * scaleModifier;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        // Reset scale if disabled mid-animation
        transform.localScale = Vector3.one;
    }
}