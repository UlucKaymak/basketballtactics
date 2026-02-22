using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum AIActionType { Move, Pass, Shoot, Attack, Wait }

public class AIDecision
{
    public AIActionType actionType;
    public Vector2Int targetGridPos;
    public PlayerUnit targetPlayer;
    public Hoop targetHoop;
    public float score;

    public AIDecision(AIActionType type, float score)
    {
        this.actionType = type;
        this.score = score;
    }
}

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    public TeamColor aiTeam = TeamColor.Tails;
    public bool isAiEnabled = true;

    [Header("Delays")]
    public float delayBetweenPlayers = 0.8f;
    public float thinkingDelay = 1.0f;

    private Coroutine currentTurnCoroutine;
    private Dictionary<PlayerUnit, PlayerUnit> markingAssignments = new Dictionary<PlayerUnit, PlayerUnit>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        TurnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(TeamColor activeTeam, int turn)
    {
        if (!isAiEnabled) return;
        if (currentTurnCoroutine != null) StopCoroutine(currentTurnCoroutine);

        if (activeTeam == aiTeam)
        {
            Debug.Log("<color=magenta>[AIManager] AI Turn Started.</color>");
            currentTurnCoroutine = StartCoroutine(PlayTurnRoutine());
        }
    }

    private IEnumerator PlayTurnRoutine()
    {
        yield return new WaitForSeconds(thinkingDelay);

        AssignMarkingTargets(); 

        List<PlayerUnit> players = TeamManager.Instance.GetTeam(aiTeam);

        foreach (var player in players)
        {
            if (TurnManager.Instance.activeTeam != aiTeam) break;
            if (player.isStunned || player.hasActed) continue;

            yield return StartCoroutine(EvaluateAndExecute(player));

            while (StateManager.Instance != null && !StateManager.Instance.IsIdle())
            {
                yield return null;
            }

            yield return new WaitForSeconds(delayBetweenPlayers);
        }

        if (TurnManager.Instance.activeTeam == aiTeam)
        {
            Debug.Log("<color=magenta>[AIManager] AI Turn Ended Normally.</color>");
            TurnManager.Instance.EndTurn();
        }
        
        currentTurnCoroutine = null;
    }

    private void AssignMarkingTargets()
    {
        markingAssignments.Clear();
        List<PlayerUnit> myTeam = TeamManager.Instance.GetTeam(aiTeam);
        TeamColor enemyColor = (aiTeam == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
        List<PlayerUnit> enemies = TeamManager.Instance.GetTeam(enemyColor);

        List<PlayerUnit> availableEnemies = new List<PlayerUnit>(enemies);
        foreach (var me in myTeam)
        {
            if (availableEnemies.Count == 0) break;
            
            PlayerUnit bestTarget = availableEnemies
                .OrderBy(e => GetDistance(me.currentGridPos, e.currentGridPos))
                .First();
            
            markingAssignments[me] = bestTarget;
            availableEnemies.Remove(bestTarget);
        }
    }

    private IEnumerator EvaluateAndExecute(PlayerUnit player)
    {
        List<AIDecision> possibleDecisions = new List<AIDecision>();

        if (player.hasBall)
        {
            Hoop targetHoop = FindEnemyHoop(player);
            if (targetHoop != null)
            {
                int dist = GetDistance(player.currentGridPos, targetHoop.gridPos);
                float shootScore = 0f;
                if (dist <= 10) 
                {
                    shootScore = 1000f; 
                    shootScore += (15 - dist) * 20f; 
                    
                    var d = new AIDecision(AIActionType.Shoot, shootScore);
                    d.targetHoop = targetHoop;
                    possibleDecisions.Add(d);
                }
            }
        }

        if (player.hasBall)
        {
            var teammates = TeamManager.Instance.GetTeam(player.team);
            Hoop targetHoop = FindEnemyHoop(player);
            foreach (var t in teammates)
            {
                if (t == player || t.isStunned) continue;
                int distToTeammate = GetDistance(player.currentGridPos, t.currentGridPos);
                if (distToTeammate > 8) continue; 

                float passScore = 150f; 
                if (targetHoop != null)
                {
                    int myDistToHoop = GetDistance(player.currentGridPos, targetHoop.gridPos);
                    int theirDistToHoop = GetDistance(t.currentGridPos, targetHoop.gridPos);
                    if (theirDistToHoop < myDistToHoop) passScore += (myDistToHoop - theirDistToHoop) * 20f;
                    else passScore -= 100f; 
                }

                var d = new AIDecision(AIActionType.Pass, passScore);
                d.targetPlayer = t;
                possibleDecisions.Add(d);
            }
        }

        if (!player.hasBall)
        {
            TeamColor enemyColor = (player.team == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
            var enemies = TeamManager.Instance.GetTeam(enemyColor);
            foreach (var e in enemies)
            {
                int dist = GetDistance(player.currentGridPos, e.currentGridPos);
                if (dist <= player.unitData.speed) 
                {
                    float attackScore = (dist <= 1) ? 500f : 180f;
                    if (!e.hasBall) attackScore -= 100f; 
                    
                    if (markingAssignments.ContainsKey(player) && markingAssignments[player] == e)
                        attackScore += 30f;

                    attackScore += (player.unitData.defenceBonus - e.unitData.defenceBonus) * 5f; 
                    
                    var d = new AIDecision(AIActionType.Attack, attackScore);
                    d.targetPlayer = e;
                    possibleDecisions.Add(d);
                }
            }
        }

        List<Vector2Int> reachableTiles = player.GetReachableTiles();
        Vector2Int moveTargetGoal;
        float multiplier;

        if (player.hasBall) 
        {
            Hoop h = FindEnemyHoop(player);
            moveTargetGoal = h != null ? h.gridPos : player.currentGridPos;
            multiplier = 10f; 
        }
        else 
        {
            if (markingAssignments.ContainsKey(player))
                moveTargetGoal = markingAssignments[player].currentGridPos;
            else
                moveTargetGoal = GetBallPosition();
            multiplier = 15f; 
        }
        
        foreach (var tile in reachableTiles)
        {
            float moveScore = 50f; 
            int distToGoal = GetDistance(tile, moveTargetGoal);
            moveScore += (25 - distToGoal) * multiplier; 

            var d = new AIDecision(AIActionType.Move, moveScore);
            d.targetGridPos = tile;
            possibleDecisions.Add(d);
        }

        possibleDecisions.Add(new AIDecision(AIActionType.Wait, 10f));

        AIDecision best = possibleDecisions.OrderByDescending(x => x.score).First();

        switch (best.actionType)
        {
            case AIActionType.Shoot:
                player.Shoot(best.targetHoop);
                break;
            case AIActionType.Pass:
                player.Pass(best.targetPlayer);
                break;
            case AIActionType.Attack:
                player.Attack(best.targetPlayer);
                break;
            case AIActionType.Move:
                yield return StartCoroutine(player.MoveTo(best.targetGridPos));
                player.hasActed = true;
                if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
                break;
            case AIActionType.Wait:
                player.hasActed = true;
                if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
                break;
        }
    }

    private Hoop FindEnemyHoop(PlayerUnit player)
    {
        TeamColor enemyTeam = (player.team == TeamColor.Heads) ? TeamColor.Tails : TeamColor.Heads;
        return Object.FindObjectsByType<Hoop>(FindObjectsSortMode.None)
                     .FirstOrDefault(h => h.hoopTeam == enemyTeam);
    }

    private Vector2Int GetBallPosition()
    {
        if (Ball.Instance.currentOwner != null) return Ball.Instance.currentOwner.currentGridPos;
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        return gm != null ? gm.GetGridPosition(Ball.Instance.transform.position) : Vector2Int.zero;
    }

    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
