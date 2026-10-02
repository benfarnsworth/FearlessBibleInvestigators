using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapCanopyFade : MonoBehaviour
{
    [Header("Tilemaps to Fade")]
    [Tooltip("Drag all tilemaps here that should fade when entering this trigger (e.g. Above Player, Foreground).")]
    [SerializeField] private List<Tilemap> targetTilemaps = new List<Tilemap>();

    [Header("Fade Settings")]
    [Range(0.1f, 1f)] [SerializeField] private float fadedAlpha = 0.4f;
    [SerializeField] private float fadeSpeed = 8f;

    private int objectsBehindCount = 0;
    private float targetAlpha = 1f;

    private void Start()
    {
        // If empty, automatically grab the Tilemap attached to this GameObject as a fallback
        if (targetTilemaps.Count == 0)
        {
            Tilemap localTilemap = GetComponent<Tilemap>();
            if (localTilemap != null) targetTilemaps.Add(localTilemap);
        }
    }

    private void Update()
    {
        // Smoothly adjust alpha for all registered tilemaps
        foreach (Tilemap tilemap in targetTilemaps)
        {
            if (tilemap == null) continue;

            Color currentColor = tilemap.color;
            if (!Mathf.Approximately(currentColor.a, targetAlpha))
            {
                currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
                tilemap.color = currentColor;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ItemHolder>() != null)
        {
            objectsBehindCount++;
            targetAlpha = fadedAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ItemHolder>() != null)
        {
            objectsBehindCount = Mathf.Max(0, objectsBehindCount - 1);
            if (objectsBehindCount == 0)
            {
                targetAlpha = 1f;
            }
        }
    }
}