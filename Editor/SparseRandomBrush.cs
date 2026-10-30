
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

[CustomGridBrush(false, false, false, "Sparse Random Brush")]
public class SparseRandomBrush : RandomBrush
{
    [Header("Density")]
    [Range(0f, 1f)]
    [Tooltip("Chance that each cell gets painted.")]
    public float placementChance = 0.35f;

    private TileBase GetRandomSingleCellTile()
    {
        var candidates = new List<TileBase>();

        // Support the newer Random Brush tile data format.
        if (randomTileChangeDataSets != null)
        {
            foreach (var set in randomTileChangeDataSets)
            {
                if (set.randomTileChangeData != null &&
                    set.randomTileChangeData.Length > 0)
                {
                    var tile = set.randomTileChangeData[0].tile;
                    if (tile != null)
                        candidates.Add(tile);
                }
            }
        }

        // Also support the standard tile-set format.
        if (candidates.Count == 0 && randomTileSets != null)
        {
            foreach (var set in randomTileSets)
            {
                if (set.randomTiles != null &&
                    set.randomTiles.Length > 0 &&
                    set.randomTiles[0] != null)
                {
                    candidates.Add(set.randomTiles[0]);
                }
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    public override void Paint(
        GridLayout grid,
        GameObject brushTarget,
        Vector3Int position)
    {
        if (Random.value > placementChance)
            return;

        if (brushTarget == null)
            return;

        Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
        if (tilemap == null)
            return;

        TileBase tile = GetRandomSingleCellTile();
        if (tile == null)
            return;

        tilemap.SetTile(position, tile);
    }

    public override void BoxFill(
        GridLayout grid,
        GameObject brushTarget,
        BoundsInt position)
    {
        foreach (Vector3Int cell in position.allPositionsWithin)
        {
            Paint(grid, brushTarget, cell);
        }
    }
}
