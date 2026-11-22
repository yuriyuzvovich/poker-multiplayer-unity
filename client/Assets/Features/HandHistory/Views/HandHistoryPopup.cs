using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Popups;

namespace Poker.Client.Features.HandHistory.Views
{
	public sealed class HandHistoryPopup : Popup<bool>
	{
		public const string Address = "ui/hand-history-popup";

		[SerializeField]
		private RectTransform _list;

		[SerializeField]
		private TMP_Text _rowTemplate;

		[SerializeField]
		private TMP_Text _empty;

		[SerializeField]
		private Button _close;

		public void Setup(IReadOnlyList<string> rows)
		{
			_empty.gameObject.SetActive(rows.Count == 0);
			for (int i = 0; i < rows.Count; i++)
			{
				TMP_Text row = Instantiate(_rowTemplate, _list);
				row.gameObject.SetActive(true);
				row.text = rows[i];
			}
		}

		private void Awake() => _close.onClick.AddListener(OnCloseClicked);

		private void OnDestroy() => _close.onClick.RemoveListener(OnCloseClicked);

		private void OnCloseClicked() => Close(true);
	}
}