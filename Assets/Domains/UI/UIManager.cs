using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI diceResultText;

    private void Awake()
    {
        Instance = this;
        if (diceResultText != null) diceResultText.text = "";
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
