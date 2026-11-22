using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views.Data;

namespace Poker.Client.Features.Hand.Views
{
	public interface IHandView
	{
		// Raised when a fresh view becomes available and needs the current state shown again.
		public event Action Attached;

		public event Action<ActionButton> ActionClicked;

		public event Action<PreAction> PreActionClicked;

		public void SetVisible(bool isVisible);

		public void SetTitle(string title);

		public void SetPot(string pot);

		public void SetStatus(string status);

		public void SetTurnClock(int seatIndex, float remaining, string seconds);

		public void SetBoard(IReadOnlyList<string> cards);

		public void SetSeats(IReadOnlyList<HandSeatLine> lines);

		public void ShowActions(ActionOptions options);

		public void HideActions();

		public void ShowPreActions(PreAction selected, bool canCheck);

		public void HidePreActions();

		public void HighlightCards(IReadOnlyList<string> codes);

		public UniTask FlyChipsAsync(ChipRoute route, IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken);

		public UniTask DealCardsAsync(IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken);

		public UniTask DealBoardCardAsync(int boardIndex, float seconds, CancellationToken cancellationToken);

		public UniTask CelebrateAsync(IReadOnlyList<SeatWin> wins, float seconds, CancellationToken cancellationToken);

		public void StopFlights();
	}
}