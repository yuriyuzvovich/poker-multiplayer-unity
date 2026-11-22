using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Wallet.Requests;

namespace Poker.Server.Features.Wallet
{
	public sealed class GetBalanceHandler : MessageHandler<GetBalanceRequest>
	{
		private readonly WalletBook _wallets;

		public GetBalanceHandler(WalletBook wallets) => _wallets = wallets ?? throw new ArgumentNullException(nameof(wallets));

		protected override Task HandleAsync(MessageContext context, GetBalanceRequest message, CancellationToken cancellationToken) =>
			context.RespondAsync(new BalanceResponse { Balance = _wallets.GetBalance(context.Connection.Player!.PlayerId) }, cancellationToken);
	}
}