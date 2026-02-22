using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.SceneManagement;

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
                    CheckForCatch();
                }
            });
    }

    public void FlyToHoopAndBounce(Vector3 hoopPos, Vector2Int playerGridPos)
    {
        if (currentOwner != null)
        {
            currentOwner.hasBall = false;
            currentOwner.UpdateVisuals();
        }
        currentOwner = null;

        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        transform.DOJump(hoopPos, 1.5f, 1, passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                Vector3 bounceDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
                int bounceDist = Random.Range(2, 4);
                
                Vector3 hoopWorldCenter = gm.GetWorldPosition(gm.GetGridPosition(hoopPos).x, gm.GetGridPosition(hoopPos).y);
                float targetDist = (bounceDist * gm.tileSize);
                Vector3 calculatedTargetPos = hoopWorldCenter + (bounceDir * targetDist);
                Vector2Int targetGrid = gm.GetGridPosition(calculatedTargetPos);

                if (!gm.IsInBounds(targetGrid))
                {
                    StartCoroutine(HandleOutOfBounds());
                    return;
                }

                Vector3 snappedTargetPos = gm.GetWorldPosition(targetGrid.x, targetGrid.y);
                gm.HighlightTile(targetGrid);
                
                transform.DOJump(snappedTargetPos, 0.8f, 1, 0.6f)
                    .SetEase(Ease.OutBounce)
                    .OnComplete(() => {
                        CheckForCatch();
                        gm.ClearHighlights();
                    });
            });
    }

    private IEnumerator HandleOutOfBounds()
    {
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);
        
        Debug.Log("<color=red>BALL OUT OF BOUNDS!</color>");
        
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(AnnouncementManager.Instance.SendAnnouncementAndWait("OUT OF BOUNDS!", 1.5f, AnnouncementType.Alert, Color.red));
            yield return StartCoroutine(AnnouncementManager.Instance.SendAnnouncementAndWait("RESTARTING MATCH...", 1.0f, AnnouncementType.Alert, Color.white));
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // Sahneyi baştan yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator GoalSequence(TeamColor scorerTeam)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scorerTeam, 2); 
        }
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
        float targetDist = (distance * gm.tileSize);
        Vector3 calculatedTargetPos = startWorldPos + (flatDirection * targetDist);
        Vector2Int targetGrid = gm.GetGridPosition(calculatedTargetPos);

        if (!gm.IsInBounds(targetGrid))
        {
            StartCoroutine(HandleOutOfBounds());
            return;
        }

        Vector3 snappedTargetPos = gm.GetWorldPosition(targetGrid.x, targetGrid.y);
        gm.HighlightTile(targetGrid);

        transform.DOJump(snappedTargetPos, 0.5f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOJump(snappedTargetPos, 0.2f, 1, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        CheckForCatch();
                        gm.ClearHighlights(); 
                    });
            });
    }

    private void CheckForCatch()
    {
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) return;

        Vector2Int currentGrid = gm.GetGridPosition(transform.position);
        PlayerUnit playerAtTile = gm.GetPlayerAt(currentGrid);

        if (playerAtTile != null)
        {
            Debug.Log($"<color=green>Ball caught by {playerAtTile.unitData.playerName} at {currentGrid}</color>");
            SetOwner(playerAtTile);
        }
        else
        {
            SetOwner(null);
        }
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
        }
    }
}
