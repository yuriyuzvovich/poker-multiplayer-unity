using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Table.Views;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Diagnostics
{
	[Explicit("A measurement, not a check: run it on its own in a rendering editor (without -batchmode) and read the [RENDER] log lines")]
	public sealed class RenderStatsMeasurement
	{
		private const int SampleFrames = 30;

		[UnityTest]
		[Timeout(240000)]
		public IEnumerator Measure_LobbyAndTableWithAHand() => UniTask.ToCoroutine(async () =>
		{
			using ProfilerRecorder batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
			using ProfilerRecorder drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
			using ProfilerRecorder setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
			using ProfilerRecorder textureMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");

			await StartAppAsNewPlayerAsync();
			await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby view");
			await UniTask.DelayFrame(SampleFrames);
			Log("lobby", batches, drawCalls, setPassCalls, textureMemory);

			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			await WaitForAsync(() => FindMyFaceUpCards(table), "a hand dealt to me", 30f);
			await UniTask.DelayFrame(SampleFrames);
			Log("table with a hand", batches, drawCalls, setPassCalls, textureMemory);

			await LeaveAsync(table, hand);
		});

		private static void Log(string screen, ProfilerRecorder batches, ProfilerRecorder drawCalls, ProfilerRecorder setPassCalls, ProfilerRecorder textureMemory) =>
			Debug.Log("[RENDER] " + screen + ": " + batches.LastValue + " batches, " + drawCalls.LastValue + " draw calls, " + setPassCalls.LastValue + " SetPass, textures " + (textureMemory.LastValue / (1024f * 1024f)).ToString("F1") + " MB, " + Screen.width + "x" + Screen.height);
	}
}