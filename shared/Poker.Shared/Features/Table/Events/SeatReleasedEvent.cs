using Poker.Shared.Features.Table.Models;

namespace Poker.Shared.Features.Table.Events
{
	public sealed class SeatReleasedEvent
	{
		public string TableId { get; set; } = "";
		public int SeatIndex { get; set; }
		public SeatReleaseReason Reason { get; set; }
	}
}