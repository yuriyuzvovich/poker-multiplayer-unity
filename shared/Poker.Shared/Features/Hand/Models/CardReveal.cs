using System;
using Poker.Shared.Rules.Hands;

namespace Poker.Shared.Features.Hand.Models
{
	public sealed class CardReveal
	{
		public int SeatIndex { get; set; }
		public string[] Cards { get; set; } = Array.Empty<string>();
		public HandCategory Category { get; set; }
		public string Description { get; set; } = "";
	}
}