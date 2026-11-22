using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Popups;

namespace Poker.Client.Features.Seating.Views
{
	public sealed class BuyInPopup : Popup<long>
	{
		public const string Address = "ui/buy-in-popup";
		public const long Cancelled = 0;

		[SerializeField]
		private TMP_Text _info;

		[SerializeField]
		private Button _min;

		[SerializeField]
		private Button _mid;

		[SerializeField]
		private Button _max;

		[SerializeField]
		private Button _cancel;

		private long _minAmount;
		private long _midAmount;
		private long _maxAmount;

		public void Setup(long min, long max, long secondsToDecide)
		{
			_minAmount = min;
			_maxAmount = max;
			_midAmount = (min + max) / 2;
			_info.text = "Choose your stack. The seat is held for about " + secondsToDecide + " s.";
			SetLabel(_min, "Min  " + _minAmount);
			SetLabel(_mid, "Mid  " + _midAmount);
			SetLabel(_max, "Max  " + _maxAmount);
		}

		private void Awake()
		{
			_min.onClick.AddListener(OnMinClicked);
			_mid.onClick.AddListener(OnMidClicked);
			_max.onClick.AddListener(OnMaxClicked);
			_cancel.onClick.AddListener(OnCancelClicked);
		}

		private void OnDestroy()
		{
			_min.onClick.RemoveListener(OnMinClicked);
			_mid.onClick.RemoveListener(OnMidClicked);
			_max.onClick.RemoveListener(OnMaxClicked);
			_cancel.onClick.RemoveListener(OnCancelClicked);
		}

		private void OnMinClicked() => Close(_minAmount);

		private void OnMidClicked() => Close(_midAmount);

		private void OnMaxClicked() => Close(_maxAmount);

		private void OnCancelClicked() => Close(Cancelled);

		private static void SetLabel(Button button, string label) => button.GetComponentInChildren<TMP_Text>(true).text = label;
	}
}