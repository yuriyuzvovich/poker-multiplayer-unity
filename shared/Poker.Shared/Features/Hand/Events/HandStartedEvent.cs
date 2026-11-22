using System;
using Poker.Shared.Features.Hand.Models;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class HandStartedEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public int ButtonSeat { get; set; }
		public int SmallBlindSeat { get; set; }
		public int BigBlindSeat { get; set; }
		public long SmallBlind { get; set; }
		public long BigBlind { get; set; }
		public HandPlayerInfo[] Players { get; set; } = Array.Empty<HandPlayerInfo>();
	}
}