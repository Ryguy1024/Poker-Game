using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Drawing;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Community Cards")]
    public List<TextMeshProUGUI> CommunityCardLabels; // 5 TMP labels

    [Header("Player Hand")]
    public TextMeshProUGUI PlayerCard1Label;
    public TextMeshProUGUI PlayerCard2Label;

    [Header("Info")]
    public TextMeshProUGUI PotLabel;
    public TextMeshProUGUI StatusLabel;
    public TextMeshProUGUI PlayerChipsLabel;
    public TextMeshProUGUI ResultLabel;

    [Header("Action Buttons")]
    public Button FoldButton;
    public Button CheckButton;
    public Button CallButton;
    public Button RaiseButton;
    public TMP_InputField RaiseInputField;

    [Header("Opponent Info")]
    public List<TextMeshProUGUI> OpponentChipsLabels;
    public List<TextMeshProUGUI> OpponentNameLabels;

    [HideInInspector] public bool ActionTaken = false;

    private PokerGameManager gm;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gm = PokerGameManager.Instance;

        FoldButton.onClick.AddListener(() => { gm.HumanAct(PlayerAction.Fold); ActionTaken = true; });
        CheckButton.onClick.AddListener(() => { gm.HumanAct(PlayerAction.Check); ActionTaken = true; });
        CallButton.onClick.AddListener(() => { gm.HumanAct(PlayerAction.Call); ActionTaken = true; });
        RaiseButton.onClick.AddListener(() =>
        {
            int amt = 0;
            if (int.TryParse(RaiseInputField.text, out amt) && amt > 0)
            {
                gm.HumanAct(PlayerAction.Raise, amt);
                ActionTaken = true;
            }
        });

        ShowActionButtons(false);
        if (ResultLabel) ResultLabel.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (gm == null) return;

        // Community cards
        for (int i = 0; i < CommunityCardLabels.Count; i++)
        {
            if (CommunityCardLabels[i] == null) continue;
            CommunityCardLabels[i].text = i < gm.CommunityCards.Count
                ? gm.CommunityCards[i].ToString()
                : "[ ]";
        }

        // Player hole cards
        Player human = gm.Players.Find(p => p.IsHuman);
        if (human != null)
        {
            PlayerCard1Label.text = human.HoleCards.Count > 0 ? human.HoleCards[0].ToString() : "?";
            PlayerCard2Label.text = human.HoleCards.Count > 1 ? human.HoleCards[1].ToString() : "?";
            PlayerChipsLabel.text = $"Chips: {human.Chips}";
        }

        // Pot and status
        PotLabel.text = $"Pot: {gm.Pot}";
        StatusLabel.text = gm.StatusMessage;

        // Call button label shows amount
        CallButton.GetComponentInChildren<TextMeshProUGUI>().text =
            gm.CurrentBet > 0 ? $"Call {gm.CurrentBet}" : "Call";

        // Check only available if no bet to match
        CheckButton.interactable = gm.CurrentBet == 0 ||
            (human != null && human.CurrentBet == gm.CurrentBet);

        // Opponent info
        var bots = gm.Players.FindAll(p => !p.IsHuman);
        for (int i = 0; i < OpponentChipsLabels.Count; i++)
        {
            if (i < bots.Count)
            {
                if (OpponentChipsLabels[i]) OpponentChipsLabels[i].text = $"{bots[i].Chips} chips";
                if (OpponentNameLabels[i])  OpponentNameLabels[i].text  = bots[i].HasFolded ? $"{bots[i].Name} (folded)" : bots[i].Name;
            }
        }
    }

    public void ShowActionButtons(bool show)
    {
        FoldButton.gameObject.SetActive(show);
        CheckButton.gameObject.SetActive(show);
        CallButton.gameObject.SetActive(show);
        RaiseButton.gameObject.SetActive(show);
        if (RaiseInputField) RaiseInputField.gameObject.SetActive(show);
    }

    public void ShowHandResult(Player winner, string handDescription)
    {
        if (ResultLabel == null) return;
        ResultLabel.gameObject.SetActive(true);
        ResultLabel.text = $"{winner.Name} — {handDescription}";
        Invoke(nameof(HideResult), 2.8f);
    }

    private void HideResult()
    {
        if (ResultLabel) ResultLabel.gameObject.SetActive(false);
    }
}