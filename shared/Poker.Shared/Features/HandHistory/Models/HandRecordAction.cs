using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;


namespace Poker.Shared.Features.HandHistory.Models
{
	public sealed class HandRecordAction
	{
		public string Name { get; set; } = "";
		public Street Street { get; set; }
		public PlayerAction Action { get; set; }
		public long Amount { get; set; }
	}
}