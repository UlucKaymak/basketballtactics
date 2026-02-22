using UnityEngine;

public class OffensivePlayerUnit : PlayerUnit
{
    public override void UpdateVisuals()
    {
        if (spriteRenderer == null || unitData == null) return;
        spriteRenderer.sprite = unitData.baseSprite;

        TeamInfo info = TeamManager.Instance.GetTeamInfo(team);
        Color finalColor = info.teamColor;

        if (isStunned) 
        {
            finalColor = Color.gray;
        }
        else if (hasBall) 
        {
            finalColor = Color.Lerp(finalColor, Color.green, 0.5f);
        }
        else 
        {
            // Offensive: Biraz daha kırmızıya çekelim
            finalColor = Color.Lerp(finalColor, Color.red, 0.3f);
        }

        // HOVER HIGHLIGHT
        if (isHovered && !isStunned)
        {
            finalColor = Color.Lerp(finalColor, Color.white, 0.25f);
        }

        spriteRenderer.color = finalColor;
    }
}
