using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerUnit : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerUnitData unitData;

    [Header("Current State")]
    public Vector2Int currentGridPos;
    public bool hasBall = false;
    public bool isStunned = false;

    [Header("Movement Settings")]
    public float moveDuration = 0.4f;
    public float jumpPower = 0.2f;

    [Header("Visual Feedback (Test)")]
    public Color colorNoBall = Color.red;
    public Color colorHasBall = Color.green;
    private SpriteRenderer spriteRenderer;

    private GridManager gridManager;

    private void Start()
    {
        gridManager = Object.FindFirstObjectByType<GridManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        SnapToGrid();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer != null)
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
        int distance = Mathf.Abs(targetPos.x - currentGridPos.x) + Mathf.Abs(targetPos.y - currentGridPos.y);
        return distance <= (unitData != null ? unitData.speed : 0);
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

        // Challenge Difficulty (CD): Her tile mesafesi için +1 (GDD kuralı)
        int distance = Mathf.Abs(targetPlayer.currentGridPos.x - currentGridPos.x) + Mathf.Abs(targetPlayer.currentGridPos.y - currentGridPos.y);
        
        int roll = Random.Range(1, 7); // d6
        int bonus = (unitData != null ? unitData.passingBonus : 0);
        int totalValue = roll + bonus;

        bool success = totalValue >= distance;

        // UI'ya yazdır
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDiceResult(roll, bonus, distance, success);
        }

        Debug.Log($"Pass attempt by {unitData?.playerName} to {targetPlayer.unitData?.playerName}. Target CD: {distance}, Total: {totalValue}");

        if (success)
        {
            Debug.Log("<color=green>Pass Successful!</color>");
            hasBall = false; // Topu elinden çıkardı
            UpdateVisuals();
            Ball.Instance.FlyTo(targetPlayer);
        }
        else
        {
            Debug.Log("<color=red>Pass Failed!</color>");
            hasBall = false; // Topu elinden çıkardı (fail olsa da)
            UpdateVisuals();
            
            // Zar sonucu kadar mesafeye topu düşür
            Vector3 direction = targetPlayer.transform.position - transform.position;
            Ball.Instance.FlyToDistance(direction, totalValue); 
        }
    }
}
