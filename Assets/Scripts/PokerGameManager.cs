using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase { PreFlop, Flop, Turn, River, Showdown }
public enum PlayerAction { None, Fold, Check, Call, Raise }

[System.Serializable]
public class Player
{
    public string Name;
    public int Chips;
    public List<Card> HoleCards = new List<Card>();
    public int CurrentBet;
    public bool HasFolded;
    public bool IsHuman;

    public Player(string name, int chips, bool isHuman)
    {
        Name = name;
        Chips = chips;
        IsHuman = isHuman;
    }

    public void Reset()
    {
        HoleCards.Clear();
        CurrentBet = 0;
        HasFolded = false;
    }
}

public class PokerGameManager : MonoBehaviour
{
    public static PokerGameManager Instance;

    [Header("Game Settings")]
    public int StartingChips = 1000;
    public int SmallBlind = 10;
    public int BigBlind = 20;

    [HideInInspector] public List<Player> Players = new List<Player>();
    [HideInInspector] public List<Card> CommunityCards = new List<Card>();
    [HideInInspector] public int Pot;
    [HideInInspector] public GamePhase CurrentPhase;
    [HideInInspector] public int CurrentBet;
    [HideInInspector] public string StatusMessage;

    private Deck deck;
    private int dealerIndex = 0;
    private UIManager ui;

    void Awake()
    {
        Instance = this;
        deck = new Deck();
        Players.Add(new Player("You", StartingChips, true));
        Players.Add(new Player("Bot 1", StartingChips, false));
        Players.Add(new Player("Bot 2", StartingChips, false));
    }

    void Start()
    {
        ui = UIManager.Instance;
        StartCoroutine(StartNewHand());
    }

    public IEnumerator StartNewHand()
    {
        // Reset
        foreach (var p in Players) p.Reset();
        CommunityCards.Clear();
        Pot = 0;
        CurrentBet = 0;
        deck.Reset();
        deck.Shuffle();

        // Post blinds
        int sbIndex = (dealerIndex + 1) % Players.Count;
        int bbIndex = (dealerIndex + 2) % Players.Count;
        PostBlind(Players[sbIndex], SmallBlind);
        PostBlind(Players[bbIndex], BigBlind);
        CurrentBet = BigBlind;

        // Deal hole cards
        for (int i = 0; i < 2; i++)
            foreach (var p in Players)
                p.HoleCards.Add(deck.Deal());

        StatusMessage = "Pre-flop — place your bet.";
        CurrentPhase = GamePhase.PreFlop;
        ui.Refresh();

        yield return StartCoroutine(BettingRound(isPreFlop: true));
        if (OnlyOneLeft()) { yield return EndHand(); yield break; }

        // Flop
        CurrentPhase = GamePhase.Flop;
        for (int i = 0; i < 3; i++) CommunityCards.Add(deck.Deal());
        StatusMessage = "Flop.";
        ResetBets();
        ui.Refresh();
        yield return StartCoroutine(BettingRound());
        if (OnlyOneLeft()) { yield return EndHand(); yield break; }

        // Turn
        CurrentPhase = GamePhase.Turn;
        CommunityCards.Add(deck.Deal());
        StatusMessage = "Turn.";
        ResetBets();
        ui.Refresh();
        yield return StartCoroutine(BettingRound());
        if (OnlyOneLeft()) { yield return EndHand(); yield break; }

        // River
        CurrentPhase = GamePhase.River;
        CommunityCards.Add(deck.Deal());
        StatusMessage = "River.";
        ResetBets();
        ui.Refresh();
        yield return StartCoroutine(BettingRound());

        yield return EndHand();
    }

