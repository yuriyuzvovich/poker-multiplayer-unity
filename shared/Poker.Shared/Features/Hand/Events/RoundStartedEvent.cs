using System;
using Poker.Shared.Features.Hand.Models;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class RoundStartedEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public Street Street { get; set; }
		public string[] Board { get; set; } = Array.Empty<string>();
	}
}