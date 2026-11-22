using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.App.Startup;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using UnityEngine;

namespace Poker.Client.App.Connection
{
	public sealed class ConnectionSupervisor : IDisposable
	{
		private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMilliseconds(500);
		private static readonly TimeSpan LongestRetryDelay = TimeSpan.FromSeconds(8);

		private readonly ServerConnection _connection;
		private readonly ServerClock _clock;
		private readonly AppStateMachine _appState;
		private readonly IMessageBus _bus;
		private readonly ConnectCommand _connect;
		private readonly HelloCommand _hello;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly System.Random _jitter = new();
		private bool _isStarted;
		private bool _canReconnect;
		private bool _isReconnecting;

		public ConnectionSupervisor(ServerConnection connection, ServerClock clock, AppStateMachine appState, IMessageBus bus, ConnectCommand connect, HelloCommand hello)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_appState = appState ?? throw new ArgumentNullException(nameof(appState));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_connect = connect ?? throw new ArgumentNullException(nameof(connect));
			_hello = hello ?? throw new ArgumentNullException(nameof(hello));
		}

		public void Start()
		{
			if (_isStarted) return;

			_isStarted = true;
			_connection.Closed += OnClosed;
			HeartbeatLoopAsync(_lifetime.Token).Forget();
		}

		public void EnableReconnect() => _canReconnect = true;

		public void Dispose()
		{
			_connection.Closed -= OnClosed;
			_lifetime.Cancel();
			_lifetime.Dispose();
		}

		private void OnClosed(string reason)
		{
			if (_appState.Current == AppState.Connected) _appState.TransitionTo(AppState.Disconnected, reason);
			if (_connection.LastCloseCode == CloseCodes.DuplicateLogin)
			{
				_canReconnect = false;
				_bus.Publish(new TableStatusMessage(reason));
				_bus.Publish(new SessionReplaced(reason));
				return;
			}

			if (!_canReconnect) return;
			if (_isReconnecting) return;

			_bus.Publish(new TableStatusMessage("Connection lost, reconnecting..."));
			ReconnectAsync(_lifetime.Token).Forget();
		}

		private async UniTaskVoid ReconnectAsync(CancellationToken cancellationToken)
		{
			_isReconnecting = true;
			TimeSpan delay = FirstRetryDelay;
			try
			{
				while (!await TryHandshakeAsync(cancellationToken))
				{
					TimeSpan jitter = TimeSpan.FromMilliseconds(_jitter.Next(0, 250));
					await UniTask.Delay(delay + jitter, DelayType.Realtime, cancellationToken: cancellationToken);
					delay = delay + delay < LongestRetryDelay ? delay + delay : LongestRetryDelay;
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			finally
			{
				_isReconnecting = false;
			}

			if (_hello.WasResumed)
			{
				_bus.Publish(new TableStatusMessage("Reconnected"));
				return;
			}

			Debug.Log("[NET] The server started a new session; reloading the table");
			_bus.Publish(new TableStatusMessage("Reconnected as a new session; the table was reloaded"));
			_bus.Publish(new SessionRestarted());
		}

		private async UniTask<bool> TryHandshakeAsync(CancellationToken cancellationToken)
		{
			if (!await _connect.ExecuteAsync(cancellationToken)) return false;
			if (await _hello.ExecuteAsync(cancellationToken)) return true;

			if (_connection.IsOpen) _connection.Drop("handshake failed");

			return false;
		}

		private async UniTaskVoid HeartbeatLoopAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				bool isCancelled = await UniTask.Delay(_connection.HeartbeatInterval, DelayType.Realtime, cancellationToken: cancellationToken).SuppressCancellationThrow();
				if (isCancelled) return;
				if (_isReconnecting) continue;
				if (!_connection.IsOpen) continue;
				if (!_connection.IsEstablished) continue;

				long silentMs = ServerClock.LocalNowUnixMs - _connection.LastPacketAtUnixMs;
				if (silentMs > _connection.HeartbeatInterval.TotalMilliseconds * 2)
				{
					_connection.Drop("no traffic for " + silentMs + " ms");
					continue;
				}

				PingAsync(cancellationToken).Forget();
			}
		}

		private async UniTaskVoid PingAsync(CancellationToken cancellationToken)
		{
			PongResponse pong;
			try
			{
				pong = await _connection.RequestAsync<PingRequest, PongResponse>(new PingRequest { ClientTimeUnixMs = ServerClock.LocalNowUnixMs }, cancellationToken);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				return;
			}

			_clock.AddSample(pong.ClientTimeUnixMs, pong.ServerTimeUnixMs, ServerClock.LocalNowUnixMs);
		}
	}
}