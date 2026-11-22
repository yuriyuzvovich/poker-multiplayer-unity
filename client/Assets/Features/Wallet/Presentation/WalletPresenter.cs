using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Wallet.Domain;
using Poker.Client.Features.Wallet.Views;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Requests;
using VContainer.Unity;

namespace Poker.Client.Features.Wallet.Presentation
{
	public sealed class WalletPresenter : IInitializable, IDisposable
	{
		private readonly IServerConnection _connection;
		private readonly PlayerSession _session;
		private readonly DisplayLock _displayLock;
		private readonly WalletModel _model;
		private readonly IWalletView _view;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly List<IDisposable> _subscriptions = new();

		public WalletPresenter(IServerConnection connection, PlayerSession session, DisplayLock displayLock, WalletModel model, IWalletView view)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_displayLock = displayLock ?? throw new ArgumentNullException(nameof(displayLock));
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_view = view ?? throw new ArgumentNullException(nameof(view));
		}

		public void Initialize()
		{
			_view.FreeChipsClicked += OnFreeChipsClicked;
			_session.Established += OnSessionEstablished;
			_model.Changed += Render;
			_displayLock.Released += Render;
			_subscriptions.Add(_connection.Subscribe<BalanceChangedEvent>(OnBalanceChanged));
			Render();
			if (_session.IsEstablished) LoadAsync().Forget();
		}

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
			_view.FreeChipsClicked -= OnFreeChipsClicked;
			_session.Established -= OnSessionEstablished;
			_model.Changed -= Render;
			_displayLock.Released -= Render;
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void OnSessionEstablished()
		{
			_model.Forget();
			LoadAsync().Forget();
		}

		private async UniTaskVoid LoadAsync()
		{
			BalanceResponse response;
			try
			{
				response = await _connection.RequestAsync<GetBalanceRequest, BalanceResponse>(new GetBalanceRequest(), _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				_view.SetMessage("Balance unavailable: " + exception.Message);
				return;
			}

			_model.SetBalance(response.Balance);
		}

		private void OnBalanceChanged(BalanceChangedEvent changed) => _model.SetBalance(changed.Balance);

		private void OnFreeChipsClicked() => ClaimFreeChipsAsync().Forget();

		private async UniTaskVoid ClaimFreeChipsAsync()
		{
			try
			{
				await _connection.RequestAsync<AddChipsRequest, BalanceResponse>(new AddChipsRequest(), _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				_view.SetMessage(exception.Message);
				return;
			}

			_view.SetMessage("");
		}

		private void Render()
		{
			if (_displayLock.IsLocked) return;

			_view.SetBalance(_model.IsKnown ? "Balance " + _model.Balance.ToString("N0", CultureInfo.InvariantCulture) : "Balance ...");
		}
	}
}