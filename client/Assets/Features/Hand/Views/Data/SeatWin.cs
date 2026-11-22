namespace Poker.Client.Features.Hand.Views.Data
{
	public readonly struct SeatWin
	{
		public SeatWin(int seatIndex, long amount)
		{
			SeatIndex = seatIndex;
			Amount = amount;
		}

		public int SeatIndex { get; }
		public long Amount { get; }
	}
}