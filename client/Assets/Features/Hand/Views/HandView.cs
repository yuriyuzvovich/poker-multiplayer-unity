using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Formatting;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.UI.Flights;
using Poker.Client.Infrastructure.UI.Motion;
using Poker.Client.Infrastructure.UI.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Hand.Views
{
	public sealed class HandView : MonoBehaviour
	{
		public const string Address = "ui/hand-layer";

		private const int ChipsPerSeat = 3;
		private const float StaggerSeconds = 0.06f;
		private const int SparksPerWin = 12;

		private static readonly Color RingFull = new(0.98f, 0.8f, 0.3f);
		private static readonly Color RingEmpty = new(0.92f, 0.22f, 0.16f);
		private static readonly Color SparkColor = new(1f, 0.92f, 0.6f);
		private static readonly Color ChosenColor = new(0.98f, 0.8f, 0.3f);
		private static readonly Color ChoiceColor = new(0.26f, 0.33f, 0.45f);
		private static readonly Color ChosenText = new(0.12f, 0.1f, 0.06f);

		[SerializeField]
		private TMP_Text _title;

		[SerializeField]
		private TMP_Text _pot;

		[SerializeField]
		private GameObject _potGroup;

		[SerializeField]
		private TMP_Text _status;

		[SerializeField]
		private GameObject _statusBadge;

		[SerializeField]
		private TMP_Text _timer;

		[SerializeField]
		private Image _turnRing;

		[SerializeField]
		private RectTransform _board;

		[SerializeField]
		private GameObject _actions;

		[SerializeField]
		private Button _fold;

		[SerializeField]
		private Button _checkOrCall;

		[SerializeField]
		private Button _minRaise;

		[SerializeField]
		private Button _allIn;

		[SerializeField]
		private GameObject _preActions;

		[SerializeField]
		private Button _checkFold;

		[SerializeField]
		private Button _check;

		[SerializeField]
		private Button _callAny;

		[SerializeField]
		private CardView _cardTemplate;

		[SerializeField]
		private RectTransform _cardStorage;

		[SerializeField]
		private FlightLayer _flights;

		[SerializeField]
		private Sprite _chip;

		[SerializeField]
		private Sprite _cardBack;

		[SerializeField]
		private RectTransform _potTarget;

		[SerializeField]
		private RectTransform _deck;

		[SerializeField]
		private RectTransform _boardSlots;

		[SerializeField]
		private RectTransform _dealer;

		[SerializeField]
		private Sprite _glow;

		[SerializeField]
		private Sprite _spark;

		private readonly List<Flight> _plan = new();
		private readonly List<string> _highlight = new();
		private readonly List<CardView> _boardCards = new();
		private readonly List<CardView>[] _seatCards = CreateSeatLists<CardView>();
		private readonly bool[] _isDecorated = new bool[SeatLayout.MaxSeats];
		private readonly bool[] _isListed = new bool[SeatLayout.MaxSeats];
		private ViewPool<CardView> _cards;
		private TableView _table;
		private TMP_Text _checkOrCallLabel;
		private TMP_Text _minRaiseLabel;
		private TMP_Text _allInLabel;
		private TMP_Text _checkFoldLabel;
		private TMP_Text _checkLabel;
		private TMP_Text _callAnyLabel;
		private string _shownSeconds = "";
		private int _dealerSeat = -1;

		public event Action<ActionButton> ActionClicked;

		public event Action<PreAction> PreActionClicked;

		public void AttachTo(TableView table) => _table = table;

		public void DetachFromTable()
		{
			for (int seatIndex = 0; seatIndex < _isDecorated.Length; seatIndex++)
			{
				if (!_isDecorated[seatIndex]) continue;

				ClearSeat(seatIndex);
			}

			_flights.Stop();
			HideDealer();
			_table = null;
		}

		public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

		public void SetTitle(string title) => _title.text = title;

		public void SetPot(string pot)
		{
			_pot.text = pot;
			_potGroup.SetActive(pot.Length > 0);
		}

		public void SetStatus(string status)
		{
			_status.text = status;
			_statusBadge.SetActive(status.Length > 0);
		}

		public void SetTurnClock(int seatIndex, float remaining, string seconds)
		{
			_timer.text = seconds;
			bool isShown = seatIndex >= 0 && _table != null;
			_turnRing.gameObject.SetActive(isShown);
			if (!isShown) return;

			_turnRing.rectTransform.position = _table.AvatarSlot(seatIndex).position;
			_turnRing.fillAmount = remaining;
			_turnRing.color = Color.Lerp(RingEmpty, RingFull, remaining);
			if (seconds == _shownSeconds) return;

			_shownSeconds = seconds;
			if (seconds.Length != 1 || seconds[0] > '5') return;

			UiMotion.Punch(_turnRing.transform, 0.12f, 0.3f);
		}

		public void SetBoard(IReadOnlyList<string> cards)
		{
			int shownCount = _boardCards.Count;
			_cards.ReleaseAll(_boardCards);
			for (int i = 0; i < cards.Count; i++)
			{
				CardView card = _cards.Get(_board);
				if (i >= shownCount) card.Reveal(cards[i]); else card.Show(cards[i]);

				card.SetEmphasis(EmphasisFor(cards[i]));
				_boardCards.Add(card);
			}
		}

		// Called on every hand event, so it only touches what changed: card views stay in place (a face-down card that
		// gets a code flips where it is) and SeatWidget skips a decoration identical to the one it shows.
		public void SetSeats(IReadOnlyList<HandSeatLine> lines)
		{
			if (_table == null) return;

			Array.Clear(_isListed, 0, _isListed.Length);
			for (int i = 0; i < lines.Count; i++)
			{
				HandSeatLine line = lines[i];
				if (line.SeatIndex < 0 || line.SeatIndex >= _seatCards.Length) continue;

				_isListed[line.SeatIndex] = true;
				SyncSeatCards(line);
				_table.Decorate(line.SeatIndex, DecorFor(line));
				_isDecorated[line.SeatIndex] = true;
			}

			for (int seatIndex = 0; seatIndex < _isDecorated.Length; seatIndex++)
			{
				if (!_isDecorated[seatIndex]) continue;
				if (_isListed[seatIndex]) continue;

				ClearSeat(seatIndex);
			}

			PlaceDealer(lines);
		}

		public void ShowActions(ActionOptions options)
		{
			bool wasHidden = !_actions.activeSelf;
			_actions.SetActive(true);
			if (wasHidden) UiMotion.SlideIn((RectTransform)_actions.transform, Vector2.zero, new Vector2(0f, -220f));

			_checkOrCallLabel.text = options.CanCheck ? "Check" : "Call\n" + Amount(options.ToCall);
			_minRaiseLabel.text = "Raise to\n" + Amount(options.MinRaiseTo);
			_allInLabel.text = "All-in\n" + Amount(options.MaxRaiseTo);
			_minRaise.gameObject.SetActive(options.CanRaise);
			_allIn.gameObject.SetActive(options.CanRaise && options.MaxRaiseTo > options.MinRaiseTo);
		}

		public void HideActions() => _actions.SetActive(false);

		public void ShowPreActions(PreAction selected, bool canCheck)
		{
			bool wasHidden = !_preActions.activeSelf;
			_preActions.SetActive(true);
			if (wasHidden) UiMotion.SlideIn((RectTransform)_preActions.transform, Vector2.zero, new Vector2(0f, -160f));

			_checkFoldLabel.text = canCheck ? "Check/Fold" : "Fold";
			_check.gameObject.SetActive(canCheck);
			Paint(_checkFold, _checkFoldLabel, selected == PreAction.CheckFold);
			Paint(_check, _checkLabel, selected == PreAction.Check);
			Paint(_callAny, _callAnyLabel, selected == PreAction.CallAny);
		}

		public void HidePreActions() => _preActions.SetActive(false);

		public void HighlightCards(IReadOnlyList<string> codes)
		{
			_highlight.Clear();
			for (int i = 0; i < codes.Count; i++)
			{
				_highlight.Add(codes[i]);
			}

			foreach (CardView card in _boardCards)
			{
				card.SetEmphasis(EmphasisFor(card.Code));
			}

			foreach (List<CardView> seatCards in _seatCards)
			{
				foreach (CardView card in seatCards)
				{
					card.SetEmphasis(EmphasisFor(card.Code));
				}
			}
		}

		public async UniTask FlyChipsAsync(ChipRoute route, IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken)
		{
			if (_table == null) return;

			_plan.Clear();
			float chipSeconds = Mathf.Max(0.05f, seconds - StaggerSeconds * (ChipsPerSeat - 1));
			for (int i = 0; i < seats.Count; i++)
			{
				int seatIndex = seats[i];
				RectTransform avatar = _table.AvatarSlot(seatIndex);
				RectTransform bet = _table.BetSlot(seatIndex);
				Vector3 from = route switch
				{
					ChipRoute.AvatarToBet => avatar.position,
					ChipRoute.BetToPot => bet.position,
					_ => _potTarget.position,
				};
				Vector3 to = route switch
				{
					ChipRoute.AvatarToBet => bet.position,
					ChipRoute.BetToPot => _potTarget.position,
					_ => avatar.position,
				};
				for (int chip = 0; chip < ChipsPerSeat; chip++)
				{
					_plan.Add(Flight.Arc(_chip, bet.rect.size, from, to, chip * StaggerSeconds, chipSeconds));
				}
			}

			await _flights.FlyAsync(_plan, cancellationToken);
			if (this == null) return;
			if (_table == null) return;

			PunchLanding(route, seats);
		}

		public UniTask DealCardsAsync(IReadOnlyList<int> seats, float seconds, CancellationToken cancellationToken)
		{
			if (_table == null) return UniTask.CompletedTask;

			_plan.Clear();
			Vector2 cardSize = CardSize;
			float cardSeconds = Mathf.Max(0.05f, seconds - StaggerSeconds * (seats.Count - 1));
			for (int i = 0; i < seats.Count; i++)
			{
				RectTransform slot = _table.CardsSlot(seats[i]);
				Vector2 size = cardSize * (slot.lossyScale.x / _flights.transform.lossyScale.x);
				Vector3 offset = slot.TransformVector(new Vector3(cardSize.x * 0.55f, 0f, 0f));
				_plan.Add(Flight.Arc(_cardBack, size, _deck.position, slot.position - offset, i * StaggerSeconds, cardSeconds, 200f));
				_plan.Add(Flight.Arc(_cardBack, size, _deck.position, slot.position + offset, i * StaggerSeconds, cardSeconds, -160f));
			}

			return _flights.FlyAsync(_plan, cancellationToken);
		}

		public UniTask DealBoardCardAsync(int boardIndex, float seconds, CancellationToken cancellationToken)
		{
			if (boardIndex < 0 || boardIndex >= _boardSlots.childCount) return UniTask.CompletedTask;

			_plan.Clear();
			Transform slot = _boardSlots.GetChild(boardIndex);
			Vector2 size = CardSize * (_boardSlots.lossyScale.x / _flights.transform.lossyScale.x);
			_plan.Add(Flight.Arc(_cardBack, size, _deck.position, slot.position, 0f, seconds, 180f));
			return _flights.FlyAsync(_plan, cancellationToken);
		}

		public UniTask CelebrateAsync(IReadOnlyList<SeatWin> wins, float seconds, CancellationToken cancellationToken)
		{
			if (_table == null) return UniTask.CompletedTask;

			_plan.Clear();
			float scale = _flights.transform.lossyScale.x;
			for (int i = 0; i < wins.Count; i++)
			{
				SeatWin win = wins[i];
				RectTransform avatar = _table.AvatarSlot(win.SeatIndex);
				Vector3 at = avatar.position;
				_plan.Add(Flight.Glow(_glow, new Vector2(320f, 320f), at, 0f, seconds, RingFull));
				for (int spark = 0; spark < SparksPerWin; spark++)
				{
					float angle = (spark * 360f / SparksPerWin + 15f) * Mathf.Deg2Rad;
					float reach = (spark % 2 == 0 ? 190f : 130f) * scale;
					Vector3 to = at + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * reach;
					_plan.Add(Flight.Burst(_spark, new Vector2(48f, 48f), at, to, spark * 0.012f, seconds * 0.8f, SparkColor));
				}

				_plan.Add(Flight.Float("+" + Amount(win.Amount), at + Vector3.up * (70f * scale), 0.1f, seconds, RingFull));
				UiMotion.Punch(avatar.parent, 0.2f, 0.45f);
			}

			return _flights.FlyAsync(_plan, cancellationToken);
		}

		public void StopFlights() => _flights.Stop();

		private void SyncSeatCards(HandSeatLine line)
		{
			List<CardView> seatCards = _seatCards[line.SeatIndex];
			string[] codes = line.Cards;
			while (seatCards.Count > codes.Length)
			{
				int last = seatCards.Count - 1;
				_cards.Release(seatCards[last]);
				seatCards.RemoveAt(last);
			}

			for (int i = 0; i < codes.Length; i++)
			{
				string code = codes[i];
				if (i == seatCards.Count)
				{
					CardView added = _cards.Get(_table.CardsSlot(line.SeatIndex));
					added.Show(code);
					added.SetEmphasis(EmphasisFor(code));
					seatCards.Add(added);
					continue;
				}

				CardView card = seatCards[i];
				if (card.Code == code) continue;

				bool turnsFaceUp = card.Code.Length == 0 && code.Length >= 2;
				if (turnsFaceUp) card.Reveal(code); else card.Show(code);

				card.SetEmphasis(EmphasisFor(code));
			}
		}

		private static SeatDecor DecorFor(HandSeatLine line)
		{
			string bet = line.Bet > 0 ? ChipText.Format(line.Bet) : "";
			return new SeatDecor(ChipText.Format(line.Stack), bet, line.Bet, line.Action, line.Description, line.IsActing, line.IsFolded);
		}

		private void ClearSeat(int seatIndex)
		{
			_isDecorated[seatIndex] = false;
			_cards.ReleaseAll(_seatCards[seatIndex]);
			if (_table != null) _table.Decorate(seatIndex, SeatDecor.None);
		}

		private CardEmphasis EmphasisFor(string code)
		{
			if (_highlight.Count == 0) return CardEmphasis.Normal;

			return _highlight.Contains(code) ? CardEmphasis.Lifted : CardEmphasis.Dimmed;
		}

		private void PlaceDealer(IReadOnlyList<HandSeatLine> lines)
		{
			int buttonSeat = -1;
			for (int i = 0; i < lines.Count; i++)
			{
				HandSeatLine line = lines[i];
				if (!line.IsButton) continue;

				buttonSeat = line.SeatIndex;
				break;
			}

			if (buttonSeat < 0)
			{
				HideDealer();
				return;
			}

			Vector3 target = _table.DealerSlot(buttonSeat).position;
			if (buttonSeat == _dealerSeat)
			{
				if (!UiMotion.IsMoving(_dealer)) _dealer.position = target;

				return;
			}

			bool isFirstShow = _dealerSeat < 0;
			_dealerSeat = buttonSeat;
			_dealer.gameObject.SetActive(true);
			if (isFirstShow) _dealer.position = target; else UiMotion.MoveTo(_dealer, target);
		}

		private void HideDealer()
		{
			_dealerSeat = -1;
			_dealer.gameObject.SetActive(false);
		}

		private void PunchLanding(ChipRoute route, IReadOnlyList<int> seats)
		{
			if (route == ChipRoute.AvatarToBet) return;

			if (route == ChipRoute.BetToPot)
			{
				UiMotion.Punch(_potTarget);
				return;
			}

			for (int i = 0; i < seats.Count; i++)
			{
				UiMotion.Punch(_table.AvatarSlot(seats[i]).parent);
			}
		}

		private Vector2 CardSize => ((RectTransform)_cardTemplate.transform).sizeDelta;

		private CardView CreateCard(Transform parent) => Instantiate(_cardTemplate, parent);

		private void Awake()
		{
			_cards = new ViewPool<CardView>(CreateCard, _cardStorage);
			_checkOrCallLabel = LabelOf(_checkOrCall);
			_minRaiseLabel = LabelOf(_minRaise);
			_allInLabel = LabelOf(_allIn);
			_checkFoldLabel = LabelOf(_checkFold);
			_checkLabel = LabelOf(_check);
			_callAnyLabel = LabelOf(_callAny);
			_fold.onClick.AddListener(OnFoldClicked);
			_checkOrCall.onClick.AddListener(OnCheckOrCallClicked);
			_minRaise.onClick.AddListener(OnMinRaiseClicked);
			_allIn.onClick.AddListener(OnAllInClicked);
			_checkFold.onClick.AddListener(OnCheckFoldClicked);
			_check.onClick.AddListener(OnCheckClicked);
			_callAny.onClick.AddListener(OnCallAnyClicked);
		}

		private void OnDestroy()
		{
			_fold.onClick.RemoveListener(OnFoldClicked);
			_checkOrCall.onClick.RemoveListener(OnCheckOrCallClicked);
			_minRaise.onClick.RemoveListener(OnMinRaiseClicked);
			_allIn.onClick.RemoveListener(OnAllInClicked);
			_checkFold.onClick.RemoveListener(OnCheckFoldClicked);
			_check.onClick.RemoveListener(OnCheckClicked);
			_callAny.onClick.RemoveListener(OnCallAnyClicked);
		}

		private void OnFoldClicked() => ActionClicked?.Invoke(ActionButton.Fold);

		private void OnCheckOrCallClicked() => ActionClicked?.Invoke(ActionButton.CheckOrCall);

		private void OnMinRaiseClicked() => ActionClicked?.Invoke(ActionButton.MinRaise);

		private void OnAllInClicked() => ActionClicked?.Invoke(ActionButton.AllIn);

		private void OnCheckFoldClicked() => PreActionClicked?.Invoke(PreAction.CheckFold);

		private void OnCheckClicked() => PreActionClicked?.Invoke(PreAction.Check);

		private void OnCallAnyClicked() => PreActionClicked?.Invoke(PreAction.CallAny);

		private static string Amount(long chips) => ChipText.Format(chips);

		private static TMP_Text LabelOf(Button button) => button.GetComponentInChildren<TMP_Text>(true);

		private static void Paint(Button button, TMP_Text label, bool isChosen)
		{
			button.image.color = isChosen ? ChosenColor : ChoiceColor;
			label.color = isChosen ? ChosenText : Color.white;
		}

		private static List<T>[] CreateSeatLists<T>()
		{
			List<T>[] lists = new List<T>[SeatLayout.MaxSeats];
			for (int i = 0; i < lists.Length; i++)
			{
				lists[i] = new List<T>();
			}

			return lists;
		}
	}
}