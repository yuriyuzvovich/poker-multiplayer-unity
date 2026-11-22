using System;

namespace Poker.Shared.Features.Hand.Models
{
	public sealed class PotInfo
	{
		public long Amount { get; set; }
		public int[] EligibleSeats { get; set; } = Array.Empty<int>();
	}
}