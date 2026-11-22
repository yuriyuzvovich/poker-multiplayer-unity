using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Lobby.Views
{
	public sealed class LobbyView : MonoBehaviour
	{
		public const string Address = "ui/lobby-screen";

		[SerializeField]
		private TMP_Text _status;

		[SerializeField]
		private RectTransform _list;

		[SerializeField]
		private LobbyEntryView _entryTemplate;

		private readonly List<LobbyEntryView> _entries = new();

		public event Action<string> TableClicked;

		public void SetStatus(string status) => _status.text = status;

		public void ShowTables(IReadOnlyList<LobbyEntry> entries)
		{
			ClearTables();
			for (int i = 0; i < entries.Count; i++)
			{
				LobbyEntry entry = entries[i];
				LobbyEntryView view = Instantiate(_entryTemplate, _list);
				view.name = "Table_" + entry.TableId;
				view.Show(entry);
				view.gameObject.SetActive(true);
				string tableId = entry.TableId;
				view.Button.onClick.AddListener(() => TableClicked?.Invoke(tableId));
				_entries.Add(view);
			}
		}

		private void ClearTables()
		{
			foreach (LobbyEntryView view in _entries)
			{
				view.Button.onClick.RemoveAllListeners();
				Destroy(view.gameObject);
			}

			_entries.Clear();
		}

		private void OnDestroy() => ClearTables();
	}
}