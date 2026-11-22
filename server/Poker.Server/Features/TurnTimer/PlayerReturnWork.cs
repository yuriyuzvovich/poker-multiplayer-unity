using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.TurnTimer;
using Poker.Shared.Features.TurnTimer.Events;
using Poker.Shared.Features.TurnTimer.Requests;

namespace Poker.Server.Features.TurnTimer
{
	public sealed class PlayerReturnWork : ITableWork
	{
		private readonly MessageContext _request;

		public PlayerReturnWork(MessageContext request) => _request = request;

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			string playerId = _request.Connection.Player!.PlayerId;
			Seat? seat = context.Table.FindSeatOf(playerId);
			if (seat is not { Status: SeatStatus.Seated }) return _request.RejectAsync(SeatErrorCodes.NotSeated, "Not seated at this table.", cancellationToken);
			if (!seat.IsSittingOut) return _request.RejectAsync(TurnTimerErrorCodes.NotSittingOut, "Already playing.", cancellationToken);

			context.Table.SetSittingOut(playerId, false);
			context.GetState<TurnTimerTableState>().Clear(seat.Index);
			context.Table.Publish(new PlayerReturnedEvent { TableId = context.Table.Definition.TableId, SeatIndex = seat.Index, PlayerId = playerId });
			return _request.RespondAsync(new PlayerReturnAcceptedResponse { SeatIndex = seat.Index }, cancellationToken);
		}
	}
}