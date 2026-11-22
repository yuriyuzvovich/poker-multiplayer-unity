using System.Collections.Generic;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Popups
{
	public sealed class PopupQueue
	{
		private readonly List<GameObject> _visible = new();
		private readonly List<GameObject> _waiting = new();

		public void Open(GameObject popup, PopupPriority priority)
		{
			if (priority == PopupPriority.Critical || _visible.Count == 0)
			{
				Show(popup);
				return;
			}

			popup.SetActive(false);
			_waiting.Add(popup);
		}

		public void Close(GameObject popup)
		{
			if (_waiting.Remove(popup)) return;
			if (!_visible.Remove(popup)) return;
			if (_visible.Count > 0) return;
			if (_waiting.Count == 0) return;

			GameObject next = _waiting[0];
			_waiting.RemoveAt(0);
			Show(next);
		}

		private void Show(GameObject popup)
		{
			popup.SetActive(true);
			popup.transform.SetAsLastSibling();
			_visible.Add(popup);
		}
	}
}