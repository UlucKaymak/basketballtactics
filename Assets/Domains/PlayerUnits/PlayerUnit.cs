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
    public bool hasActed = false; 

    [Header("Movement Settings")]
    public float moveDuration = 0.4f;
    public float jumpPower = 0.2f;

    [Header("Visual Feedback (Test)")]
    public Color colorNoBall = Color.red;
    public Color colorHasBall = Color.green;
    public SpriteRenderer highlightSprite; // Seçim halkası referansı
    private SpriteRenderer spriteRenderer;

    private GridManager gridManager;

    public void SetSelected(bool isSelected)
    {
        if (highlightSprite != null)
        {
            highlightSprite.gameObject.SetActive(isSelected);
            if (isSelected)
            {
                // Seçildiğinde hafif bir zıplama animasyonu
                highlightSprite.transform.localScale = Vector3.zero;
                highlightSprite.transform.DOScale(Vector3.one * 1.2f, 0.2f).SetEase(Ease.OutBack);
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
        hasBall = false;
        
        if (gridManager != null)
        {
            gridManager.UpdateOccupantPositions(oldPos);
            gridManager.UpdateOccupantPositions(currentGridPos);
        }
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer == null || unitData == null) return;
        spriteRenderer.sprite = (team == TeamColor.Blue) ? unitData.blueTeamSprite : unitData.redTeamSprite;

        if (isStunned) spriteRenderer.color = Color.gray;
        else spriteRenderer.color = hasBall ? colorHasBall : colorNoBall;
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
        
        // State'i meşgul yap
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);

        Vector2Int oldPos = currentGridPos;
        Vector3 targetWorldPos = gridManager.GetWorldPosition(targetPos.x, targetPos.y);
        
        // DOTween jump movement
        transform.DOJump(targetWorldPos, jumpPower, 1, moveDuration).SetEase(Ease.OutQuad);
        
        yield return new WaitForSeconds(moveDuration);

        currentGridPos = targetPos;

        // Her iki karedeki oyuncu dizilimini güncelle
        gridManager.UpdateOccupantPositions(oldPos);
        gridManager.UpdateOccupantPositions(currentGridPos);

        // HAREKET BİTİNCE TOPU KONTROL ET
        CheckForBallPickup();

        // State'i tekrar boşa çıkar
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Idle);
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
        int roll = Random.Range(1, 7);
        int bonus = (unitData != null ? unitData.passingBonus : 0);
        int totalValue = roll + bonus;
        bool success = totalValue >= distance;

        Color myColor = (team == TeamColor.Blue) ? Color.blue : Color.red;

        // Polish: Zar dönsün sonra işlem yapılsın
        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll, myColor, null, null, "", () => {
            if (UIManager.Instance != null)
            {
                string icon = UIManager.Instance.GetDiceIcon(roll);
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(myColor);
                UIManager.Instance.ShowCalculation($"Pass: <color={colorHex}>{icon}</color> + {bonus} = {totalValue} (Target: {distance})");
            }

            hasBall = false;
            UpdateVisuals();

            if (success) Ball.Instance.FlyTo(targetPlayer);
            else Ball.Instance.FlyToDistance(currentGridPos, targetPlayer.transform.position - transform.position, totalValue);
            
            hasActed = true;
        }));
    }

    public void Shoot(Hoop targetHoop)
    {
        if (!hasBall || targetHoop == null) return;
        
        int distance = Mathf.Abs(targetHoop.gridPos.x - currentGridPos.x) + Mathf.Abs(targetHoop.gridPos.y - currentGridPos.y);
        int roll = Random.Range(1, 7);
        int bonus = (unitData != null ? unitData.shootingBonus : 0);
        int totalValue = roll + bonus;
        bool success = totalValue >= distance;

        Color myColor = (team == TeamColor.Blue) ? Color.blue : Color.red;

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(roll, myColor, null, null, "", () => {
            if (UIManager.Instance != null)
            {
                string icon = UIManager.Instance.GetDiceIcon(roll);
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(myColor);
                UIManager.Instance.ShowCalculation($"Shoot: <color={colorHex}>{icon}</color> + {bonus} = {totalValue} (Target: {distance})");
            }

            hasBall = false;
            UpdateVisuals();

            if (success) Ball.Instance.FlyToPosition(targetHoop.transform.position, true, team);
            else Ball.Instance.FlyToDistance(currentGridPos, new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0), 3);
            
            hasActed = true;
        }));
    }

    public void Attack(PlayerUnit targetPlayer)
    {
        if (hasBall) return;
        
        int myRoll = Random.Range(1, 7);
        int myBonus = (unitData != null ? unitData.defenceBonus : 0);
        int myTotal = myRoll + myBonus;
        
        int targetRoll = Random.Range(1, 7);
        int targetBonus = (targetPlayer.unitData != null ? targetPlayer.unitData.defenceBonus : 0);
        int targetTotal = targetRoll + targetBonus;

        // Zar sıralamasını sabitleyelim: Red her zaman solda, Blue her zaman sağda.
        int rollRed = (team == TeamColor.Red) ? myRoll : targetRoll;
        int rollBlue = (team == TeamColor.Blue) ? myRoll : targetRoll;

        StartCoroutine(UIManager.Instance.AnimateDiceRoll(rollRed, Color.red, rollBlue, Color.blue, "/", () => {
            if (UIManager.Instance != null)
            {
                string rIcon = UIManager.Instance.GetDiceIcon(rollRed);
                string bIcon = UIManager.Instance.GetDiceIcon(rollBlue);
                
                UIManager.Instance.ShowCalculation($"<color=red>{rIcon}</color> / <color=blue>{bIcon}</color>");
            }

            StartCoroutine(MoveTo(targetPlayer.currentGridPos));

            if (myTotal > targetTotal)
            {
                targetPlayer.Stun();
                if (targetPlayer.hasBall) Ball.Instance.SetOwner(this);
            }
            else
            {
                this.Stun();
                if (this.hasBall) Ball.Instance.SetOwner(targetPlayer);
            }
            hasActed = true;
        }));
    }

    public void Stun()
    {
        isStunned = true;
        UpdateVisuals();
    }
}
