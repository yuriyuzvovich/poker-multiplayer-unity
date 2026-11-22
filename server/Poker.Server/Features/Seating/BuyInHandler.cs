using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Seating.Requests;

namespace Poker.Server.Features.Seating
{
	public sealed class BuyInHandler : MessageHandler<BuyInRequest>
	{
		private readonly TableDirectory _directory;

		public BuyInHandler(TableDirectory directory) => _directory = directory ?? throw new ArgumentNullException(nameof(directory));

		protected override Task HandleAsync(MessageContext context, BuyInRequest message, CancellationToken cancellationToken) =>
			_directory.PostOrRejectAsync(context, message.TableId, new BuyInWork(context, message.Amount), cancellationToken);
	}
}