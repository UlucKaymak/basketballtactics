using UnityEngine;
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
        // Follow the owner if held
        if (currentOwner != null)
        {
            // Position slightly offset to look like they are holding it
            transform.position = currentOwner.transform.position + new Vector3(0, 0.5f, 0);
        }
    }

    public void FlyTo(PlayerUnit targetPlayer)
    {
        // Önceki sahibinden tamamen ayır
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
            // Oyuncunun bulunduğu karesinin tam merkezine atıyoruz
            finalPos = gm.GetWorldPosition(targetPlayer.currentGridPos.x, targetPlayer.currentGridPos.y);
        }

        transform.DOMove(finalPos, passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                SetOwner(targetPlayer);
            });
    }

    /// <summary>
    /// Topun belli bir pozisyona (Pota gibi) uçması
    /// </summary>
    public void FlyToPosition(Vector3 targetPos, bool isGoal)
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
            // Potanın olduğu karesinin tam merkezine atıyoruz
            Vector2Int hoopGrid = gm.GetGridPosition(targetPos);
            finalPos = gm.GetWorldPosition(hoopGrid.x, hoopGrid.y);
        }

        transform.DOMove(finalPos, passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (isGoal)
                {
                    Debug.Log("<color=orange>GOAL EFFECT!</color>");
                }
                SetOwner(null);
            });
    }

    /// <summary>
    /// Pas başarısız olduğunda topun zar kadar mesafeye zıplayarak (bounce) düşmesi
    /// </summary>
    public void FlyToDistance(Vector2Int startGridPos, Vector3 direction, int distance)
    {
        // Önceki sahibinden tamamen ayır
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

        // Hedeflenen Tile'ı hesapla
        Vector3 startWorldPos = gm.GetWorldPosition(startGridPos.x, startGridPos.y);
        float targetDist = (distance * gm.tileSize) - 0.1f;
        Vector3 calculatedTargetPos = startWorldPos + (flatDirection * targetDist);
        Vector2Int targetGrid = gm.GetGridPosition(calculatedTargetPos);

        // Grid'in TAM merkezini al
        Vector3 snappedTargetPos = gm.GetWorldPosition(targetGrid.x, targetGrid.y);

        // Maviyle highlightla
        gm.HighlightTile(targetGrid);
        Debug.Log($"<color=aqua>Ball targeting Grid: {targetGrid}</color>");

        // Bounce Efekti (Snapped pozisyona git)
        transform.DOJump(snappedTargetPos, 0.5f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOJump(snappedTargetPos, 0.2f, 1, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        SetOwner(null);
                        gm.ClearHighlights(); // İşlem bitince temizle
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
            previousOwner.UpdateVisuals(); // Görseli güncelle (Kırmızı)
        }
        
        currentOwner = player;
        if (player != null)
        {
            player.hasBall = true;
            player.UpdateVisuals(); // Görseli güncelle (Yeşil)
            Debug.Log($"Ball is now held by {player.unitData?.playerName}");
        }
    }
}
