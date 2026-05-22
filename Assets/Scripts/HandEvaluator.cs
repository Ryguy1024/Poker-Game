using System.Collections.Generic;
using System.Linq;

public enum HandRank
{
    HighCard, OnePair, TwoPair, ThreeOfAKind,
    Straight, Flush, FullHouse, FourOfAKind,
    StraightFlush, RoyalFlush
}

public class HandResult
{
    public HandRank Rank;
    public List<int> Tiebreakers; // high cards for comparing equal hand ranks
    public string Description;

    public HandResult(HandRank rank, List<int> tiebreakers, string description)
    {
        Rank = rank;
        Tiebreakers = tiebreakers;
        Description = description;
    }
}

public static class HandEvaluator
{
    // Evaluates best 5-card hand from any number of cards (e.g. 7 in Texas Hold'em)
    public static HandResult Evaluate(List<Card> cards)
    {
        HandResult best = null;
        var combos = GetCombinations(cards, 5);
        foreach (var combo in combos)
        {
            var result = EvaluateFive(combo);
            if (best == null || Compare(result, best) > 0)
                best = result;
        }
        return best;
    }

    private static HandResult EvaluateFive(List<Card> five)
    {
        var ranks = five.Select(c => (int)c.Rank).OrderByDescending(r => r).ToList();
        var suits = five.Select(c => c.Suit).ToList();
        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks, out List<int> straightRanks);
        var groups = ranks.GroupBy(r => r).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        int firstCount = groups[0].Count();
        int secondCount = groups.Count > 1 ? groups[1].Count() : 0;

        if (isFlush && isStraight)
        {
            if (straightRanks[0] == 14)
                return new HandResult(HandRank.RoyalFlush, straightRanks, "Royal Flush");
            return new HandResult(HandRank.StraightFlush, straightRanks, "Straight Flush");
        }
        if (firstCount == 4)
            return new HandResult(HandRank.FourOfAKind, GroupTiebreakers(groups), "Four of a Kind");
        if (firstCount == 3 && secondCount == 2)
            return new HandResult(HandRank.FullHouse, GroupTiebreakers(groups), "Full House");
        if (isFlush)
            return new HandResult(HandRank.Flush, ranks, "Flush");
        if (isStraight)
            return new HandResult(HandRank.Straight, straightRanks, "Straight");
        if (firstCount == 3)
            return new HandResult(HandRank.ThreeOfAKind, GroupTiebreakers(groups), "Three of a Kind");
        if (firstCount == 2 && secondCount == 2)
            return new HandResult(HandRank.TwoPair, GroupTiebreakers(groups), "Two Pair");
        if (firstCount == 2)
            return new HandResult(HandRank.OnePair, GroupTiebreakers(groups), "One Pair");
        return new HandResult(HandRank.HighCard, ranks, "High Card");
    }

    private static bool IsStraight(List<int> sortedRanks, out List<int> straightRanks)
    {
        straightRanks = sortedRanks;
        // Normal straight
        bool normal = true;
        for (int i = 0; i < sortedRanks.Count - 1; i++)
            if (sortedRanks[i] - sortedRanks[i + 1] != 1) { normal = false; break; }
        if (normal) return true;

        // Ace-low straight: A-2-3-4-5
        if (sortedRanks[0] == 14 && sortedRanks[1] == 5 && sortedRanks[2] == 4
            && sortedRanks[3] == 3 && sortedRanks[4] == 2)
        {
            straightRanks = new List<int> { 5, 4, 3, 2, 1 };
            return true;
        }
        return false;
    }

    private static List<int> GroupTiebreakers(List<IGrouping<int, int>> groups)
    {
        return groups.SelectMany(g => Enumerable.Repeat(g.Key, g.Count())).ToList();
    }

    public static int Compare(HandResult a, HandResult b)
    {
        if (a.Rank != b.Rank) return a.Rank.CompareTo(b.Rank);
        for (int i = 0; i < System.Math.Min(a.Tiebreakers.Count, b.Tiebreakers.Count); i++)
        {
            int cmp = a.Tiebreakers[i].CompareTo(b.Tiebreakers[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    // Generate all combinations of size k from list
    private static List<List<Card>> GetCombinations(List<Card> list, int k)
    {
        var result = new List<List<Card>>();
        Combine(list, k, 0, new List<Card>(), result);
        return result;
    }

    private static void Combine(List<Card> list, int k, int start, List<Card> current, List<List<Card>> result)
    {
        if (current.Count == k) { result.Add(new List<Card>(current)); return; }
        for (int i = start; i < list.Count; i++)
        {
            current.Add(list[i]);
            Combine(list, k, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }
}

// needed for Mathf reference without full UnityEngine import overhead
namespace UnityEngine { public static partial class Mathf { public static int Min(int a, int b) => a < b ? a : b; } }
