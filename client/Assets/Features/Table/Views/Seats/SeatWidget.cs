using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Table.Views.Seats
{
	public sealed class SeatWidget : MonoBehaviour
	{
		private static readonly Color IdleRing = Color.white;
		private static readonly Color ActingRing = new(0.98f, 0.78f, 0.22f);

		[SerializeField]
		private int _seatIndex;

		[SerializeField]
		private CanvasGroup _group;

		[SerializeField]
		private Image _ring;

		[SerializeField]
		private Image _avatar;

		[SerializeField]
		private GameObject _youTag;

		[SerializeField]
		private Button _emptyMark;

		[SerializeField]
		private GameObject _info;

		[SerializeField]
		private TMP_Text _name;

		[SerializeField]
		private TMP_Text _stack;

		[SerializeField]
		private TMP_Text _badge;

		[SerializeField]
		private TMP_Text _bubble;

		[SerializeField]
		private GameObject _bubblePill;

		[SerializeField]
		private TMP_Text _note;

		[SerializeField]
		private GameObject _notePill;

		[SerializeField]
		private RectTransform _betGroup;

		[SerializeField]
		private ChipStackView _betStack;

		[SerializeField]
		private TMP_Text _bet;

		[SerializeField]
		private RectTransform _dealerSlot;

		[SerializeField]
		private RectTransform _cards;

		[SerializeField]
		private Button _action;

		[SerializeField]
		private TMP_Text _actionLabel;

		private string _tableStack = "";
		private bool _isEmpty;
		private SeatDecor _decor = SeatDecor.None;

		public event Action<int> ActionClicked;

		public int SeatIndex => _seatIndex;

		public RectTransform CardsSlot => _cards;

		public RectTransform AvatarSlot => _avatar.rectTransform;

		public RectTransform BetSlot => (RectTransform)_betStack.transform;

		public RectTransform DealerSlot => _dealerSlot;

		public void Render(SeatRow row, Sprite avatar, bool isMine)
		{
			_isEmpty = row.Name.Length == 0;
			bool canAct = row.ActionLabel.Length > 0;
			_avatar.gameObject.SetActive(!_isEmpty);
			_ring.gameObject.SetActive(!_isEmpty);
			_info.SetActive(!_isEmpty);
			_emptyMark.gameObject.SetActive(_isEmpty && canAct);
			_avatar.sprite = avatar;
			_youTag.SetActive(isMine);
			_name.text = row.Name;
			_tableStack = row.Stack;
			_stack.text = row.Stack;
			_badge.text = row.Badge;
			_action.gameObject.SetActive(canAct);
			_actionLabel.text = row.ActionLabel;
			// A seat that empties forgets its decoration so the next player does not inherit it; otherwise the
			// decoration a slice set stays on top of the table values just written.
			if (_isEmpty) _decor = SeatDecor.None;

			ShowDecor(_decor);
		}

		// Slices call this on every event, so an unchanged decoration is skipped instead of rewriting texts and layouts.
		public void Decorate(SeatDecor decor)
		{
			SeatDecor shown = !_isEmpty ? decor : SeatDecor.None;
			if (shown.Equals(_decor)) return;

			_decor = shown;
			ShowDecor(shown);
		}

		public void Place(Vector2 anchor, bool isMine)
		{
			RectTransform rect = (RectTransform)transform;
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.anchoredPosition = Vector2.zero;
			Vector2 inward = SeatLayout.TowardCenter(anchor);
			bool isSide = Mathf.Abs(inward.x) > 0.3f;
			float cardsSide = inward.x < -0.3f ? -1f : 1f;
			_cards.anchoredPosition = isMine ? new Vector2(cardsSide * 165f, 55f) : new Vector2(cardsSide * 105f, 30f);
			_cards.localScale = isMine ? new Vector3(1.5f, 1.5f, 1f) : new Vector3(0.8f, 0.8f, 1f);
			_betGroup.anchoredPosition = isSide ? new Vector2(cardsSide * 215f, -60f) : inward * 190f;
			_dealerSlot.anchoredPosition = new Vector2(-cardsSide * 82f, 92f);
		}

		private void ShowDecor(SeatDecor decor)
		{
			_stack.text = decor.Stack.Length > 0 ? decor.Stack : _tableStack;
			_bet.text = decor.Bet;
			_betGroup.gameObject.SetActive(decor.Bet.Length > 0);
			_betStack.Show(decor.BetChips);
			_bubble.text = decor.Bubble;
			_bubblePill.SetActive(decor.Bubble.Length > 0);
			_note.text = decor.Note;
			_notePill.SetActive(decor.Note.Length > 0);
			_ring.color = decor.IsActing ? ActingRing : IdleRing;
			_group.alpha = decor.IsDimmed ? 0.45f : 1f;
		}

		private void Awake()
		{
			_action.onClick.AddListener(OnActionClicked);
			_emptyMark.onClick.AddListener(OnActionClicked);
		}

		private void OnDestroy()
		{
			_action.onClick.RemoveListener(OnActionClicked);
			_emptyMark.onClick.RemoveListener(OnActionClicked);
		}

		private void OnActionClicked() => ActionClicked?.Invoke(_seatIndex);
	}
}