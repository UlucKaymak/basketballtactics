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
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
            mouseWorldPos.z = 0; 
            
            Vector2Int clickedGridPos = gridManager.GetGridPosition(mouseWorldPos);

            // Tıklanan yer grid dışındaysa seçimi bırak
            if (!gridManager.IsInBounds(clickedGridPos))
            {
                DeselectPlayer();
                return;
            }

            HandleClick(clickedGridPos);
        }
    }

    private void HandleClick(Vector2Int gridPos)
    {
        PlayerUnit clickedPlayer = GetPlayerAt(gridPos);

        if (clickedPlayer != null)
        {
            // Eğer elimizde top varsa ve başka bir oyuncuya tıkladıysak: PAS AT
            if (selectedPlayer != null && selectedPlayer.hasBall && selectedPlayer != clickedPlayer)
            {
                selectedPlayer.Pass(clickedPlayer);
                DeselectPlayer();
                return;
            }

            // Eğer aynı oyuncuya tekrar basarsak seçimi bırak
            if (selectedPlayer == clickedPlayer)
            {
                DeselectPlayer();
                return;
            }

            selectedPlayer = clickedPlayer;
            string pName = (selectedPlayer.unitData != null) ? selectedPlayer.unitData.playerName : "Unnamed Player (Missing UnitData)";
            Debug.Log("Selected Player: " + pName);

            if (selectedPlayer.unitData != null)
            {
                gridManager.ShowMovementRange(selectedPlayer.currentGridPos, selectedPlayer.unitData.speed);
            }
        }
        else if (selectedPlayer != null)
        {
            // Hareket etmeye çalış
            if (selectedPlayer.IsInMovementRange(gridPos))
            {
                StartCoroutine(selectedPlayer.MoveTo(gridPos));
                Debug.Log("Moving " + (selectedPlayer.unitData != null ? selectedPlayer.unitData.playerName : "Player") + " to " + gridPos);
                
                // Hareket başlayınca highlight'ı temizle ve seçimi bırak (Veya seçili kalsın istersen bırakmayabiliriz)
                DeselectPlayer(); 
            }
            else
            {
                // Menzil dışı boş bir yere tıklandıysa seçimi bırak
                DeselectPlayer();
                Debug.Log("Clicked empty tile out of range. Deselecting.");
            }
        }
    }

    private void DeselectPlayer()
    {
        selectedPlayer = null;
        gridManager.ClearHighlights();
        Debug.Log("Player Deselected.");
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
