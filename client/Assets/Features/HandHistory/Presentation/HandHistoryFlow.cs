using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.HandHistory.Views;
using Poker.Client.Features.Table.Events;
using Poker.Client.Features.Table.Presentation;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Shared.Features.HandHistory.Models;
using Poker.Shared.Features.HandHistory.Requests;
using VContainer.Unity;

namespace Poker.Client.Features.HandHistory.Presentation
{
	public sealed class HandHistoryFlow : IInitializable, IDisposable
	{
		private readonly TableToolbar _toolbar;
		private readonly IServerConnection _connection;
		private readonly PopupService _popups;
		private readonly IMessageBus _bus;
		private readonly List<string> _rows = new();
		private readonly CancellationTokenSource _lifetime = new();
		private bool _isOpen;

		public HandHistoryFlow(TableToolbar toolbar, IServerConnection connection, PopupService popups, IMessageBus bus)
		{
			_toolbar = toolbar ?? throw new ArgumentNullException(nameof(toolbar));
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
		}

		public void Initialize() => _toolbar.Add("History", "History", OnHistoryClicked);

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
		}

		private void OnHistoryClicked()
		{
			if (_lifetime.IsCancellationRequested) return;

			ShowAsync(_lifetime.Token).Forget();
		}

		private async UniTaskVoid ShowAsync(CancellationToken cancellationToken)
		{
			if (_isOpen) return;

			_isOpen = true;
			try
			{
				HandHistoryResponse history;
				try
				{
					history = await _connection.RequestAsync<GetHandHistoryRequest, HandHistoryResponse>(new GetHandHistoryRequest(), cancellationToken);
				}
				catch (Exception exception) when (RequestErrors.IsExpected(exception))
				{
					_bus.Publish(new TableStatusMessage("History unavailable: " + exception.Message));
					return;
				}

				_rows.Clear();
				foreach (HandRecord hand in history.Hands)
				{
					_rows.Add(HandHistoryText.Format(hand));
				}

				HandHistoryPopup popup = await _popups.LoadAsync<HandHistoryPopup>(HandHistoryPopup.Address, cancellationToken);
				popup.Setup(_rows);
				await _popups.ShowAsync(popup, cancellationToken);
			}
			finally
			{
				_isOpen = false;
			}
		}
	}
}