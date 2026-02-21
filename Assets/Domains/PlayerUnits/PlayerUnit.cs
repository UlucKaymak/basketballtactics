using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerUnit : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerUnitData unitData;
    public TeamColor team; // Takım bilgisi eklendi

    [Header("Current State")]
    public Vector2Int startingGridPos; // Reset için eklendi
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
    private SpriteRenderer spriteRenderer;

    private GridManager gridManager;

    public void Initialize(PlayerUnitData data, TeamColor teamColor, Vector2Int startPos)
    {
        unitData = data;
        team = teamColor;
        startingGridPos = startPos; // Sakla
        currentGridPos = startPos;
        
        gridManager = Object.FindFirstObjectByType<GridManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        SnapToGrid();
        UpdateVisuals();
    }

    public void ResetToStart()
    {
        currentGridPos = startingGridPos;
        hasActed = false;
        isStunned = false;
        hasBall = false;
        SnapToGrid();
        UpdateVisuals();
    }

    private void Start()
    {
        // Initialize çağrıldığı için Start'ta tekrar gridManager bulmaya gerek yok ama güvenlik için:
        if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer == null || unitData == null) return;

        // 1. Takıma göre sprite'ı ayarla
        spriteRenderer.sprite = (team == TeamColor.Blue) ? unitData.blueTeamSprite : unitData.redTeamSprite;

        // 2. Duruma göre rengi/tinti ayarla
        if (isStunned)
        {
            spriteRenderer.color = Color.gray;
        }
        else
        {
            spriteRenderer.color = hasBall ? colorHasBall : colorNoBall;
        }
    }

    public void SnapToGrid()
    {
        if (gridManager != null)
        {
            transform.position = gridManager.GetWorldPosition(currentGridPos.x, currentGridPos.y);
        }
    }

    public bool IsInMovementRange(Vector2Int targetPos)
    {
        // Hedef kare doluysa (ve hedef biz değilsek) oraya gidemeyiz
        if (gridManager != null && gridManager.IsTileOccupied(targetPos) && targetPos != currentGridPos)
        {
            // NOT: Combat için rakip karesini seçebiliyoruz, onu InputController halledecek.
            // Ama düz yürüme için hedef boş olmalı.
            return false; 
        }

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
            
            if (dist > 0) reachableTiles.Add(current); // Kendi karemizi eklemeyelim

            if (dist >= unitData.speed) continue;

            foreach (var offset in neighbors)
            {
                Vector2Int next = current + offset;

                if (gridManager.IsInBounds(next) && !visited.Contains(next))
                {
                    // GDD: Başka bir oyuncu varsa o kare duvar sayılır (geçilemez)
                    if (!gridManager.IsTileOccupied(next))
                    {
                        visited.Add(next);
                        queue.Enqueue((next, dist + 1));
                    }
                }
            }
        }

        return reachableTiles;
    }

    public IEnumerator MoveTo(Vector2Int targetPos)
    {
        if (gridManager == null) yield break;

        Vector3 targetWorldPos = gridManager.GetWorldPosition(targetPos.x, targetPos.y);
        
        // DOTween jump movement
        transform.DOJump(targetWorldPos, jumpPower, 1, moveDuration).SetEase(Ease.OutQuad);
        
        yield return new WaitForSeconds(moveDuration);

        currentGridPos = targetPos;

        // HAREKET BİTİNCE TOPU KONTROL ET
        CheckForBallPickup();
    }

    private void CheckForBallPickup()
    {
        if (hasBall) return; // Zaten top varsa bir şey yapma

        Ball ball = Ball.Instance;
        if (ball != null && ball.currentOwner == null) // Top sahipsizse
        {
            Vector2Int ballGridPos = gridManager.GetGridPosition(ball.transform.position);
            if (ballGridPos == currentGridPos)
            {
                ball.SetOwner(this);
                Debug.Log($"{unitData?.playerName} picked up the ball!");
            }
        }
    }

    /// <summary>
    /// Try to pass the ball to another player.
    /// Distance-based d6 challenge.
    /// </summary>
    public void Pass(PlayerUnit targetPlayer)
    {
        if (!hasBall || targetPlayer == null || targetPlayer == this) return;

        // CD: Her tile mesafesi için +1
        int distance = Mathf.Abs(targetPlayer.currentGridPos.x - currentGridPos.x) + Mathf.Abs(targetPlayer.currentGridPos.y - currentGridPos.y);
        
        int roll = Random.Range(1, 7); // d6
        int bonus = (unitData != null ? unitData.passingBonus : 0);
        int totalValue = roll + bonus;

        bool success = totalValue >= distance;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDiceResult(roll, bonus, distance, success);
        }

        Debug.Log($"Pass attempt by {unitData?.playerName} to {targetPlayer.unitData?.playerName}. Target CD: {distance}, Total: {totalValue}");

        hasBall = false;
        UpdateVisuals();

        if (success)
        {
            Debug.Log("<color=green>Pass Successful!</color>");
            Ball.Instance.FlyTo(targetPlayer);
        }
        else
        {
            Debug.Log("<color=red>Pass Failed!</color>");
            // Atılan toplam zar (roll + bonus) kadar mesafeye topu düşür
            Vector3 direction = targetPlayer.transform.position - transform.position;
            Ball.Instance.FlyToDistance(currentGridPos, direction, totalValue); 
        }
    }

    /// <summary>
    /// Try to shoot the ball into a hoop.
    /// </summary>
    public void Shoot(Hoop targetHoop)
    {
        if (!hasBall || targetHoop == null) return;

        int distance = Mathf.Abs(targetHoop.gridPos.x - currentGridPos.x) + Mathf.Abs(targetHoop.gridPos.y - currentGridPos.y);
        
        int roll = Random.Range(1, 7);
        int bonus = (unitData != null ? unitData.shootingBonus : 0);
        int totalValue = roll + bonus;

        bool success = totalValue >= distance;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowDiceResult(roll, bonus, distance, success);

        hasBall = false;
        UpdateVisuals();

        if (success)
        {
            Ball.Instance.FlyToPosition(targetHoop.transform.position, true, team);
        }
        else
        {
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            Ball.Instance.FlyToDistance(currentGridPos, randomDir, 3);
        }
        
        hasActed = true;
    }

    public void Attack(PlayerUnit targetPlayer)
    {
        if (hasBall) 
        {
            Debug.Log("Cannot attack while holding the ball!");
            return;
        }

        // Zar Atışları
        int myRoll = Random.Range(1, 7);
        int myBonus = (unitData != null ? unitData.defenceBonus : 0);
        int myTotal = myRoll + myBonus;

        int targetRoll = Random.Range(1, 7);
        int targetBonus = (targetPlayer.unitData != null ? targetPlayer.unitData.defenceBonus : 0);
        int targetTotal = targetRoll + targetBonus;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.diceResultText.text = $"Combat! {myTotal} vs {targetTotal}";
        }

        // Görsel olarak rakibin karesine git
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
    }

    public void Stun()
    {
        isStunned = true;
        UpdateVisuals(); 
        Debug.Log($"{unitData?.playerName} is STUNNED!");
    }
}
