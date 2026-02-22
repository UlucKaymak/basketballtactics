using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NaughtyAttributes;

public class MatchSetter : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public List<Vector2Int> headsSpawnPos = new List<Vector2Int>();
    public List<Vector2Int> tailsSpawnPos = new List<Vector2Int>();

    [Header("Initial Possession")]
    public TeamColor startingBallTeam = TeamColor.Heads;

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

        List<PlayerUnit> headsUnits = new List<PlayerUnit>();
        List<PlayerUnit> tailsUnits = new List<PlayerUnit>();

        // 1. Takımları TeamManager'daki roster'lardan çekip spawn et
        SpawnTeam(TeamManager.Instance.headsInfo.roster, TeamColor.Heads, headsSpawnPos, headsUnits);
        SpawnTeam(TeamManager.Instance.tailsInfo.roster, TeamColor.Tails, tailsSpawnPos, tailsUnits);

        // 2. Takımları Kaydet (Runtime listeler)
        TeamManager.Instance.RegisterActiveUnits(headsUnits, tailsUnits);

        // 3. Hava Atışı (Jump Ball)
        StartCoroutine(PlayJumpBallRoutine(headsUnits, tailsUnits));

        Debug.Log("Match Setup initiated with Jump Ball!");
    }

    private IEnumerator PlayJumpBallRoutine(List<PlayerUnit> heads, List<PlayerUnit> tails)
    {
        int headsRoll = DiceManager.Instance.RollD6();
        int tailsRoll = DiceManager.Instance.RollD6();

        while (headsRoll == tailsRoll)
        {
            headsRoll = DiceManager.Instance.RollD6();
            tailsRoll = DiceManager.Instance.RollD6();
        }

        startingBallTeam = (headsRoll > tailsRoll) ? TeamColor.Heads : TeamColor.Tails;
        
        if (UIManager.Instance != null)
        {
            Color headsColor = TeamManager.Instance.headsInfo.teamColor;
            Color tailsColor = TeamManager.Instance.tailsInfo.teamColor;

            yield return StartCoroutine(UIManager.Instance.AnimateDiceRoll(
                headsRoll, headsColor, tailsRoll, tailsColor, "vs", null));
            
            string startMsg = $"{TeamManager.Instance.GetTeamInfo(startingBallTeam).teamName} STARTS!";
            yield return StartCoroutine(AnnouncementManager.Instance.SendAnnouncementAndWait(startMsg, 1.5f, AnnouncementType.Turn, TeamManager.Instance.GetTeamInfo(startingBallTeam).teamColor));
        }

        TurnManager.Instance.InitializeMatch(startingBallTeam);

        AssignInitialBall(heads, tails);
    }

    private void SpawnTeam(List<PlayerUnitData> roster, TeamColor team, List<Vector2Int> positions, List<PlayerUnit> listToFill)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            Vector2Int pos = (i < positions.Count) ? positions[i] : new Vector2Int(team == TeamColor.Heads ? 0 : 6, i);
            GameObject go = Instantiate(playerPrefab);
            go.name = roster[i].playerName + $" ({team})";
            
            PlayerUnit existing = go.GetComponent<PlayerUnit>();
            if (existing != null) DestroyImmediate(existing);

            PlayerUnit pu = null;
            switch (roster[i].playerType)
            {
                case PlayerType.Offensive: pu = go.AddComponent<OffensivePlayerUnit>(); break;
                case PlayerType.Defensive: pu = go.AddComponent<DefensivePlayerUnit>(); break;
                case PlayerType.Support:   pu = go.AddComponent<SupportPlayerUnit>(); break;
                default:                  pu = go.AddComponent<PlayerUnit>(); break;
            }
            
            if (pu != null)
            {
                pu.Initialize(roster[i], team, pos);
                listToFill.Add(pu);
            }
        }
    }

    private void AssignInitialBall(List<PlayerUnit> heads, List<PlayerUnit> tails)
    {
        if (Ball.Instance == null) return;

        List<PlayerUnit> team = (startingBallTeam == TeamColor.Heads) ? heads : tails;
        if (team.Count > 0)
        {
            Ball.Instance.SetOwner(team[0]);
        }
    }
}
