using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Seating.Views;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using VContainer.Unity;

namespace Poker.Client.Features.Seating.Application
{
	public sealed class SeatingFlow : IInitializable, IDisposable
	{
		private readonly IServerConnection _connection;
		private readonly IMessageBus _bus;
		private readonly PopupService _popups;
		private readonly IServerClock _clock;
		private readonly TableModel _table;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly List<IDisposable> _subscriptions = new();
		private BuyInPopup _openPopup;
		private int _reservedSeat = -1;

		public SeatingFlow(IServerConnection connection, IMessageBus bus, PopupService popups, IServerClock clock, TableModel table)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_table = table ?? throw new ArgumentNullException(nameof(table));
		}

		public void Initialize()
		{
			_subscriptions.Add(_bus.Subscribe<SitRequested>(OnSitRequested));
			_subscriptions.Add(_bus.Subscribe<StandUpRequested>(OnStandUpRequested));
			_subscriptions.Add(_bus.Subscribe<SessionRestarted>(OnSessionRestarted));
			_subscriptions.Add(_connection.Subscribe<SeatReleasedEvent>(OnSeatReleased));
		}

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void OnSitRequested(SitRequested request) => SitAsync(request).Forget();

		private void OnStandUpRequested(StandUpRequested request) => StandUpAsync(request.TableId).Forget();

		private async UniTaskVoid SitAsync(SitRequested request)
		{
			if (_openPopup != null) return;

			SeatReservationResponse reservation;
			try
			{
				ReserveSeatRequest reserve = new() { TableId = request.TableId, SeatIndex = request.SeatIndex };
				reservation = await _connection.RequestAsync<ReserveSeatRequest, SeatReservationResponse>(reserve, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				Notify("Cannot sit there: " + exception.Message);
				return;
			}

			_reservedSeat = reservation.SeatIndex;
			long secondsLeft = Math.Max(0, (reservation.ExpiresAtUnixMs - _clock.NowUnixMs) / 1000);
			BuyInPopup popup = await _popups.LoadAsync<BuyInPopup>(BuyInPopup.Address, _lifetime.Token);
			if (_reservedSeat < 0)
			{
				_popups.Discard(popup);
				return;
			}

			popup.Setup(reservation.MinBuyIn, reservation.MaxBuyIn, secondsLeft);
			_openPopup = popup;
			long amount = await _popups.ShowAsync(popup, _lifetime.Token);
			_openPopup = null;
			if (_reservedSeat < 0) return;

			if (amount == BuyInPopup.Cancelled)
			{
				_reservedSeat = -1;
				await StandUpAsync(request.TableId);
				return;
			}

			try
			{
				await _connection.RequestAsync<BuyInRequest, BuyInAcceptedResponse>(new BuyInRequest { TableId = request.TableId, Amount = amount }, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				Notify("Buy-in refused: " + exception.Message);
			}

			_reservedSeat = -1;
		}

		private async UniTask StandUpAsync(string tableId)
		{
			StandUpAcceptedResponse response;
			try
			{
				response = await _connection.RequestAsync<StandUpRequest, StandUpAcceptedResponse>(new StandUpRequest { TableId = tableId }, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				Notify("Cannot stand up: " + exception.Message);
				return;
			}

			if (!response.LeavesAfterHand) return;

			Notify("You leave the seat when this hand ends.");
		}

		private void OnSessionRestarted(SessionRestarted restarted)
		{
			_reservedSeat = -1;
			if (_openPopup != null) _openPopup.Close(BuyInPopup.Cancelled);
		}

		private void OnSeatReleased(SeatReleasedEvent released)
		{
			if (released.TableId != _table.TableId) return;
			if (released.SeatIndex != _reservedSeat) return;

			_reservedSeat = -1;
			if (released.Reason == SeatReleaseReason.ReservationExpired) Notify("The seat reservation expired.");
			if (_openPopup != null) _openPopup.Close(BuyInPopup.Cancelled);
		}

		private void Notify(string text) => _bus.Publish(new TableStatusMessage(text));
	}
}