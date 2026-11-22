using System;
using System.Collections.Generic;
using Poker.Server.Features.Hand.Bots;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Shared.Features.Hand;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;

namespace Poker.Server.Features.Hand.Runtime
{
	public sealed class HandEngine
	{
		private readonly IDeckFactory _decks;
		private readonly HandOptions _options;

		public HandEngine(IDeckFactory decks, HandOptions options)
		{
			_decks = decks ?? throw new ArgumentNullException(nameof(decks));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public void Tick(TableContext context)
		{
			HandTableState state = context.GetState<HandTableState>();
			DateTimeOffset now = context.Now;
			PokerHand? hand = state.Current;
			if (hand != null)
			{
				FoldPlayersWhoLeft(hand, context.Table, now);
				TimeOutPlayerWhoSatOut(hand, context.Table, now);
				TryBotAct(state, hand, now);
				hand.TimeOut(now);
				AfterChange(state, context);
				return;
			}

			if (now < state.NextHandAt) return;

			TryStartHand(state, context);
		}

		public string Act(TableContext context, string playerId, TurnActRequest request)
		{
			HandTableState state = context.GetState<HandTableState>();
			PokerHand? hand = state.Current;
			if (hand == null) return HandErrorCodes.NoHand;

			string error = hand.Act(playerId, request.TurnId, request.Action, request.Amount, context.Now);
			AfterChange(state, context);
			return error;
		}

		private void TryStartHand(HandTableState state, TableContext context)
		{
			PokerTable table = context.Table;
			TopUpBots(state, table);
			List<HandParticipant> participants = new();
			foreach (Seat seat in table.Seats)
			{
				if (seat.Status != SeatStatus.Seated) continue;
				if (seat.Stack <= 0) continue;
				if (seat.IsSittingOut) continue;

				participants.Add(new HandParticipant(seat.Index, seat.Player!.PlayerId, seat.Stack));
			}

			if (participants.Count < 2) return;

			state.ButtonSeat = NextButton(participants, state.ButtonSeat);
			state.HandsPlayed++;
			TableDefinition definition = table.Definition;
			HandSettings settings = new(definition.SmallBlind, definition.BigBlind, _options.TurnTime, _options.TurnLeeway, _options.NextHandDelay);
			foreach (HandParticipant participant in participants)
			{
				table.EnterHand(participant.SeatIndex);
			}

			state.Current = PokerHand.Start(definition.TableId, state.HandsPlayed, participants, state.ButtonSeat, settings, _decks.CreateShuffled(), context.Now);
			AfterChange(state, context);
		}

		private void AfterChange(HandTableState state, TableContext context)
		{
			PokerHand? hand = state.Current;
			if (hand == null) return;

			hand.TakeEvents(state.Events);
			foreach (TableEvent tableEvent in state.Events)
			{
				context.Table.Publish(tableEvent);
			}

			state.Events.Clear();
			if (!hand.IsComplete)
			{
				ScheduleBot(state, hand, context.Now);
				return;
			}

			ApplyResults(hand, context.Table);
			state.Current = null;
			state.NextHandAt = context.Now + _options.NextHandDelay;
		}

		private void ScheduleBot(HandTableState state, PokerHand hand, DateTimeOffset now)
		{
			HandPlayer? actor = hand.CurrentActor;
			if (actor == null || !BotRoster.IsBot(actor.PlayerId)) return;
			if (state.BotTurnId == hand.TurnId) return;

			Random random = RandomFor(state);
			double thinkMs = _options.BotThinkMin.TotalMilliseconds + random.NextDouble() * (_options.BotThinkMax - _options.BotThinkMin).TotalMilliseconds;
			state.BotTurnId = hand.TurnId;
			state.BotActAt = now + TimeSpan.FromMilliseconds(thinkMs);
		}

		private void TryBotAct(HandTableState state, PokerHand hand, DateTimeOffset now)
		{
			HandPlayer? actor = hand.CurrentActor;
			if (actor == null || !BotRoster.IsBot(actor.PlayerId)) return;
			if (state.BotTurnId != hand.TurnId) return;
			if (now < state.BotActAt) return;

			TurnStartedEvent turn = hand.CreateCurrentTurn()!;
			PlayerAction action = BotPolicy.Decide(turn, actor.Stack, RandomFor(state), out long raiseTo);
			hand.Act(actor.PlayerId, hand.TurnId, action, raiseTo, now);
		}

		private static void FoldPlayersWhoLeft(PokerHand hand, PokerTable table, DateTimeOffset now)
		{
			foreach (HandPlayer player in hand.Players)
			{
				if (player.IsFolded) continue;
				if (!table.Seats[player.SeatIndex].IsLeaving) continue;

				hand.ForceFold(player.PlayerId, now);
			}
		}

		private static void TimeOutPlayerWhoSatOut(PokerHand hand, PokerTable table, DateTimeOffset now)
		{
			HandPlayer? actor = hand.CurrentActor;
			if (actor == null) return;
			if (!table.Seats[actor.SeatIndex].IsSittingOut) return;

			hand.TimeOutNow(now);
		}

		private static void ApplyResults(PokerHand hand, PokerTable table)
		{
			foreach (HandPlayer player in hand.Players)
			{
				if (!table.TrySetStack(player.SeatIndex, player.PlayerId, player.Stack)) throw new InvalidOperationException("Seat " + player.SeatIndex + " changed owner during hand " + hand.HandNumber + ".");

				table.LeaveHand(player.SeatIndex);
			}
		}

		private static void TopUpBots(HandTableState state, PokerTable table)
		{
			long topUpTo = table.Definition.MaxBuyIn / 2;
			foreach (Seat seat in table.Seats)
			{
				if (seat.Status != SeatStatus.Seated) continue;
				if (!BotRoster.IsBot(seat.Player!.PlayerId)) continue;
				if (seat.Stack >= table.Definition.BigBlind) continue;

				state.ChipsAddedToBots += topUpTo - seat.Stack;
				table.TrySetStack(seat.Index, seat.Player.PlayerId, topUpTo);
				table.Publish(new PlayerSeatedEvent { TableId = table.Definition.TableId, Seat = seat.ToSnapshot() });
			}
		}

		private static int NextButton(List<HandParticipant> participants, int previousButton)
		{
			foreach (HandParticipant participant in participants)
			{
				if (participant.SeatIndex <= previousButton) continue;

				return participant.SeatIndex;
			}

			return participants[0].SeatIndex;
		}

		private Random RandomFor(HandTableState state) =>
			state.Random ??= _options.BotSeed.HasValue ? new Random(_options.BotSeed.Value) : new Random();
	}
}