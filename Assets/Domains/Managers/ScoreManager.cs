using UnityEngine;
using System;
using NaughtyAttributes;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Scores")]
    [ReadOnly] public int headsScore = 0;
    [ReadOnly] public int tailsScore = 0;

    public static event Action<int, int> OnScoreChanged;
    public static event Action<TeamColor, int> OnGoalScored;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(TeamColor team, int amount)
    {
        if (team == TeamColor.Heads) headsScore += amount;
        else tailsScore += amount;

        Debug.Log($"<color=orange>[ScoreManager] Goal! {team} scored {amount}. Score: {headsScore}-{tailsScore}</color>");

        OnScoreChanged?.Invoke(headsScore, tailsScore);
        OnGoalScored?.Invoke(team, amount);
    }

    [Button("Reset Scores")]
    public void ResetScores()
    {
        headsScore = 0;
        tailsScore = 0;
        OnScoreChanged?.Invoke(headsScore, tailsScore);
    }
}
