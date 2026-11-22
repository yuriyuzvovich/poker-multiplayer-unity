using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.HandHistory.Requests;

namespace Poker.Server.Features.HandHistory
{
	public sealed class GetHandHistoryHandler : MessageHandler<GetHandHistoryRequest>
	{
		private readonly HandHistoryBook _book;

		public GetHandHistoryHandler(HandHistoryBook book) => _book = book ?? throw new ArgumentNullException(nameof(book));

		protected override Task HandleAsync(MessageContext context, GetHandHistoryRequest message, CancellationToken cancellationToken) =>
			context.RespondAsync(new HandHistoryResponse { Hands = _book.Get(context.Connection.Player!.PlayerId) }, cancellationToken);
	}
}