namespace Poker.Shared.Rules.Pots
{
	public readonly struct PotContribution
	{
		public PotContribution(int seatIndex, long amount, bool isFolded)
		{
			SeatIndex = seatIndex;
			Amount = amount;
			IsFolded = isFolded;
		}

		public int SeatIndex { get; }
		public long Amount { get; }
		public bool IsFolded { get; }
	}
}