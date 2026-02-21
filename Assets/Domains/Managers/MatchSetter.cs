using UnityEngine;
using System.Collections.Generic;

public class MatchSetter : MonoBehaviour
{
    [Header("Team Rosters (Scriptable Objects)")]
    public List<PlayerUnitData> blueRoster = new List<PlayerUnitData>();
    public List<PlayerUnitData> redRoster = new List<PlayerUnitData>();

    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public List<Vector2Int> blueSpawnPos = new List<Vector2Int>();
    public List<Vector2Int> redSpawnPos = new List<Vector2Int>();

    [Header("Initial Possession")]
    public TeamColor startingBallTeam = TeamColor.Blue;

    private void Start()
    {
        SetupMatch();
    }

    private void SetupMatch()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager Instance is missing!");
            return;
        }

        List<PlayerUnit> bluePlayers = new List<PlayerUnit>();
        List<PlayerUnit> redPlayers = new List<PlayerUnit>();

        // 1. Oyuncuları oluştur ve geçici listelere doldur
        SpawnTeam(blueRoster, TeamColor.Blue, blueSpawnPos, bluePlayers);
        SpawnTeam(redRoster, TeamColor.Red, redSpawnPos, redPlayers);

        // 2. TurnManager'a hazır listeleri teslim et (Kontrolü ona devret)
        TurnManager.Instance.InitializeTeams(redPlayers, bluePlayers);

        // 3. Topu ata
        AssignInitialBall(redPlayers, bluePlayers);

        Debug.Log("Match Setup Complete. Control handed over to TurnManager.");
    }

    private void SpawnTeam(List<PlayerUnitData> roster, TeamColor team, List<Vector2Int> positions, List<PlayerUnit> listToFill)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            Vector2Int pos = (i < positions.Count) ? positions[i] : new Vector2Int(team == TeamColor.Red ? 6 : 0, i);
            GameObject go = Instantiate(playerPrefab);
            go.name = roster[i].playerName + $" ({team})";
            PlayerUnit pu = go.GetComponent<PlayerUnit>();
            
            if (pu != null)
            {
                pu.Initialize(roster[i], team, pos);
                listToFill.Add(pu);
            }
        }
    }

    private void AssignInitialBall(List<PlayerUnit> red, List<PlayerUnit> blue)
    {
        if (Ball.Instance == null) return;

        List<PlayerUnit> team = (startingBallTeam == TeamColor.Red) ? red : blue;
        if (team.Count > 0)
        {
            Ball.Instance.SetOwner(team[0]);
        }
    }
}
