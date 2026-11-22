using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Formatting;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using VContainer.Unity;

namespace Poker.Client.Features.Table.Presentation
{
	public sealed class TablePresenter : IInitializable, IDisposable
	{
		private readonly IServerConnection _connection;
		private readonly IMessageBus _bus;
		private readonly PlayerSession _session;
		private readonly TableModel _model;
		private readonly ITableView _view;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly List<IDisposable> _subscriptions = new();
		private readonly List<SeatRow> _rows = new();

		public TablePresenter(IServerConnection connection, IMessageBus bus, PlayerSession session, TableModel model, ITableView view)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_view = view ?? throw new ArgumentNullException(nameof(view));
		}

		public void Initialize()
		{
			_view.BackClicked += OnBackClicked;
			_view.SeatActionClicked += OnSeatActionClicked;
			_model.Changed += Render;
			_subscriptions.Add(_bus.Subscribe<JoinTableRequested>(OnJoinRequested));
			_subscriptions.Add(_bus.Subscribe<TableStatusMessage>(OnStatusMessage));
			_subscriptions.Add(_bus.Subscribe<SessionRestarted>(OnSessionRestarted));
			_subscriptions.Add(_connection.Subscribe<SeatReservedEvent>(OnSeatReserved));
			_subscriptions.Add(_connection.Subscribe<PlayerSeatedEvent>(OnPlayerSeated));
			_subscriptions.Add(_connection.Subscribe<SeatReleasedEvent>(OnSeatReleased));
			_view.SetVisible(false);
		}

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
			_view.BackClicked -= OnBackClicked;
			_view.SeatActionClicked -= OnSeatActionClicked;
			_model.Changed -= Render;
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void OnJoinRequested(JoinTableRequested request) => JoinAsync(request.TableId).Forget();

		private async UniTaskVoid JoinAsync(string tableId)
		{
			TableSnapshot snapshot;
			try
			{
				snapshot = await _connection.RequestAsync<JoinTableRequest, TableSnapshot>(new JoinTableRequest { TableId = tableId }, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				_bus.Publish(new TableJoinFailed(tableId, exception.Message));
				return;
			}

			_model.Load(snapshot);
			_view.SetTitle(snapshot.Name);
			_view.SetStatus("Blinds " + snapshot.SmallBlind + "/" + snapshot.BigBlind + ", buy-in " + snapshot.MinBuyIn + " to " + snapshot.MaxBuyIn);
			_view.SetVisible(true);
			_bus.Publish(new TableJoined(snapshot.TableId));
		}

		private void OnSessionRestarted(SessionRestarted restarted)
		{
			if (!_model.IsJoined) return;

			JoinAsync(_model.TableId).Forget();
		}

		private void OnSeatReserved(SeatReservedEvent reserved) => _model.ApplySeat(reserved.TableId, reserved.Seat);

		private void OnPlayerSeated(PlayerSeatedEvent seated) => _model.ApplySeat(seated.TableId, seated.Seat);

		private void OnSeatReleased(SeatReleasedEvent released) => _model.ReleaseSeat(released.TableId, released.SeatIndex);

		private void OnStatusMessage(TableStatusMessage message) => _view.SetStatus(message.Text);

		private void Render()
		{
			_rows.Clear();
			int mySeat = _model.FindSeatOf(_session.PlayerId);
			IReadOnlyList<SeatSnapshot> seats = _model.Seats;
			for (int i = 0; i < seats.Count; i++)
			{
				SeatSnapshot seat = seats[i];
				_rows.Add(new SeatRow(seat.PlayerName, FormatStack(seat), BadgeFor(seat), ActionFor(seat, mySeat)));
			}

			_view.ShowSeats(_rows, mySeat);
		}

		private void OnSeatActionClicked(int seatIndex)
		{
			if (!_model.IsJoined) return;

			if (seatIndex == _model.FindSeatOf(_session.PlayerId)) _bus.Publish(new StandUpRequested(_model.TableId)); else _bus.Publish(new SitRequested(_model.TableId, seatIndex));
		}

		private void OnBackClicked()
		{
			_model.Clear();
			_view.SetVisible(false);
			_bus.Publish(new TableLeft());
		}

		private static string ActionFor(SeatSnapshot seat, int mySeat)
		{
			if (seat.Index == mySeat) return "Stand up";
			if (seat.Status != SeatStatus.Empty) return "";

			return mySeat < 0 ? "Sit" : "";
		}

		private static string FormatStack(SeatSnapshot seat) => seat.Status == SeatStatus.Seated ? ChipText.Format(seat.Stack) : "";

		private static string BadgeFor(SeatSnapshot seat)
		{
			if (seat.Status == SeatStatus.Empty) return "empty";
			if (seat.Status == SeatStatus.Reserved) return "reserved";

			return seat.IsSittingOut ? "away" : "";
		}
	}
}