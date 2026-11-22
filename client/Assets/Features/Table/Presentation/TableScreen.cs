using System;
using System.Collections.Generic;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Features.Table.Presentation
{
	public sealed class TableScreen : LazyView<TableView>, ITableView, IDisposable
	{
		private readonly UiLayers _layers;
		private readonly TableToolbar _toolbar;
		private IReadOnlyList<SeatRow> _rows = Array.Empty<SeatRow>();
		private int _heroSeat = -1;
		private string _title = "";
		private string _status = "";

		public TableScreen(ViewLoader loader, UiLayers layers, TableToolbar toolbar) : base(loader, TableView.Address)
		{
			_layers = layers ?? throw new ArgumentNullException(nameof(layers));
			_toolbar = toolbar ?? throw new ArgumentNullException(nameof(toolbar));
			_toolbar.Added += OnToolbarButtonAdded;
		}

		public event Action BackClicked;
		public event Action<int> SeatActionClicked;

		public void Dispose() => _toolbar.Added -= OnToolbarButtonAdded;

		public void SetVisible(bool isVisible)
		{
			if (isVisible) Open(_layers.Screens); else Close();
		}

		public void SetTitle(string title)
		{
			_title = title;
			Current?.SetTitle(title);
		}

		public void SetStatus(string status)
		{
			_status = status;
			Current?.SetStatus(status);
		}

		public void ShowSeats(IReadOnlyList<SeatRow> rows, int heroSeat)
		{
			_rows = rows;
			_heroSeat = heroSeat;
			Current?.ShowSeats(rows, heroSeat);
		}

		protected override void OnAttached(TableView view)
		{
			view.BackClicked += OnBackClicked;
			view.SeatActionClicked += OnSeatActionClicked;
			view.SetTitle(_title);
			view.SetStatus(_status);
			view.ShowSeats(_rows, _heroSeat);
			for (int i = 0; i < _toolbar.Buttons.Count; i++)
			{
				TableToolbarButton button = _toolbar.Buttons[i];
				view.AddToolbarButton(button.Name, button.Label, button.Click);
			}
		}

		protected override void OnDetaching(TableView view)
		{
			view.BackClicked -= OnBackClicked;
			view.SeatActionClicked -= OnSeatActionClicked;
		}

		private void OnToolbarButtonAdded(TableToolbarButton button) => Current?.AddToolbarButton(button.Name, button.Label, button.Click);

		private void OnBackClicked() => BackClicked?.Invoke();

		private void OnSeatActionClicked(int seatIndex) => SeatActionClicked?.Invoke(seatIndex);
	}
}