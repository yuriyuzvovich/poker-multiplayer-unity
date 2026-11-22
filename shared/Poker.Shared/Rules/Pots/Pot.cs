using System;

namespace Poker.Shared.Rules.Pots
{
	public sealed class Pot
	{
		public long Amount { get; set; }
		public int[] EligibleSeats { get; set; } = Array.Empty<int>();
	}
}