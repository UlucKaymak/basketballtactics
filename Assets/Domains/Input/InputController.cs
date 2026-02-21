using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private GridManager gridManager;
    private PlayerUnit selectedPlayer;

    private void Start()
    {
        gridManager = Object.FindFirstObjectByType<GridManager>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
        mouseWorldPos.z = 0; 
        
        Vector2Int currentGridPos = gridManager.GetGridPosition(mouseWorldPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            
            // 1. ŞUT KONTROLÜ
            if (hit.collider != null)
            {
                Hoop hoop = hit.collider.GetComponent<Hoop>();
                if (hoop != null && selectedPlayer != null && selectedPlayer.hasBall)
                {
                    selectedPlayer.Shoot(hoop);
                    DeselectPlayer();
                    return;
                }
            }

            // 2. OYUNCU KONTROLÜ (Raycast veya Grid üzerinden)
            PlayerUnit clickedPlayer = null;
            if (hit.collider != null) clickedPlayer = hit.collider.GetComponentInParent<PlayerUnit>();
            if (clickedPlayer == null) clickedPlayer = GetPlayerAt(currentGridPos);

            if (gridManager.IsInBounds(currentGridPos))
            {
                Debug.Log($"<color=white>Clicked on Grid: {currentGridPos}</color>");
            }

            if (clickedPlayer != null)
            {
                HandlePlayerClick(clickedPlayer);
            }
            else if (gridManager.IsInBounds(currentGridPos))
            {
                HandleEmptyTileClick(currentGridPos);
            }
            else
            {
                DeselectPlayer();
            }
        }
    }

    private void HandlePlayerClick(PlayerUnit clickedPlayer)
    {
        // PAS ATMA
        if (selectedPlayer != null && selectedPlayer.hasBall && selectedPlayer != clickedPlayer)
        {
            Debug.Log($"Passing to {clickedPlayer.name}");
            selectedPlayer.Pass(clickedPlayer);
            DeselectPlayer();
            return;
        }

        // SEÇİMİ İPTAL
        if (selectedPlayer == clickedPlayer)
        {
            DeselectPlayer();
            return;
        }

        // YENİ SEÇİM
        selectedPlayer = clickedPlayer;
        Debug.Log("Selected: " + (selectedPlayer.unitData != null ? selectedPlayer.unitData.playerName : "Unknown"));
        
        if (selectedPlayer.unitData != null)
        {
            gridManager.ShowMovementRange(selectedPlayer.currentGridPos, selectedPlayer.unitData.speed);
        }
    }

    private void HandleEmptyTileClick(Vector2Int gridPos)
    {
        if (selectedPlayer != null)
        {
            if (selectedPlayer.IsInMovementRange(gridPos))
            {
                StartCoroutine(selectedPlayer.MoveTo(gridPos));
                DeselectPlayer(); 
            }
            else
            {
                DeselectPlayer();
            }
        }
    }

    private void DeselectPlayer()
    {
        selectedPlayer = null;
        if (gridManager != null) gridManager.ClearHighlights();
        Debug.Log("Deselected.");
    }

    private PlayerUnit GetPlayerAt(Vector2Int gridPos)
    {
        PlayerUnit[] allPlayers = Object.FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.currentGridPos == gridPos)
                return player;
        }
        return null;
    }
}
