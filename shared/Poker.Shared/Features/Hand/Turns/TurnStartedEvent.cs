namespace Poker.Shared.Features.Hand.Turns
{
	public sealed class TurnStartedEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public long TurnId { get; set; }
		public int SeatIndex { get; set; }
		public long DeadlineUnixMs { get; set; }
		public long LeewayMs { get; set; }
		public long ToCall { get; set; }
		public bool CanCheck { get; set; }
		public bool CanRaise { get; set; }
		public long MinRaiseTo { get; set; }
		public long MaxRaiseTo { get; set; }
	}
}