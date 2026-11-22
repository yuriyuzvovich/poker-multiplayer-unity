namespace Poker.Shared.Features.Hand.Models
{
	public sealed class PotAward
	{
		public int PotIndex { get; set; }
		public int SeatIndex { get; set; }
		public long Amount { get; set; }
	}
}