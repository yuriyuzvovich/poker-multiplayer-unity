using System;
using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;
using Poker.Shared.Features.Hand;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Rules.Cards;
using Poker.Shared.Rules.Hands;
using Poker.Shared.Rules.Pots;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed class PokerHand
	{
		private readonly string _tableId;
		private readonly HandSettings _settings;
		private readonly Deck _deck;
		private readonly List<HandPlayer> _players = new();
		private readonly List<Card> _board = new(5);
		private readonly List<TableEvent> _events = new();
		private readonly int _buttonIndex;
		private int _actorIndex = -1;
		private long _currentBet;
		private long _minRaise;
		private int _turnSequence;

		private PokerHand(string tableId, long handNumber, IReadOnlyList<HandParticipant> participants, int buttonSeat, HandSettings settings, Deck deck)
		{
			_tableId = tableId;
			HandNumber = handNumber;
			_settings = settings;
			_deck = deck;
			for (int i = 0; i < participants.Count; i++)
			{
				HandParticipant participant = participants[i];
				_players.Add(new HandPlayer(participant));
				if (participant.SeatIndex == buttonSeat) _buttonIndex = i;
			}
		}

		public long HandNumber { get; }
		public Street Street { get; private set; }
		public bool IsComplete { get; private set; }
		public DateTimeOffset Deadline { get; private set; }
		public int ButtonSeat => _players[_buttonIndex].SeatIndex;
		public long TurnId => HandNumber * 1000 + _turnSequence;
		public IReadOnlyList<HandPlayer> Players => _players;
		public IReadOnlyList<Card> Board => _board;
		public HandPlayer? CurrentActor => _actorIndex >= 0 ? _players[_actorIndex] : null;

		public long PotTotal
		{
			get
			{
				long total = 0;
				foreach (HandPlayer player in _players)
				{
					total += player.TotalCommitted;
				}

				return total;
			}
		}

		public static PokerHand Start(string tableId, long handNumber, IReadOnlyList<HandParticipant> participants, int buttonSeat, HandSettings settings, Deck deck, DateTimeOffset now)
		{
			if (participants.Count < 2) throw new ArgumentException("A hand needs at least two players.", nameof(participants));

			PokerHand hand = new(tableId, handNumber, participants, buttonSeat, settings, deck);
			hand.PostBlindsAndDeal(now);
			return hand;
		}

		public void TakeEvents(List<TableEvent> destination)
		{
			destination.AddRange(_events);
			_events.Clear();
		}

		public string Act(string playerId, long turnId, PlayerAction action, long amount, DateTimeOffset now) =>
			Act(playerId, turnId, action, amount, now, ActionReason.Player);

		public void TimeOut(DateTimeOffset now)
		{
			if (now < Deadline) return;

			TimeOutNow(now);
		}

		public void TimeOutNow(DateTimeOffset now)
		{
			HandPlayer? actor = CurrentActor;
			if (actor == null) return;

			PlayerAction action = actor.Committed == _currentBet ? PlayerAction.Check : PlayerAction.Fold;
			Act(actor.PlayerId, TurnId, action, 0, now, ActionReason.Timeout);
		}

		public void ForceFold(string playerId, DateTimeOffset now)
		{
			if (IsComplete) return;

			HandPlayer? player = Find(playerId);
			if (player == null || player.IsFolded) return;

			if (player == CurrentActor)
			{
				Act(playerId, TurnId, PlayerAction.Fold, 0, now, ActionReason.LeftTable);
				return;
			}

			player.IsFolded = true;
			Emit(CreateActed(player, PlayerAction.Fold, 0, ActionReason.LeftTable));
			if (CountNotFolded() == 1) FinishUncontested();
		}

		public TurnStartedEvent? CreateCurrentTurn()
		{
			HandPlayer? actor = CurrentActor;
			if (actor == null) return null;

			long toCall = _currentBet - actor.Committed;
			bool canRaise = actor.Stack > toCall;
			long maxRaiseTo = actor.Committed + actor.Stack;
			long minRaiseTo = Math.Min(_currentBet + _minRaise, maxRaiseTo);
			return new TurnStartedEvent
			{
				TableId = _tableId,
				HandNumber = HandNumber,
				TurnId = TurnId,
				SeatIndex = actor.SeatIndex,
				DeadlineUnixMs = Deadline.ToUnixTimeMilliseconds(),
				LeewayMs = (long)_settings.TurnLeeway.TotalMilliseconds,
				ToCall = Math.Min(toCall, actor.Stack),
				CanCheck = toCall == 0,
				CanRaise = canRaise,
				MinRaiseTo = canRaise ? minRaiseTo : 0,
				MaxRaiseTo = canRaise ? maxRaiseTo : 0,
			};
		}

		public HandSnapshotEvent CreateSnapshot(string? viewerPlayerId)
		{
			HandPlayerInfo[] players = new HandPlayerInfo[_players.Count];
			string[] myCards = Array.Empty<string>();
			for (int i = 0; i < _players.Count; i++)
			{
				HandPlayer player = _players[i];
				players[i] = player.ToInfo();
				if (player.PlayerId != viewerPlayerId) continue;

				myCards = ToCodes(player.HoleCards);
			}

			return new HandSnapshotEvent
			{
				TableId = _tableId,
				HandNumber = HandNumber,
				Street = Street,
				ButtonSeat = ButtonSeat,
				Board = ToCodes(_board),
				Pots = ToPotInfos(CalculatePots()),
				Players = players,
				CurrentTurn = CreateCurrentTurn(),
				MyCards = myCards,
			};
		}

		private string Act(string playerId, long turnId, PlayerAction action, long amount, DateTimeOffset now, ActionReason reason)
		{
			if (IsComplete) return HandErrorCodes.NoHand;

			HandPlayer? actor = CurrentActor;
			if (actor == null || actor.PlayerId != playerId) return HandErrorCodes.NotYourTurn;
			if (turnId != TurnId) return HandErrorCodes.StaleTurn;

			long toCall = _currentBet - actor.Committed;
			long paid;
			switch (action)
			{
				case PlayerAction.Fold:
					actor.IsFolded = true;
					paid = 0;
					break;
				case PlayerAction.Check:
					if (toCall > 0) return HandErrorCodes.InvalidAction;

					paid = 0;
					break;
				case PlayerAction.Call:
					if (toCall == 0) return HandErrorCodes.InvalidAction;

					paid = actor.Commit(toCall);
					break;
				case PlayerAction.Raise:
					string error = ValidateRaise(actor, toCall, amount);
					if (error.Length > 0) return error;

					paid = Raise(actor, amount);
					break;
				default:
					return HandErrorCodes.InvalidAction;
			}

			actor.HasActed = true;
			Emit(CreateActed(actor, action, paid, reason));
			AfterAction(now);
			return "";
		}

		private string ValidateRaise(HandPlayer actor, long toCall, long raiseTo)
		{
			if (actor.Stack <= toCall) return HandErrorCodes.InvalidAction;

			long maxRaiseTo = actor.Committed + actor.Stack;
			long minRaiseTo = Math.Min(_currentBet + _minRaise, maxRaiseTo);
			if (raiseTo < minRaiseTo || raiseTo > maxRaiseTo) return HandErrorCodes.InvalidAmount;

			return "";
		}

		private long Raise(HandPlayer actor, long raiseTo)
		{
			long raiseSize = raiseTo - _currentBet;
			if (raiseSize >= _minRaise) _minRaise = raiseSize;

			_currentBet = raiseTo;
			foreach (HandPlayer player in _players)
			{
				if (player == actor) continue;

				player.HasActed = false;
			}

			return actor.Commit(raiseTo - actor.Committed);
		}

		private void PostBlindsAndDeal(DateTimeOffset now)
		{
			bool isHeadsUp = _players.Count == 2;
			int smallBlindIndex = isHeadsUp ? _buttonIndex : Next(_buttonIndex);
			int bigBlindIndex = Next(smallBlindIndex);
			HandPlayer smallBlind = _players[smallBlindIndex];
			HandPlayer bigBlind = _players[bigBlindIndex];
			smallBlind.Commit(_settings.SmallBlind);
			bigBlind.Commit(_settings.BigBlind);
			_currentBet = _settings.BigBlind;
			_minRaise = _settings.BigBlind;

			for (int round = 0; round < 2; round++)
			{
				for (int i = 0; i < _players.Count; i++)
				{
					_players[(smallBlindIndex + i) % _players.Count].Deal(_deck.Draw());
				}
			}

			HandPlayerInfo[] players = new HandPlayerInfo[_players.Count];
			for (int i = 0; i < _players.Count; i++)
			{
				players[i] = _players[i].ToInfo();
			}

			Emit(new HandStartedEvent
			{
				TableId = _tableId,
				HandNumber = HandNumber,
				ButtonSeat = ButtonSeat,
				SmallBlindSeat = smallBlind.SeatIndex,
				BigBlindSeat = bigBlind.SeatIndex,
				SmallBlind = _settings.SmallBlind,
				BigBlind = _settings.BigBlind,
				Players = players,
			});

			foreach (HandPlayer player in _players)
			{
				HoleCardsEvent holeCards = new() { TableId = _tableId, HandNumber = HandNumber, SeatIndex = player.SeatIndex, Cards = ToCodes(player.HoleCards) };
				_events.Add(new TableEvent(holeCards, player.PlayerId));
			}

			int firstToAct = isHeadsUp ? smallBlindIndex : Next(bigBlindIndex);
			_actorIndex = _players[firstToAct].CanAct ? firstToAct : NextToAct(firstToAct);
			if (_actorIndex < 0 || IsStreetComplete())
			{
				EndStreet(now);
				return;
			}

			StartTurn(now);
		}

		private void AfterAction(DateTimeOffset now)
		{
			if (CountNotFolded() == 1)
			{
				FinishUncontested();
				return;
			}

			if (IsStreetComplete())
			{
				EndStreet(now);
				return;
			}

			_actorIndex = NextToAct(_actorIndex);
			StartTurn(now);
		}

		private bool IsStreetComplete()
		{
			foreach (HandPlayer player in _players)
			{
				if (!player.CanAct) continue;
				if (player.Committed < _currentBet) return false;
				if (!player.HasActed) return false;
			}

			return true;
		}

		private void EndStreet(DateTimeOffset now)
		{
			_actorIndex = -1;
			Emit(new RoundEndedEvent { TableId = _tableId, HandNumber = HandNumber, Street = Street, Pots = ToPotInfos(CalculatePots()) });
			foreach (HandPlayer player in _players)
			{
				player.EndStreet();
			}

			_currentBet = 0;
			_minRaise = _settings.BigBlind;
			if (Street == Street.River)
			{
				Showdown();
				return;
			}

			if (CountCanAct() <= 1)
			{
				while (Street != Street.River)
				{
					DealNextStreet();
				}

				Showdown();
				return;
			}

			DealNextStreet();
			_actorIndex = NextToAct(_buttonIndex);
			StartTurn(now);
		}

		private void DealNextStreet()
		{
			int cards = Street == Street.Preflop ? 3 : 1;
			for (int i = 0; i < cards; i++)
			{
				_board.Add(_deck.Draw());
			}

			Street++;
			Emit(new RoundStartedEvent { TableId = _tableId, HandNumber = HandNumber, Street = Street, Board = ToCodes(_board) });
		}

		private void Showdown()
		{
			Street = Street.Showdown;
			List<CardReveal> reveals = new();
			Dictionary<int, HandValue> values = new();
			List<Card> cards = new(7);
			foreach (HandPlayer player in _players)
			{
				if (player.IsFolded) continue;

				cards.Clear();
				cards.AddRange(player.HoleCards);
				cards.AddRange(_board);
				HandValue value = HandEvaluator.Evaluate(cards);
				values.Add(player.SeatIndex, value);
				reveals.Add(new CardReveal { SeatIndex = player.SeatIndex, Cards = ToCodes(player.HoleCards), Category = value.Category, Description = value.ToString() });
			}

			Emit(new ShowdownEvent { TableId = _tableId, HandNumber = HandNumber, Board = ToCodes(_board), Reveals = reveals.ToArray() });

			List<PotAward> awards = new();
			List<Pot> pots = CalculatePots();
			for (int potIndex = 0; potIndex < pots.Count; potIndex++)
			{
				AwardPot(potIndex, pots[potIndex], values, awards);
			}

			Finish(awards);
		}

		private void AwardPot(int potIndex, Pot pot, Dictionary<int, HandValue> values, List<PotAward> awards)
		{
			HandValue best = new(-1);
			foreach (int seat in pot.EligibleSeats)
			{
				HandValue value = values[seat];
				if (value.Score <= best.Score) continue;

				best = value;
			}

			List<HandPlayer> winners = new();
			for (int i = 1; i <= _players.Count; i++)
			{
				HandPlayer player = _players[(_buttonIndex + i) % _players.Count];
				if (!values.TryGetValue(player.SeatIndex, out HandValue value)) continue;
				if (Array.IndexOf(pot.EligibleSeats, player.SeatIndex) < 0) continue;
				if (value.Score != best.Score) continue;

				winners.Add(player);
			}

			long share = pot.Amount / winners.Count;
			long oddChips = pot.Amount % winners.Count;
			for (int i = 0; i < winners.Count; i++)
			{
				long amount = share + (i < oddChips ? 1 : 0);
				winners[i].Win(amount);
				awards.Add(new PotAward { PotIndex = potIndex, SeatIndex = winners[i].SeatIndex, Amount = amount });
			}
		}

		private void FinishUncontested()
		{
			_actorIndex = -1;
			HandPlayer winner = _players[0];
			foreach (HandPlayer player in _players)
			{
				if (player.IsFolded) continue;

				winner = player;
				break;
			}

			long pot = PotTotal;
			winner.Win(pot);
			Finish(new List<PotAward> { new PotAward { PotIndex = 0, SeatIndex = winner.SeatIndex, Amount = pot } });
		}

		private void Finish(List<PotAward> awards)
		{
			IsComplete = true;
			_actorIndex = -1;
			SeatStack[] stacks = new SeatStack[_players.Count];
			for (int i = 0; i < _players.Count; i++)
			{
				stacks[i] = new SeatStack { SeatIndex = _players[i].SeatIndex, Stack = _players[i].Stack };
			}

			Emit(new HandEndedEvent
			{
				TableId = _tableId,
				HandNumber = HandNumber,
				Awards = awards.ToArray(),
				Stacks = stacks,
				NextHandInMs = (long)_settings.NextHandDelay.TotalMilliseconds,
			});
		}

		private void StartTurn(DateTimeOffset now)
		{
			_turnSequence++;
			Deadline = now + _settings.TurnTime + _settings.TurnLeeway;
			Emit(CreateCurrentTurn()!);
		}

		private TurnActedEvent CreateActed(HandPlayer player, PlayerAction action, long paid, ActionReason reason) => new()
		{
			TableId = _tableId,
			HandNumber = HandNumber,
			TurnId = TurnId,
			SeatIndex = player.SeatIndex,
			Action = action,
			Amount = paid,
			Committed = player.Committed,
			Stack = player.Stack,
			PotTotal = PotTotal,
			IsAllIn = player.IsAllIn,
			Reason = reason,
		};

		private List<Pot> CalculatePots()
		{
			PotContribution[] contributions = new PotContribution[_players.Count];
			for (int i = 0; i < _players.Count; i++)
			{
				HandPlayer player = _players[i];
				contributions[i] = new PotContribution(player.SeatIndex, player.TotalCommitted, player.IsFolded);
			}

			return PotCalculator.Calculate(contributions);
		}

		private int Next(int index) => (index + 1) % _players.Count;

		private int NextToAct(int fromIndex)
		{
			for (int step = 1; step <= _players.Count; step++)
			{
				int index = (fromIndex + step) % _players.Count;
				if (!_players[index].CanAct) continue;

				return index;
			}

			return -1;
		}

		private int CountNotFolded()
		{
			int count = 0;
			foreach (HandPlayer player in _players)
			{
				if (player.IsFolded) continue;

				count++;
			}

			return count;
		}

		private int CountCanAct()
		{
			int count = 0;
			foreach (HandPlayer player in _players)
			{
				if (!player.CanAct) continue;

				count++;
			}

			return count;
		}

		private HandPlayer? Find(string playerId)
		{
			foreach (HandPlayer player in _players)
			{
				if (player.PlayerId != playerId) continue;

				return player;
			}

			return null;
		}

		private void Emit(object message) => _events.Add(new TableEvent(message, null));

		private static string[] ToCodes(IReadOnlyList<Card> cards)
		{
			string[] codes = new string[cards.Count];
			for (int i = 0; i < cards.Count; i++)
			{
				codes[i] = cards[i].ToString();
			}

			return codes;
		}

		private static PotInfo[] ToPotInfos(List<Pot> pots)
		{
			PotInfo[] infos = new PotInfo[pots.Count];
			for (int i = 0; i < pots.Count; i++)
			{
				infos[i] = new PotInfo { Amount = pots[i].Amount, EligibleSeats = pots[i].EligibleSeats };
			}

			return infos;
		}
	}
}