using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table;

namespace Poker.Server.Features.Seating
{
	public sealed class ReserveSeatWork : ITableWork
	{
		private readonly MessageContext _request;
		private readonly int _seatIndex;

		public ReserveSeatWork(MessageContext request, int seatIndex)
		{
			_request = request;
			_seatIndex = seatIndex;
		}

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			if (!context.IsWatching(_request.Connection))
			{
				return _request.RejectAsync(TableErrorCodes.NotJoined, "Join the table first.", cancellationToken);
			}

			SeatingResult result = context.Table.Reserve(_seatIndex, _request.Connection.Player!, context.Now);
			if (!result.IsSuccess) return _request.RejectAsync(result.ErrorCode, "Cannot reserve seat " + _seatIndex + ".", cancellationToken);

			Seat seat = result.Seat!;
			TableDefinition definition = context.Table.Definition;
			return _request.RespondAsync(new SeatReservationResponse
			{
				SeatIndex = seat.Index,
				ExpiresAtUnixMs = seat.ReservedUntil!.Value.ToUnixTimeMilliseconds(),
				MinBuyIn = definition.MinBuyIn,
				MaxBuyIn = definition.MaxBuyIn,
			}, cancellationToken);
		}
	}
}