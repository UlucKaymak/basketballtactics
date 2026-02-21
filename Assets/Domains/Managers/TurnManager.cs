using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TeamColor activeTeam = TeamColor.Blue;
    public int currentTurn = 1;

    [Header("Teams (Managed)")]
    public List<PlayerUnit> redTeam = new List<PlayerUnit>();
    public List<PlayerUnit> blueTeam = new List<PlayerUnit>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// MatchSetter tarafından çağrılır. Oyunun katılımcılarını belirler.
    /// </summary>
    public void InitializeTeams(List<PlayerUnit> red, List<PlayerUnit> blue)
    {
        redTeam = red;
        blueTeam = blue;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(activeTeam, currentTurn);
        }
        Debug.Log($"TurnManager: Teams registered. Red: {redTeam.Count}, Blue: {blueTeam.Count}");
    }

    [Button("Skip Turn")]
    public void EndTurn()
    {
        activeTeam = (activeTeam == TeamColor.Blue) ? TeamColor.Red : TeamColor.Blue;
        if (activeTeam == TeamColor.Blue) currentTurn++;

        Debug.Log($"<color=yellow>Turn Changed! New Active Team: {activeTeam}</color>");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(activeTeam, currentTurn);
        }

        List<PlayerUnit> currentActivePlayers = (activeTeam == TeamColor.Red) ? redTeam : blueTeam;
        foreach (var player in currentActivePlayers)
        {
            player.isStunned = false; 
            player.hasActed = false; 
            player.UpdateVisuals(); 
        }
    }

    public bool IsPlayerTurn(PlayerUnit player)
    {
        return player.team == activeTeam;
    }

    public void OnGoalScored(TeamColor scorerTeam)
    {
        Debug.Log($"<color=gold>Goal reset initiated! Scorer: {scorerTeam}</color>");
        
        // 1. Tüm oyuncuları yerlerine gönder ve durumlarını sıfırla
        foreach (var p in redTeam) p.ResetToStart();
        foreach (var p in blueTeam) p.ResetToStart();

        // 2. Sırayı gol atan takıma ver
        activeTeam = scorerTeam;
        
        // 3. Topu gol atan takımdaki ilk oyuncuya ver
        List<PlayerUnit> teamList = (scorerTeam == TeamColor.Red) ? redTeam : blueTeam;
        if (teamList.Count > 0 && Ball.Instance != null)
        {
            Ball.Instance.SetOwner(teamList[0]);
        }

        // 4. UI'yı güncelle
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(activeTeam, currentTurn);
        }
    }

    /// <summary>
    /// Aktif takımdaki herkes hareket ettiyse turu otomatik bitirir.
    /// </summary>
    public void CheckAutoEndTurn()
    {
        List<PlayerUnit> currentActivePlayers = (activeTeam == TeamColor.Red) ? redTeam : blueTeam;
        
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
            Debug.Log($"All players of {activeTeam} have acted. Auto-ending turn.");
            EndTurn();
        }
    }
}
