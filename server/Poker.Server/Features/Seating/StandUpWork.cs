using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Seating.Requests;

namespace Poker.Server.Features.Seating
{
	public sealed class StandUpWork : ITableWork
	{
		private readonly MessageContext _request;

		public StandUpWork(MessageContext request) => _request = request;

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			SeatingResult result = context.Table.StandUp(_request.Connection.Player!.PlayerId);
			if (!result.IsSuccess) return _request.RejectAsync(result.ErrorCode, "Not seated at this table.", cancellationToken);

			return _request.RespondAsync(new StandUpAcceptedResponse { SeatIndex = result.Seat!.Index, LeavesAfterHand = result.Seat.IsLeaving }, cancellationToken);
		}
	}
}