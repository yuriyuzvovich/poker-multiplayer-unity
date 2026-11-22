using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Presentation;
using Poker.Client.Features.Table.Views;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Features.Hand.Presentation
{
	// Loads the hand layer with the table screen and forwards to it while it exists. It keeps no copy of what is
	// shown: when a view loads it raises Attached, and the presenter has HandScene replay its state into it.
	public sealed class HandLayer : LazyView<HandView>, IHandView, IDisposable
	{
		private readonly TableScreen _table;
		private bool _isVisible;

		public HandLayer(ViewLoader loader, TableScreen table) : base(loader, HandView.Address)
		{
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_table.Opened += OnTableOpened;
			_table.Closing += OnTableClosing;
		}

		public event Action Attached;

		public event Action<ActionButton> ActionClicked;

		public event Action<PreAction> PreActionClicked;

		public void Dispose()
		{
			_table.Opened -= OnTableOpened;
			_table.Closing -= OnTableClosing;
		}

		public void SetVisible(bool isVisible)
		{
			_isVisible = isVisible;
			Current?.SetVisible(isVisible);
		}

		public void SetTitle(string title) => Current?.SetTitle(title);

		public void SetPot(string pot) => Current?.SetPot(pot);

		public void SetStatus(string status) => Current?.SetStatus(status);

		public void SetTurnClock(int seatIndex, float remaining, string seconds) => Current?.SetTurnClock(seatIndex, remaining, seconds);

		public void SetBoard(IReadOnlyList<string> cards) => Current?.SetBoard(cards);

		public void SetSeats(IReadOnlyList<HandSeatLine> lines) => Current?.SetSeats(lines);

		public void ShowActions(ActionOptions options) => Current?.ShowActions(options);

		public void HideActions() => Current?.HideActions();

		public void ShowPreActions(PreAction selected, bool canCheck) => Current?.ShowPreActions(selected, canCheck);

		public void HidePreActions() => Current?.HidePreActions();

		public void HighlightCards(IReadOnlyList<string> codes) => Current?.HighlightCards(codes);

		public UniTask FlyChipsAsync(ChipRoute route, IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken) =>
			Current != null ? Current.FlyChipsAsync(route, seats, seconds, cancellationToken) : UniTask.CompletedTask;

		public UniTask DealCardsAsync(IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken) =>
			Current != null ? Current.DealCardsAsync(seats, seconds, cancellationToken) : UniTask.CompletedTask;

		public UniTask DealBoardCardAsync(int boardIndex, float seconds, CancellationToken cancellationToken) =>
			Current != null ? Current.DealBoardCardAsync(boardIndex, seconds, cancellationToken) : UniTask.CompletedTask;

		public UniTask CelebrateAsync(IReadOnlyList<SeatWin> wins, float seconds, CancellationToken cancellationToken) =>
			Current != null ? Current.CelebrateAsync(wins, seconds, cancellationToken) : UniTask.CompletedTask;

		public void StopFlights() => Current?.StopFlights();

		protected override void OnAttached(HandView view)
		{
			view.AttachTo(_table.Current);
			view.ActionClicked += OnActionClicked;
			view.PreActionClicked += OnPreActionClicked;
			view.SetVisible(_isVisible);
			Attached?.Invoke();
		}

		protected override void OnDetaching(HandView view)
		{
			view.ActionClicked -= OnActionClicked;
			view.PreActionClicked -= OnPreActionClicked;
			view.DetachFromTable();
		}

		private void OnTableOpened(TableView table) => Open(table.LayerSlot);

		private void OnTableClosing(TableView table) => Close();

		private void OnActionClicked(ActionButton button) => ActionClicked?.Invoke(button);

		private void OnPreActionClicked(PreAction preAction) => PreActionClicked?.Invoke(preAction);
	}
}