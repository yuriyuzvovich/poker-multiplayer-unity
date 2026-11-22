using System;
using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.HandHistory.Models;

namespace Poker.Server.Features.HandHistory
{
	public sealed class HandHistoryDraft
	{
		private readonly List<DraftSeat> _seats = new();
		private readonly List<HandRecordAction> _actions = new();
		private string[] _board = Array.Empty<string>();
		private Street _street;
		private long _handNumber;

		public void Start(HandStartedEvent started, PokerTable table)
		{
			_handNumber = started.HandNumber;
			_street = Street.Preflop;
			_board = Array.Empty<string>();
			_actions.Clear();
			_seats.Clear();
			foreach (HandPlayerInfo player in started.Players)
			{
				string name = table.Seats[player.SeatIndex].Player?.Name ?? player.PlayerId;
				_seats.Add(new DraftSeat(player.SeatIndex, player.PlayerId, name, player.Stack + player.Committed));
			}
		}

		public void SetHoleCards(HoleCardsEvent cards)
		{
			if (cards.HandNumber != _handNumber) return;

			DraftSeat? seat = Find(cards.SeatIndex);
			if (seat == null) return;

			seat.HoleCards = cards.Cards;
		}

		public void AddAction(TurnActedEvent acted)
		{
			if (acted.HandNumber != _handNumber) return;

			DraftSeat? seat = Find(acted.SeatIndex);
			if (seat == null) return;

			_actions.Add(new HandRecordAction { Name = seat.Name, Street = _street, Action = acted.Action, Amount = acted.Action == PlayerAction.Raise ? acted.Committed : acted.Amount });
		}

		public void StartRound(RoundStartedEvent round)
		{
			if (round.HandNumber != _handNumber) return;

			_street = round.Street;
			_board = round.Board;
		}

		public void Reveal(ShowdownEvent showdown)
		{
			if (showdown.HandNumber != _handNumber) return;

			_board = showdown.Board;
			foreach (CardReveal reveal in showdown.Reveals)
			{
				DraftSeat? seat = Find(reveal.SeatIndex);
				if (seat == null) continue;

				seat.ShownCards = reveal.Cards;
				seat.Description = reveal.Description;
			}
		}

		public void Finish(HandEndedEvent ended, string tableName, DateTimeOffset now, HandHistoryBook book)
		{
			if (ended.HandNumber != _handNumber) return;

			foreach (PotAward award in ended.Awards)
			{
				DraftSeat? seat = Find(award.SeatIndex);
				if (seat == null) continue;

				seat.Won += award.Amount;
			}

			foreach (SeatStack stack in ended.Stacks)
			{
				DraftSeat? seat = Find(stack.SeatIndex);
				if (seat == null) continue;

				seat.EndStack = stack.Stack;
			}

			HandRecordAction[] actions = _actions.ToArray();
			foreach (DraftSeat viewer in _seats)
			{
				if (BotRoster.IsBot(viewer.PlayerId)) continue;

				book.Add(viewer.PlayerId, CreateRecord(viewer, tableName, now, actions));
			}

			_handNumber = 0;
		}

		private HandRecord CreateRecord(DraftSeat viewer, string tableName, DateTimeOffset now, HandRecordAction[] actions)
		{
			HandRecordPlayer[] players = new HandRecordPlayer[_seats.Count];
			for (int i = 0; i < _seats.Count; i++)
			{
				DraftSeat seat = _seats[i];
				bool isMe = seat == viewer;
				players[i] = new HandRecordPlayer
				{
					Name = seat.Name,
					IsMe = isMe,
					Cards = isMe ? seat.HoleCards : seat.ShownCards,
					Description = seat.Description,
					Won = seat.Won,
				};
			}

			return new HandRecord
			{
				TableName = tableName,
				HandNumber = _handNumber,
				EndedAtUnixMs = now.ToUnixTimeMilliseconds(),
				Board = _board,
				NetChips = viewer.EndStack - viewer.StartStack,
				Players = players,
				Actions = actions,
			};
		}

		private DraftSeat? Find(int seatIndex)
		{
			foreach (DraftSeat seat in _seats)
			{
				if (seat.SeatIndex != seatIndex) continue;

				return seat;
			}

			return null;
		}

		private sealed class DraftSeat
		{
			public DraftSeat(int seatIndex, string playerId, string name, long startStack)
			{
				SeatIndex = seatIndex;
				PlayerId = playerId;
				Name = name;
				StartStack = startStack;
				EndStack = startStack;
			}

			public int SeatIndex { get; }
			public string PlayerId { get; }
			public string Name { get; }
			public long StartStack { get; }
			public long EndStack { get; set; }
			public long Won { get; set; }
			public string[] HoleCards { get; set; } = Array.Empty<string>();
			public string[] ShownCards { get; set; } = Array.Empty<string>();
			public string Description { get; set; } = "";
		}
	}
}