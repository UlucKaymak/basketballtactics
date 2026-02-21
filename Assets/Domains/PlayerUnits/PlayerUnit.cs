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

    private GridManager gridManager;

    private void Start()
    {
        gridManager = Object.FindFirstObjectByType<GridManager>();
        SnapToGrid();
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
    }
}
