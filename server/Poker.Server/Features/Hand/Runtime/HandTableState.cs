using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Features.Hand.Runtime
{
	public sealed class HandTableState : ISnapshotContributor
	{
		public List<TableEvent> Events { get; } = new();
		public PokerHand? Current { get; set; }
		public long HandsPlayed { get; set; }
		public int ButtonSeat { get; set; } = -1;
		public DateTimeOffset NextHandAt { get; set; }
		public long BotTurnId { get; set; }
		public DateTimeOffset BotActAt { get; set; }
		public long ChipsAddedToBots { get; set; }
		public Random? Random { get; set; }

		public Task SendSnapshotAsync(TableContext context, IClientConnection viewer, CancellationToken cancellationToken)
		{
			PokerHand? hand = Current;
			if (hand == null || hand.IsComplete) return Task.CompletedTask;

			return viewer.SendAsync(hand.CreateSnapshot(viewer.Player?.PlayerId), 0, cancellationToken);
		}
	}
}