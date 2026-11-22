using System;
using System.Collections.Generic;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Features.Hand.Domain
{
	public sealed class HandModel
	{
		private readonly List<string> _board = new();
		private HandPlayerInfo[] _players = Array.Empty<HandPlayerInfo>();
		private string[] _myCards = Array.Empty<string>();

		public long HandNumber { get; private set; }
		public bool IsRunning { get; private set; }
		public int MySeat { get; private set; } = -1;
		public TurnStartedEvent CurrentTurn { get; private set; }
		public PreAction PreAction { get; private set; }

		public IReadOnlyList<string> Board => _board;
		public IReadOnlyList<string> MyCards => _myCards;

		public bool IsMyTurn => CurrentTurn != null && CurrentTurn.SeatIndex == MySeat;

		public bool IsInputOpen(long nowUnixMs) => IsMyTurn && nowUnixMs < CurrentTurn.DeadlineUnixMs - CurrentTurn.LeewayMs;

		public bool Accepts(long handNumber) => IsRunning && handNumber == HandNumber;

		public void Start(long handNumber, HandPlayerInfo[] players, int mySeat)
		{
			HandNumber = handNumber;
			IsRunning = true;
			MySeat = mySeat;
			CurrentTurn = null;
			PreAction = PreAction.None;
			_players = players;
			_myCards = Array.Empty<string>();
			_board.Clear();
		}

		public void Load(HandSnapshotEvent snapshot, int mySeat)
		{
			Start(snapshot.HandNumber, snapshot.Players, mySeat);
			_board.AddRange(snapshot.Board);
			_myCards = snapshot.MyCards;
			CurrentTurn = snapshot.CurrentTurn;
		}

		public void SetMyCards(string[] cards) => _myCards = cards;

		public void StartTurn(TurnStartedEvent turn) => CurrentTurn = turn;

		public void TogglePreAction(PreAction preAction) => PreAction = PreAction != preAction ? preAction : PreAction.None;

		public PlayerAction? TakePreAction(TurnStartedEvent turn)
		{
			PreAction chosen = PreAction;
			PreAction = PreAction.None;
			return chosen switch
			{
				PreAction.CheckFold => turn.CanCheck ? PlayerAction.Check : PlayerAction.Fold,
				PreAction.Check when turn.CanCheck => PlayerAction.Check,
				PreAction.CallAny => turn.CanCheck ? PlayerAction.Check : PlayerAction.Call,
				_ => null,
			};
		}

		public void EndTurn(long turnId)
		{
			if (CurrentTurn == null) return;
			if (CurrentTurn.TurnId != turnId) return;

			CurrentTurn = null;
		}

		public void SetBoard(string[] board)
		{
			_board.Clear();
			_board.AddRange(board);
		}

		public string PlayerAt(int seatIndex)
		{
			foreach (HandPlayerInfo player in _players)
			{
				if (player.SeatIndex != seatIndex) continue;

				return player.PlayerId;
			}

			return "";
		}

		public void End()
		{
			IsRunning = false;
			CurrentTurn = null;
			PreAction = PreAction.None;
		}

		public void Clear()
		{
			HandNumber = 0;
			MySeat = -1;
			End();
			_players = Array.Empty<HandPlayerInfo>();
			_myCards = Array.Empty<string>();
			_board.Clear();
		}
	}
}