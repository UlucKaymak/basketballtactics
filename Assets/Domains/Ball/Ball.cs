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
        
        // DOJump yerine DOMove kullanarak düz bir çizgide pas atma
        transform.DOMove(targetPlayer.transform.position + new Vector3(0, 0.5f, 0), passDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                SetOwner(targetPlayer);
            });
    }

    /// <summary>
    /// Pas başarısız olduğunda topun zar kadar mesafeye zıplayarak (bounce) düşmesi
    /// </summary>
    public void FlyToDistance(Vector3 direction, int distance)
    {
        // Önceki sahibinden tamamen ayır
        if (currentOwner != null)
        {
            currentOwner.hasBall = false;
            currentOwner.UpdateVisuals();
        }
        currentOwner = null;
        
        float currentTileSize = 1f;
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        if (gm != null) currentTileSize = gm.tileSize;

        direction.z = 0; 
        Vector3 flatDirection = direction.normalized;

        // Hedeflenen zıplama noktası
        Vector3 targetPos = transform.position + (flatDirection * (distance * currentTileSize));

        // Bounce Efekti (DOJump ile 2-3 küçük zıplama)
        // İlk büyük zıplama/uçuş (0.5 saniye)
        transform.DOJump(targetPos, 0.5f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // İkinci küçük zıplama (Bounce)
                transform.DOJump(transform.position + (flatDirection * 0.5f), 0.2f, 1, 0.3f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        SnapToGridAndFinalize(gm);
                    });
            });
    }

    private void SnapToGridAndFinalize(GridManager gm)
    {
        // Grid'e sabitlemeyi kaldırdık, sadece olduğu yerde bırakıyoruz.
        SetOwner(null); 
        Debug.Log("Ball bounced and stayed where it landed.");
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
