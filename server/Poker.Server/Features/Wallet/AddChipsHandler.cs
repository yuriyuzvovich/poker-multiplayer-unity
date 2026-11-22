using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Wallet;
using Poker.Shared.Features.Wallet.Requests;

namespace Poker.Server.Features.Wallet
{
	public sealed class AddChipsHandler : MessageHandler<AddChipsRequest>
	{
		private readonly WalletBook _wallets;

		public AddChipsHandler(WalletBook wallets) => _wallets = wallets ?? throw new ArgumentNullException(nameof(wallets));

		protected override Task HandleAsync(MessageContext context, AddChipsRequest message, CancellationToken cancellationToken)
		{
			if (!_wallets.TryClaimFreeChips(context.Connection.Player!.PlayerId, out long balance))
			{
				return context.RejectAsync(WalletErrorCodes.NotEligible, "Free chips are only for balances under the limit.", cancellationToken);
			}

			return context.RespondAsync(new BalanceResponse { Balance = balance }, cancellationToken);
		}
	}
}