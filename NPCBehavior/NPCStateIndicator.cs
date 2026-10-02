using System.Collections;
using UnityEngine;

public class NPCStateIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite alertSprite;
    [SerializeField] private Sprite stunSprite;

    private Coroutine flashRoutine;

    public void ShowAlert(float duration = 1.0f)
    {
        TriggerIcon(alertSprite, duration);
    }

    public void ShowStun(float duration)
    {
        TriggerIcon(stunSprite, duration);
    }

    private void TriggerIcon(Sprite sprite, float duration)
    {
        if (iconRenderer == null || sprite == null) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DisplayRoutine(sprite, duration));
    }

    private IEnumerator DisplayRoutine(Sprite sprite, float duration)
    {
        iconRenderer.sprite = sprite;
        iconRenderer.enabled = true;

        yield return new WaitForSeconds(duration);

        iconRenderer.enabled = false;
    }
}