using System;
using System.Collections.Generic;
using Poker.Client.Features.Table.Views.Seats;

namespace Poker.Client.Features.Table.Views
{
	public interface ITableView
	{
		public event Action BackClicked;
		public event Action<int> SeatActionClicked;

		public void SetVisible(bool isVisible);

		public void SetTitle(string title);

		public void SetStatus(string status);

		public void ShowSeats(IReadOnlyList<SeatRow> rows, int heroSeat);
	}
}