using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Status")]
    [ReadOnly] public TeamColor activeTeam = TeamColor.Blue;
    [ReadOnly] public int currentTurn = 1;

    // Tur değiştiğinde tetiklenecek olay: (Yeni Sıra Kimde, Kaçıncı Tur)
    public static event Action<TeamColor, int> OnTurnChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Gol olduğunda sırayı golü atan takıma verelim
        ScoreManager.OnGoalScored += HandleGoalScored;
    }

    private void OnDestroy()
    {
        ScoreManager.OnGoalScored -= HandleGoalScored;
    }

    public void InitializeMatch(TeamColor startingTeam)
    {
        activeTeam = startingTeam;
        currentTurn = 1;
        
        Debug.Log($"<color=cyan>[TurnManager] Match Initialized. Starting Team: {startingTeam}</color>");
        
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);
        
        OnTurnChanged?.Invoke(activeTeam, currentTurn);
    }

    [Button("End Turn")]
    public void EndTurn()
    {
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        // 1. Sırayı Değiştir
        activeTeam = (activeTeam == TeamColor.Blue) ? TeamColor.Red : TeamColor.Blue;
        if (activeTeam == TeamColor.Blue) currentTurn++;

        Debug.Log($"<color=yellow>[TurnManager] Turn Ended! Next: {activeTeam} (Turn {currentTurn})</color>");
        
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);

        // 2. Event Ateşle (UI dinleyebilir)
        OnTurnChanged?.Invoke(activeTeam, currentTurn);

        // 3. Stun/Action durumlarını sıfırla (TeamManager üzerinden)
        if (TeamManager.Instance != null)
        {
            var activePlayers = TeamManager.Instance.GetTeam(activeTeam);
            foreach (var p in activePlayers)
            {
                p.isStunned = false; 
                p.hasActed = false; 
                p.UpdateVisuals(); 
            }
        }

        // Kısa bekleme süresi (UI animasyonu için)
        yield return new WaitForSeconds(1.0f);

        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);
    }

    public bool IsPlayerTurn(PlayerUnit player)
    {
        return player.team == activeTeam;
    }

    private void HandleGoalScored(TeamColor scorerTeam, int points)
    {
        StartCoroutine(GoalResetRoutine(scorerTeam));
    }

    private IEnumerator GoalResetRoutine(TeamColor scorerTeam)
    {
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);
        
        // 1. Takımları Resetle (TeamManager üzerinden)
        if (TeamManager.Instance != null)
        {
            TeamManager.Instance.ResetAllPlayers(scorerTeam);
        }

        activeTeam = scorerTeam;
        Debug.Log($"<color=gold>[TurnManager] Goal Reset Complete. Possession: {scorerTeam}</color>");

        // UI Güncelle (Event üzerinden)
        OnTurnChanged?.Invoke(activeTeam, currentTurn);
        
        yield return new WaitForSeconds(0.5f);

        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);
    }

    public void CheckAutoEndTurn()
    {
        if (StateManager.Instance != null && !StateManager.Instance.IsIdle()) return;
        if (TeamManager.Instance == null) return;

        List<PlayerUnit> currentActivePlayers = TeamManager.Instance.GetTeam(activeTeam);
        
        bool allActed = true;
        foreach (var p in currentActivePlayers)
        {
            if (!p.hasActed && !p.isStunned)
            {
                allActed = false;
                break;
            }
        }

        if (allActed)
        {
            Debug.Log($"[TurnManager] Auto-End Turn Triggered for {activeTeam}");
            EndTurn();
        }
    }
}
