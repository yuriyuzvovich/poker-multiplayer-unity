using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Seating;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Features.TurnTimer;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.TurnTimer;
using Poker.Shared.Features.TurnTimer.Events;
using Poker.Shared.Features.TurnTimer.Requests;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.TurnTimer
{
	public sealed class TurnTimerTests
	{
		private const int AliceSeat = 1;

		private static readonly PlayerIdentity Alice = new("p-alice", "Alice");
		private static readonly TimeSpan PastTheDeadline = TimeSpan.FromSeconds(17);

		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly PokerTable _table = new(new TableDefinition("t1", "Test", 2, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
		private readonly TableActor _actor;
		private readonly HandEngine _engine = new(new SeededDeckFactory(1), new HandOptions { BotSeed = 1 });
		private readonly HandTickWork _tick;
		private readonly RecordingConnection _alice = new(Alice);
		private ushort _requestId;
		private int _seen;

		public TurnTimerTests()
		{
			_table.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 2000);
			_actor = new TableActor(_table, _clock, NullLogger<TableActor>.Instance, new ITableEventListener[] { new TimeoutStrikeListener(new TurnTimerOptions()) });
			_tick = new HandTickWork(_engine);
		}

		[Fact]
		public async Task TwoTimeoutsInARow_SitThePlayerOut_AndTheNextHandSkipsThem()
		{
			await SitAliceAsync();

			await TimeOutAsync(await WaitForAliceTurnAsync());
			Assert.Empty(_alice.SentOfType<PlayerSatOutEvent>());
			await TimeOutAsync(await WaitForAliceTurnAsync());

			PlayerSatOutEvent satOut = Assert.Single(_alice.SentOfType<PlayerSatOutEvent>());
			Assert.Equal(AliceSeat, satOut.SeatIndex);
			Assert.Equal(Alice.PlayerId, satOut.PlayerId);
			int satOutIndex = IndexOf<PlayerSatOutEvent>();
			TurnActedEvent cause = Assert.IsType<TurnActedEvent>(_alice.Sent[satOutIndex - 1].Message);
			Assert.Equal(ActionReason.Timeout, cause.Reason);
			Assert.True(_table.Seats[AliceSeat].IsSittingOut);
			int handsBefore = _alice.SentOfType<HandStartedEvent>().Count;
			await TickForAsync(TimeSpan.FromSeconds(20));
			Assert.Equal(handsBefore, _alice.SentOfType<HandStartedEvent>().Count);
		}

		[Fact]
		public async Task OwnActionBetweenTimeouts_StartsTheCountAgain()
		{
			await SitAliceAsync();

			await TimeOutAsync(await WaitForAliceTurnAsync());
			TurnStartedEvent turn = await WaitForAliceTurnAsync();
			await ActAsync(new TurnActRequest { TableId = "t1", TurnId = turn.TurnId, Action = turn.CanCheck ? PlayerAction.Check : PlayerAction.Fold });
			await TimeOutAsync(await WaitForAliceTurnAsync());

			Assert.Empty(_alice.SentOfType<PlayerSatOutEvent>());
			Assert.False(_table.Seats[AliceSeat].IsSittingOut);
		}

		[Fact]
		public async Task Return_ClearsTheSitOut_AndThePlayerIsDealtAgain()
		{
			await SitAliceAsync();
			await TimeOutAsync(await WaitForAliceTurnAsync());
			await TimeOutAsync(await WaitForAliceTurnAsync());
			int handsBefore = _alice.SentOfType<HandStartedEvent>().Count;

			await _actor.ExecuteAsync(new PlayerReturnWork(new MessageContext(_alice, ++_requestId)), CancellationToken.None);

			Assert.Equal(AliceSeat, Assert.Single(_alice.SentOfType<PlayerReturnAcceptedResponse>()).SeatIndex);
			Assert.Equal(AliceSeat, Assert.Single(_alice.SentOfType<PlayerReturnedEvent>()).SeatIndex);
			Assert.False(_table.Seats[AliceSeat].IsSittingOut);
			await PlayAliceUntilAsync(() => _alice.SentOfType<HandStartedEvent>().Count > handsBefore);
		}

		[Fact]
		public async Task Return_WhenNotSittingOut_IsRejected()
		{
			await SitAliceAsync();

			await _actor.ExecuteAsync(new PlayerReturnWork(new MessageContext(_alice, ++_requestId)), CancellationToken.None);

			Assert.Equal(TurnTimerErrorCodes.NotSittingOut, Assert.Single(_alice.SentOfType<ErrorResponse>()).Code);
			Assert.Empty(_alice.SentOfType<PlayerReturnedEvent>());
		}

		private int IndexOf<T>()
		{
			for (int i = 0; i < _alice.Sent.Count; i++)
			{
				if (_alice.Sent[i].Message is not T) continue;

				return i;
			}

			return -1;
		}

		private async Task SitAliceAsync()
		{
			await _actor.ExecuteAsync(new JoinTableWork(new MessageContext(_alice, ++_requestId)), CancellationToken.None);
			await _actor.ExecuteAsync(new ReserveSeatWork(new MessageContext(_alice, ++_requestId), AliceSeat), CancellationToken.None);
			await _actor.ExecuteAsync(new BuyInWork(new MessageContext(_alice, ++_requestId), 1000), CancellationToken.None);
		}

		private async Task<TurnStartedEvent> WaitForAliceTurnAsync()
		{
			for (int i = 0; i < 2000; i++)
			{
				TurnStartedEvent? turn = NextAliceTurn();
				if (turn != null) return turn;

				_clock.Advance(TimeSpan.FromMilliseconds(250));
				await _actor.ExecuteAsync(_tick, CancellationToken.None);
			}

			throw new Xunit.Sdk.XunitException("Alice never got a turn.");
		}

		private async Task PlayAliceUntilAsync(Func<bool> isDone)
		{
			for (int i = 0; i < 2000; i++)
			{
				if (isDone()) return;

				TurnStartedEvent? turn = NextAliceTurn();
				if (turn != null)
				{
					await ActAsync(new TurnActRequest { TableId = "t1", TurnId = turn.TurnId, Action = turn.CanCheck ? PlayerAction.Check : PlayerAction.Call });
					continue;
				}

				_clock.Advance(TimeSpan.FromMilliseconds(250));
				await _actor.ExecuteAsync(_tick, CancellationToken.None);
			}

			throw new Xunit.Sdk.XunitException("The condition never came true.");
		}

		private TurnStartedEvent? NextAliceTurn()
		{
			IReadOnlyList<SentMessage> sent = _alice.Sent;
			for (; _seen < sent.Count; _seen++)
			{
				if (sent[_seen].Message is not TurnStartedEvent turn) continue;
				if (turn.SeatIndex != AliceSeat) continue;

				_seen++;
				return turn;
			}

			return null;
		}

		private async Task TimeOutAsync(TurnStartedEvent turn)
		{
			_clock.Advance(PastTheDeadline);
			await _actor.ExecuteAsync(_tick, CancellationToken.None);

			TurnActedEvent acted = _alice.SentOfType<TurnActedEvent>().FindLast(a => a.TurnId == turn.TurnId)!;
			Assert.Equal(ActionReason.Timeout, acted.Reason);
		}

		private Task ActAsync(TurnActRequest act) =>
			_actor.ExecuteAsync(new TurnActWork(new MessageContext(_alice, ++_requestId), act, _engine), CancellationToken.None);

		private async Task TickForAsync(TimeSpan duration)
		{
			for (TimeSpan elapsed = TimeSpan.Zero; elapsed < duration; elapsed += TimeSpan.FromMilliseconds(250))
			{
				_clock.Advance(TimeSpan.FromMilliseconds(250));
				await _actor.ExecuteAsync(_tick, CancellationToken.None);
			}
		}
	}
}