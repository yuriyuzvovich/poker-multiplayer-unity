namespace Poker.Shared.Features.Seating.Requests
{
	public sealed class ReserveSeatRequest
	{
		public string TableId { get; set; } = "";
		public int SeatIndex { get; set; }
	}
}