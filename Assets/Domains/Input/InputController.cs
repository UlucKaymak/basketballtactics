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

        // --- DEBUG: Sadece sol tıklandığında durumu raporla ---
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            string stateStr = StateManager.Instance != null ? StateManager.Instance.CurrentState.ToString() : "NULL";
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"<color=white>[Input Debug] Click Detected! State: {stateStr}, OverUI: {overUI}</color>");
        }

        // 1. RESOLUTION DURUMU: Ekrana tıklandığında zarları temizle
        // UI Engelinden ÖNCE kontrol ediyoruz ki zarları her durumda geçebilelim.
        if (StateManager.Instance != null && StateManager.Instance.IsResolution())
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log("<color=green>[Input] Clearing Resolution via click.</color>");
                if (UIManager.Instance != null) UIManager.Instance.OnCalculationClick();
            }
            return;
        }

        // 2. BUSY DURUMU: Animasyonlar varken tıklamayı tamamen blokla
        if (StateManager.Instance != null && !StateManager.Instance.IsIdle())
        {
            return;
        }

        // 3. UI ENGELİ: Eğer mouse bir UI elemanı üzerindeyse (buton vs) dünya tıklamasını engelle
        // DİKKAT: Bu kontrol dünya tıklamalarını (oyuncu seçme vb.) engeller, 
        // ama butonların kendi OnClick olaylarının çalışmasına izin verir.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Mouse bir buton üzerindeyken dünya tıklaması (Raycast) yapma, çık.
            return;
        }

        // 4. KLAVYE KISAYOLLARI (Sadece bir oyuncu seçiliyken)
        if (selectedPlayer != null && StateManager.Instance != null && StateManager.Instance.IsIdle())
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame && UIManager.Instance.moveBtn.interactable)
                OnMoveBtn();
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && UIManager.Instance.passBtn.interactable)
                OnPassBtn();
            else if (Keyboard.current.digit3Key.wasPressedThisFrame && UIManager.Instance.shootBtn.interactable)
                OnShootBtn();
            else if (Keyboard.current.digit4Key.wasPressedThisFrame && UIManager.Instance.waitBtn.interactable)
                OnWaitBtn();
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                DeselectPlayer();
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
        mouseWorldPos.z = 0; 
        
        Vector2Int currentGridPos = gridManager != null ? gridManager.GetGridPosition(mouseWorldPos) : Vector2Int.zero;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Grid koordinatını her durumda logla
            if (gridManager != null && gridManager.IsInBounds(currentGridPos))
            {
                Debug.Log($"<color=orange>[Input] Final Grid Click: {currentGridPos}</color>");
            }
            else if (gridManager != null)
            {
                Debug.Log($"<color=red>[Input] Click Out of Bounds: {currentGridPos}</color>");
            }

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
            if (selectedPlayer != null) selectedPlayer.SetSelected(false); // Önceki seçimi kaldır
            
            selectedPlayer = clickedPlayer;
            selectedPlayer.SetSelected(true); // Yeni oyuncuyu highlight et
            
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
                selectedPlayer.SetSelected(false); // Hareket başladığında highlight kapat
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
        if (selectedPlayer != null) 
        {
            selectedPlayer.hasActed = true;
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }
        DeselectPlayer(); 
    }

    private void DeselectPlayer()
    {
        if (selectedPlayer != null)
        {
            selectedPlayer.SetSelected(false); // Highlight'ı kapat
            if (selectedPlayer.hasActed)
            {
                if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
            }
        }

        selectedPlayer = null;
        currentMode = ActionMode.None;
        if (gridManager != null) gridManager.ClearHighlights();
        if (UIManager.Instance != null) UIManager.Instance.HideActionPanel();
    }
}
