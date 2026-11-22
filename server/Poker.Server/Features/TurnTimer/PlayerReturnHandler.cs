using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.TurnTimer.Requests;

namespace Poker.Server.Features.TurnTimer
{
	public sealed class PlayerReturnHandler : MessageHandler<PlayerReturnRequest>
	{
		private readonly TableDirectory _directory;

		public PlayerReturnHandler(TableDirectory directory) => _directory = directory ?? throw new ArgumentNullException(nameof(directory));

		protected override Task HandleAsync(MessageContext context, PlayerReturnRequest message, CancellationToken cancellationToken) =>
			_directory.PostOrRejectAsync(context, message.TableId, new PlayerReturnWork(context), cancellationToken);
	}
}