namespace Poker.Shared.Features.Lobby.Models
{
	public sealed class TableSummary
	{
		public string TableId { get; set; } = "";
		public string Name { get; set; } = "";
		public long SmallBlind { get; set; }
		public long BigBlind { get; set; }
		public int SeatCount { get; set; }
		public int OccupiedSeats { get; set; }
	}
}