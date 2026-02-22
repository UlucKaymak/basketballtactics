using UnityEngine;

public class Hoop : MonoBehaviour
{
    public TeamColor hoopTeam;
    public Vector2Int gridPos; // Potanın grid üzerindeki konumu

    private void Start()
    {
        // GridManager üzerinden kendi grid pozisyonunu bul (Eğer manuel setlenmediyse)
        GridManager gm = Object.FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gridPos = gm.GetGridPosition(transform.position);
        }
    }
}
