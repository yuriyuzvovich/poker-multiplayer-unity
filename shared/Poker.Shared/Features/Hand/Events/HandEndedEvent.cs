using System;
using Poker.Shared.Features.Hand.Models;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class HandEndedEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public PotAward[] Awards { get; set; } = Array.Empty<PotAward>();
		public SeatStack[] Stacks { get; set; } = Array.Empty<SeatStack>();
		public long NextHandInMs { get; set; }
	}
}