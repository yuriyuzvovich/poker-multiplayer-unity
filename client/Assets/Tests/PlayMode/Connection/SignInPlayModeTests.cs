using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Connection
{
	public sealed class SignInPlayModeTests
	{
		[UnityTest]
		[Timeout(240000)]
		public IEnumerator AppRestart_ReturnsToMySeatOnItsOwn() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView _) = await SitAtHeadsUpAsync();
			await WaitForAsync(() => FindMyFaceUpCards(table), "a hand dealt to me", 20f);
			Object.FindAnyObjectByType<LifetimeScope>().Container.Resolve<ServerConnection>().SimulateDrop(TimeSpan.FromSeconds(60));
			Object.Destroy(GameObject.Find("Root"));
			await WaitForAsync(() => table == null ? new object() : null, "the old app gone");

			await StartAppAsync();
			TableView returned = await WaitForAsync(() => Object.FindAnyObjectByType<TableView>(), "the table opened without a tap", 20f);
			await WaitForAsync(() => SeatLabel(returned, 2).Contains("YOU") ? returned : null, "back in my seat");
			HandView returnedHand = await WaitForAsync(() => Object.FindAnyObjectByType<HandView>(), "the hand layer");
			await LeaveAsync(returned, returnedHand);
		});

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator SignInFromAnotherDevice_ClosesThisApp_UntilPlayHereReloadsIt() => UniTask.ToCoroutine(async () =>
		{
			await StartAppAsNewPlayerAsync();
			LobbyView lobby = await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby view");
			IObjectResolver container = Object.FindAnyObjectByType<LifetimeScope>().Container;
			ServerConnection connection = container.Resolve<ServerConnection>();
			await WaitForAsync(() => FindComponentOrNull<Button>(lobby, "Table_heads-up"), "startup finished with the tables listed");
			NetworkSettings settings = container.Resolve<NetworkSettings>();
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
			using WebSocketConnection other = new(new NetworkStats());
			await other.ConnectAsync(settings.ServerUri, timeout.Token);
			MessageCodec codec = new(container.Resolve<MessageRegistry>(), new NewtonsoftPayloadSerializer());
			HelloRequest hello = new() { ProtocolVersion = ProtocolInfo.Version, PlayerName = "Other", DeviceKey = settings.DeviceKey };
			await other.SendAsync(codec.Encode(hello, requestId: 1), timeout.Token);

			await WaitForAsync(() => connection.LastCloseCode == CloseCodes.DuplicateLogin ? connection : null, "this app replaced by the other sign-in");
			TMP_Text status = FindComponent<TMP_Text>(lobby, "Status");
			await WaitForAsync(() => status.text.Contains("another device") ? status : null, "the reason shown in the lobby");
			await UniTask.Delay(TimeSpan.FromSeconds(3), DelayType.Realtime);
			Assert.IsFalse(connection.IsOpen, "a replaced app does not reconnect");

			NoticePopup notice = await WaitForAsync(() => Object.FindAnyObjectByType<NoticePopup>(), "the play-here offer");
			FindComponent<Button>(notice, "Action").onClick.Invoke();
			await WaitForAsync(() => other.CloseCode == CloseCodes.DuplicateLogin ? other : null, "the other sign-in replaced in turn");
			await WaitForAsync(() => FindReloadedConnection(connection), "the app reloaded and signed in again");

			Object.Destroy(GameObject.Find("Root"));
			await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);
		});

		private static ServerConnection FindReloadedConnection(ServerConnection old)
		{
			LifetimeScope scope = Object.FindAnyObjectByType<LifetimeScope>();
			if (scope == null) return null;

			ServerConnection current = scope.Container.Resolve<ServerConnection>();
			if (current == old) return null;

			return current.IsEstablished ? current : null;
		}
	}
}