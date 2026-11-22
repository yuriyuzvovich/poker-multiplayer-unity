using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Infrastructure.UI.Popups
{
	public sealed class NoticePopup : Popup<bool>
	{
		public const string Address = "ui/notice-popup";

		[SerializeField]
		private TMP_Text _title;

		[SerializeField]
		private TMP_Text _message;

		[SerializeField]
		private Button _action;

		public void Setup(string title, string message, string actionLabel)
		{
			_title.text = title;
			_message.text = message;
			_action.GetComponentInChildren<TMP_Text>(true).text = actionLabel;
		}

		private void Awake() => _action.onClick.AddListener(OnActionClicked);

		private void OnDestroy() => _action.onClick.RemoveListener(OnActionClicked);

		private void OnActionClicked() => Close(true);
	}
}