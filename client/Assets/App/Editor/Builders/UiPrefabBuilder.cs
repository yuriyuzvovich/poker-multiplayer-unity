using System;
using System.IO;
using System.Text.RegularExpressions;
using Poker.Client.App.Editor.Import;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.HandHistory.Views;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Seating.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.UI.Flights;
using Poker.Client.Infrastructure.UI.Layout;
using Poker.Client.Infrastructure.UI.Motion;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Client.Infrastructure.UI.Views;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Poker.Client.App.Editor.Builders
{
	public static class UiPrefabBuilder
	{
		private const string Folder = "Assets/Content/UI";
		private const string GroupName = "UI";
		private const int SeatSlots = 6;
		private const int AvatarCount = 6;
		private const int BoardCards = 5;
		private const float BoardSpacing = 8f;

		private static readonly Vector2 CardSize = new(62f, 90f);
		private static readonly Vector2 Center = new(0.5f, 0.5f);

		private static readonly Color Bar = new(0.03f, 0.04f, 0.07f, 0.92f);
		private static readonly Color Gold = new(0.98f, 0.8f, 0.3f);
		private static readonly Color DeepGold = new(0.93f, 0.55f, 0.16f);
		private static readonly Color Muted = new(0.74f, 0.79f, 0.85f);
		private static readonly Color DarkText = new(0.12f, 0.1f, 0.06f);
		private static readonly Color PanelColor = new(0.08f, 0.1f, 0.15f, 0.97f);
		private static readonly Color EntryColor = new(0.06f, 0.09f, 0.13f, 0.86f);
		private static readonly Color PlateColor = new(0.02f, 0.03f, 0.06f, 0.72f);
		private static readonly Color BadgeColor = new(0f, 0f, 0f, 0.5f);
		private static readonly Color BubbleColor = new(0.07f, 0.09f, 0.14f, 0.92f);
		private static readonly Color SlotColor = new(1f, 1f, 1f, 0.1f);
		private static readonly Color FoldColor = new(0.72f, 0.21f, 0.21f);
		private static readonly Color CallColor = new(0.16f, 0.6f, 0.36f);
		private static readonly Color RaiseColor = new(0.93f, 0.6f, 0.15f);
		private static readonly Color AllInColor = new(0.6f, 0.27f, 0.66f);
		private static readonly Color Neutral = new(0.26f, 0.33f, 0.45f);

		private static Sprite _pill;
		private static Sprite _panel;

		[MenuItem("mp-poker/Rebuild UI Prefabs")]
		public static void Build()
		{
			UiSkinBuilder.Build();
			_pill = LoadArt("Skin/Pill.png");
			_panel = LoadArt("Skin/Panel.png");
			EnsureFolder();
			Save(BuildLobbyScreen(), "LobbyScreen", LobbyView.Address);
			Save(BuildTableScreen(), "TableScreen", TableView.Address);
			Save(BuildHandLayer(), "HandLayer", HandView.Address);
			Save(BuildBuyInPopup(), "BuyInPopup", BuyInPopup.Address);
			Save(BuildNoticePopup(), "NoticePopup", NoticePopup.Address);
			Save(BuildHandHistoryPopup(), "HandHistoryPopup", HandHistoryPopup.Address);
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
			AssetDatabase.SaveAssets();
			Debug.Log("[SETUP] Rebuilt the UI prefabs in " + Folder + " and their Addressables entries in group " + GroupName + ".");
		}

		private static GameObject BuildLobbyScreen()
		{
			RectTransform root = CreateScreenRoot("LobbyScreen");
			AddAppearMotion(root, null);
			CreateBackground(root, "LobbyBackground.jpg");

			TMP_Text title = CreateLabel(root, "Title", 104, Color.white, new Vector2(0.5f, 1f), new Vector2(1000f, 130f), new Vector2(0f, -250f), true);
			title.text = "mp-poker";
			title.enableVertexGradient = true;
			title.colorGradient = new VertexGradient(Gold, Gold, DeepGold, DeepGold);
			TMP_Text subtitle = CreateLabel(root, "Subtitle", 32, Muted, new Vector2(0.5f, 1f), new Vector2(1000f, 50f), new Vector2(0f, -345f));
			subtitle.text = "Pick a table";
			TMP_Text status = CreateLabel(root, "Status", 26, Muted, new Vector2(0.5f, 1f), new Vector2(1000f, 44f), new Vector2(0f, -400f));

			RectTransform list = UiBuilder.CreatePanel(root, "Tables", 1000f);
			UiBuilder.Place(list, new Vector2(0.5f, 1f), new Vector2(920f, 1000f), new Vector2(0f, -960f));
			VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
			layout.spacing = 24f;
			layout.childAlignment = TextAnchor.UpperCenter;

			LobbyEntryView template = BuildLobbyEntry(list);
			template.gameObject.SetActive(false);

			LobbyView view = root.gameObject.AddComponent<LobbyView>();
			Wire(view, ("_status", status), ("_list", list), ("_entryTemplate", template));
			return root.gameObject;
		}

		private static LobbyEntryView BuildLobbyEntry(Transform parent)
		{
			Image card = UiBuilder.CreateImage(parent, "TableEntryTemplate", EntryColor, _panel);
			card.rectTransform.sizeDelta = new Vector2(920f, 170f);
			Button button = card.gameObject.AddComponent<Button>();
			button.targetGraphic = card;
			card.gameObject.AddComponent<PressFeedback>();

			TMP_Text name = CreateLabel(card.transform, "Name", 38, Color.white, new Vector2(0f, 0.5f), new Vector2(520f, 50f), new Vector2(300f, 26f), true);
			name.alignment = TextAlignmentOptions.MidlineLeft;
			TMP_Text blinds = CreateLabel(card.transform, "Blinds", 26, Muted, new Vector2(0f, 0.5f), new Vector2(520f, 40f), new Vector2(300f, -28f));
			blinds.alignment = TextAlignmentOptions.MidlineLeft;
			TMP_Text seats = CreateLabel(card.transform, "Seats", 36, Gold, new Vector2(1f, 0.5f), new Vector2(140f, 44f), new Vector2(-300f, 14f), true);
			TMP_Text seatsLabel = CreateLabel(card.transform, "SeatsLabel", 20, Muted, new Vector2(1f, 0.5f), new Vector2(140f, 30f), new Vector2(-300f, -24f));
			seatsLabel.text = "seated";

			Image play = UiBuilder.CreateImage(card.transform, "Play", CallColor, _pill);
			play.raycastTarget = false;
			UiBuilder.Place(play.rectTransform, new Vector2(1f, 0.5f), new Vector2(170f, 84f), new Vector2(-125f, 0f));
			TMP_Text playLabel = CreateLabel(play.transform, "Label", 30, Color.white, Center, new Vector2(170f, 84f), Vector2.zero, true);
			playLabel.text = "Play";

			LobbyEntryView entry = card.gameObject.AddComponent<LobbyEntryView>();
			Wire(entry, ("_button", button), ("_name", name), ("_blinds", blinds), ("_seats", seats));
			return entry;
		}

		private static GameObject BuildTableScreen()
		{
			RectTransform root = CreateScreenRoot("TableScreen");
			AddAppearMotion(root, null);
			CreateBackground(root, "RoomBackground.jpg");
			Image table = CreateArt(root, "Table", "Table.png");
			UiBuilder.Place(table.rectTransform, Center, new Vector2(940f, 1340f), new Vector2(0f, 60f));
			Image emblem = CreateArt(root, "FeltEmblem", "FeltEmblem.png");
			emblem.color = new Color(1f, 1f, 1f, 0.12f);
			UiBuilder.Place(emblem.rectTransform, Center, new Vector2(400f, 400f), new Vector2(0f, 140f));

			RectTransform seatArea = UiBuilder.CreateRect(root, "SeatArea");
			UiBuilder.Place(seatArea, Center, new Vector2(1040f, 1360f), new Vector2(0f, 80f));
			Isolate(seatArea, true);
			SeatWidget[] seats = new SeatWidget[SeatSlots];
			for (int i = 0; i < SeatSlots; i++)
			{
				seats[i] = BuildSeat(seatArea, i);
			}

			RectTransform layerSlot = UiBuilder.CreateRect(root, "LayerSlot");
			UiBuilder.Stretch(layerSlot);

			Image topBar = UiBuilder.CreateImage(root, "TopBar", Bar);
			RectTransform topBarRect = topBar.rectTransform;
			topBarRect.anchorMin = new Vector2(0f, 1f);
			topBarRect.anchorMax = new Vector2(1f, 1f);
			topBarRect.pivot = new Vector2(0.5f, 1f);
			topBarRect.sizeDelta = new Vector2(0f, 140f);
			topBarRect.anchoredPosition = Vector2.zero;
			AddSafeAreaInset(topBarRect, true);
			Button back = CreateButton(topBar.transform, "BackButton", "\u2039  Lobby", Neutral, new Vector2(210f, 84f), 30);
			UiBuilder.Place((RectTransform)back.transform, Vector2.zero, new Vector2(210f, 84f), new Vector2(129f, 70f));
			TMP_Text title = CreateLabel(topBar.transform, "Title", 36, Color.white, new Vector2(0.5f, 0f), new Vector2(420f, 96f), new Vector2(-60f, 70f), true);
			TMP_Text status = CreateLabel(topBar.transform, "Toast", 26, Gold, new Vector2(0.5f, 0f), new Vector2(1000f, 50f), new Vector2(0f, -35f));

			RectTransform toolbar = BuildToolbar(root, out Button toolbarTemplate);

			TableView view = root.gameObject.AddComponent<TableView>();
			Wire(view, ("_backButton", back), ("_title", title), ("_status", status), ("_layerSlot", layerSlot), ("_toolbar", toolbar), ("_toolbarButtonTemplate", toolbarTemplate));
			WireArray(view, "_seats", seats);
			WireArray(view, "_avatars", LoadAvatars());
			return root.gameObject;
		}

		// A column under the top bar where slices add their buttons through TableToolbar (hand history today).
		private static RectTransform BuildToolbar(Transform parent, out Button template)
		{
			Vector2 buttonSize = new(190f, 72f);
			RectTransform toolbar = UiBuilder.CreateRect(parent, "Toolbar");
			toolbar.anchorMin = new Vector2(1f, 1f);
			toolbar.anchorMax = new Vector2(1f, 1f);
			toolbar.pivot = new Vector2(1f, 1f);
			toolbar.sizeDelta = new Vector2(buttonSize.x, 400f);
			toolbar.anchoredPosition = new Vector2(-16f, -150f);
			SafeAreaInset.Attach(toolbar, true, false);
			Isolate(toolbar, true);
			VerticalLayoutGroup layout = toolbar.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 12f;
			layout.childAlignment = TextAnchor.UpperRight;
			layout.childControlWidth = false;
			layout.childControlHeight = false;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;

			RectTransform storage = UiBuilder.CreateRect(parent, "ToolbarStorage");
			storage.gameObject.SetActive(false);
			template = CreateButton(storage, "ToolbarButton", "", Neutral, buttonSize, 28);
			return toolbar;
		}

		private static SeatWidget BuildSeat(Transform parent, int seatIndex)
		{
			RectTransform seat = UiBuilder.CreateRect(parent, "Seat_" + (seatIndex + 1));
			UiBuilder.Place(seat, Center, new Vector2(270f, 270f), Vector2.zero);
			CanvasGroup group = seat.gameObject.AddComponent<CanvasGroup>();

			RectTransform portrait = UiBuilder.CreateRect(seat, "Portrait");
			UiBuilder.Place(portrait, Center, new Vector2(150f, 150f), new Vector2(0f, 45f));
			Image avatar = CreateArt(portrait, "Avatar", "Avatar1.png");
			UiBuilder.Stretch(avatar.rectTransform, 7f, 7f, 7f, 7f);
			Image ring = CreateArt(portrait, "Ring", "AvatarFrame.png");
			UiBuilder.Stretch(ring.rectTransform);
			Image youTag = UiBuilder.CreateImage(portrait, "YouTag", Gold, _pill);
			youTag.raycastTarget = false;
			UiBuilder.Place(youTag.rectTransform, new Vector2(0.5f, 0f), new Vector2(96f, 36f), new Vector2(0f, 20f));
			TMP_Text youLabel = CreateLabel(youTag.transform, "Label", 24, DarkText, Center, new Vector2(96f, 36f), Vector2.zero, true);
			youLabel.text = "YOU";
			youTag.gameObject.SetActive(false);

			Image emptyMark = UiBuilder.CreateImage(seat, "EmptyMark", new Color(1f, 1f, 1f, 0.55f), LoadArt("Skin/DashedRing.png"));
			UiBuilder.Place(emptyMark.rectTransform, Center, new Vector2(130f, 130f), new Vector2(0f, 45f));
			Button emptyButton = emptyMark.gameObject.AddComponent<Button>();
			emptyButton.targetGraphic = emptyMark;
			emptyMark.gameObject.AddComponent<PressFeedback>();
			emptyMark.gameObject.SetActive(false);

			RectTransform info = UiBuilder.CreateRect(seat, "Info");
			UiBuilder.Stretch(info);
			Image plate = UiBuilder.CreateImage(info, "Plate", PlateColor, _panel);
			plate.raycastTarget = false;
			UiBuilder.Place(plate.rectTransform, Center, new Vector2(220f, 74f), new Vector2(0f, -67f));
			TMP_Text name = CreateLabel(info, "Name", 24, Color.white, Center, new Vector2(210f, 32f), new Vector2(0f, -49f));
			RectTransform stackRow = CreateFittedRow(info, "StackRow", new Vector2(0f, -84f), 32f);
			CreateIcon(stackRow, "Chip", "Chip.png", 26f);
			TMP_Text stack = CreateFittedLabel(stackRow, "Stack", 26, Gold, true);
			TMP_Text badge = CreateLabel(seat, "Badge", 22, Muted, Center, new Vector2(270f, 30f), new Vector2(0f, -120f));
			Image notePill = CreateBadge(seat, "NotePill", new Vector2(0f, -152f), 40f, BubbleColor);
			TMP_Text note = CreateFittedLabel(notePill.transform, "Note", 20, Gold, true);
			notePill.gameObject.SetActive(false);

			Image bubblePill = CreateBadge(seat, "BubblePill", new Vector2(0f, 142f), 46f, BubbleColor);
			TMP_Text bubble = CreateFittedLabel(bubblePill.transform, "Bubble", 22, Color.white, false);
			bubblePill.gameObject.SetActive(false);

			RectTransform betGroup = UiBuilder.CreateRect(seat, "Bet");
			UiBuilder.Place(betGroup, Center, new Vector2(200f, 48f), Vector2.zero);
			ChipStackView betStack = BuildChipStack(betGroup);
			TMP_Text bet = CreateLabel(betGroup, "Amount", 28, Gold, Center, new Vector2(140f, 40f), new Vector2(32f, 0f), true);
			bet.alignment = TextAlignmentOptions.MidlineLeft;
			betGroup.gameObject.SetActive(false);

			RectTransform dealerSlot = UiBuilder.CreateRect(seat, "DealerSlot");
			UiBuilder.Place(dealerSlot, Center, new Vector2(50f, 50f), new Vector2(82f, 92f));

			RectTransform cards = UiBuilder.CreateRow(seat, "Cards", CardSize.y);
			UiBuilder.Place(cards, Center, new Vector2(150f, CardSize.y), Vector2.zero);
			HorizontalLayoutGroup cardsLayout = cards.GetComponent<HorizontalLayoutGroup>();
			cardsLayout.spacing = 6f;
			cardsLayout.childAlignment = TextAnchor.MiddleCenter;

			Button action = CreateButton(seat, "Action", "", Neutral, new Vector2(210f, 66f), 26);
			UiBuilder.Place((RectTransform)action.transform, Center, new Vector2(210f, 66f), new Vector2(0f, -196f));
			TMP_Text actionLabel = action.GetComponentInChildren<TMP_Text>(true);

			SeatWidget widget = seat.gameObject.AddComponent<SeatWidget>();
			Wire(widget, ("_group", group), ("_ring", ring), ("_avatar", avatar), ("_youTag", youTag.gameObject), ("_emptyMark", emptyButton), ("_info", info.gameObject), ("_name", name), ("_stack", stack), ("_badge", badge), ("_bubble", bubble), ("_bubblePill", bubblePill.gameObject), ("_note", note), ("_notePill", notePill.gameObject), ("_betGroup", betGroup), ("_betStack", betStack), ("_bet", bet), ("_dealerSlot", dealerSlot), ("_cards", cards), ("_action", action), ("_actionLabel", actionLabel));
			SerializedObject serialized = new(widget);
			serialized.FindProperty("_seatIndex").intValue = seatIndex;
			serialized.ApplyModifiedPropertiesWithoutUndo();
			return widget;
		}

		private static GameObject BuildHandLayer()
		{
			RectTransform root = CreateScreenRoot("HandLayer");

			RectTransform center = UiBuilder.CreateRect(root, "Center");
			UiBuilder.Place(center, Center, new Vector2(760f, 420f), new Vector2(0f, 80f));
			Isolate(center, false);
			TMP_Text title = CreateLabel(center, "Title", 22, Muted, Center, new Vector2(700f, 32f), new Vector2(0f, 250f));
			RectTransform deck = UiBuilder.CreateRect(center, "Deck");
			UiBuilder.Place(deck, Center, CardSize, new Vector2(0f, 320f));

			RectTransform potGroup = UiBuilder.CreateRect(center, "PotGroup");
			UiBuilder.Stretch(potGroup);
			Image potPile = CreateArt(potGroup, "PotChips", "ChipPile.png");
			UiBuilder.Place(potPile.rectTransform, Center, new Vector2(110f, 75f), new Vector2(0f, 172f));
			Image potBadge = CreateBadge(potGroup, "PotBadge", new Vector2(0f, 100f), 56f, BadgeColor);
			CreateIcon(potBadge.transform, "Chip", "Chip.png", 36f);
			TMP_Text pot = CreateFittedLabel(potBadge.transform, "Pot", 30, Gold, true);
			potGroup.gameObject.SetActive(false);

			RectTransform boardSlots = CreateBoardRow(center, "BoardSlots");
			for (int i = 0; i < BoardCards; i++)
			{
				Image slot = UiBuilder.CreateImage(boardSlots, "Slot", SlotColor, _panel);
				slot.raycastTarget = false;
				slot.pixelsPerUnitMultiplier = 3f;
				slot.rectTransform.sizeDelta = CardSize;
			}

			RectTransform board = CreateBoardRow(center, "Board");

			Image statusBadge = CreateBadge(center, "StatusBadge", new Vector2(0f, -122f), 56f, BadgeColor);
			TMP_Text status = CreateFittedLabel(statusBadge.transform, "Status", 26, Color.white, false);
			statusBadge.gameObject.SetActive(false);

			Image dealer = BuildDealer(root);
			Image turnRing = BuildTurnRing(root, out TMP_Text timer);
			FlightLayer flights = BuildFlightLayer(root);

			Image actions = UiBuilder.CreateImage(root, "Actions", Bar);
			RectTransform actionsRect = actions.rectTransform;
			actionsRect.anchorMin = Vector2.zero;
			actionsRect.anchorMax = new Vector2(1f, 0f);
			actionsRect.pivot = new Vector2(0.5f, 0f);
			actionsRect.sizeDelta = new Vector2(0f, 200f);
			actionsRect.anchoredPosition = Vector2.zero;
			Isolate(actionsRect, true);
			AddSafeAreaInset(actionsRect, false);
			HorizontalLayoutGroup actionsLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
			actionsLayout.spacing = 16f;
			actionsLayout.padding = new RectOffset(20, 20, 32, 32);
			actionsLayout.childAlignment = TextAnchor.UpperCenter;
			actionsLayout.childControlWidth = false;
			actionsLayout.childControlHeight = false;
			actionsLayout.childForceExpandWidth = false;
			Vector2 buttonSize = new(238f, 136f);
			Button fold = CreateButton(actions.transform, "Fold", "Fold", FoldColor, buttonSize, 32);
			Button checkOrCall = CreateButton(actions.transform, "CheckOrCall", "Check", CallColor, buttonSize, 30);
			Button minRaise = CreateButton(actions.transform, "MinRaise", "Raise", RaiseColor, buttonSize, 30);
			Button allIn = CreateButton(actions.transform, "AllIn", "All-in", AllInColor, buttonSize, 30);
			actions.gameObject.SetActive(false);

			Image preActions = UiBuilder.CreateImage(root, "PreActions", Bar);
			RectTransform preActionsRect = preActions.rectTransform;
			preActionsRect.anchorMin = Vector2.zero;
			preActionsRect.anchorMax = new Vector2(1f, 0f);
			preActionsRect.pivot = new Vector2(0.5f, 0f);
			preActionsRect.sizeDelta = new Vector2(0f, 150f);
			preActionsRect.anchoredPosition = Vector2.zero;
			Isolate(preActionsRect, true);
			AddSafeAreaInset(preActionsRect, false);
			HorizontalLayoutGroup preActionsLayout = preActions.gameObject.AddComponent<HorizontalLayoutGroup>();
			preActionsLayout.spacing = 16f;
			preActionsLayout.padding = new RectOffset(20, 20, 28, 28);
			preActionsLayout.childAlignment = TextAnchor.UpperCenter;
			preActionsLayout.childControlWidth = false;
			preActionsLayout.childControlHeight = false;
			preActionsLayout.childForceExpandWidth = false;
			Vector2 preActionSize = new(320f, 94f);
			Button checkFold = CreateButton(preActions.transform, "CheckFold", "Check/Fold", Neutral, preActionSize, 30);
			Button check = CreateButton(preActions.transform, "Check", "Check", Neutral, preActionSize, 30);
			Button callAny = CreateButton(preActions.transform, "CallAny", "Call any", Neutral, preActionSize, 30);
			preActions.gameObject.SetActive(false);

			RectTransform storage = UiBuilder.CreateRect(root, "CardStorage");
			storage.gameObject.SetActive(false);
			CardView cardTemplate = BuildCard(storage);

			HandView view = root.gameObject.AddComponent<HandView>();
			Wire(view, ("_title", title), ("_pot", pot), ("_potGroup", potGroup.gameObject), ("_status", status), ("_statusBadge", statusBadge.gameObject), ("_timer", timer), ("_turnRing", turnRing), ("_board", board), ("_actions", actions.gameObject), ("_fold", fold), ("_checkOrCall", checkOrCall), ("_minRaise", minRaise), ("_allIn", allIn),("_preActions", preActions.gameObject), ("_checkFold", checkFold), ("_check", check), ("_callAny", callAny), ("_cardTemplate", cardTemplate), ("_cardStorage", storage), ("_flights", flights), ("_chip", LoadArt("Chip.png")), ("_cardBack", LoadArt("CardBack.png")), ("_potTarget", potPile.rectTransform), ("_deck", deck), ("_boardSlots", boardSlots), ("_dealer", dealer.rectTransform), ("_glow", LoadArt("Skin/Glow.png")), ("_spark", LoadArt("Skin/Spark.png")));
			return root.gameObject;
		}

		private static Image BuildTurnRing(Transform parent, out TMP_Text timer)
		{
			Image ring = UiBuilder.CreateImage(parent, "TurnRing", Gold, LoadArt("Skin/Ring.png"));
			ring.raycastTarget = false;
			ring.type = Image.Type.Filled;
			ring.fillMethod = Image.FillMethod.Radial360;
			ring.fillOrigin = (int)Image.Origin360.Top;
			ring.fillClockwise = false;
			UiBuilder.Place(ring.rectTransform, Center, new Vector2(176f, 176f), Vector2.zero);
			Isolate(ring.rectTransform, false);
			Image badge = UiBuilder.CreateImage(ring.transform, "TimerBadge", BubbleColor, _pill);
			badge.raycastTarget = false;
			UiBuilder.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(50f, 50f), new Vector2(-16f, -16f));
			timer = CreateLabel(badge.transform, "Timer", 24, Color.white, Center, new Vector2(50f, 50f), Vector2.zero, true);
			ring.gameObject.SetActive(false);
			return ring;
		}

		private static Image BuildDealer(Transform parent)
		{
			Image dealer = CreateArt(parent, "Dealer", "DealerButton.png");
			UiBuilder.Place(dealer.rectTransform, Center, new Vector2(50f, 50f), Vector2.zero);
			Isolate(dealer.rectTransform, false);
			TMP_Text label = CreateLabel(dealer.transform, "Label", 26, DarkText, Center, new Vector2(50f, 50f), Vector2.zero, true);
			label.text = "D";
			dealer.gameObject.SetActive(false);
			return dealer;
		}

		private static FlightLayer BuildFlightLayer(Transform parent)
		{
			RectTransform layer = UiBuilder.CreateRect(parent, "Flights");
			UiBuilder.Stretch(layer);
			Isolate(layer, false);
			RectTransform storage = UiBuilder.CreateRect(layer, "FlightStorage");
			storage.gameObject.SetActive(false);
			Image template = UiBuilder.CreateImage(storage, "FlyingItem", Color.white);
			template.preserveAspect = true;
			template.raycastTarget = false;
			TMP_Text labelTemplate = CreateLabel(storage, "FloatingLabel", 44, Gold, Center, new Vector2(320f, 60f), Vector2.zero, true);
			FlightLayer flights = layer.gameObject.AddComponent<FlightLayer>();
			Wire(flights, ("_template", template), ("_labelTemplate", labelTemplate), ("_storage", storage));
			return flights;
		}

		private static CardView BuildCard(Transform parent)
		{
			Image background = CreateArt(parent, "Card", "CardBack.png");
			background.rectTransform.sizeDelta = CardSize;
			Image highlight = UiBuilder.CreateImage(background.transform, "Highlight", Gold, LoadArt("Skin/Frame.png"));
			highlight.raycastTarget = false;
			highlight.pixelsPerUnitMultiplier = 3f;
			UiBuilder.Stretch(highlight.rectTransform, -5f, -5f, -5f, -5f);
			highlight.gameObject.SetActive(false);
			TMP_Text rank = CreateLabel(background.transform, "Rank", 32, Color.black, new Vector2(0f, 1f), new Vector2(40f, 36f), new Vector2(21f, -22f), true);
			Image suit = CreateArt(background.transform, "Suit", "SuitSpade.png");
			UiBuilder.Place(suit.rectTransform, new Vector2(1f, 0f), new Vector2(34f, 34f), new Vector2(-20f, 22f));
			CardView card = background.gameObject.AddComponent<CardView>();
			Wire(card, ("_background", background), ("_rank", rank), ("_suit", suit), ("_highlight", highlight.gameObject), ("_face", LoadArt("CardFace.png")), ("_back", background.sprite), ("_spade", LoadArt("SuitSpade.png")), ("_heart", LoadArt("SuitHeart.png")), ("_diamond", LoadArt("SuitDiamond.png")), ("_club", LoadArt("SuitClub.png")));
			return card;
		}

		private static GameObject BuildBuyInPopup()
		{
			RectTransform root = CreateScreenRoot("BuyInPopup");
			UiBuilder.Stretch(UiBuilder.CreateImage(root, "Dim", new Color(0f, 0f, 0f, 0.75f)).rectTransform);
			Image panel = UiBuilder.CreateImage(root, "Panel", PanelColor, _panel);
			UiBuilder.Place(panel.rectTransform, Center, new Vector2(860f, 860f), Vector2.zero);
			AddAppearMotion(root, panel.rectTransform);
			VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 24f;
			layout.padding = new RectOffset(50, 50, 56, 50);
			layout.childAlignment = TextAnchor.UpperCenter;
			layout.childControlHeight = false;
			layout.childControlWidth = false;
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = false;
			TMP_Text title = CreateLabel(panel.transform, "Title", 48, Gold, Center, new Vector2(760f, 70f), Vector2.zero, true);
			title.text = "Buy in";
			TMP_Text info = CreateLabel(panel.transform, "Info", 28, Muted, Center, new Vector2(760f, 80f), Vector2.zero);
			Vector2 size = new(760f, 116f);
			Button min = CreateButton(panel.transform, "Min", "Min", CallColor, size, 34);
			Button mid = CreateButton(panel.transform, "Mid", "Mid", CallColor, size, 34);
			Button max = CreateButton(panel.transform, "Max", "Max", CallColor, size, 34);
			Button cancel = CreateButton(panel.transform, "Cancel", "Cancel", Neutral, size, 34);

			BuyInPopup popup = root.gameObject.AddComponent<BuyInPopup>();
			Wire(popup, ("_info", info), ("_min", min), ("_mid", mid), ("_max", max), ("_cancel", cancel));
			return root.gameObject;
		}

		private static GameObject BuildNoticePopup()
		{
			RectTransform root = CreateScreenRoot("NoticePopup");
			UiBuilder.Stretch(UiBuilder.CreateImage(root, "Dim", new Color(0f, 0f, 0f, 0.75f)).rectTransform);
			Image panel = UiBuilder.CreateImage(root, "Panel", PanelColor, _panel);
			UiBuilder.Place(panel.rectTransform, Center, new Vector2(860f, 520f), Vector2.zero);
			AddAppearMotion(root, panel.rectTransform);
			VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 28f;
			layout.padding = new RectOffset(50, 50, 56, 50);
			layout.childAlignment = TextAnchor.UpperCenter;
			layout.childControlHeight = false;
			layout.childControlWidth = false;
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = false;
			TMP_Text title = CreateLabel(panel.transform, "Title", 48, Gold, Center, new Vector2(760f, 70f), Vector2.zero, true);
			TMP_Text message = CreateLabel(panel.transform, "Message", 30, Muted, Center, new Vector2(760f, 120f), Vector2.zero);
			Button action = CreateButton(panel.transform, "Action", "OK", CallColor, new Vector2(760f, 116f), 34);

			NoticePopup popup = root.gameObject.AddComponent<NoticePopup>();
			Wire(popup, ("_title", title), ("_message", message), ("_action", action));
			return root.gameObject;
		}

		private static GameObject BuildHandHistoryPopup()
		{
			RectTransform root = CreateScreenRoot("HandHistoryPopup");
			UiBuilder.Stretch(UiBuilder.CreateImage(root, "Dim", new Color(0f, 0f, 0f, 0.75f)).rectTransform);
			Image panel = UiBuilder.CreateImage(root, "Panel", PanelColor, _panel);
			UiBuilder.Place(panel.rectTransform, Center, new Vector2(960f, 1500f), Vector2.zero);
			AddAppearMotion(root, panel.rectTransform);
			TMP_Text title = CreateLabel(panel.transform, "Title", 48, Gold, new Vector2(0.5f, 1f), new Vector2(860f, 70f), new Vector2(0f, -70f), true);
			title.text = "Hand history";

			RectTransform viewport = UiBuilder.CreateRect(panel.transform, "Viewport");
			UiBuilder.Stretch(viewport, 40f, 40f, 130f, 170f);
			viewport.gameObject.AddComponent<RectMask2D>();
			RectTransform list = UiBuilder.CreateRect(viewport, "List");
			list.anchorMin = new Vector2(0f, 1f);
			list.anchorMax = new Vector2(1f, 1f);
			list.pivot = new Vector2(0.5f, 1f);
			list.sizeDelta = Vector2.zero;
			list.anchoredPosition = Vector2.zero;
			VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 26f;
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = true;
			ContentSizeFitter fitter = list.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
			scroll.content = list;
			scroll.viewport = viewport;
			scroll.horizontal = false;
			scroll.movementType = ScrollRect.MovementType.Clamped;

			TMP_Text rowTemplate = CreateLabel(list, "Row", 26, Color.white, Center, new Vector2(880f, 120f), Vector2.zero);
			rowTemplate.alignment = TextAlignmentOptions.TopLeft;
			rowTemplate.textWrappingMode = TextWrappingModes.Normal;
			rowTemplate.gameObject.SetActive(false);
			TMP_Text empty = CreateLabel(viewport, "Empty", 30, Muted, Center, new Vector2(860f, 80f), Vector2.zero);
			empty.text = "No finished hands yet";

			Button close = CreateButton(panel.transform, "Close", "Close", Neutral, new Vector2(860f, 110f), 34);
			UiBuilder.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(860f, 110f), new Vector2(0f, 90f));

			HandHistoryPopup popup = root.gameObject.AddComponent<HandHistoryPopup>();
			Wire(popup, ("_list", list), ("_rowTemplate", rowTemplate), ("_empty", empty), ("_close", close));
			return root.gameObject;
		}

		private static RectTransform CreateScreenRoot(string name)
		{
			RectTransform root = UiBuilder.CreateRect(null, name);
			UiBuilder.Stretch(root);
			return root;
		}

		private static ChipStackView BuildChipStack(Transform parent)
		{
			RectTransform stack = UiBuilder.CreateRect(parent, "ChipStack");
			UiBuilder.Place(stack, Center, new Vector2(44f, 44f), new Vector2(-62f, 0f));
			Sprite body = LoadArt("Skin/ChipBody.png");
			Sprite spot = LoadArt("Skin/ChipSpots.png");
			Image[] chips = new Image[5];
			Image[] spots = new Image[chips.Length];
			for (int i = 0; i < chips.Length; i++)
			{
				chips[i] = UiBuilder.CreateImage(stack, "Chip_" + (i + 1), Color.white, body);
				chips[i].raycastTarget = false;
				UiBuilder.Place(chips[i].rectTransform, Center, new Vector2(40f, 40f), new Vector2(0f, i * 5f));
				spots[i] = UiBuilder.CreateImage(chips[i].transform, "Spots", Color.white, spot);
				spots[i].raycastTarget = false;
				UiBuilder.Stretch(spots[i].rectTransform);
			}

			ChipStackView view = stack.gameObject.AddComponent<ChipStackView>();
			WireArray(view, "_chips", chips);
			WireArray(view, "_spots", spots);
			return view;
		}

		private static void AddSafeAreaInset(RectTransform rect, bool isTop)
		{
			SafeAreaInset inset = rect.gameObject.AddComponent<SafeAreaInset>();
			SerializedObject serialized = new(inset);
			serialized.FindProperty("_isTop").boolValue = isTop;
			serialized.FindProperty("_grows").boolValue = true;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void AddAppearMotion(RectTransform root, RectTransform pop)
		{
			CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
			AppearMotion motion = root.gameObject.AddComponent<AppearMotion>();
			Wire(motion, ("_group", group), ("_pop", pop));
		}

		private static void Isolate(RectTransform rect, bool isInteractive)
		{
			rect.gameObject.AddComponent<Canvas>();
			if (isInteractive) rect.gameObject.AddComponent<GraphicRaycaster>();
		}

		private static void CreateBackground(Transform parent, string fileName)
		{
			Image background = UiBuilder.CreateImage(parent, "Background", Color.white, LoadArt(fileName));
			background.raycastTarget = false;
			UiBuilder.Stretch(background.rectTransform);
			AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
			fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
			fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
		}

		private static RectTransform CreateBoardRow(Transform parent, string name)
		{
			RectTransform row = UiBuilder.CreateRow(parent, name, CardSize.y);
			UiBuilder.Place(row, Center, new Vector2(BoardCards * CardSize.x + (BoardCards - 1) * BoardSpacing, CardSize.y), Vector2.zero);
			row.localScale = new Vector3(1.5f, 1.5f, 1f);
			HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
			layout.spacing = BoardSpacing;
			layout.childAlignment = TextAnchor.MiddleLeft;
			return row;
		}

		private static Image CreateBadge(Transform parent, string name, Vector2 offset, float height, Color color)
		{
			Image badge = UiBuilder.CreateImage(parent, name, color, _pill);
			badge.raycastTarget = false;
			UiBuilder.Place(badge.rectTransform, Center, new Vector2(200f, height), offset);
			HorizontalLayoutGroup layout = badge.gameObject.AddComponent<HorizontalLayoutGroup>();
			layout.padding = new RectOffset(22, 26, 6, 6);
			layout.spacing = 10f;
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
			ContentSizeFitter fitter = badge.gameObject.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			return badge;
		}

		private static RectTransform CreateFittedRow(Transform parent, string name, Vector2 offset, float height)
		{
			RectTransform row = UiBuilder.CreateRect(parent, name);
			UiBuilder.Place(row, Center, new Vector2(200f, height), offset);
			HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 6f;
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
			ContentSizeFitter fitter = row.gameObject.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			return row;
		}

		private static void CreateIcon(Transform parent, string name, string fileName, float size)
		{
			Image icon = CreateArt(parent, name, fileName);
			LayoutElement element = icon.gameObject.AddComponent<LayoutElement>();
			element.preferredWidth = size;
			element.preferredHeight = size;
		}

		private static TMP_Text CreateFittedLabel(Transform parent, string name, int fontSize, Color color, bool isBold)
		{
			TMP_Text text = CreateLabel(parent, name, fontSize, color, Center, new Vector2(100f, 40f), Vector2.zero, isBold);
			text.textWrappingMode = TextWrappingModes.NoWrap;
			return text;
		}

		private static Image CreateArt(Transform parent, string name, string fileName)
		{
			Image image = UiBuilder.CreateImage(parent, name, Color.white, LoadArt(fileName));
			image.preserveAspect = true;
			image.raycastTarget = false;
			return image;
		}

		private static Sprite LoadArt(string fileName)
		{
			string path = ArtImportPostprocessor.Folder + "/" + fileName;
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null) throw new InvalidOperationException("No sprite at " + path + "; import the art first");

			return sprite;
		}

		private static Sprite[] LoadAvatars()
		{
			Sprite[] avatars = new Sprite[AvatarCount];
			for (int i = 0; i < avatars.Length; i++)
			{
				avatars[i] = LoadArt("Avatar" + (i + 1) + ".png");
			}

			return avatars;
		}

		private static TMP_Text CreateLabel(Transform parent, string name, int fontSize, Color color, Vector2 anchor, Vector2 size, Vector2 offset, bool isBold = false)
		{
			TMP_Text text = UiBuilder.CreateText(parent, name, fontSize, size.y, size.x);
			text.color = color;
			text.alignment = TextAlignmentOptions.Center;
			if (isBold) text.fontStyle = FontStyles.Bold;

			UiBuilder.Place(text.rectTransform, anchor, size, offset);
			return text;
		}

		private static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 size, int fontSize)
		{
			Button button = UiBuilder.CreateButton(parent, name, label, size.x);
			Image image = button.GetComponent<Image>();
			image.sprite = _pill;
			image.type = Image.Type.Sliced;
			image.color = color;
			((RectTransform)button.transform).sizeDelta = size;
			TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
			text.fontSize = fontSize;
			text.fontStyle = FontStyles.Bold;
			text.lineSpacing = -12f;
			button.gameObject.AddComponent<PressFeedback>();
			return button;
		}

		private static void Wire(Object target, params (string Field, Object Value)[] references)
		{
			SerializedObject serialized = new(target);
			foreach ((string field, Object value) in references)
			{
				SerializedProperty property = serialized.FindProperty(field);
				if (property == null) throw new MissingFieldException(target.GetType().Name + " has no serialized field " + field);

				property.objectReferenceValue = value;
			}

			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void WireArray(Object target, string field, Object[] values)
		{
			SerializedObject serialized = new(target);
			SerializedProperty property = serialized.FindProperty(field);
			property.arraySize = values.Length;
			for (int i = 0; i < values.Length; i++)
			{
				property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
			}

			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void Save(GameObject root, string fileName, string address)
		{
			string path = Folder + "/" + fileName + ".prefab";
			PrefabUtility.SaveAsPrefabAsset(root, path);
			Object.DestroyImmediate(root);
			DropDefaultFontReferences(path);

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
			AddressableAssetGroup group = settings.FindGroup(GroupName);
			if (group == null) group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

			AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, false, false);
			entry.address = address;
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
		}

		private static void DropDefaultFontReferences(string path)
		{
			string fontGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset));
			string yaml = File.ReadAllText(path);
			string stripped = Regex.Replace(yaml, @"(m_fontAsset|m_sharedMaterial): \{fileID: -?\d+, guid: " + fontGuid + @", type: 2\}", "$1: {fileID: 0}");
			if (stripped == yaml) return;

			File.WriteAllText(path, stripped);
			AssetDatabase.ImportAsset(path);
		}

		private static void EnsureFolder()
		{
			if (!AssetDatabase.IsValidFolder("Assets/Content")) AssetDatabase.CreateFolder("Assets", "Content");
			if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Content", "UI");
		}
	}
}