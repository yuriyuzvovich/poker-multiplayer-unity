namespace Poker.Shared.Features.Hand.Turns
{
	public sealed class TurnActRequest
	{
		public string TableId { get; set; } = "";
		public long TurnId { get; set; }
		public PlayerAction Action { get; set; }
		public long Amount { get; set; }
	}
}