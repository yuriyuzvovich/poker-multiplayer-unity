using System;
using Poker.Server.Features.HandHistory;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.HandHistory.Models;
using Xunit;

namespace Poker.Tests.Features.HandHistory
{
	public sealed class HandHistoryDraftTests
	{
		private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

		private readonly PokerTable _table = new(new TableDefinition("t1", "Test", 2, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
		private readonly HandHistoryDraft _draft = new();
		private readonly HandHistoryBook _book = new();

		public HandHistoryDraftTests()
		{
			_table.SeatBot(0, new PlayerIdentity("p-alice", "Alice"), 1000);
			_table.SeatBot(1, new PlayerIdentity("p-bob", "Bob"), 1000);
		}

		[Fact]
		public void EachPlayer_SeesOwnCards_AndNotTheOthersWithoutAShowdown()
		{
			PlayHand(1, showdown: false);

			HandRecord alice = Assert.Single(_book.Get("p-alice"));
			Assert.Equal(new[] { "As", "Ad" }, alice.Players[0].Cards);
			Assert.True(alice.Players[0].IsMe);
			Assert.Empty(alice.Players[1].Cards);
			HandRecord bob = Assert.Single(_book.Get("p-bob"));
			Assert.Equal(new[] { "Kc", "Kd" }, bob.Players[1].Cards);
			Assert.Empty(bob.Players[0].Cards);
		}

		[Fact]
		public void Record_HoldsTheResultBoardAndActions()
		{
			PlayHand(1, showdown: false);

			HandRecord alice = _book.Get("p-alice")[0];
			Assert.Equal("Test", alice.TableName);
			Assert.Equal(1, alice.HandNumber);
			Assert.Equal(-20, alice.NetChips);
			Assert.Equal(20, _book.Get("p-bob")[0].NetChips);
			Assert.Equal(new[] { "2c", "7d", "9h" }, alice.Board);
			Assert.Equal(2, alice.Actions.Length);
			Assert.Equal("Alice", alice.Actions[0].Name);
			Assert.Equal(Street.Preflop, alice.Actions[0].Street);
			Assert.Equal(PlayerAction.Fold, alice.Actions[1].Action);
			Assert.Equal(Street.Flop, alice.Actions[1].Street);
			Assert.Equal(40, alice.Players[1].Won);
		}

		[Fact]
		public void Showdown_RevealsTheOtherCardsWithTheirHand()
		{
			PlayHand(1, showdown: true);

			HandRecordPlayer bob = _book.Get("p-alice")[0].Players[1];
			Assert.Equal(new[] { "Kc", "Kd" }, bob.Cards);
			Assert.Equal("Pair of kings", bob.Description);
		}

		[Fact]
		public void Book_KeepsTheNewestTwentyHands()
		{
			for (int hand = 1; hand <= 22; hand++)
			{
				PlayHand(hand, showdown: false);
			}

			HandRecord[] hands = _book.Get("p-alice");
			Assert.Equal(20, hands.Length);
			Assert.Equal(22, hands[0].HandNumber);
			Assert.Equal(3, hands[19].HandNumber);
		}

		[Fact]
		public void Bots_GetNoRecords()
		{
			PokerTable table = new(new TableDefinition("t2", "Bots", 2, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
			table.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 1000);
			table.SeatBot(1, new PlayerIdentity("p-bob", "Bob"), 1000);
			_draft.Start(Started(1), table);
			_draft.Finish(Ended(1), "Bots", Now, _book);

			Assert.Empty(_book.Get("bot-ada"));
			Assert.Single(_book.Get("p-bob"));
		}

		private void PlayHand(long handNumber, bool showdown)
		{
			_draft.Start(Started(handNumber), _table);
			_draft.SetHoleCards(new HoleCardsEvent { HandNumber = handNumber, SeatIndex = 0, Cards = new[] { "As", "Ad" } });
			_draft.SetHoleCards(new HoleCardsEvent { HandNumber = handNumber, SeatIndex = 1, Cards = new[] { "Kc", "Kd" } });
			_draft.AddAction(new TurnActedEvent { HandNumber = handNumber, SeatIndex = 0, Action = PlayerAction.Call, Amount = 10 });
			_draft.StartRound(new RoundStartedEvent { HandNumber = handNumber, Street = Street.Flop, Board = new[] { "2c", "7d", "9h" } });
			_draft.AddAction(new TurnActedEvent { HandNumber = handNumber, SeatIndex = 0, Action = PlayerAction.Fold });
			if (showdown)
			{
				_draft.Reveal(new ShowdownEvent
				{
					HandNumber = handNumber,
					Board = new[] { "2c", "7d", "9h", "Js", "3h" },
					Reveals = new[] { new CardReveal { SeatIndex = 1, Cards = new[] { "Kc", "Kd" }, Description = "Pair of kings" } },
				});
			}

			_draft.Finish(Ended(handNumber), "Test", Now, _book);
		}

		private static HandStartedEvent Started(long handNumber) => new()
		{
			HandNumber = handNumber,
			Players = new[]
			{
				new HandPlayerInfo { SeatIndex = 0, PlayerId = "p-alice", Stack = 990, Committed = 10 },
				new HandPlayerInfo { SeatIndex = 1, PlayerId = "p-bob", Stack = 980, Committed = 20 },
			},
		};

		private static HandEndedEvent Ended(long handNumber) => new()
		{
			HandNumber = handNumber,
			Awards = new[] { new PotAward { SeatIndex = 1, Amount = 40 } },
			Stacks = new[] { new SeatStack { SeatIndex = 0, Stack = 980 }, new SeatStack { SeatIndex = 1, Stack = 1020 } },
		};
	}
}