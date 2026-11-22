using System;
using System.Collections.Generic;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Table.Domain;
using Poker.Shared.Features.Hand;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Hand
{
	public sealed class PokerHandTests
	{
		private static readonly HandSettings Settings = new(10, 20, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
		private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

		[Fact]
		public void HeadsUp_ButtonPostsSmallBlindAndActsFirst_FoldGivesTheBlindsAway()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000));
			List<TableEvent> events = Take(hand);

			HandStartedEvent started = Single<HandStartedEvent>(events);
			Assert.Equal(0, started.SmallBlindSeat);
			Assert.Equal(1, started.BigBlindSeat);
			TurnStartedEvent turn = Single<TurnStartedEvent>(events);
			Assert.Equal(0, turn.SeatIndex);
			Assert.Equal(10, turn.ToCall);
			Assert.Equal(Now.AddSeconds(17).ToUnixTimeMilliseconds(), turn.DeadlineUnixMs);
			Assert.Equal(2000, turn.LeewayMs);

			Assert.Equal("", hand.Act("p0", hand.TurnId, PlayerAction.Fold, 0, Now));

			Assert.True(hand.IsComplete);
			HandEndedEvent ended = Single<HandEndedEvent>(Take(hand));
			Assert.Equal(1, Assert.Single(ended.Awards).SeatIndex);
			Assert.Equal(30, ended.Awards[0].Amount);
			Assert.Equal(990, hand.Players[0].Stack);
			Assert.Equal(1010, hand.Players[1].Stack);
		}

		[Fact]
		public void HoleCards_AreAddressedToTheirOwnerOnly()
		{
			PokerHand hand = Start(0, "As Kd Qh Jc", (0, 1000), (1, 1000));

			List<TableEvent> holeCards = new();
			foreach (TableEvent tableEvent in Take(hand))
			{
				if (tableEvent.Message is not HoleCardsEvent) continue;

				holeCards.Add(tableEvent);
			}

			Assert.Equal(2, holeCards.Count);
			Assert.Equal("p0", holeCards[0].RecipientPlayerId);
			Assert.Equal(new[] { "As", "Qh" }, ((HoleCardsEvent)holeCards[0].Message).Cards);
			Assert.Equal("p1", holeCards[1].RecipientPlayerId);
			Assert.Equal(new[] { "Kd", "Jc" }, ((HoleCardsEvent)holeCards[1].Message).Cards);
		}

		[Fact]
		public void ThreeHanded_UnderTheGunActsFirst_ThenFlopStartsLeftOfTheButton()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000), (2, 1000));
			Assert.Equal(0, hand.CurrentActor!.SeatIndex);

			Act(hand, "p0", PlayerAction.Call);
			Act(hand, "p1", PlayerAction.Call);
			Act(hand, "p2", PlayerAction.Check);

			List<TableEvent> events = Take(hand);
			Assert.Equal(60, Single<RoundEndedEvent>(events).Pots[0].Amount);
			RoundStartedEvent flop = Single<RoundStartedEvent>(events);
			Assert.Equal(Street.Flop, flop.Street);
			Assert.Equal(3, flop.Board.Length);
			Assert.Equal(1, hand.CurrentActor!.SeatIndex);
			Assert.True(hand.CreateCurrentTurn()!.CanCheck);
		}

		[Fact]
		public void Reraise_ReopensTheAction_AndSetsTheNewMinimumRaise()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000), (2, 1000));

			Act(hand, "p0", PlayerAction.Raise, 60);
			Act(hand, "p1", PlayerAction.Call);
			Act(hand, "p2", PlayerAction.Raise, 200);

			Assert.Equal(0, hand.CurrentActor!.SeatIndex);
			TurnStartedEvent turn = hand.CreateCurrentTurn()!;
			Assert.Equal(140, turn.ToCall);
			Assert.Equal(340, turn.MinRaiseTo);
			Assert.Equal(1000, turn.MaxRaiseTo);
		}

		[Fact]
		public void AllInPreflop_RunsTheBoardOut_AndTheBestHandWins()
		{
			PokerHand hand = Start(0, "As Kc Ad Kd 2h 7s 9c Jd 3h", (0, 500), (1, 500));

			Act(hand, "p0", PlayerAction.Raise, 500);
			Act(hand, "p1", PlayerAction.Call);

			List<TableEvent> events = Take(hand);
			Assert.Equal(3, Count<RoundStartedEvent>(events));
			ShowdownEvent showdown = Single<ShowdownEvent>(events);
			Assert.Equal(2, showdown.Reveals.Length);
			Assert.Equal(new[] { "2h", "7s", "9c", "Jd", "3h" }, showdown.Board);
			Assert.True(hand.IsComplete);
			Assert.Equal(1000, hand.Players[0].Stack);
			Assert.Equal(0, hand.Players[1].Stack);
		}

		[Fact]
		public void ThreeWayAllIn_ShortStackWinsTheMainPot_MiddleStackWinsTheSidePot()
		{
			PokerHand hand = Start(0, "Kh Qh Ah Kc Qc Ac 2d 7s 9c Jd 3s", (0, 100), (1, 300), (2, 300));

			Act(hand, "p0", PlayerAction.Raise, 100);
			Act(hand, "p1", PlayerAction.Raise, 300);
			Act(hand, "p2", PlayerAction.Call);

			HandEndedEvent ended = Single<HandEndedEvent>(Take(hand));
			Assert.Equal(2, ended.Awards.Length);
			Assert.Equal(0, ended.Awards[0].SeatIndex);
			Assert.Equal(300, ended.Awards[0].Amount);
			Assert.Equal(1, ended.Awards[1].SeatIndex);
			Assert.Equal(400, ended.Awards[1].Amount);
			Assert.Equal(new long[] { 300, 400, 0 }, Stacks(hand));
		}

		[Fact]
		public void SplitPot_GivesTheOddChipToTheFirstWinnerLeftOfTheButton()
		{
			PokerHand hand = StartWith(new HandSettings(5, 10, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)), 0,
				"6h 4c 2c 6s 5d 3d Ah Kd Qs Jc Th", (0, 1000), (1, 1000), (2, 1000));

			Act(hand, "p0", PlayerAction.Call);
			Act(hand, "p1", PlayerAction.Fold);
			Act(hand, "p2", PlayerAction.Check);
			for (int street = 0; street < 3; street++)
			{
				Act(hand, "p2", PlayerAction.Check);
				Act(hand, "p0", PlayerAction.Check);
			}

			HandEndedEvent ended = Single<HandEndedEvent>(Take(hand));
			Assert.Equal(2, ended.Awards.Length);
			Assert.Equal(2, ended.Awards[0].SeatIndex);
			Assert.Equal(13, ended.Awards[0].Amount);
			Assert.Equal(0, ended.Awards[1].SeatIndex);
			Assert.Equal(12, ended.Awards[1].Amount);
			Assert.Equal(new long[] { 1002, 995, 1003 }, Stacks(hand));
		}

		[Fact]
		public void InvalidActions_AreRejected_WithoutChangingTheHand()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000), (2, 1000));
			long turnId = hand.TurnId;

			Assert.Equal(HandErrorCodes.NotYourTurn, hand.Act("p1", turnId, PlayerAction.Call, 0, Now));
			Assert.Equal(HandErrorCodes.StaleTurn, hand.Act("p0", turnId - 1, PlayerAction.Call, 0, Now));
			Assert.Equal(HandErrorCodes.InvalidAction, hand.Act("p0", turnId, PlayerAction.Check, 0, Now));
			Assert.Equal(HandErrorCodes.InvalidAmount, hand.Act("p0", turnId, PlayerAction.Raise, 30, Now));
			Assert.Equal(HandErrorCodes.InvalidAmount, hand.Act("p0", turnId, PlayerAction.Raise, 1001, Now));

			Assert.Equal(0, hand.CurrentActor!.SeatIndex);
			Assert.Equal(turnId, hand.TurnId);
			Assert.Equal(30, hand.PotTotal);
		}

		[Fact]
		public void TimeOut_InsideTheLeeway_DoesNothing_AfterItFoldsFacingABet()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000));
			Take(hand);

			hand.TimeOut(Now.AddSeconds(16.9));
			Assert.Empty(Take(hand));

			hand.TimeOut(Now.AddSeconds(17));
			TurnActedEvent acted = Single<TurnActedEvent>(Take(hand));
			Assert.Equal(PlayerAction.Fold, acted.Action);
			Assert.Equal(ActionReason.Timeout, acted.Reason);
			Assert.True(hand.IsComplete);
		}

		[Fact]
		public void TimeOut_WhenCheckingIsFree_Checks()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000));
			Act(hand, "p0", PlayerAction.Call);
			Take(hand);

			hand.TimeOut(Now.AddSeconds(17));

			TurnActedEvent acted = Single<TurnActedEvent>(Take(hand));
			Assert.Equal(PlayerAction.Check, acted.Action);
			Assert.Equal(Street.Flop, hand.Street);
		}

		[Fact]
		public void ForceFold_OfAPlayerWaitingForTheirTurn_KeepsTheHandGoing()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000), (2, 1000));
			Take(hand);

			hand.ForceFold("p2", Now);

			TurnActedEvent acted = Single<TurnActedEvent>(Take(hand));
			Assert.Equal(2, acted.SeatIndex);
			Assert.Equal(ActionReason.LeftTable, acted.Reason);
			Assert.Equal(0, hand.CurrentActor!.SeatIndex);
			Assert.False(hand.IsComplete);
		}

		[Fact]
		public void ForceFold_LeavingOnePlayer_EndsTheHand()
		{
			PokerHand hand = Start(0, "", (0, 1000), (1, 1000));

			hand.ForceFold("p1", Now);

			Assert.True(hand.IsComplete);
			Assert.Equal(new long[] { 1020, 980 }, Stacks(hand));
		}

		[Fact]
		public void Snapshot_ShowsOnlyTheViewersOwnCards()
		{
			PokerHand hand = Start(0, "As Kd Qh Jc", (0, 1000), (1, 1000));

			HandSnapshotEvent forSeat0 = hand.CreateSnapshot("p0");
			HandSnapshotEvent forSpectator = hand.CreateSnapshot(null);

			Assert.Equal(new[] { "As", "Qh" }, forSeat0.MyCards);
			Assert.Empty(forSpectator.MyCards);
			Assert.Equal(0, forSeat0.CurrentTurn!.SeatIndex);
			Assert.Equal(30, forSeat0.Pots[0].Amount + (forSeat0.Pots.Length > 1 ? forSeat0.Pots[1].Amount : 0));
		}

		private static PokerHand Start(int buttonSeat, string deck, params (int Seat, long Stack)[] players) =>
			StartWith(Settings, buttonSeat, deck, players);

		private static PokerHand StartWith(HandSettings settings, int buttonSeat, string deck, params (int Seat, long Stack)[] players)
		{
			List<HandParticipant> participants = new();
			foreach ((int seat, long stack) in players)
			{
				participants.Add(new HandParticipant(seat, "p" + seat, stack));
			}

			return PokerHand.Start("t1", 1, participants, buttonSeat, settings, ScriptedDeck.Create(deck), Now);
		}

		private static void Act(PokerHand hand, string playerId, PlayerAction action, long amount = 0)
		{
			long before = Total(hand);
			Assert.Equal("", hand.Act(playerId, hand.TurnId, action, amount, Now));
			Assert.Equal(before, Total(hand));
		}

		private static long Total(PokerHand hand)
		{
			long total = 0;
			foreach (HandPlayer player in hand.Players)
			{
				total += player.Stack;
			}

			return hand.IsComplete ? total : total + hand.PotTotal;
		}

		private static long[] Stacks(PokerHand hand)
		{
			long[] stacks = new long[hand.Players.Count];
			for (int i = 0; i < stacks.Length; i++)
			{
				stacks[i] = hand.Players[i].Stack;
			}

			return stacks;
		}

		private static List<TableEvent> Take(PokerHand hand)
		{
			List<TableEvent> events = new();
			hand.TakeEvents(events);
			return events;
		}

		private static T Single<T>(List<TableEvent> events) where T : class
		{
			T? found = null;
			foreach (TableEvent tableEvent in events)
			{
				if (tableEvent.Message is not T typed) continue;

				Assert.Null(found);
				found = typed;
			}

			return found ?? throw new Xunit.Sdk.XunitException("No " + typeof(T).Name + " event.");
		}

		private static int Count<T>(List<TableEvent> events)
		{
			int count = 0;
			foreach (TableEvent tableEvent in events)
			{
				if (tableEvent.Message is not T) continue;

				count++;
			}

			return count;
		}
	}
}