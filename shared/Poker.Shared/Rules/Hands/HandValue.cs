using System;

namespace Poker.Shared.Rules.Hands
{
	public readonly struct HandValue : IComparable<HandValue>, IEquatable<HandValue>
	{
		public HandValue(int score) => Score = score;

		public int Score { get; }

		public HandCategory Category => (HandCategory)(Score >> 20);

		public int CompareTo(HandValue other) => Score.CompareTo(other.Score);

		public bool Equals(HandValue other) => Score == other.Score;

		public override bool Equals(object? obj) => obj is HandValue other && Equals(other);

		public override int GetHashCode() => Score;

		public override string ToString() => Category switch
		{
			HandCategory.HighCard => "High card",
			HandCategory.OnePair => "Pair",
			HandCategory.TwoPair => "Two pair",
			HandCategory.ThreeOfAKind => "Three of a kind",
			HandCategory.Straight => "Straight",
			HandCategory.Flush => "Flush",
			HandCategory.FullHouse => "Full house",
			HandCategory.FourOfAKind => "Four of a kind",
			_ => "Straight flush",
		};

		public static bool operator >(HandValue left, HandValue right) => left.Score > right.Score;

		public static bool operator <(HandValue left, HandValue right) => left.Score < right.Score;
	}
}