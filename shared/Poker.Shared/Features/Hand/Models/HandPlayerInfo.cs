namespace Poker.Shared.Features.Hand.Models
{
	public sealed class HandPlayerInfo
	{
		public int SeatIndex { get; set; }
		public string PlayerId { get; set; } = "";
		public long Stack { get; set; }
		public long Committed { get; set; }
		public bool IsFolded { get; set; }
		public bool IsAllIn { get; set; }
	}
}