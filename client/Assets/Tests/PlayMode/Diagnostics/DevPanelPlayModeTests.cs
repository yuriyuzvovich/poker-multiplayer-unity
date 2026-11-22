using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Poker.Client.Features.Hand.Presentation;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Infrastructure.Diagnostics.DevPanel;
using Poker.Client.Infrastructure.Diagnostics.Reports;
using Poker.Client.Infrastructure.Network.Connection;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Diagnostics
{
	public sealed class DevPanelPlayModeTests
	{
		[UnityTest]
		[Timeout(240000)]
		public IEnumerator DevPanel_WritesAReplayableBugReport_AndSimulatesADrop() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			await WaitForAsync(() => FindMyFaceUpCards(table), "a hand dealt to me", 20f);
			DevPanelView devPanel = Object.FindAnyObjectByType<DevPanelView>();
			IObjectResolver container = Object.FindAnyObjectByType<LifetimeScope>().Container;

			FindComponent<Button>(devPanel, "DevToggle").onClick.Invoke();
			TMP_Text info = FindComponent<TMP_Text>(devPanel, "Info");
			StringAssert.Contains("from a snapshot", info.text);
			StringAssert.Contains("Net in ", info.text);
			StringAssert.Contains("Most bytes in: ", info.text);
			StringAssert.Contains("Slowest handler: ", info.text);
			HandTimings timings = container.Resolve<HandTimings>();
			float normalActed = timings.PlayerActedSeconds;
			FindComponent<Button>(devPanel, "FX win").onClick.Invoke();
			FindComponent<Button>(devPanel, "Fast anims").onClick.Invoke();
			Assert.Less(timings.PlayerActedSeconds, normalActed, "fast animations shorten the hand timings");
			FindComponent<Button>(devPanel, "Fast anims").onClick.Invoke();
			Assert.AreEqual(normalActed, timings.PlayerActedSeconds, 0.0001f, "a second tap brings the normal timings back");
			Assert.IsNotNull(FindComponentOrNull<Button>(devPanel, "FX deal"));
			Assert.IsNotNull(FindComponentOrNull<Button>(devPanel, "FX bets"));
			Assert.IsNotNull(FindComponentOrNull<Button>(devPanel, "FX to pot"));
			FindComponent<Button>(devPanel, "BugReport").onClick.Invoke();
			string folder = container.Resolve<BugReportWriter>().LastReportFolder;
			try
			{
				string report = File.ReadAllText(Path.Combine(folder, "report.txt"));
				StringAssert.Contains("[NET] Welcome", report);
				StringAssert.Contains("Transcript: ", report);
				JObject transcript = JObject.Parse(File.ReadAllText(Path.Combine(folder, "transcript.json")));
				Assert.IsTrue((bool)transcript["startsAtBaseline"]);
				JArray messages = (JArray)transcript["messages"];
				Assert.AreEqual("table.snapshot", (string)messages[0]["type"]);
				Assert.IsTrue(HasMessage(messages, "hand.hole_cards"), "the transcript holds the running hand");
				StringAssert.Contains("Report: " + folder, info.text);
			}
			finally
			{
				Directory.Delete(folder, true);
			}

			ServerConnection connection = container.Resolve<ServerConnection>();
			FindComponent<Button>(devPanel, "ShortDrop").onClick.Invoke();
			Assert.IsFalse(connection.IsOpen, "the drop closes the socket at once");
			await WaitForAsync(() => connection.IsOpen && connection.IsEstablished ? connection : null, "the session back after the drop", 30f);
			StringAssert.Contains("YOU", SeatLabel(table, 2));

			FindComponent<Button>(devPanel, "DevToggle").onClick.Invoke();
			await LeaveAsync(table, hand);
		});

		private static bool HasMessage(JArray messages, string type)
		{
			foreach (JToken message in messages)
			{
				if ((string)message["type"] != type) continue;

				return true;
			}

			return false;
		}
	}
}