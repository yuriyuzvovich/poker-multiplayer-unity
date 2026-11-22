using System;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.TurnTimer.Events;

namespace Poker.Server.Features.TurnTimer
{
	public sealed class TimeoutStrikeListener : ITableEventListener
	{
		private readonly TurnTimerOptions _options;

		public TimeoutStrikeListener(TurnTimerOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));

		public void OnEvent(TableContext context, TableEvent tableEvent)
		{
			if (tableEvent.Message is SeatReleasedEvent released)
			{
				context.GetState<TurnTimerTableState>().Clear(released.SeatIndex);
				return;
			}

			if (tableEvent.Message is not TurnActedEvent acted) return;

			TurnTimerTableState state = context.GetState<TurnTimerTableState>();
			if (acted.Reason == ActionReason.Player)
			{
				state.Clear(acted.SeatIndex);
				return;
			}

			if (acted.Reason != ActionReason.Timeout) return;
			if (state.AddStrike(acted.SeatIndex) < _options.StrikesToSitOut) return;

			Seat seat = context.Table.Seats[acted.SeatIndex];
			if (seat.Player == null) return;
			if (seat.IsSittingOut) return;

			context.Table.SetSittingOut(seat.Player.PlayerId, true);
			context.Table.Publish(new PlayerSatOutEvent { TableId = acted.TableId, SeatIndex = seat.Index, PlayerId = seat.Player.PlayerId });
		}
	}
}