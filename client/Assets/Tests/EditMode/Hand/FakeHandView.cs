using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;

namespace Poker.Client.Tests.EditMode.Hand
{
	public sealed class FakeHandView : IHandView
	{
		public event Action Attached;

		public event Action<ActionButton> ActionClicked;

		public event Action<PreAction> PreActionClicked;

		public bool IsVisible { get; private set; }
		public string Title { get; private set; } = "";
		public string Pot { get; private set; } = "";
		public string Status { get; private set; } = "";
		public string Timer { get; private set; } = "";
		public int TimerSeat { get; private set; } = -1;
		public float TimerRemaining { get; private set; }
		public List<string> Board { get; private set; } = new();
		public List<int> BoardHistory { get; } = new();
		public List<HandSeatLine> Seats { get; private set; } = new();
		public bool AreActionsVisible { get; private set; }
		public ActionOptions Options { get; private set; }
		public int ShowActionsCount { get; private set; }
		public bool ArePreActionsVisible { get; private set; }
		public PreAction ChosenPreAction { get; private set; }
		public bool CanPreCheck { get; private set; }
		public List<string> Flights { get; } = new();
		public int StopFlightsCount { get; private set; }
		public bool HoldsFlights { get; set; }
		public List<string> Highlighted { get; private set; } = new();

		public void SetVisible(bool isVisible) => IsVisible = isVisible;

		public void SetTitle(string title) => Title = title;

		public void SetPot(string pot) => Pot = pot;

		public void SetStatus(string status) => Status = status;

		public void SetTurnClock(int seatIndex, float remaining, string seconds)
		{
			TimerSeat = seatIndex;
			TimerRemaining = remaining;
			Timer = seconds;
		}

		public void SetBoard(IReadOnlyList<string> cards)
		{
			Board = new List<string>(cards);
			BoardHistory.Add(cards.Count);
		}

		public void SetSeats(IReadOnlyList<HandSeatLine> lines) => Seats = new List<HandSeatLine>(lines);

		public void ShowActions(ActionOptions options)
		{
			Options = options;
			AreActionsVisible = true;
			ShowActionsCount++;
		}

		public void HideActions() => AreActionsVisible = false;

		public void ShowPreActions(PreAction selected, bool canCheck)
		{
			ArePreActionsVisible = true;
			ChosenPreAction = selected;
			CanPreCheck = canCheck;
		}

		public void HidePreActions() => ArePreActionsVisible = false;

		public void HighlightCards(IReadOnlyList<string> codes) => Highlighted = new List<string>(codes);

		public UniTask FlyChipsAsync(ChipRoute route, IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken) =>
			Fly(route + ":" + string.Join(",", seats), cancellationToken);

		public UniTask DealCardsAsync(IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken) =>
			Fly("Deal:" + string.Join(",", seats), cancellationToken);

		public UniTask DealBoardCardAsync(int boardIndex, float seconds, CancellationToken cancellationToken) =>
			Fly("Board:" + boardIndex, cancellationToken);

		public UniTask CelebrateAsync(IReadOnlyList<SeatWin> wins, float seconds, CancellationToken cancellationToken)
		{
			List<string> parts = new();
			foreach (SeatWin win in wins)
			{
				parts.Add(win.SeatIndex + "+" + win.Amount);
			}

			return Fly("Win:" + string.Join(",", parts), cancellationToken);
		}

		public void StopFlights() => StopFlightsCount++;

		public void Click(ActionButton button) => ActionClicked?.Invoke(button);

		public void Click(PreAction preAction) => PreActionClicked?.Invoke(preAction);

		// Acts like the table being closed and reopened: a blank view arrives and asks for the state again.
		public void Reattach()
		{
			Title = "";
			Pot = "";
			Status = "";
			Board = new List<string>();
			Seats = new List<HandSeatLine>();
			AreActionsVisible = false;
			ArePreActionsVisible = false;
			Highlighted = new List<string>();
			Attached?.Invoke();
		}

		private UniTask Fly(string flight, CancellationToken cancellationToken)
		{
			Flights.Add(flight);
			return HoldsFlights ? UniTask.Never(cancellationToken) : UniTask.CompletedTask;
		}

		public HandSeatLine SeatLine(int seatIndex)
		{
			foreach (HandSeatLine line in Seats)
			{
				if (line.SeatIndex != seatIndex) continue;

				return line;
			}

			return null;
		}
	}
}