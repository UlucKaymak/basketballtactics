using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 7;
    public int height = 12;
    public float tileSize = 1f;

    [Header("References")]
    public Tilemap tilemap; // Reference to your Unity Tilemap

    [Header("Visuals (Optional)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gridColor = Color.white;
    [SerializeField] private Color targetHighlightColor = Color.aquamarine;

    private List<Vector3Int> highlightedTiles = new List<Vector3Int>();

    public void HighlightTile(Vector2Int gridPos)
    {
        Vector3Int pos = new Vector3Int(gridPos.x, gridPos.y, 0);
        if (tilemap != null)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, targetHighlightColor);
            highlightedTiles.Add(pos);
        }
    }

    public void ShowMovementRange(List<Vector2Int> reachableTiles)
    {
        ClearHighlights();
        if (tilemap == null) return;

        Color moveRangeColor = new Color(0.5f, 1f, 0.5f, 1f); // Light Green

        foreach (var tilePos in reachableTiles)
        {
            Vector3Int pos = new Vector3Int(tilePos.x, tilePos.y, 0);
            tilemap.SetTileFlags(pos, TileFlags.None);
            tilemap.SetColor(pos, moveRangeColor);
            highlightedTiles.Add(pos);
        }
    }

    public void ClearHighlights()
    {
        if (tilemap == null) return;

        foreach (var pos in highlightedTiles)
        {
            tilemap.SetColor(pos, Color.white);
        }
        highlightedTiles.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gridColor;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // GetWorldPosition already handles center if tilemap is present
                // or if calculated manually below.
                Vector3 center = GetWorldPosition(x, y);
                Gizmos.DrawWireCube(center, new Vector3(tileSize, tileSize, 0.1f));
            }
        }
    }

    /// <summary>
    /// Returns the world position of the center of a grid cell.
    /// </summary>
    public Vector3 GetWorldPosition(int x, int y)
    {
        if (tilemap != null)
        {
            return tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }
        // Manual calculation including center offset
        return transform.position + new Vector3(x * tileSize + tileSize / 2f, y * tileSize + tileSize / 2f, 0);
    }

    /// <summary>
    /// Returns the grid coordinates for a given world position.
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        if (tilemap != null)
        {
            Vector3Int cellPos = tilemap.WorldToCell(worldPosition);
            return new Vector2Int(cellPos.x, cellPos.y);
        }

        Vector3 relativePos = worldPosition - transform.position;
        int x = Mathf.FloorToInt(relativePos.x / tileSize);
        int y = Mathf.FloorToInt(relativePos.y / tileSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Checks if the given grid coordinates are within the grid bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return IsInBounds(pos.x, pos.y);
    }

    public PlayerUnit GetPlayerAt(Vector2Int gridPos)
    {
        PlayerUnit[] allPlayers = Object.FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p.currentGridPos == gridPos) return p;
        }
        return null;
    }

    public bool IsTileOccupied(Vector2Int gridPos)
    {
        return GetPlayerAt(gridPos) != null;
    }
}
