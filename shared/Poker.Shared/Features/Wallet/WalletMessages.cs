using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.Wallet
{
	public static class WalletMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<GetBalanceRequest>(WalletMessageNames.GetBalance);
			registry.Register<BalanceResponse>(WalletMessageNames.Balance);
			registry.Register<AddChipsRequest>(WalletMessageNames.AddChips);
			registry.Register<BalanceChangedEvent>(WalletMessageNames.BalanceChanged);
		}
	}
}