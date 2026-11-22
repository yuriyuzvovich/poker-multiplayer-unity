namespace Poker.Shared.Features.Seating.Requests
{
	public sealed class SeatReservationResponse
	{
		public int SeatIndex { get; set; }
		public long ExpiresAtUnixMs { get; set; }
		public long MinBuyIn { get; set; }
		public long MaxBuyIn { get; set; }
	}
}