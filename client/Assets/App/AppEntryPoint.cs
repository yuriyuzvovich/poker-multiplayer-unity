using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.App.Connection;
using Poker.Client.App.Startup;
using Poker.Client.Features.Lobby.Application;
using Poker.Client.Infrastructure.Core.Commands;
using UnityEngine;
using VContainer.Unity;

namespace Poker.Client.App
{
	public sealed class AppEntryPoint : IAsyncStartable
	{
		private readonly ConnectionSupervisor _supervisor;
		private readonly ICommand _startup;

		public AppEntryPoint(ConnectionSupervisor supervisor, ConnectCommand connect, HelloCommand hello, SyncClockCommand syncClock, LoadLobbyCommand loadLobby, ReturnToTableCommand returnToTable)
		{
			_supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
			_startup = new SequenceCommand("Startup",
				new TimedCommand(new RetryCommand(connect, 3, TimeSpan.FromSeconds(2))),
				new TimedCommand(hello),
				new ParallelCommand("ClockAndLobby", new TimedCommand(syncClock), new TimedCommand(loadLobby)),
				new TimedCommand(returnToTable));
		}

		public async UniTask StartAsync(CancellationToken cancellation)
		{
			Application.targetFrameRate = 60;
			_supervisor.Start();
			bool started;
			try
			{
				started = await _startup.ExecuteAsync(cancellation);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			Debug.Log("[INIT] " + _startup.Name + (started ? " complete" : " failed"));
			if (!started) return;

			_supervisor.EnableReconnect();
		}
	}
}