using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("Teams (Managed)")]
    public List<PlayerUnit> redTeam = new List<PlayerUnit>();
    public List<PlayerUnit> blueTeam = new List<PlayerUnit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterTeams(List<PlayerUnit> red, List<PlayerUnit> blue)
    {
        redTeam = red;
        blueTeam = blue;
        Debug.Log($"<color=green>[TeamManager] Teams Registered. Red: {red.Count}, Blue: {blue.Count}</color>");
    }

    public List<PlayerUnit> GetTeam(TeamColor color)
    {
        return (color == TeamColor.Red) ? redTeam : blueTeam;
    }

    public void ResetAllPlayers(TeamColor scorerTeam)
    {
        foreach (var p in redTeam) p.ResetToStart();
        foreach (var p in blueTeam) p.ResetToStart();

        // Topu golü atan takıma ver
        List<PlayerUnit> teamList = GetTeam(scorerTeam);
        if (teamList.Count > 0 && Ball.Instance != null)
        {
            Ball.Instance.SetOwner(teamList[0]);
        }
    }
}
