namespace Poker.Shared.Features.Hand.Turns
{
	public sealed class TurnActedEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public long TurnId { get; set; }
		public int SeatIndex { get; set; }
		public PlayerAction Action { get; set; }
		public long Amount { get; set; }
		public long Committed { get; set; }
		public long Stack { get; set; }
		public long PotTotal { get; set; }
		public bool IsAllIn { get; set; }
		public ActionReason Reason { get; set; }
	}
}