using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Core.Commands;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using UnityEngine;

namespace Poker.Client.App.Startup
{
	public sealed class SyncClockCommand : ICommand
	{
		private readonly IServerConnection _connection;
		private readonly ServerClock _clock;

		public SyncClockCommand(IServerConnection connection, ServerClock clock)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		}

		public string Name => "SyncClock";

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			for (int i = 0; i < 5; i++)
			{
				PingRequest ping = new() { ClientTimeUnixMs = ServerClock.LocalNowUnixMs };
				PongResponse pong;
				try
				{
					pong = await _connection.RequestAsync<PingRequest, PongResponse>(ping, cancellationToken);
				}
				catch (Exception exception) when (RequestErrors.IsExpected(exception))
				{
					Debug.LogWarning("[CLOCK] Ping failed: " + exception.Message);
					return false;
				}

				_clock.AddSample(pong.ClientTimeUnixMs, pong.ServerTimeUnixMs, ServerClock.LocalNowUnixMs);
			}

			Debug.Log("[CLOCK] Offset " + _clock.OffsetMs + " ms, best round trip " + _clock.BestRoundTripMs + " ms");
			return true;
		}
	}
}