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

    private void HandleScoreChanged(int heads, int tails)
    {
        TeamInfo hInfo = TeamManager.Instance.headsInfo;
        TeamInfo tInfo = TeamManager.Instance.tailsInfo;
        AnnouncementManager.Instance.SendMiniAnnouncement($"{hInfo.teamName}: {heads} - {tInfo.teamName}: {tails}", Color.white);
    }

    private void HandleGoalScored(TeamColor team, int points)
    {
        TeamInfo info = TeamManager.Instance.GetTeamInfo(team);
        AnnouncementManager.Instance.SendAnnouncement($"{info.teamName} Scored! (+{points})", 2.0f, AnnouncementType.Goal, info.teamColor);
    }

    private void HandleTurnChanged(TeamColor activeTeam, int turn)
    {
        TeamInfo info = TeamManager.Instance.GetTeamInfo(activeTeam);
        AnnouncementManager.Instance.SendAnnouncement($"{info.teamName}'s Turn!", 1.5f, AnnouncementType.Turn, info.teamColor);
        AnnouncementManager.Instance.SendMiniAnnouncement($"Turn {turn}: {info.teamName}", info.teamColor);
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
            announcementText.raycastTarget = false; 
        }

        if (miniAnnouncementText != null)
        {
            miniAnnouncementText.text = "";
            miniAnnouncementText.alpha = 0;
            miniAnnouncementText.raycastTarget = false; 
        }

        if (calculationText != null)
        {
            calculationText.text = "";
            calculationText.alpha = 0;
            calculationText.raycastTarget = false; 
        }
        
        if (actionPanel != null) 
        {
            actionPanel.SetActive(false);
            if (actionPanelCG == null) actionPanelCG = actionPanel.GetComponent<CanvasGroup>();
        }
    }

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

    public void SendAnnouncement(string message, float duration = 1.5f)
    {
        AnnouncementManager.Instance.SendAnnouncement(message, duration);
    }

    public void SendMiniAnnouncement(string message, Color? color = null)
    {
        AnnouncementManager.Instance.SendMiniAnnouncement(message, color);
    }

    public void OnCalculationClick()
    {
        if (!isWaitingForClick) return;
        ForceClearCalculation();
    }

    public void ForceClearCalculation()
    {
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
            calculationText.raycastTarget = false; 
        }
    }

    public string GetDiceIcon(int roll)
    {
        return roll.ToString();
    }

    public IEnumerator AnimateDiceRoll(int roll1, Color color1, int? roll2 = null, Color? color2 = null, string label = "/", System.Action onComplete = null, string finalNote = "")
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
            int temp1 = DiceManager.Instance.RollD6();
            string diceText = $"<color={c1Hex}>{GetDiceIcon(temp1)}</color>";

            if (roll2.HasValue)
            {
                int temp2 = DiceManager.Instance.RollD6();
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

        if (!string.IsNullOrEmpty(finalNote))
        {
            calculationText.text = finalNote;
        }
        else
        {
            calculationText.text = finalDiceText;
        }
        
        calculationText.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 5, 0.5f);
        
        yield return new WaitForSeconds(0.4f);
        
        isWaitingForClick = true; 
        calculationText.raycastTarget = true; 
        if (StateManager.Instance != null) StateManager.Instance.SetState(GameState.Resolution);

        while (isWaitingForClick) yield return null;

        onComplete?.Invoke();
        ClearCalculationUI();
    }
}
