using System;

namespace Poker.Shared.Rules.Cards
{
	public readonly struct Card : IEquatable<Card>
	{
		private const string RankCodes = "23456789TJQKA";
		private const string SuitCodes = "cdhs";

		public Card(Rank rank, Suit suit)
		{
			Rank = rank;
			Suit = suit;
		}

		public Rank Rank { get; }
		public Suit Suit { get; }

		public static Card Parse(string code)
		{
			if (TryParse(code, out Card card)) return card;

			throw new FormatException("Not a card code: '" + code + "'.");
		}

		public static bool TryParse(string code, out Card card)
		{
			card = default;
			if (code == null || code.Length != 2) return false;

			int rank = RankCodes.IndexOf(code[0]);
			int suit = SuitCodes.IndexOf(code[1]);
			if (rank < 0 || suit < 0) return false;

			card = new Card((Rank)(rank + 2), (Suit)suit);
			return true;
		}

		public bool Equals(Card other) => Rank == other.Rank && Suit == other.Suit;

		public override bool Equals(object? obj) => obj is Card other && Equals(other);

		public override int GetHashCode() => (int)Rank * 4 + (int)Suit;

		public override string ToString() => new string(new[] { RankCodes[(int)Rank - 2], SuitCodes[(int)Suit] });

		public static bool operator ==(Card left, Card right) => left.Equals(right);

		public static bool operator !=(Card left, Card right) => !left.Equals(right);
	}
}