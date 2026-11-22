using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Features.Table.Views.Seats;

namespace Poker.Client.Features.Table.Views
{
	public sealed class TableView : MonoBehaviour
	{
		public const string Address = "ui/table-screen";

		[SerializeField]
		private Button _backButton;

		[SerializeField]
		private TMP_Text _title;

		[SerializeField]
		private TMP_Text _status;

		[SerializeField]
		private RectTransform _layerSlot;

		[SerializeField]
		private SeatWidget[] _seats = Array.Empty<SeatWidget>();

		[SerializeField]
		private Sprite[] _avatars = Array.Empty<Sprite>();

		[SerializeField]
		private RectTransform _toolbar;

		[SerializeField]
		private Button _toolbarButtonTemplate;

		private readonly List<Button> _toolbarButtons = new();

		public event Action BackClicked;
		public event Action<int> SeatActionClicked;

		public RectTransform LayerSlot => _layerSlot;

		public void AddToolbarButton(string buttonName, string label, Action click)
		{
			Button button = Instantiate(_toolbarButtonTemplate, _toolbar);
			button.name = buttonName;
			button.GetComponentInChildren<TMP_Text>(true).text = label;
			button.onClick.AddListener(click.Invoke);
			button.gameObject.SetActive(true);
			_toolbarButtons.Add(button);
		}

		public void SetTitle(string title) => _title.text = title;

		public void SetStatus(string status) => _status.text = status;

		public void ShowSeats(IReadOnlyList<SeatRow> rows, int heroSeat)
		{
			for (int i = 0; i < _seats.Length; i++)
			{
				SeatWidget seat = _seats[i];
				bool isUsed = i < rows.Count;
				seat.gameObject.SetActive(isUsed);
				if (!isUsed) continue;

				SeatRow row = rows[i];
				bool isMine = i == heroSeat;
				seat.Render(row, AvatarFor(row.Name), isMine);
				seat.Place(SeatLayout.AnchorFor(i, rows.Count), isMine);
			}
		}

		public void Decorate(int seatIndex, SeatDecor decor)
		{
			if (seatIndex < 0 || seatIndex >= _seats.Length) return;

			_seats[seatIndex].Decorate(decor);
		}

		public RectTransform CardsSlot(int seatIndex) => _seats[seatIndex].CardsSlot;

		public RectTransform AvatarSlot(int seatIndex) => _seats[seatIndex].AvatarSlot;

		public RectTransform BetSlot(int seatIndex) => _seats[seatIndex].BetSlot;

		public RectTransform DealerSlot(int seatIndex) => _seats[seatIndex].DealerSlot;

		private void Awake()
		{
			_backButton.onClick.AddListener(OnBackClicked);
			foreach (SeatWidget seat in _seats)
			{
				seat.ActionClicked += OnSeatActionClicked;
			}
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveListener(OnBackClicked);
			foreach (Button button in _toolbarButtons)
			{
				if (button == null) continue;

				button.onClick.RemoveAllListeners();
			}

			foreach (SeatWidget seat in _seats)
			{
				if (seat == null) continue;

				seat.ActionClicked -= OnSeatActionClicked;
			}
		}

		private void OnBackClicked() => BackClicked?.Invoke();

		private void OnSeatActionClicked(int seatIndex) => SeatActionClicked?.Invoke(seatIndex);

		private Sprite AvatarFor(string name)
		{
			if (name.Length == 0) return null;
			if (_avatars.Length == 0) return null;

			int hash = 0;
			foreach (char letter in name)
			{
				hash = hash * 31 + letter;
			}

			return _avatars[(hash & 0x7FFFFFFF) % _avatars.Length];
		}
	}
}