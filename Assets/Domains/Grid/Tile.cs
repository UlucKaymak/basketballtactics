using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Coordinates")]
    public Vector2Int gridPos;

    [Header("State")]
    public bool isOccupied = false;
    public PlayerUnit occupiedBy; // Need to create PlayerUnit later

    // This script will be used later for pathfinding and interaction
}
