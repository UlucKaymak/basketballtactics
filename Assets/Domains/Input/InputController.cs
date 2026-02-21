using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public enum ActionMode { None, Moving, Passing, Shooting }

public class InputController : MonoBehaviour
{
    private GridManager gridManager;
    private PlayerUnit selectedPlayer;
    private ActionMode currentMode = ActionMode.None;

    private void Start()
    {
        gridManager = Object.FindFirstObjectByType<GridManager>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // UI üzerine tıklandığında oyun içi kontrolleri devre dışı bırak
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
        mouseWorldPos.z = 0; 
        
        Vector2Int currentGridPos = gridManager.GetGridPosition(mouseWorldPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            
            // 1. ŞUT KONTROLÜ (Eğer Shooting Modundaysak)
            if (currentMode == ActionMode.Shooting && hit.collider != null)
            {
                Hoop hoop = hit.collider.GetComponent<Hoop>();
                if (hoop != null && selectedPlayer != null)
                {
                    selectedPlayer.Shoot(hoop);
                    selectedPlayer.hasActed = true; // AKSİYON BİTTİ
                    DeselectPlayer();
                    return;
                }
            }

            // 2. OYUNCU KONTROLÜ
            PlayerUnit clickedPlayer = null;
            if (hit.collider != null) clickedPlayer = hit.collider.GetComponentInParent<PlayerUnit>();
            if (clickedPlayer == null) clickedPlayer = GetPlayerAt(currentGridPos);

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
        // ATAK YAPMA: Moving modundayken rakibe tıklanırsa
        if (currentMode == ActionMode.Moving && selectedPlayer != null && selectedPlayer.team != clickedPlayer.team)
        {
            if (selectedPlayer.IsInMovementRange(clickedPlayer.currentGridPos))
            {
                selectedPlayer.Attack(clickedPlayer);
                DeselectPlayer();
            }
            return;
        }

        // PAS ATMA MODUNDAYSAK
        if (currentMode == ActionMode.Passing && selectedPlayer != null && selectedPlayer != clickedPlayer)
        {
            if (selectedPlayer.team == clickedPlayer.team)
            {
                selectedPlayer.Pass(clickedPlayer);
                selectedPlayer.hasActed = true; // AKSİYON BİTTİ
                DeselectPlayer();
            }
            return;
        }

        // SEÇİMİ İPTAL
        if (selectedPlayer == clickedPlayer)
        {
            DeselectPlayer();
            return;
        }

        // YENİ SEÇİM (Sadece turu gelmiş ve aksiyon almamışsa)
        if (TurnManager.Instance != null && TurnManager.Instance.activeTeam == clickedPlayer.team && !clickedPlayer.hasActed)
        {
            selectedPlayer = clickedPlayer;
            currentMode = ActionMode.None;
            if (gridManager != null) gridManager.ClearHighlights();
            if (UIManager.Instance != null) UIManager.Instance.ShowActionPanel(selectedPlayer);
        }
    }

    private void HandleEmptyTileClick(Vector2Int gridPos)
    {
        if (currentMode == ActionMode.Moving && selectedPlayer != null)
        {
            if (selectedPlayer.IsInMovementRange(gridPos))
            {
                StartCoroutine(selectedPlayer.MoveTo(gridPos));
                selectedPlayer.hasActed = true; // AKSİYON BİTTİ
                DeselectPlayer(); 
            }
        }
    }

    // BUTTON CALLBACKS (Unity Editor'deki butonlara bunları bağlayın)
    public void OnMoveBtn() 
    { 
        currentMode = ActionMode.Moving; 
        if (selectedPlayer != null && gridManager != null)
            gridManager.ShowMovementRange(selectedPlayer.currentGridPos, selectedPlayer.unitData.speed);
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
    }

    public void OnPassBtn() 
    { 
        currentMode = ActionMode.Passing; 
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
    }

    public void OnShootBtn() 
    { 
        currentMode = ActionMode.Shooting; 
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
    }

    public void OnWaitBtn() 
    { 
        if (selectedPlayer != null) selectedPlayer.hasActed = true;
        DeselectPlayer(); 
    }

    private void DeselectPlayer()
    {
        // Eğer bir oyuncu aksiyonunu bitirdiyse tur sonunu kontrol et
        if (selectedPlayer != null && selectedPlayer.hasActed)
        {
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }

        selectedPlayer = null;
        currentMode = ActionMode.None;
        if (gridManager != null) gridManager.ClearHighlights();
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
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
