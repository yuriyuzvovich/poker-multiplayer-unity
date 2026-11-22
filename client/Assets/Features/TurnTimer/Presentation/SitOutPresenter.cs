using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Features.TurnTimer.Views;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Features.TurnTimer.Events;
using Poker.Shared.Features.TurnTimer.Requests;
using VContainer.Unity;

namespace Poker.Client.Features.TurnTimer.Presentation
{
	public sealed class SitOutPresenter : IInitializable, IDisposable
	{
		private readonly IServerConnection _connection;
		private readonly IMessageBus _bus;
		private readonly PlayerSession _session;
		private readonly TableModel _table;
		private readonly PresentationQueue _queue;
		private readonly IAwayView _view;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly List<IDisposable> _subscriptions = new();
		private bool _isReturning;

		public SitOutPresenter(IServerConnection connection, IMessageBus bus, PlayerSession session, TableModel table, PresentationQueue queue, IAwayView view)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_view = view ?? throw new ArgumentNullException(nameof(view));
		}

		public void Initialize()
		{
			_view.ReturnClicked += OnReturnClicked;
			_table.Changed += Render;
			_subscriptions.Add(_connection.Subscribe<PlayerSatOutEvent>(OnSatOut));
			_subscriptions.Add(_connection.Subscribe<PlayerReturnedEvent>(OnReturned));
			_view.SetVisible(false);
		}

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
			_view.ReturnClicked -= OnReturnClicked;
			_table.Changed -= Render;
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void OnSatOut(PlayerSatOutEvent satOut) => _queue.Enqueue(new SitOutChangedCommand(_table, satOut.TableId, satOut.SeatIndex, true));

		private void OnReturned(PlayerReturnedEvent returned) => _queue.Enqueue(new SitOutChangedCommand(_table, returned.TableId, returned.SeatIndex, false));

		private void Render()
		{
			int mySeat = _table.FindSeatOf(_session.PlayerId);
			_view.SetVisible(mySeat >= 0 && _table.Seats[mySeat].IsSittingOut);
		}

		private void OnReturnClicked() => ReturnAsync().Forget();

		private async UniTaskVoid ReturnAsync()
		{
			if (_isReturning) return;

			_isReturning = true;
			try
			{
				await _connection.RequestAsync<PlayerReturnRequest, PlayerReturnAcceptedResponse>(new PlayerReturnRequest { TableId = _table.TableId }, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				_bus.Publish(new TableStatusMessage("Cannot return: " + exception.Message));
			}
			finally
			{
				_isReturning = false;
			}
		}
	}
}