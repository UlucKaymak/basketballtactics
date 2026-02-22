using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Ball : MonoBehaviour
{
    public static Ball Instance;

    public PlayerUnit currentOwner;
    public float passDuration = 0.5f;
    public float passJumpPower = 1.0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (currentOwner != null)
        {
            transform.position = currentOwner.transform.position + new Vector3(0, 0.5f, 0);
        }
    }

    public void FlyTo(PlayerUnit targetPlayer)
    {
        if (currentOwner != null)
        {
            currentOwner.hasBall = false;
            currentOwner.UpdateVisuals();
        }
        currentOwner = null; 
        
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        Vector3 finalPos = targetPlayer.transform.position;

        if (gm != null)
        {
            finalPos = gm.GetWorldPosition(targetPlayer.currentGridPos.x, targetPlayer.currentGridPos.y);
        }

        transform.DOMove(finalPos, passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                SetOwner(targetPlayer);
            });
    }

    public void FlyToPosition(Vector3 targetPos, bool isGoal, TeamColor scorerTeam)
    {
        if (currentOwner != null)
        {
            currentOwner.hasBall = false;
            currentOwner.UpdateVisuals();
        }
        currentOwner = null;

        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        Vector3 finalPos = targetPos;

        if (gm != null)
        {
            Vector2Int hoopGrid = gm.GetGridPosition(targetPos);
            finalPos = gm.GetWorldPosition(hoopGrid.x, hoopGrid.y);
        }

        transform.DOMove(finalPos, passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (isGoal)
                {
                    StartCoroutine(GoalSequence(scorerTeam));
                }
                else
                {
                    SetOwner(null);
                }
            });
    }

    private IEnumerator GoalSequence(TeamColor scorerTeam)
    {
        // 1. Skoru Ekle (ScoreManager) - Bu olay, UI ve TurnManager'ı otomatik tetikler
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scorerTeam, 2); 
        }

        SetOwner(null);
        yield return null;
    }

    public void FlyToDistance(Vector2Int startGridPos, Vector3 direction, int distance)
    {
        if (currentOwner != null)
        {
            currentOwner.hasBall = false;
            currentOwner.UpdateVisuals();
        }
        currentOwner = null;
        
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        direction.z = 0; 
        Vector3 flatDirection = direction.normalized;

        Vector3 startWorldPos = gm.GetWorldPosition(startGridPos.x, startGridPos.y);
        float targetDist = (distance * gm.tileSize) - 0.1f;
        Vector3 calculatedTargetPos = startWorldPos + (flatDirection * targetDist);
        Vector2Int targetGrid = gm.GetGridPosition(calculatedTargetPos);

        Vector3 snappedTargetPos = gm.GetWorldPosition(targetGrid.x, targetGrid.y);

        gm.HighlightTile(targetGrid);
        Debug.Log($"<color=aqua>Ball targeting Grid: {targetGrid}</color>");

        transform.DOJump(snappedTargetPos, 0.5f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOJump(snappedTargetPos, 0.2f, 1, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        SetOwner(null);
                        gm.ClearHighlights(); 
                        Debug.Log("Ball settled in grid center.");
                    });
            });
    }

    public void SetOwner(PlayerUnit player)
    {
        PlayerUnit previousOwner = currentOwner;
        
        if (previousOwner != null)
        {
            previousOwner.hasBall = false;
            previousOwner.UpdateVisuals();
        }
        
        currentOwner = player;
        if (player != null)
        {
            player.hasBall = true;
            player.UpdateVisuals();
            Debug.Log($"Ball is now held by {player.unitData?.playerName}");
        }
    }
}
