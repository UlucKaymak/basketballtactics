using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using NaughtyAttributes;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Core Texts")]
    public TextMeshProUGUI announcementText; 
    public TextMeshProUGUI miniAnnouncementText;
    public TextMeshProUGUI calculationText;

    [Header("Action Panel")]
    public GameObject actionPanel;
    public CanvasGroup actionPanelCG; 
    public Button moveBtn, passBtn, shootBtn, waitBtn;

    [Header("State Visualiser")]
    [ReadOnly] public string stateVisualiser;

    [Header("State")]
    private bool isWaitingForClick = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupInitialState();
    }

    private void OnEnable()
    {
        // Event Abonelikleri (Subscribe)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.OnScoreChanged += HandleScoreChanged;
            ScoreManager.OnGoalScored += HandleGoalScored;
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.OnTurnChanged += HandleTurnChanged;
        }
    }

    private void OnDisable()
    {
        // Abonelik İptali (Unsubscribe) - Memory Leak Önlemek İçin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
            ScoreManager.OnGoalScored -= HandleGoalScored;
        }

        if (TurnManager.Instance != null)
        {
            TurnManager.OnTurnChanged -= HandleTurnChanged;
        }
    }

    private void HandleScoreChanged(int red, int blue)
    {
        SendMiniAnnouncement($"Blue: {blue} - Red: {red}", Color.white);
    }

    private void HandleGoalScored(TeamColor team, int points)
    {
        SendAnnouncement($"{team} Team Scored! (+{points})", 2.0f);
    }

    private void HandleTurnChanged(TeamColor activeTeam, int turn)
    {
        SendAnnouncement($"{activeTeam} Team's Turn!", 1.5f);
        SendMiniAnnouncement($"Turn {turn}: {activeTeam}", (activeTeam == TeamColor.Red) ? Color.red : Color.cyan);
    }

    private void Update()
    {
        if (StateManager.Instance != null)
        {
            stateVisualiser = StateManager.Instance.CurrentState.ToString();
            if (isWaitingForClick) stateVisualiser += " (WAITING FOR CLICK)";
        }
    }

    private void SetupInitialState()
    {
        if (announcementText != null) 
        {
            announcementText.text = "";
            announcementText.alpha = 0;
            announcementText.raycastTarget = false; // Tıklamayı engellemesin
        }

        if (miniAnnouncementText != null)
        {
            miniAnnouncementText.text = "";
            miniAnnouncementText.alpha = 0;
            miniAnnouncementText.raycastTarget = false; // Tıklamayı engellemesin
        }

        if (calculationText != null)
        {
            calculationText.text = "";
            calculationText.alpha = 0;
            // DİKKAT: Sadece tıklama beklerken açılmalı. Başlangıçta kapalı olmalı!
            calculationText.raycastTarget = false; 
        }
        
        if (actionPanel != null) 
        {
            actionPanel.SetActive(false);
            if (actionPanelCG == null) actionPanelCG = actionPanel.GetComponent<CanvasGroup>();
        }
    }

    #region Quick Access Methods

    public void SendAnnouncement(string message, float duration = 1.5f)
    {
        StartCoroutine(ShowAnnouncementRoutine(message, duration));
    }

    public void SendMiniAnnouncement(string message, Color? color = null)
    {
        if (miniAnnouncementText == null) return;
        miniAnnouncementText.text = message;
        miniAnnouncementText.color = color ?? Color.white;
        miniAnnouncementText.alpha = 1f;
        miniAnnouncementText.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
    }

    public void SendCalculation(string message)
    {
        ShowCalculation(message);
    }

    #endregion

    public void ShowActionPanel(PlayerUnit player)
    {
        if (actionPanel == null || player == null || player.hasActed) return;

        actionPanel.SetActive(true);
        actionPanel.transform.localScale = Vector3.one * 0.8f;
        actionPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        
        if (actionPanelCG != null)
        {
            actionPanelCG.alpha = 0;
            actionPanelCG.DOFade(1f, 0.2f);
        }

        if (moveBtn != null) moveBtn.interactable = true;
        if (passBtn != null) passBtn.interactable = player.hasBall;
        if (shootBtn != null) shootBtn.interactable = player.hasBall;
        if (waitBtn != null) waitBtn.interactable = true;
    }

    public void HideActionPanel()
    {
        if (actionPanel == null || !actionPanel.activeSelf) return;

        actionPanel.transform.DOScale(0.8f, 0.15f).SetEase(Ease.InBack).OnComplete(() => {
            actionPanel.SetActive(false);
        });
        if (actionPanelCG != null) actionPanelCG.DOFade(0, 0.15f);
    }

    public IEnumerator ShowAnnouncementRoutine(string message, float duration = 1.0f)
    {
        if (announcementText == null) yield break;
        announcementText.text = message;
        announcementText.alpha = 0;
        announcementText.transform.localScale = Vector3.one * 0.5f;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(announcementText.DOFade(1f, 0.3f));
        seq.Join(announcementText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
        seq.AppendInterval(duration);
        seq.Append(announcementText.DOFade(0f, 0.3f));
        seq.Join(announcementText.transform.DOScale(0.8f, 0.3f));

        yield return seq.WaitForCompletion();
    }

    public void ShowCalculation(string text)
    {
        StartCoroutine(ShowCalculationRoutine(text));
    }

    public IEnumerator ShowCalculationRoutine(string text)
    {
        if (calculationText != null)
        {
            calculationText.text = text;
            calculationText.alpha = 1f;
            calculationText.raycastTarget = true; // Sadece gösterirken tıklama beklesin
            calculationText.transform.localScale = Vector3.one;

            calculationText.transform.DOPunchPosition(Vector3.up * 0.1f, 0.2f);
            isWaitingForClick = true;

            if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Resolution);

            while (isWaitingForClick) yield return null;

            ClearCalculationUI();
        }
    }

    public void OnCalculationClick()
    {
        if (!isWaitingForClick) return;
        isWaitingForClick = false;
        
        ClearCalculationUI();

        if (StateManager.Instance != null && StateManager.Instance.IsResolution())
        {
            StateManager.Instance.SetState(GameState.Idle);
        }
    }

    private void ClearCalculationUI()
    {
        if (calculationText != null)
        {
            calculationText.DOFade(0f, 0.2f);
            calculationText.text = "";
            calculationText.raycastTarget = false; // Temizlendiğinde tıklama engelini kaldır
        }
    }

    public string GetDiceIcon(int roll)
    {
        return roll.ToString();
    }

    public IEnumerator AnimateDiceRoll(int roll1, Color color1, int? roll2 = null, Color? color2 = null, string label = "/", System.Action onComplete = null)
    {
        if (calculationText == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Busy);
        
        float duration = 1.0f;
        float elapsed = 0;
        float tickRate = 0.05f;

        string c1Hex = "#" + ColorUtility.ToHtmlStringRGB(color1);
        string c2Hex = roll2.HasValue ? "#" + ColorUtility.ToHtmlStringRGB(color2.Value) : "";

        calculationText.alpha = 1f;
        calculationText.transform.localScale = Vector3.one; 

        while (elapsed < duration)
        {
            int temp1 = Random.Range(1, 7);
            string diceText = $"<color={c1Hex}>{GetDiceIcon(temp1)}</color>";

            if (roll2.HasValue)
            {
                int temp2 = Random.Range(1, 7);
                diceText = $"<color={c1Hex}>{GetDiceIcon(temp1)}</color> {label} <color={c2Hex}>{GetDiceIcon(temp2)}</color>";
            }

            calculationText.text = diceText;
            calculationText.transform.DOPunchPosition(Vector3.up * 0.05f, tickRate, 2, 0); 
            
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }

        string finalDiceText = $"<color={c1Hex}>{GetDiceIcon(roll1)}</color>";
        if (roll2.HasValue)
        {
            finalDiceText = $"<color={c1Hex}>{GetDiceIcon(roll1)}</color> {label} <color={c2Hex}>{GetDiceIcon(roll2.Value)}</color>";
        }

        calculationText.text = finalDiceText;
        
        calculationText.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 5, 0.5f);
        
        yield return new WaitForSeconds(0.4f);
        
        isWaitingForClick = true; 
        calculationText.raycastTarget = true; // Tıklama beklerken aktif et
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Resolution);

        while (isWaitingForClick) yield return null;

        onComplete?.Invoke();
        ClearCalculationUI();
    }
}
