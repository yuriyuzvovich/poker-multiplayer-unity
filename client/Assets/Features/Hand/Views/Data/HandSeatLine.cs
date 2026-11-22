using System;

namespace Poker.Client.Features.Hand.Views.Data
{
	public sealed class HandSeatLine
	{
		public int SeatIndex { get; set; }
		public string Name { get; set; } = "";
		public bool IsMine { get; set; }
		public bool IsButton { get; set; }
		public bool IsActing { get; set; }
		public bool IsFolded { get; set; }
		public long Stack { get; set; }
		public long Bet { get; set; }
		public string Action { get; set; } = "";
		public string Description { get; set; } = "";
		public string[] Cards { get; set; } = Array.Empty<string>();
	}
}