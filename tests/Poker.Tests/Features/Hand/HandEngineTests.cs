using System;
using System.Collections.Generic;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Hand;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Hand
{
	public sealed class HandEngineTests
	{
		private static readonly PlayerIdentity Alice = new("p-alice", "Alice");

		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly RecordingChipBank _bank = new();
		private readonly PokerTable _table;
		private readonly TableContext _context;
		private readonly HandEngine _engine = new(new SeededDeckFactory(1), new HandOptions { BotSeed = 1 });
		private readonly List<TableEvent> _events = new();

		public HandEngineTests()
		{
			_bank.Deposit(Alice.PlayerId, 5000);
			_table = new PokerTable(new TableDefinition("t1", "Test", 2, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)), _bank);
			_table.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 1000);
			_table.Reserve(1, Alice, _clock.UtcNow);
			_table.BuyIn(Alice.PlayerId, 1000, _clock.UtcNow);
			_context = new TableContext(_table, _clock);
			Drain();
		}

		[Fact]
		public void Tick_WithTwoSeatedPlayers_StartsAHand_AndAddressesHoleCards()
		{
			_engine.Tick(_context);

			List<TableEvent> events = Drain();
			Assert.Contains(events, e => e.Message is HandStartedEvent);
			TableEvent aliceCards = Assert.Single(events, e => e.Message is HoleCardsEvent && e.RecipientPlayerId == Alice.PlayerId);
			Assert.Equal(1, ((HoleCardsEvent)aliceCards.Message).SeatIndex);
		}

		[Fact]
		public void Act_OnTheHumansTurn_IsAccepted_AndAStaleTurnIsRejected()
		{
			PokerHand hand = TickUntilAlicesTurn();
			long turnId = hand.TurnId;

			string error = _engine.Act(_context, Alice.PlayerId, new TurnActRequest { TableId = "t1", TurnId = turnId, Action = PlayerAction.Fold });

			Assert.Equal("", error);
			Assert.Equal(HandErrorCodes.NoHand, _engine.Act(_context, Alice.PlayerId, new TurnActRequest { TableId = "t1", TurnId = turnId, Action = PlayerAction.Fold }));
		}

		[Fact]
		public void HumanTimeout_IsAppliedByTheTick()
		{
			PokerHand hand = TickUntilAlicesTurn();
			long turnId = hand.TurnId;
			Drain();

			_clock.Advance(TimeSpan.FromSeconds(17));
			_engine.Tick(_context);

			TurnActedEvent acted = (TurnActedEvent)Assert.Single(Drain(), e => e.Message is TurnActedEvent).Message;
			Assert.Equal(turnId, acted.TurnId);
			Assert.Equal(ActionReason.Timeout, acted.Reason);
		}

		[Fact]
		public void PlayerStandingUpMidHand_KeepsTheSeatUntilTheHandEnds_ThenCashesOutTheFinalStack()
		{
			_engine.Tick(_context);
			Assert.True(_table.StandUp(Alice.PlayerId).IsSuccess);
			Assert.Equal(SeatStatus.Seated, _table.Seats[1].Status);
			Assert.True(_table.Seats[1].IsLeaving);
			Assert.Equal(4000, _bank.BalanceOf(Alice.PlayerId));
			Drain();

			_engine.Tick(_context);

			List<TableEvent> events = Drain();
			TurnActedEvent folded = (TurnActedEvent)Assert.Single(events, e => e.Message is TurnActedEvent).Message;
			Assert.Equal(ActionReason.LeftTable, folded.Reason);
			int endedAt = events.FindIndex(e => e.Message is HandEndedEvent);
			int releasedAt = events.FindIndex(e => e.Message is SeatReleasedEvent);
			Assert.True(endedAt >= 0 && releasedAt > endedAt, "the seat is released after the hand ended");
			HandEndedEvent ended = (HandEndedEvent)events[endedAt].Message;
			long finalStack = Array.Find(ended.Stacks, s => s.SeatIndex == 1)!.Stack;
			Assert.True(finalStack < 1000, "the blind she posted stays in the pot");
			Assert.Equal(4000 + finalStack, _bank.BalanceOf(Alice.PlayerId));
			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
		}

		[Fact]
		public void BrokeBot_IsToppedUp_BeforeTheNextHand()
		{
			_table.TrySetStack(0, "bot-ada", 5);

			_engine.Tick(_context);

			HandStartedEvent started = (HandStartedEvent)Assert.Single(Drain(), e => e.Message is HandStartedEvent).Message;
			HandPlayerInfo bot = Array.Find(started.Players, p => p.SeatIndex == 0)!;
			Assert.Equal(1000, bot.Stack + bot.Committed);
			Assert.Equal(995, _context.GetState<HandTableState>().ChipsAddedToBots);
		}

		[Fact]
		public void SittingOutActor_ActsAtOnce_WithoutWaitingForTheDeadline()
		{
			PokerHand hand = TickUntilAlicesTurn();
			long turnId = hand.TurnId;
			_table.SetSittingOut(Alice.PlayerId, true);
			Drain();

			_engine.Tick(_context);

			TurnActedEvent acted = (TurnActedEvent)Assert.Single(Drain(), e => e.Message is TurnActedEvent).Message;
			Assert.Equal(turnId, acted.TurnId);
			Assert.Equal(ActionReason.Timeout, acted.Reason);
		}

		[Fact]
		public void SittingOutSeat_IsNotDealtIn()
		{
			_table.SetSittingOut(Alice.PlayerId, true);

			_engine.Tick(_context);

			Assert.DoesNotContain(Drain(), e => e.Message is HandStartedEvent);
			Assert.Null(_context.GetState<HandTableState>().Current);
		}

		private PokerHand TickUntilAlicesTurn()
		{
			for (int i = 0; i < 400; i++)
			{
				_engine.Tick(_context);
				PokerHand? hand = _context.GetState<HandTableState>().Current;
				if (hand?.CurrentActor?.PlayerId == Alice.PlayerId) return hand;

				_clock.Advance(TimeSpan.FromMilliseconds(250));
			}

			throw new Xunit.Sdk.XunitException("Alice never got a turn.");
		}

		private List<TableEvent> Drain()
		{
			_events.Clear();
			_table.TakeEvents(_events);
			return new List<TableEvent>(_events);
		}
	}
}