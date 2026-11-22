using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Seating.Requests;

namespace Poker.Server.Features.Seating
{
	public sealed class BuyInWork : ITableWork
	{
		private readonly MessageContext _request;
		private readonly long _amount;

		public BuyInWork(MessageContext request, long amount)
		{
			_request = request;
			_amount = amount;
		}

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			SeatingResult result = context.Table.BuyIn(_request.Connection.Player!.PlayerId, _amount, context.Now);
			if (!result.IsSuccess) return _request.RejectAsync(result.ErrorCode, "Buy-in of " + _amount + " refused.", cancellationToken);

			Seat seat = result.Seat!;
			return _request.RespondAsync(new BuyInAcceptedResponse { SeatIndex = seat.Index, Stack = seat.Stack }, cancellationToken);
		}
	}
}