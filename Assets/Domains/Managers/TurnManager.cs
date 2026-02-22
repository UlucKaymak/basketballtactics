using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Status")]
    [ReadOnly] public TeamColor activeTeam = TeamColor.Heads;
    [ReadOnly] public int currentTurn = 1;

    public static event Action<TeamColor, int> OnTurnChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
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
        if (StateManager.Instance != null && StateManager.Instance.IsBusy()) return;
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        activeTeam = (activeTeam == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
        if (activeTeam == TeamColor.Heads) currentTurn++;

        Debug.Log($"<color=yellow>[TurnManager] Turn Ended! Next: {activeTeam} (Turn {currentTurn})</color>");
        
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);

        OnTurnChanged?.Invoke(activeTeam, currentTurn);

        if (TeamManager.Instance != null)
        {
            var activePlayers = TeamManager.Instance.GetTeam(activeTeam);
            foreach (var p in activePlayers)
            {
                if (p.isStunned)
                {
                    p.stunTurnsLeft--;
                    if (p.stunTurnsLeft < 0) 
                    {
                        p.isStunned = false;
                        p.stunTurnsLeft = 0;
                    }
                }
                
                p.hasActed = false; 
                p.UpdateVisuals(); 
            }
        }

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
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ForceClearCalculation();
        }

        if (TeamManager.Instance != null)
        {
            TeamManager.Instance.ResetAllPlayers(scorerTeam);
        }

        activeTeam = (scorerTeam == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
        
        if (activeTeam == TeamColor.Heads) currentTurn++;

        Debug.Log($"<color=gold>[TurnManager] Goal Reset Complete. Next Possession: {activeTeam}</color>");

        yield return new WaitForSeconds(0.5f);

        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);

        OnTurnChanged?.Invoke(activeTeam, currentTurn);
    }

    public void CheckAutoEndTurn()
    {
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
            Debug.Log($"<color=yellow>[TurnManager] Auto-End Turn Triggered for {activeTeam}</color>");
            EndTurn();
        }
    }
}
