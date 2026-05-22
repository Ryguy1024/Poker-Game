using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<Card> cards = new List<Card>();

    public Deck() { Reset(); }

    public void Reset()
    {
        cards.Clear();
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                cards.Add(new Card(suit, rank));
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public Card Deal()
    {
        if (cards.Count == 0)
        {
            Debug.LogError("Deck is empty!");
            return null;
        }
        Card top = cards[0];
        cards.RemoveAt(0);
        return top;
    }

    public int CardsRemaining => cards.Count;
}
