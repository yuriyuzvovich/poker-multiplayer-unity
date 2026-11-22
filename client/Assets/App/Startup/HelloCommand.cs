using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Core.Commands;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using UnityEngine;

namespace Poker.Client.App.Startup
{
	public sealed class HelloCommand : ICommand
	{
		private readonly ServerConnection _connection;
		private readonly PlayerSession _session;
		private readonly ServerClock _clock;
		private readonly NetworkSettings _settings;

		public HelloCommand(ServerConnection connection, PlayerSession session, ServerClock clock, NetworkSettings settings)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		public string Name => "Hello";

		public bool WasResumed { get; private set; }

		public string SeatedTableId { get; private set; } = "";

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			HelloRequest hello = new() { ProtocolVersion = ProtocolInfo.Version, PlayerName = _settings.PlayerName, ResumeKey = _connection.ResumeKey, DeviceKey = _settings.DeviceKey };
			WelcomeResponse welcome;
			long sentAtUnixMs = ServerClock.LocalNowUnixMs;
			try
			{
				welcome = await _connection.RequestAsync<HelloRequest, WelcomeResponse>(hello, cancellationToken);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				Debug.LogWarning("[NET] Hello failed: " + exception.Message);
				return false;
			}

			_clock.AddSample(sentAtUnixMs, welcome.ServerTimeUnixMs, ServerClock.LocalNowUnixMs);
			WasResumed = welcome.Resumed;
			SeatedTableId = welcome.SeatedTableId;
			if (welcome.Resumed)
			{
				Debug.Log("[NET] Session " + welcome.SessionId + " resumed");
				return true;
			}

			_session.Establish(welcome.SessionId, welcome.PlayerId, welcome.PlayerName);
			Debug.Log("[NET] Welcome " + welcome.PlayerName + " (" + welcome.PlayerId + ")");
			return true;
		}
	}
}