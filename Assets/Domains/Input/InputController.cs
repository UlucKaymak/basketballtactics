using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
            
            // 1. ŞUT KONTROLÜ
            if (currentMode == ActionMode.Shooting && hit.collider != null)
            {
                Hoop hoop = hit.collider.GetComponent<Hoop>();
                if (hoop != null && selectedPlayer != null)
                {
                    selectedPlayer.Shoot(hoop);
                    DeselectPlayer();
                    return;
                }
            }

            // 2. OYUNCU KONTROLÜ
            PlayerUnit clickedPlayer = null;
            if (hit.collider != null) clickedPlayer = hit.collider.GetComponentInParent<PlayerUnit>();
            if (clickedPlayer == null && gridManager != null) clickedPlayer = gridManager.GetPlayerAt(currentGridPos);

            if (gridManager != null && gridManager.IsInBounds(currentGridPos))
            {
                Debug.Log($"<color=white>Clicked on Grid: {currentGridPos}</color>");
            }

            if (clickedPlayer != null)
            {
                HandlePlayerClick(clickedPlayer);
            }
            else if (gridManager != null && gridManager.IsInBounds(currentGridPos))
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
        // ATAK / COMBAT KONTROLÜ
        if (currentMode == ActionMode.Moving && selectedPlayer != null && selectedPlayer.team != clickedPlayer.team)
        {
            // Rakibin karesine Manhattan mesafesiyle bakabiliriz (Atak komutu için)
            int dist = Mathf.Abs(selectedPlayer.currentGridPos.x - clickedPlayer.currentGridPos.x) + 
                       Mathf.Abs(selectedPlayer.currentGridPos.y - clickedPlayer.currentGridPos.y);
            
            if (dist <= selectedPlayer.unitData.speed)
            {
                selectedPlayer.Attack(clickedPlayer);
                DeselectPlayer();
            }
            return;
        }

        // PAS ATMA
        if (currentMode == ActionMode.Passing && selectedPlayer != null && selectedPlayer != clickedPlayer)
        {
            if (selectedPlayer.team == clickedPlayer.team)
            {
                selectedPlayer.Pass(clickedPlayer);
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

        // YENİ SEÇİM
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
                selectedPlayer.hasActed = true; 
                DeselectPlayer(); 
            }
        }
    }

    public void OnMoveBtn() 
    { 
        currentMode = ActionMode.Moving; 
        if (selectedPlayer != null && gridManager != null)
        {
            gridManager.ShowMovementRange(selectedPlayer.GetReachableTiles());
        }
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
        if (selectedPlayer != null && selectedPlayer.hasActed)
        {
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }

        selectedPlayer = null;
        currentMode = ActionMode.None;
        if (gridManager != null) gridManager.ClearHighlights();
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
    }
}
