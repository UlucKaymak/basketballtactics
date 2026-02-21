using UnityEngine;

[CreateAssetMenu(fileName = "New Player Unit", menuName = "BasketballTactics/Player Unit")]
public class PlayerUnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string playerName;

    [Header("Stats")]
    public int speed = 3;
    public int shootingBonus = 0;
    public int passingBonus = 0;
    public int defenceBonus = 0;

    [Header("Visuals")]
    public Sprite blueTeamSprite;
    public Sprite redTeamSprite;
    // public Sprite playerIcon;
}
