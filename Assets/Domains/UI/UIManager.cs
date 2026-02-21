using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI diceResultText;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI scoreText;

    [Header("Action Panel")]
    public GameObject actionPanel; // UI Panel containing the buttons
    public UnityEngine.UI.Button moveBtn, passBtn, shootBtn, waitBtn;

    private int redScore = 0;
    private int blueScore = 0;

    private void Awake()
    {
        Instance = this;
        if (diceResultText != null) diceResultText.text = "";
        if (actionPanel != null) actionPanel.SetActive(false); // Hide at start
        UpdateTurnUI(TeamColor.Blue, 1);
        UpdateScoreUI();
    }

    public void ShowActionPanel(PlayerUnit player)
    {
        if (actionPanel == null || player == null || player.hasActed) return;

        actionPanel.SetActive(true);

        // Güvenlik kontrolleri: Slotlar boş olsa bile oyun çökmesin
        if (moveBtn != null) moveBtn.interactable = true;
        if (passBtn != null) passBtn.interactable = player.hasBall;
        if (shootBtn != null) shootBtn.interactable = player.hasBall;
        if (waitBtn != null) waitBtn.interactable = true;
    }

    public void HideActionPanel()
    {
        if (actionPanel != null) actionPanel.SetActive(false);
    }

    public void AddScore(TeamColor team, int points)
    {
        if (team == TeamColor.Red) redScore += points;
        else blueScore += points;
        UpdateScoreUI();
    }

    public void UpdateScoreUI()
    {
        if (scoreText == null) return;
        scoreText.text = $"BLUE {blueScore} - {redScore} RED";
    }

    public void UpdateTurnUI(TeamColor team, int turn)
    {
        if (turnText == null) return;
        turnText.text = $"Turn {turn}: {team} Team";
        turnText.color = (team == TeamColor.Red) ? Color.red : Color.cyan;
    }

    public void ShowDiceResult(int roll, int bonus, int target, bool success)
    {
        if (diceResultText == null) return;

        string color = success ? "green" : "red";
        diceResultText.text = $"Roll: {roll} dice + {bonus} bonus = {roll + bonus} (Target: {target})";
        diceResultText.color = success ? Color.green : Color.red;

        // Küçük bir animasyon (opsiyonel)
        diceResultText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
    }
}
