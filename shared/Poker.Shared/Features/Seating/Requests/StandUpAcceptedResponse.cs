namespace Poker.Shared.Features.Seating.Requests
{
	public sealed class StandUpAcceptedResponse
	{
		public int SeatIndex { get; set; }
		public bool LeavesAfterHand { get; set; }
	}
}