namespace Poker.Shared.Features.Seating.Requests
{
	public sealed class BuyInAcceptedResponse
	{
		public int SeatIndex { get; set; }
		public long Stack { get; set; }
	}
}