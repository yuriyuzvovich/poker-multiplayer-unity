using System;
using System.Collections.Generic;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Hand
{
	public sealed class HandSimulationTests
	{
		private const int BotCount = 5;
		private const long BotStack = 1000;

		[Fact]
		public void ThousandBotHands_KeepEveryInvariant()
		{
			Simulation simulation = new(deckSeed: 12345, botSeed: 777);

			simulation.Run(hands: 1000);

			Assert.Equal(1000, simulation.HandsCompleted);
			Assert.True(simulation.Showdowns > 50, "showdowns: " + simulation.Showdowns);
			Assert.True(simulation.Raises > 100, "raises: " + simulation.Raises);
		}

		[Fact]
		public void SameSeeds_PlayTheSameGame()
		{
			Simulation first = new(deckSeed: 42, botSeed: 7);
			Simulation second = new(deckSeed: 42, botSeed: 7);

			first.Run(hands: 100);
			second.Run(hands: 100);

			Assert.Equal(first.FinalStacks(), second.FinalStacks());
			Assert.Equal(first.EventCount, second.EventCount);
		}

		private sealed class Simulation
		{
			private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
			private readonly PokerTable _table = new(new TableDefinition("sim", "Simulation", 6, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
			private readonly TableContext _context;
			private readonly HandEngine _engine;
			private readonly List<TableEvent> _events = new();
			private readonly Dictionary<int, string> _handPlayers = new();
			private long _handStartTotal;
			private long _lastTurnId;

			public Simulation(int deckSeed, int botSeed)
			{
				for (int seat = 0; seat < BotCount; seat++)
				{
					_table.SeatBot(seat, new PlayerIdentity("bot-" + seat, "Bot " + seat), BotStack);
				}

				_context = new TableContext(_table, _clock);
				_engine = new HandEngine(new SeededDeckFactory(deckSeed), new HandOptions { BotSeed = botSeed });
			}

			public int HandsCompleted { get; private set; }
			public int Showdowns { get; private set; }
			public int Raises { get; private set; }
			public int EventCount { get; private set; }

			public void Run(int hands)
			{
				for (int tick = 0; tick < 1_000_000 && HandsCompleted < hands; tick++)
				{
					_clock.Advance(TimeSpan.FromMilliseconds(250));
					_engine.Tick(_context);
					_table.TakeEvents(_events);
					foreach (TableEvent tableEvent in _events)
					{
						Check(tableEvent);
					}

					EventCount += _events.Count;
					_events.Clear();
					CheckTableTotal();
				}
			}

			public long[] FinalStacks()
			{
				long[] stacks = new long[BotCount];
				for (int seat = 0; seat < BotCount; seat++)
				{
					stacks[seat] = _table.Seats[seat].Stack;
				}

				return stacks;
			}

			private void Check(TableEvent tableEvent)
			{
				switch (tableEvent.Message)
				{
					case HandStartedEvent started:
						Assert.True(tableEvent.IsPublic);
						_handPlayers.Clear();
						_handStartTotal = 0;
						foreach (HandPlayerInfo player in started.Players)
						{
							_handPlayers.Add(player.SeatIndex, player.PlayerId);
							_handStartTotal += player.Stack + player.Committed;
						}

						break;
					case HoleCardsEvent holeCards:
						Assert.False(tableEvent.IsPublic, "hole cards must be addressed");
						Assert.Equal(_handPlayers[holeCards.SeatIndex], tableEvent.RecipientPlayerId);
						Assert.Equal(2, holeCards.Cards.Length);
						break;
					case TurnStartedEvent turn:
						Assert.True(tableEvent.IsPublic);
						Assert.True(_handPlayers.ContainsKey(turn.SeatIndex));
						Assert.True(turn.TurnId > _lastTurnId, "turn ids must grow");
						_lastTurnId = turn.TurnId;
						break;
					case TurnActedEvent acted:
						Assert.True(acted.Stack >= 0);
						if (acted.Action == PlayerAction.Raise) Raises++;

						break;
					case ShowdownEvent:
						Showdowns++;
						break;
					case HandEndedEvent ended:
						long endTotal = 0;
						foreach (SeatStack stack in ended.Stacks)
						{
							Assert.True(stack.Stack >= 0);
							endTotal += stack.Stack;
						}

						Assert.Equal(_handStartTotal, endTotal);
						HandsCompleted++;
						break;
					default:
						Assert.True(tableEvent.IsPublic);
						break;
				}
			}

			private void CheckTableTotal()
			{
				HandTableState state = _context.GetState<HandTableState>();
				long total = 0;
				PokerHand? hand = state.Current;
				foreach (Seat seat in _table.Seats)
				{
					if (hand != null && IsInHand(hand, seat.Index)) continue;

					total += seat.Stack;
				}

				if (hand != null)
				{
					foreach (HandPlayer player in hand.Players)
					{
						total += player.Stack;
					}

					total += hand.PotTotal;
				}

				Assert.Equal(BotCount * BotStack + state.ChipsAddedToBots, total);
			}

			private static bool IsInHand(PokerHand hand, int seatIndex)
			{
				foreach (HandPlayer player in hand.Players)
				{
					if (player.SeatIndex == seatIndex) return true;
				}

				return false;
			}
		}
	}
}