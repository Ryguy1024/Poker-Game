using System;

public enum Suit { Hearts, Diamonds, Clubs, Spades }
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

[Serializable]
public class Card
{
    public Suit Suit;
    public Rank Rank;

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public string SuitSymbol()
    {
        return Suit switch
        {
            Suit.Hearts   => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs    => "♣",
            Suit.Spades   => "♠",
            _ => "?"
        };
    }

    public string RankString()
    {
        return Rank switch
        {
            Rank.Ace   => "A",
            Rank.King  => "K",
            Rank.Queen => "Q",
            Rank.Jack  => "J",
            Rank.Ten   => "10",
            _ => ((int)Rank).ToString()
        };
    }

    public override string ToString() => $"{RankString()}{SuitSymbol()}";
}