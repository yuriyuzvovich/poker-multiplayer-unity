using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Seating.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.UI.Flights;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Poker.Client.App.Editor
{
	public static class UiSnapshot
	{
		private static string Output => Environment.GetEnvironmentVariable("POKER_SNAPSHOT_DIR") ?? "Library/UiSnapshots";

		[MenuItem("mp-poker/Render UI Snapshots")]
		public static void Capture()
		{
			Directory.CreateDirectory(Output);
			Shoot("lobby", canvas =>
			{
				LobbyView lobby = Spawn<LobbyView>("Assets/Content/UI/LobbyScreen.prefab", canvas.transform);
				lobby.SetStatus("Connected as Guest-2299");
				lobby.ShowTables(new List<LobbyEntry>
				{
					new LobbyEntry("practice", "Practice table", "Blinds 10/20", "3/6"),
					new LobbyEntry("heads-up", "Heads-up", "Blinds 50/100", "1/2"),
				});
			});
			Shoot("table", canvas =>
			{
				ShowSampleHand(canvas);
				DOTween.CompleteAll(true);
			});
			Shoot("showdown", canvas =>
			{
				HandView hand = ShowSampleHand(canvas);
				hand.HighlightCards(new[] { "Ks", "Kd", "Ah", "Qh", "7c" });
				DOTween.CompleteAll(true);
			});
			Shoot("win", canvas =>
			{
				HandView hand = ShowSampleHand(canvas);
				DOTween.CompleteAll(true);
				using CancellationTokenSource celebration = new();
				hand.CelebrateAsync(new[] { new SeatWin(0, 1250) }, 0.9f, celebration.Token).Forget();
				DOTween.Goto(hand.GetComponentInChildren<FlightLayer>(), 0.3f);
			});
			Shoot("buy-in", canvas =>
			{
				BuyInPopup popup = Spawn<BuyInPopup>("Assets/Content/UI/BuyInPopup.prefab", canvas.transform);
				popup.Setup(400, 2000, 30);
			});
			Debug.Log("[SNAPSHOT] Wrote UI snapshots to " + Path.GetFullPath(Output));
		}

		private static HandView ShowSampleHand(Canvas canvas)
		{
			TableView table = Spawn<TableView>("Assets/Content/UI/TableScreen.prefab", canvas.transform);
			HandView hand = Spawn<HandView>("Assets/Content/UI/HandLayer.prefab", table.LayerSlot);
			table.SetTitle("Practice table");
			table.SetStatus("Blinds 10/20, buy-in 400 to 2000");
			table.ShowSeats(new List<SeatRow>
			{
				new SeatRow("Ada", "4,870", "", ""),
				new SeatRow("Linus", "2,990", "", ""),
				new SeatRow("Grace", "1,840", "", ""),
				new SeatRow("", "", "empty", "Sit"),
				new SeatRow("Edsger", "960", "away", ""),
				new SeatRow("Barbara", "3,300", "", ""),
			}, 0);
			hand.AttachTo(table);
			hand.SetTitle("Hand #102, flop");
			hand.SetPot("Pot 180");
			hand.SetStatus("Grace to act");
			hand.SetBoard(new[] { "Ah", "Kd", "7c" });
			hand.SetSeats(new List<HandSeatLine>
			{
				new HandSeatLine { SeatIndex = 0, Name = "Ada", IsMine = true, Stack = 4870, Bet = 1250, Action = "call 40", Description = "Pair of kings", Cards = new[] { "Ks", "Qh" } },
				new HandSeatLine { SeatIndex = 1, Name = "Linus", IsButton = true, Stack = 2990, Bet = 200, Action = "raise to 40", Cards = new[] { "", "" } },
				new HandSeatLine { SeatIndex = 2, Name = "Grace", IsActing = true, Stack = 1840, Cards = new[] { "", "" } },
				new HandSeatLine { SeatIndex = 5, Name = "Barbara", IsFolded = true, Stack = 33300, Action = "fold" },
			});
			hand.ShowActions(new ActionOptions(40, false, true, 80, 4870));
			hand.SetTurnClock(2, 0.6f, "9");
			return hand;
		}

		private static void Shoot(string name, Action<Canvas> fill)
		{
			RenderTexture target = new(1080, 1920, 24);
			GameObject cameraObject = new("SnapshotCamera");
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.orthographic = true;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Color.black;
			camera.targetTexture = target;
			GameObject canvasObject = new("SnapshotCanvas", typeof(Canvas), typeof(CanvasScaler));
			Canvas canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = camera;
			canvas.planeDistance = 10f;
			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.matchWidthOrHeight = 0f;

			fill(canvas);
			for (int i = 0; i < 3; i++)
			{
				Canvas.ForceUpdateCanvases();
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasObject.transform);
			}

			camera.Render();
			RenderTexture.active = target;
			Texture2D image = new(1080, 1920, TextureFormat.RGB24, false);
			image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
			image.Apply();
			RenderTexture.active = null;
			File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
			Object.DestroyImmediate(image);
			Object.DestroyImmediate(canvasObject);
			Object.DestroyImmediate(cameraObject);
			target.Release();
		}

		private static T Spawn<T>(string path, Transform parent) where T : Component
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			GameObject instance = Object.Instantiate(prefab, parent, false);
			foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
			{
				MethodInfo awake = behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (awake == null) continue;
				if (awake.DeclaringType.Assembly.GetName().Name.StartsWith("Unity")) continue;

				awake.Invoke(behaviour, null);
			}

			return instance.GetComponent<T>();
		}
	}
}