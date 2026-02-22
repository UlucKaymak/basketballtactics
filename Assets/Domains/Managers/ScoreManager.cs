using UnityEngine;
using System;
using NaughtyAttributes;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Scores")]
    [ReadOnly] public int redScore = 0;
    [ReadOnly] public int blueScore = 0;

    // Skor değiştiğinde tetiklenecek olay: (RedScore, BlueScore)
    public static event Action<int, int> OnScoreChanged;
    // Gol olduğunda tetiklenecek olay: (Golü Atan Takım, Kazanılan Puan)
    public static event Action<TeamColor, int> OnGoalScored;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(TeamColor team, int amount)
    {
        if (team == TeamColor.Red) redScore += amount;
        else blueScore += amount;

        Debug.Log($"<color=orange>[ScoreManager] Goal! {team} scored {amount}. Score: {blueScore}-{redScore}</color>");

        // Eventleri ateşle
        OnScoreChanged?.Invoke(redScore, blueScore);
        OnGoalScored?.Invoke(team, amount);
    }

    [Button("Reset Scores")]
    public void ResetScores()
    {
        redScore = 0;
        blueScore = 0;
        OnScoreChanged?.Invoke(redScore, blueScore);
    }
}
