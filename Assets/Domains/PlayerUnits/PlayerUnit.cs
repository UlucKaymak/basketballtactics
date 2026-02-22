using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerUnit : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerUnitData unitData;
    public TeamColor team; 

    [Header("Current State")]
    public Vector2Int startingGridPos;
    public Vector2Int currentGridPos;
    public bool hasBall = false;
    public bool isStunned = false;
    public int stunTurnsLeft = 0; 
    public bool hasActed = false; 
    protected bool isHovered = false; // Hover durumu takibi

    [Header("Movement Settings")]
    public float moveDuration = 0.4f;
    public float jumpPower = 0.2f;

    [Header("Visual Feedback (Test)")]
    public Color colorHasBall = Color.green;
    protected SpriteRenderer spriteRenderer;

    protected GridManager gridManager;

    public void SetSelected(bool isSelected)
    {
        if (spriteRenderer != null)
        {
            if (isSelected)
            {
                transform.DOScale(Vector3.one * 1.1f, 0.2f).SetEase(Ease.OutBack);
            }
            else
            {
                transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.Linear);
            }
        }
    }

    public void Initialize(PlayerUnitData data, TeamColor teamColor, Vector2Int startPos)
    {
        unitData = data;
        team = teamColor;
        startingGridPos = startPos;
        currentGridPos = startPos;
        
        gridManager = Object.FindFirstObjectByType<GridManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        SnapToGrid();
        UpdateVisuals();
    }

    private void Start()
    {
        if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void ResetToStart()
    {
        Vector2Int oldPos = currentGridPos;
        currentGridPos = startingGridPos;
        hasActed = false;
        isStunned = false;
        stunTurnsLeft = 0; 
        hasBall = false;
        
        if (gridManager != null)
        {
            transform.position = gridManager.GetWorldPosition(currentGridPos.x, currentGridPos.y);
            gridManager.UpdateOccupantPositions(oldPos);
            gridManager.UpdateOccupantPositions(currentGridPos);
        }
        UpdateVisuals();
    }

    public virtual void UpdateVisuals()
    {
        if (spriteRenderer == null || unitData == null) return;
        spriteRenderer.sprite = unitData.baseSprite;

        TeamInfo info = TeamManager.Instance.GetTeamInfo(team);
        Color baseColor = info.teamColor;

        if (isStunned) 
        {
            baseColor = Color.gray;
        }
        else if (hasBall) 
        {
            baseColor = Color.Lerp(baseColor, Color.green, 0.5f);
        }

        // HOVER HIGHLIGHT: Eğer mouse üzerindeyse rengi %25 beyaza (parlaklığa) yaklaştır
        if (isHovered && !isStunned)
        {
            baseColor = Color.Lerp(baseColor, Color.white, 0.25f);
        }

        spriteRenderer.color = baseColor;
    }

    private void OnMouseEnter()
    {
        isHovered = true;
        UpdateVisuals();
    }

    private void OnMouseExit()
    {
        isHovered = false;
        UpdateVisuals();
    }

    public void SnapToGrid()
    {
        if (gridManager != null)
        {
            gridManager.UpdateOccupantPositions(currentGridPos);
        }
    }

    public bool IsInMovementRange(Vector2Int targetPos)
    {
        if (gridManager != null && gridManager.IsTileOccupied(targetPos) && targetPos != currentGridPos) return false; 
        List<Vector2Int> reachable = GetReachableTiles();
        return reachable.Contains(targetPos);
    }

    public List<Vector2Int> GetReachableTiles()
    {
        List<Vector2Int> reachableTiles = new List<Vector2Int>();
        if (unitData == null || gridManager == null) return reachableTiles;

        Queue<(Vector2Int pos, int dist)> queue = new Queue<(Vector2Int, int)>();
        queue.Enqueue((currentGridPos, 0));
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        visited.Add(currentGridPos);
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (dist > 0) reachableTiles.Add(current);
            if (dist >= unitData.speed) continue;
            foreach (var offset in neighbors)
            {
                Vector2Int next = current + offset;
                if (gridManager.IsInBounds(next) && !visited.Contains(next) && !gridManager.IsTileOccupied(next))
                {
                    visited.Add(next);
                    queue.Enqueue((next, dist + 1));
                }
            }
        }
        return reachableTiles;
    }

    public IEnumerator MoveTo(Vector2Int targetPos)
    {
        if (gridManager == null) yield break;
        
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);

        Vector2Int oldPos = currentGridPos;
        Vector3 targetWorldPos = gridManager.GetWorldPosition(targetPos.x, targetPos.y);
        
        transform.DOJump(targetWorldPos, jumpPower, 1, moveDuration).SetEase(Ease.OutQuad);
        
        yield return new WaitForSeconds(moveDuration);

        currentGridPos = targetPos;

        gridManager.UpdateOccupantPositions(oldPos);
        gridManager.UpdateOccupantPositions(currentGridPos);

        CheckForBallPickup();

        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);

        if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
    }

    private void CheckForBallPickup()
    {
        if (hasBall) return;
        Ball ball = Ball.Instance;
        if (ball != null && ball.currentOwner == null)
        {
            Vector2Int ballGridPos = gridManager.GetGridPosition(ball.transform.position);
            if (ballGridPos == currentGridPos) ball.SetOwner(this);
        }
    }

    public void Pass(PlayerUnit targetPlayer)
    {
        if (!hasBall || targetPlayer == null || targetPlayer == this) return;
        
        int distance = Mathf.Abs(targetPlayer.currentGridPos.x - currentGridPos.x) + Mathf.Abs(targetPlayer.currentGridPos.y - currentGridPos.y);
        int roll = DiceManager.Instance.RollD6();
        int bonus = (unitData != null ? unitData.passingBonus : 0);
        int totalValue = roll + bonus;
        bool success = totalValue >= distance;

        Color myColor = TeamManager.Instance.GetTeamInfo(team).teamColor;
        string icon = UIManager.Instance.GetDiceIcon(roll);
        string colorHex = "#" + ColorUtility.ToHtmlStringRGB(myColor);
        string note = $"<color={colorHex}>{icon}</color> + {bonus} = {totalValue} ({distance})";

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll, myColor, null, null, "", () => {
            hasBall = false;
            UpdateVisuals();

            if (success) 
            {
                Ball.Instance.FlyTo(targetPlayer);
                AnnouncementManager.Instance.SendMiniAnnouncement("Great Pass!", myColor);
            }
            else 
            {
                Ball.Instance.FlyToDistance(currentGridPos, targetPlayer.transform.position - transform.position, totalValue);
                AnnouncementManager.Instance.SendMiniAnnouncement("Pass Intercepted/Failed!", Color.gray);
            }
            
            hasActed = true;
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }, note));
    }

    public void Shoot(Hoop targetHoop)
    {
        if (!hasBall || targetHoop == null) return;
        
        int distance = Mathf.Abs(targetHoop.gridPos.x - currentGridPos.x) + Mathf.Abs(targetHoop.gridPos.y - currentGridPos.y);
        int roll = DiceManager.Instance.RollD6();
        int bonus = (unitData != null ? unitData.shootingBonus : 0);
        int totalValue = roll + bonus;
        bool success = totalValue >= distance;

        Color myColor = TeamManager.Instance.GetTeamInfo(team).teamColor;
        string icon = UIManager.Instance.GetDiceIcon(roll);
        string colorHex = "#" + ColorUtility.ToHtmlStringRGB(myColor);
        string note = $"<color={colorHex}>{icon}</color> + {bonus} = {totalValue} ({distance})";

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll, myColor, null, null, "", () => {
            hasBall = false;
            UpdateVisuals();

            if (success) 
            {
                Ball.Instance.FlyToPosition(targetHoop.transform.position, true, team);
                AnnouncementManager.Instance.SendMiniAnnouncement("He takes the shot!", myColor);
            }
            else 
            {
                // FAILURE: Önce potaya gitsin sonra bouncelansın
                Ball.Instance.FlyToHoopAndBounce(targetHoop.transform.position, currentGridPos);
                AnnouncementManager.Instance.SendMiniAnnouncement("Off the rim!", Color.gray);
            }
            
            hasActed = true;
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }, note));
    }

    public void Attack(PlayerUnit targetPlayer)
    {
        if (hasBall) return;
        
        int myRoll = DiceManager.Instance.RollD6();
        int myBonus = (unitData != null ? unitData.defenceBonus : 0);
        int myTotal = myRoll + myBonus;
        
        int targetRoll = DiceManager.Instance.RollD6();
        int targetBonus = (targetPlayer.unitData != null ? targetPlayer.unitData.defenceBonus : 0);
        int targetTotal = targetRoll + targetBonus;

        Color myColor = TeamManager.Instance.GetTeamInfo(team).teamColor;
        Color targetColor = TeamManager.Instance.GetTeamInfo(targetPlayer.team).teamColor;

        int rollHeads = (team == TeamColor.Heads) ? myRoll : targetRoll;
        int rollTails = (team == TeamColor.Tails) ? myRoll : targetRoll;

        string hIcon = UIManager.Instance.GetDiceIcon(rollHeads);
        string tIcon = UIManager.Instance.GetDiceIcon(rollTails);
        string note = $"<color=white>{hIcon}</color> / <color=white>{tIcon}</color>";

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(rollHeads, TeamManager.Instance.headsInfo.teamColor, rollTails, TeamManager.Instance.tailsInfo.teamColor, "/", () => {
            StartCoroutine(MoveTo(targetPlayer.currentGridPos));

            if (myTotal > targetTotal)
            {
                targetPlayer.Stun();
                if (targetPlayer.hasBall) 
                {
                    Ball.Instance.SetOwner(this);
                    AnnouncementManager.Instance.SendAnnouncement("STEAL!", 1.0f, AnnouncementType.Combat, myColor);
                }
                else
                {
                    AnnouncementManager.Instance.SendAnnouncement("CRUSHED!", 1.0f, AnnouncementType.Combat, myColor);
                }
            }
            else
            {
                this.Stun();
                if (this.hasBall) 
                {
                    Ball.Instance.SetOwner(targetPlayer);
                    AnnouncementManager.Instance.SendAnnouncement("STRIPPED!", 1.0f, AnnouncementType.Combat, targetColor);
                }
                else
                {
                    AnnouncementManager.Instance.SendAnnouncement("BLOCKED!", 1.0f, AnnouncementType.Combat, targetColor);
                }
            }
            
            hasActed = true;
            if (TurnManager.Instance != null) TurnManager.Instance.CheckAutoEndTurn();
        }, note));
    }

    public void Stun()
    {
        isStunned = true;
        stunTurnsLeft = 1; 
        UpdateVisuals();
    }
}
