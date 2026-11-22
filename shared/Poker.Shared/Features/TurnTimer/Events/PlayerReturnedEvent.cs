namespace Poker.Shared.Features.TurnTimer.Events
{
	public sealed class PlayerReturnedEvent
	{
		public string TableId { get; set; } = "";
		public int SeatIndex { get; set; }
		public string PlayerId { get; set; } = "";
	}
}