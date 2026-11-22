using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Lobby.Views
{
	public sealed class LobbyEntryView : MonoBehaviour
	{
		[SerializeField]
		private Button _button;

		[SerializeField]
		private TMP_Text _name;

		[SerializeField]
		private TMP_Text _blinds;

		[SerializeField]
		private TMP_Text _seats;

		public Button Button => _button;

		public void Show(LobbyEntry entry)
		{
			_name.text = entry.Name;
			_blinds.text = entry.Blinds;
			_seats.text = entry.Seats;
		}
	}
}