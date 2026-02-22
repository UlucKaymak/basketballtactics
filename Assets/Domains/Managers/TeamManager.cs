using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public enum TeamColor { Heads, Tails }

[System.Serializable]
public class TeamInfo
{
    public string teamName;
    public Color teamColor;
    public TeamColor side;
    public List<PlayerUnitData> roster = new List<PlayerUnitData>(); // Roster artık burada

    public TeamInfo(TeamColor side, string name, Color color)
    {
        this.side = side;
        this.teamName = name;
        this.teamColor = color;
    }
}

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    [Header("Team Identity & Rosters")]
    public TeamInfo headsInfo;
    public TeamInfo tailsInfo;

    [Header("Active Units (Runtime)")]
    [ReadOnly] public List<PlayerUnit> headsUnits = new List<PlayerUnit>();
    [ReadOnly] public List<PlayerUnit> tailsUnits = new List<PlayerUnit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GenerateTeamIdentities();
    }

    private void GenerateTeamIdentities()
    {
        // Not: Roster listeleri Inspector'dan atanacağı için burada onları ezmiyoruz.
        headsInfo.side = TeamColor.Heads;
        headsInfo.teamName = TeamNameGenerator.Generate();
        headsInfo.teamColor = Random.ColorHSV(0f, 0.5f, 0.6f, 1f, 0.7f, 1f);

        tailsInfo.side = TeamColor.Tails;
        tailsInfo.teamName = TeamNameGenerator.Generate();
        tailsInfo.teamColor = Random.ColorHSV(0.5f, 1f, 0.6f, 1f, 0.7f, 1f);
        
        Debug.Log($"<color=cyan>[TeamManager] Match: {headsInfo.teamName} vs {tailsInfo.teamName}</color>");
    }

    public void RegisterActiveUnits(List<PlayerUnit> heads, List<PlayerUnit> tails)
    {
        headsUnits = heads;
        tailsUnits = tails;
        Debug.Log($"<color=green>[TeamManager] Units Registered. Heads: {heads.Count}, Tails: {tails.Count}</color>");
    }

    public List<PlayerUnit> GetTeam(TeamColor color)
    {
        return (color == TeamColor.Heads) ? headsUnits : tailsUnits;
    }

    public TeamInfo GetTeamInfo(TeamColor color)
    {
        return (color == TeamColor.Heads) ? headsInfo : tailsInfo;
    }

    public void ResetAllPlayers(TeamColor scorerTeam)
    {
        foreach (var p in headsUnits) p.ResetToStart();
        foreach (var p in tailsUnits) p.ResetToStart();

        TeamColor defenderTeam = (scorerTeam == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
        List<PlayerUnit> teamList = GetTeam(defenderTeam);
        
        if (teamList.Count > 0 && Ball.Instance != null)
        {
            PlayerUnit newOwner = teamList.Count > 1 ? teamList[1] : teamList[0];
            Ball.Instance.SetOwner(newOwner);
        }
    }
}

public static class TeamNameGenerator
{
    private static string[] adjectives = { "Mighty", "Swift", "Golden", "Iron", "Wild", "Brave", "Silent", "Neon", "Atomic", "Turbo" };
    private static string[] nouns = { "Eagles", "Sharks", "Lions", "Cobras", "Wolves", "Dragons", "Knights", "Titans", "Rockets", "Comets" };

    public static string Generate()
    {
        return adjectives[Random.Range(0, adjectives.Length)] + " " + nouns[Random.Range(0, nouns.Length)];
    }
}
