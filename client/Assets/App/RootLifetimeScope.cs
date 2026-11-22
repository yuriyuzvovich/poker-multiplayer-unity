using System;
using System.IO;
using Poker.Client.App.Connection;
using Poker.Client.App.Startup;
using Poker.Client.Features.Hand;
using Poker.Client.Features.HandHistory;
using Poker.Client.Features.Lobby;
using Poker.Client.Features.Seating;
using Poker.Client.Features.Table;
using Poker.Client.Features.TurnTimer;
using Poker.Client.Features.Wallet;
using Poker.Client.Infrastructure.Core;
using Poker.Client.Infrastructure.Diagnostics;
using Poker.Client.Infrastructure.Network;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Client.Infrastructure.UI.Views;
using Poker.Shared;
using Poker.Shared.Features.Table;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.App
{
	public sealed class RootLifetimeScope : LifetimeScope
	{
		[SerializeField] private string _serverUrl = "ws://localhost:5080/ws";
		[SerializeField] private float _requestTimeoutSeconds = 10f;
		[SerializeField] private string _playerName = "";
		[SerializeField] private int _simulatedRoundTripMs;

		protected override void Configure(IContainerBuilder builder)
		{
			string playerName = _playerName.Trim().Length > 0 ? _playerName.Trim() : "Guest-" + UnityEngine.Random.Range(1000, 10000);
			TimeSpan simulatedRoundTrip = TimeSpan.FromMilliseconds(ReadRoundTripOverride() ?? _simulatedRoundTripMs);
			builder.RegisterInstance(MessageCatalog.CreateRegistry());
			builder.RegisterInstance(new NetworkSettings(new Uri(_serverUrl), TimeSpan.FromSeconds(_requestTimeoutSeconds), playerName, DeviceKey.LoadOrCreate(), simulatedRoundTrip));
			builder.RegisterInstance(UiLayers.Create(transform));
			builder.Register<ViewLoader>(Lifetime.Singleton);
			builder.Register<PopupService>(Lifetime.Singleton);
			builder.RegisterInstance(new TranscriptRecorder(TableMessageNames.Snapshot, Debug.isDebugBuild ? 10000 : 0)).AsSelf().As<IPacketTap>();
			CoreInstaller.Install(builder);
			NetworkInstaller.Install(builder);
			DiagnosticsInstaller.Install(builder, transform, Path.Combine(Application.persistentDataPath, "BugReports"));
			TableInstaller.Install(builder);
			LobbyInstaller.Install(builder);
			SeatingInstaller.Install(builder);
			HandInstaller.Install(builder);
			HandHistoryInstaller.Install(builder);
			TurnTimerInstaller.Install(builder, transform);
			WalletInstaller.Install(builder, transform);
			builder.Register<ConnectCommand>(Lifetime.Singleton);
			builder.Register<HelloCommand>(Lifetime.Singleton);
			builder.Register<SyncClockCommand>(Lifetime.Singleton);
			builder.Register<ReturnToTableCommand>(Lifetime.Singleton);
			builder.Register<AppReloader>(Lifetime.Singleton);
			builder.RegisterEntryPoint<ReplacedSessionFlow>();
			builder.Register<ConnectionSupervisor>(Lifetime.Singleton);
			builder.RegisterEntryPoint<AppEntryPoint>();
		}

		private static int? ReadRoundTripOverride()
		{
			string value = Environment.GetEnvironmentVariable("POKER_SIMULATED_RTT_MS");
			if (!int.TryParse(value, out int roundTripMs)) return null;

			return roundTripMs;
		}
	}
}