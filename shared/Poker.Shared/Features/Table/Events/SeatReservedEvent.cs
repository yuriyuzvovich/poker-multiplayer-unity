using Poker.Shared.Features.Table.Models;

namespace Poker.Shared.Features.Table.Events
{
	public sealed class SeatReservedEvent
	{
		public string TableId { get; set; } = "";
		public SeatSnapshot Seat { get; set; } = new();
	}
}