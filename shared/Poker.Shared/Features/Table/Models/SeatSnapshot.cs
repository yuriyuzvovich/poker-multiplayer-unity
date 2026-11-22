namespace Poker.Shared.Features.Table.Models
{
	public sealed class SeatSnapshot
	{
		public int Index { get; set; }
		public SeatStatus Status { get; set; }
		public string PlayerId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public long Stack { get; set; }
		public long ReservedUntilUnixMs { get; set; }
		public bool IsSittingOut { get; set; }
	}
}