    private IEnumerator BettingRound(bool isPreFlop = false)
    {
        // In pre-flop, UTG acts first (index after BB); otherwise SB acts first
        int startIndex = isPreFlop
            ? (dealerIndex + 3) % Players.Count
            : (dealerIndex + 1) % Players.Count;

        for (int i = 0; i < Players.Count; i++)
        {
            int idx = (startIndex + i) % Players.Count;
            Player p = Players[idx];
            if (p.HasFolded || p.Chips == 0) continue;

            if (p.IsHuman)
            {
                ui.ShowActionButtons(true);
                yield return new WaitUntil(() => ui.ActionTaken);
                ui.ActionTaken = false;
                ui.ShowActionButtons(false);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                AIAct(p);
            }
            ui.Refresh();
        }
    }

    public void HumanAct(PlayerAction action, int raiseAmount = 0)
    {
        Player human = Players.Find(p => p.IsHuman);
        Act(human, action, raiseAmount);
    }

    private void AIAct(Player p)
    {
        // Simple AI: calls most of the time, occasionally raises, sometimes folds
        int roll = Random.Range(0, 10);
        if (roll < 1 && CurrentBet > 0)
            Act(p, PlayerAction.Fold);
        else if (roll < 3 && p.Chips >= CurrentBet * 2)
            Act(p, PlayerAction.Raise, CurrentBet);
        else if (CurrentBet == p.CurrentBet)
            Act(p, PlayerAction.Check);
        else
            Act(p, PlayerAction.Call);
    }

    private void Act(Player p, PlayerAction action, int raiseAmount = 0)
    {
        switch (action)
        {
            case PlayerAction.Fold:
                p.HasFolded = true;
                StatusMessage = $"{p.Name} folds.";
                break;

            case PlayerAction.Check:
                StatusMessage = $"{p.Name} checks.";
                break;

            case PlayerAction.Call:
                int callAmt = Mathf.Min(CurrentBet - p.CurrentBet, p.Chips);
                p.Chips -= callAmt;
                p.CurrentBet += callAmt;
                Pot += callAmt;
                StatusMessage = $"{p.Name} calls {callAmt}.";
                break;

            case PlayerAction.Raise:
                int total = CurrentBet + raiseAmount;
                int put = Mathf.Min(total - p.CurrentBet, p.Chips);
                p.Chips -= put;
                p.CurrentBet += put;
                Pot += put;
                CurrentBet = p.CurrentBet;
                StatusMessage = $"{p.Name} raises to {CurrentBet}.";
                break;
        }
    }

    private IEnumerator EndHand()
    {
        CurrentPhase = GamePhase.Showdown;
        Player winner = DetermineWinner();
        winner.Chips += Pot;
        StatusMessage = $"{winner.Name} wins {Pot} chips with {GetBestHand(winner).Description}!";
        ui.Refresh();
        ui.ShowHandResult(winner, GetBestHand(winner).Description);

        yield return new WaitForSeconds(3f);
        dealerIndex = (dealerIndex + 1) % Players.Count;

        // Remove broke players
        Players.RemoveAll(p => p.Chips <= 0);
        if (Players.Count < 2)
        {
            StatusMessage = $"{Players[0].Name} wins the game!";
            ui.Refresh();
            yield break;
        }
        yield return StartCoroutine(StartNewHand());
    }

    private Player DetermineWinner()
    {
        Player best = null;
        HandResult bestResult = null;
        foreach (var p in Players)
        {
            if (p.HasFolded) continue;
            var result = GetBestHand(p);
            if (bestResult == null || HandEvaluator.Compare(result, bestResult) > 0)
            {
                best = p;
                bestResult = result;
            }
        }
        return best ?? Players.Find(p => !p.HasFolded);
    }

    private HandResult GetBestHand(Player p)
    {
        var all = new List<Card>(p.HoleCards);
        all.AddRange(CommunityCards);
        return HandEvaluator.Evaluate(all);
    }

    private void PostBlind(Player p, int amount)
    {
        int actual = Mathf.Min(amount, p.Chips);
        p.Chips -= actual;
        p.CurrentBet += actual;
        Pot += actual;
    }

    private void ResetBets()
    {
        CurrentBet = 0;
        foreach (var p in Players) p.CurrentBet = 0;
    }

    private bool OnlyOneLeft() => Players.FindAll(p => !p.HasFolded).Count == 1;
}
