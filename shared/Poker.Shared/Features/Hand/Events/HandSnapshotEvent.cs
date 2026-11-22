using System;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class HandSnapshotEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public Street Street { get; set; }
		public int ButtonSeat { get; set; }
		public string[] Board { get; set; } = Array.Empty<string>();
		public PotInfo[] Pots { get; set; } = Array.Empty<PotInfo>();
		public HandPlayerInfo[] Players { get; set; } = Array.Empty<HandPlayerInfo>();
		public TurnStartedEvent? CurrentTurn { get; set; }
		public string[] MyCards { get; set; } = Array.Empty<string>();
	}
}