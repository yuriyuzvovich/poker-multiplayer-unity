using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Infrastructure.Network.Connection;
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
	public sealed class ReconnectPlayModeTests
	{
		private readonly List<string> _netLog = new();

		[SetUp]
		public void SetUp()
		{
			_netLog.Clear();
			Application.logMessageReceived += OnLog;
		}

		[TearDown]
		public void TearDown() => Application.logMessageReceived -= OnLog;

		[UnityTest]
		[Timeout(240000)]
		public IEnumerator ShortDrop_ResumesTheSession_KeepsMySeat_AndTheHandGoesOn() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			await WaitForAsync(() => FindMyFaceUpCards(table), "a hand dealt to me", 20f);

			ResolveConnection().SimulateDrop(TimeSpan.FromSeconds(2));
			await WaitForAsync(() => FindLog("resumed"), "the session resumed", 30f);

			Assert.IsNull(FindLog("new session"), "a short drop must not start a new session");
			StringAssert.Contains("YOU", SeatLabel(table, 2), "the seat survived the drop");
			TMP_Text status = FindComponent<TMP_Text>(hand, "Status");
			Button checkOrCall = FindComponent<Button>(hand, "CheckOrCall");
			int clicks = 0;
			await WaitForAsync(() =>
			{
				if (clicks > 0 && status.text.Contains(" wins ")) return status;
				if (!checkOrCall.gameObject.activeInHierarchy) return null;

				checkOrCall.onClick.Invoke();
				clicks++;
				return null;
			}, "a hand in which I acted after the resume to end", 90f);

			await LeaveAsync(table, hand);
		});

		[UnityTest]
		[Timeout(240000)]
		public IEnumerator DropLongerThanTheZombieWindow_ReconnectsAsANewSession_AndReloadsTheTable() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();

			ResolveConnection().SimulateDrop(TimeSpan.FromSeconds(35));
			await WaitForAsync(() => FindLog("new session"), "a new session after the zombie window", 90f);
			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") ? table : null, "my old seat shown as released after the reload", 10f);

			Assert.IsTrue(hand.gameObject.activeSelf, "still watching the table");
			Assert.IsNull(FindLog("resumed"));
			await BackToLobbyAndCloseAsync(table, hand);
		});

		private static ServerConnection ResolveConnection() => Object.FindAnyObjectByType<LifetimeScope>().Container.Resolve<ServerConnection>();

		private string FindLog(string fragment)
		{
			foreach (string line in _netLog)
			{
				if (!line.Contains(fragment)) continue;

				return line;
			}

			return null;
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			if (!message.StartsWith("[NET]")) return;

			_netLog.Add(message);
		}
	}
}