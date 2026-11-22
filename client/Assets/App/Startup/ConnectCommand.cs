using System;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Core;
using Poker.Client.Infrastructure.Core.Commands;
using Poker.Client.Infrastructure.Network.Connection;

namespace Poker.Client.App.Startup
{
	public sealed class ConnectCommand : ICommand
	{
		private readonly ServerConnection _connection;
		private readonly AppStateMachine _appState;
		private readonly NetworkSettings _settings;

		public ConnectCommand(ServerConnection connection, AppStateMachine appState, NetworkSettings settings)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_appState = appState ?? throw new ArgumentNullException(nameof(appState));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		public string Name => "Connect";

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			_appState.TransitionTo(AppState.Connecting, _settings.ServerUri.ToString());
			try
			{
				await _connection.ConnectAsync(cancellationToken);
			}
			catch (WebSocketException exception)
			{
				_appState.TransitionTo(AppState.Failed, exception.Message);
				return false;
			}

			_appState.TransitionTo(AppState.Connected);
			return true;
		}
	}
}