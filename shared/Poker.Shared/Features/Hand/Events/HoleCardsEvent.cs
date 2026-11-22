using System;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class HoleCardsEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public int SeatIndex { get; set; }
		public string[] Cards { get; set; } = Array.Empty<string>();
	}
}