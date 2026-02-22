using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;

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
        if (TeamManager.Instance == null || TurnManager.Instance == null)
        {
            Debug.LogError("Required Managers (TeamManager/TurnManager) are missing!");
            return;
        }

        List<PlayerUnit> bluePlayers = new List<PlayerUnit>();
        List<PlayerUnit> redPlayers = new List<PlayerUnit>();

        // 1. Takımları oluştur
        SpawnTeam(blueRoster, TeamColor.Blue, blueSpawnPos, bluePlayers);
        SpawnTeam(redRoster, TeamColor.Red, redSpawnPos, redPlayers);

        // 2. Takımları Kaydet (TeamManager)
        TeamManager.Instance.RegisterTeams(redPlayers, bluePlayers);

        // 3. Hava Atışı (Jump Ball)
        StartCoroutine(PlayJumpBallRoutine(redPlayers, bluePlayers));

        Debug.Log("Match Setup initiated with Jump Ball!");
    }

    private IEnumerator PlayJumpBallRoutine(List<PlayerUnit> red, List<PlayerUnit> blue)
    {
        int blueRoll = Random.Range(1, 7);
        int redRoll = Random.Range(1, 7);

        while (blueRoll == redRoll)
        {
            blueRoll = Random.Range(1, 7);
            redRoll = Random.Range(1, 7);
        }

        startingBallTeam = (blueRoll > redRoll) ? TeamColor.Blue : TeamColor.Red;
        
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.AnimateDiceRoll(
                redRoll, Color.red, blueRoll, Color.blue, "vs", null));
            
            string startMsg = (startingBallTeam == TeamColor.Blue) ? "BLUE TEAM STARTS!" : "RED TEAM STARTS!";
            yield return StartCoroutine(UIManager.Instance.ShowAnnouncementRoutine(startMsg, 1.5f));
        }

        // 4. Maçı Başlat (TurnManager) - Bu otomatik olarak UI'ı da güncelleyecek
        TurnManager.Instance.InitializeMatch(startingBallTeam);

        AssignInitialBall(red, blue);
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
