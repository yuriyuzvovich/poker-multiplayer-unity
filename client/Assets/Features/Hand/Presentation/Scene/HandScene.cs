using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Views.Formatting;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Features.Hand.Presentation.Scene
{
	// The hand at animation time: presentation commands move it forward one event at a time, behind the network.
	// It is the only holder of what the hand layer shows; a view loaded later is filled through Replay.
	public sealed class HandScene
	{
		private static readonly string[] FaceDown = { "", "" };

		private readonly IHandView _view;
		private readonly TableModel _table;
		private readonly HandModel _hand;
		private readonly TurnClock _clock = new();
		private readonly HandReader _reader = new();
		private readonly HandShownState _shown = new();
		private readonly List<HandSeatLine> _lines = new();
		private readonly List<string> _board = new();
		private readonly List<int> _flightSeats = new();
		private readonly List<SeatWin> _wins = new();
		private readonly List<string> _winningCards = new();
		private readonly StringBuilder _text = new();
		private long _handNumber;
		private bool _hadShowdown;
		private int _mySeat = -1;
		private bool _isPlaying;

		public HandScene(IHandView view, TableModel table, HandModel hand)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_hand = hand ?? throw new ArgumentNullException(nameof(hand));
		}

		public void Reset()
		{
			_view.StopFlights();
			_handNumber = 0;
			_mySeat = -1;
			_isPlaying = false;
			ClearHighlight();
			ClearTurn();
			_lines.Clear();
			_board.Clear();
			ShowTitle("Waiting for a hand");
			ShowPot("");
			ShowStatus("");
			_view.SetBoard(_board);
			_view.SetSeats(_lines);
			HideActions();
			HidePreActions();
		}

		// Fills a freshly loaded view with everything the scene shows, in the order a live hand would have set it.
		public void Replay()
		{
			_view.SetTitle(_shown.Title);
			_view.SetPot(_shown.Pot);
			_view.SetStatus(_shown.Status);
			_view.SetBoard(_board);
			_view.SetSeats(_lines);
			_view.HighlightCards(_winningCards);
			_view.SetTurnClock(_clock.IsRunning ? _clock.Turn.SeatIndex : -1, _clock.Remaining, _clock.SecondsText);
			if (_shown.Actions is { } actions) _view.ShowActions(actions); else _view.HideActions();
			if (_shown.PreAction is { } preAction) _view.ShowPreActions(preAction, _shown.CanPreCheck); else _view.HidePreActions();
		}

		public void StartHand(HandStartedEvent started, int mySeat)
		{
			_handNumber = started.HandNumber;
			_mySeat = mySeat;
			_isPlaying = true;
			ClearHighlight();
			ClearTurn();
			_lines.Clear();
			long pot = 0;
			foreach (HandPlayerInfo player in started.Players)
			{
				HandSeatLine line = CreateLine(player, started.ButtonSeat, mySeat);
				if (player.SeatIndex == started.SmallBlindSeat) line.Action = "small blind";
				if (player.SeatIndex == started.BigBlindSeat) line.Action = "big blind";
				pot += player.Committed;
				_lines.Add(line);
			}

			_board.Clear();
			ShowTitle(HandSceneText.Title(_handNumber, "preflop"));
			ShowPot("Pot " + ChipText.Format(pot));
			ShowStatus("");
			_view.SetBoard(_board);
			HideActions();
			RefreshPreActions();
			_view.SetSeats(_lines);
		}

		public void LoadSnapshot(HandSnapshotEvent snapshot, int mySeat)
		{
			_handNumber = snapshot.HandNumber;
			_mySeat = mySeat;
			_isPlaying = true;
			ClearHighlight();
			ClearTurn();
			_lines.Clear();
			foreach (HandPlayerInfo player in snapshot.Players)
			{
				HandSeatLine line = CreateLine(player, snapshot.ButtonSeat, mySeat);
				if (line.IsMine) line.Cards = snapshot.MyCards;
				_lines.Add(line);
			}

			_board.Clear();
			_board.AddRange(snapshot.Board);
			ShowTitle(HandSceneText.Title(_handNumber, HandText.StreetName(snapshot.Street)));
			ShowPot(HandSceneText.Pots(snapshot.Pots, _text));
			ShowStatus("");
			_view.SetBoard(_board);
			DescribeMyHand();
			TurnStartedEvent turn = snapshot.CurrentTurn;
			if (turn != null)
			{
				StartTurn(turn, turn.SeatIndex == mySeat);
				return;
			}

			HideActions();
			RefreshPreActions();
			_view.SetSeats(_lines);
		}

		public void ShowHoleCards(HoleCardsEvent cards)
		{
			HandSeatLine line = FindLine(cards.SeatIndex);
			if (line == null) return;

			line.Cards = cards.Cards;
			DescribeMyHand();
			_view.SetSeats(_lines);
		}

		public void StartTurn(TurnStartedEvent turn, bool isMine)
		{
			_clock.Start(turn, isMine);
			foreach (HandSeatLine line in _lines)
			{
				line.IsActing = line.SeatIndex == turn.SeatIndex;
			}

			ShowStatus(isMine ? "Your turn" : NameOf(turn.SeatIndex) + " to act");
			if (isMine) ShowActions(turn); else HideActions();

			RefreshPreActions();
			_view.SetSeats(_lines);
		}

		public void ShowActions(TurnStartedEvent turn)
		{
			ActionOptions options = new(turn.ToCall, turn.CanCheck, turn.CanRaise, turn.MinRaiseTo, turn.MaxRaiseTo);
			_shown.Actions = options;
			_view.ShowActions(options);
		}

		public void HideActions()
		{
			_shown.Actions = null;
			_view.HideActions();
		}

		public void RefreshPreActions()
		{
			HandSeatLine mine = FindLine(_mySeat);
			if (!CanPreAct(mine))
			{
				HidePreActions();
				return;
			}

			_shown.PreAction = _hand.PreAction;
			_shown.CanPreCheck = mine.Bet >= HighestBet();
			_view.ShowPreActions(_hand.PreAction, _shown.CanPreCheck);
		}

		public void ApplyActed(TurnActedEvent acted)
		{
			HandSeatLine line = FindLine(acted.SeatIndex);
			if (line == null) return;

			if (_clock.Turn != null && _clock.Turn.TurnId == acted.TurnId) ClearTurn();

			line.IsActing = false;
			line.Bet = acted.Committed;
			line.Stack = acted.Stack;
			line.Action = HandSceneText.Action(acted);
			if (acted.Action == PlayerAction.Fold)
			{
				line.IsFolded = true;
				line.Cards = Array.Empty<string>();
				line.Description = "";
			}

			if (line.IsMine) HideActions();

			RefreshPreActions();
			ShowPot("Pot " + ChipText.Format(acted.PotTotal));
			ShowStatus("");
			_view.SetSeats(_lines);
		}

		public void EndRound(RoundEndedEvent ended)
		{
			foreach (HandSeatLine line in _lines)
			{
				line.Bet = 0;
				line.IsActing = false;
				if (line.IsFolded) continue;

				line.Action = "";
			}

			ShowPot(HandSceneText.Pots(ended.Pots, _text));
			RefreshPreActions();
			_view.SetSeats(_lines);
		}

		public void ShowBoard(Street street, string[] board, int visibleCount)
		{
			_board.Clear();
			for (int i = 0; i < visibleCount && i < board.Length; i++)
			{
				_board.Add(board[i]);
			}

			ShowTitle(HandSceneText.Title(_handNumber, HandText.StreetName(street)));
			_view.SetBoard(_board);
			DescribeMyHand();
			_view.SetSeats(_lines);
		}

		public void ShowShowdown(ShowdownEvent showdown)
		{
			foreach (CardReveal reveal in showdown.Reveals)
			{
				HandSeatLine line = FindLine(reveal.SeatIndex);
				if (line == null) continue;

				line.Cards = reveal.Cards;
				line.Description = reveal.Description;
				line.IsActing = false;
			}

			_hadShowdown = true;
			_board.Clear();
			_board.AddRange(showdown.Board);
			ShowTitle(HandSceneText.Title(_handNumber, "showdown"));
			_view.SetBoard(_board);
			_view.SetSeats(_lines);
		}

		public void EndHand(HandEndedEvent ended)
		{
			_isPlaying = false;
			ClearTurn();
			foreach (SeatStack stack in ended.Stacks)
			{
				HandSeatLine line = FindLine(stack.SeatIndex);
				if (line == null) continue;

				line.Stack = stack.Stack;
				line.Bet = 0;
				line.IsActing = false;
			}

			ShowTitle(HandSceneText.Title(_handNumber, "finished"));
			ShowPot("");
			ShowStatus(HandSceneText.Winners(_lines, ended.Awards, _text));
			HideActions();
			HidePreActions();
			_view.SetSeats(_lines);
			if (_hadShowdown) HighlightWinners(ended);
		}

		public UniTask DealAsync(HandStartedEvent started, float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			foreach (HandSeatLine line in _lines)
			{
				line.Cards = Array.Empty<string>();
				line.Description = "";
			}

			_view.SetSeats(_lines);
			_flightSeats.Clear();
			foreach (HandPlayerInfo player in started.Players)
			{
				if (player.IsFolded) continue;

				_flightSeats.Add(player.SeatIndex);
			}

			return _view.DealCardsAsync(_flightSeats, seconds, cancellationToken);
		}

		public UniTask FlyBetAsync(TurnActedEvent acted, float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			HandSeatLine line = FindLine(acted.SeatIndex);
			if (line == null) return UniTask.CompletedTask;
			if (acted.Committed <= line.Bet) return UniTask.CompletedTask;

			_flightSeats.Clear();
			_flightSeats.Add(acted.SeatIndex);
			return _view.FlyChipsAsync(ChipRoute.AvatarToBet, _flightSeats, seconds, cancellationToken);
		}

		public UniTask CollectBetsAsync(float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			_flightSeats.Clear();
			foreach (HandSeatLine line in _lines)
			{
				if (line.Bet <= 0) continue;

				_flightSeats.Add(line.SeatIndex);
			}

			if (_flightSeats.Count == 0) return UniTask.CompletedTask;

			return _view.FlyChipsAsync(ChipRoute.BetToPot, _flightSeats, seconds, cancellationToken);
		}

		public UniTask PayWinnersAsync(HandEndedEvent ended, float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			CollectWins(ended);
			if (_wins.Count == 0) return UniTask.CompletedTask;

			_flightSeats.Clear();
			foreach (SeatWin win in _wins)
			{
				_flightSeats.Add(win.SeatIndex);
			}

			return _view.FlyChipsAsync(ChipRoute.PotToAvatar, _flightSeats, seconds, cancellationToken);
		}

		public UniTask CelebrateAsync(HandEndedEvent ended, float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			CollectWins(ended);
			if (_wins.Count == 0) return UniTask.CompletedTask;

			return _view.CelebrateAsync(_wins, seconds, cancellationToken);
		}

		public UniTask DealBoardCardAsync(int boardIndex, float seconds, CancellationToken cancellationToken)
		{
			if (seconds <= 0f) return UniTask.CompletedTask;

			return _view.DealBoardCardAsync(boardIndex, seconds, cancellationToken);
		}

		public void StopFlights() => _view.StopFlights();

		public void UpdateTimer(long nowUnixMs)
		{
			if (!_clock.IsRunning) return;

			bool isTimeUp = _clock.Advance(nowUnixMs);
			if (isTimeUp && _clock.IsMine)
			{
				HideActions();
				ShowStatus("Time is up");
			}

			_view.SetTurnClock(_clock.Turn.SeatIndex, _clock.Remaining, _clock.SecondsText);
		}

		private void ClearTurn()
		{
			_clock.Clear();
			_view.SetTurnClock(-1, 0f, "");
		}

		private void ShowTitle(string title)
		{
			_shown.Title = title;
			_view.SetTitle(title);
		}

		private void ShowPot(string pot)
		{
			_shown.Pot = pot;
			_view.SetPot(pot);
		}

		private void ShowStatus(string status)
		{
			_shown.Status = status;
			_view.SetStatus(status);
		}

		private void HidePreActions()
		{
			_shown.PreAction = null;
			_view.HidePreActions();
		}

		private HandSeatLine CreateLine(HandPlayerInfo player, int buttonSeat, int mySeat) => new HandSeatLine
		{
			SeatIndex = player.SeatIndex,
			Name = NameOf(player.SeatIndex),
			IsMine = player.SeatIndex == mySeat,
			IsButton = player.SeatIndex == buttonSeat,
			IsFolded = player.IsFolded,
			Stack = player.Stack,
			Bet = player.Committed,
			Action = player.IsFolded ? "fold" : "",
			Cards = player.IsFolded ? Array.Empty<string>() : FaceDown,
		};

		private void CollectWins(HandEndedEvent ended)
		{
			_wins.Clear();
			foreach (PotAward award in ended.Awards)
			{
				if (award.Amount <= 0) continue;

				int index = IndexOfWin(award.SeatIndex);
				if (index >= 0)
				{
					_wins[index] = new SeatWin(award.SeatIndex, _wins[index].Amount + award.Amount);
					continue;
				}

				_wins.Add(new SeatWin(award.SeatIndex, award.Amount));
			}
		}

		private int IndexOfWin(int seatIndex)
		{
			for (int i = 0; i < _wins.Count; i++)
			{
				if (_wins[i].SeatIndex != seatIndex) continue;

				return i;
			}

			return -1;
		}

		private bool CanPreAct(HandSeatLine mine)
		{
			if (!_isPlaying) return false;
			if (_clock.IsMine) return false;
			if (mine == null) return false;
			if (mine.IsFolded) return false;

			return mine.Stack > 0;
		}

		private long HighestBet()
		{
			long highest = 0;
			foreach (HandSeatLine line in _lines)
			{
				highest = Math.Max(highest, line.Bet);
			}

			return highest;
		}

		private HandSeatLine FindLine(int seatIndex)
		{
			foreach (HandSeatLine line in _lines)
			{
				if (line.SeatIndex != seatIndex) continue;

				return line;
			}

			return null;
		}

		private void DescribeMyHand()
		{
			foreach (HandSeatLine line in _lines)
			{
				if (!line.IsMine) continue;
				if (line.IsFolded) continue;

				line.Description = _reader.Describe(line.Cards, _board);
			}
		}

		private void HighlightWinners(HandEndedEvent ended)
		{
			CollectWins(ended);
			_winningCards.Clear();
			foreach (SeatWin win in _wins)
			{
				HandSeatLine line = FindLine(win.SeatIndex);
				if (line == null) continue;

				_reader.AddBestFive(line.Cards, _board, _winningCards);
			}

			_view.HighlightCards(_winningCards);
		}

		private void ClearHighlight()
		{
			_hadShowdown = false;
			_winningCards.Clear();
			_view.HighlightCards(_winningCards);
		}

		private string NameOf(int seatIndex)
		{
			string name = _table.NameAt(seatIndex);
			return name.Length > 0 ? name : "Seat " + (seatIndex + 1);
		}
	}
}