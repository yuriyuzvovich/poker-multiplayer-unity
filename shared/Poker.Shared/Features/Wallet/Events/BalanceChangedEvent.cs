using Poker.Shared.Features.Wallet.Models;

namespace Poker.Shared.Features.Wallet.Events
{
	public sealed class BalanceChangedEvent
	{
		public long Balance { get; set; }
		public long Delta { get; set; }
		public BalanceChangeReason Reason { get; set; }
	}
}