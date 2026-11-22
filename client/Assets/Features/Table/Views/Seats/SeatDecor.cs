using System;

namespace Poker.Client.Features.Table.Views.Seats
{
	public readonly struct SeatDecor : IEquatable<SeatDecor>
	{
		public SeatDecor(string stack, string bet, long betChips, string bubble, string note, bool isActing, bool isDimmed)
		{
			Stack = stack;
			Bet = bet;
			BetChips = betChips;
			Bubble = bubble;
			Note = note;
			IsActing = isActing;
			IsDimmed = isDimmed;
		}

		public string Stack { get; }
		public string Bet { get; }
		public long BetChips { get; }
		public string Bubble { get; }
		public string Note { get; }
		public bool IsActing { get; }
		public bool IsDimmed { get; }

		public static SeatDecor None => new SeatDecor("", "", 0, "", "", false, false);

		public bool Equals(SeatDecor other)
		{
			if (BetChips != other.BetChips) return false;
			if (IsActing != other.IsActing) return false;
			if (IsDimmed != other.IsDimmed) return false;
			if (Stack != other.Stack) return false;
			if (Bet != other.Bet) return false;
			if (Bubble != other.Bubble) return false;

			return Note == other.Note;
		}

		public override bool Equals(object obj) => obj is SeatDecor other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Stack, Bet, BetChips, Bubble, Note, IsActing, IsDimmed);
	}
}