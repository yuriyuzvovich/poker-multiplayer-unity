using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Lobby.Requests;

namespace Poker.Server.Features.Lobby
{
	public sealed class ListTablesHandler : MessageHandler<ListTablesRequest>
	{
		private readonly TableDirectory _directory;

		public ListTablesHandler(TableDirectory directory) => _directory = directory ?? throw new ArgumentNullException(nameof(directory));

		protected override Task HandleAsync(MessageContext context, ListTablesRequest message, CancellationToken cancellationToken) =>
			context.RespondAsync(new TableListResponse { Tables = _directory.GetSummaries() }, cancellationToken);
	}
}