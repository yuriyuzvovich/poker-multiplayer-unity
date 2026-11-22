using System;
using System.Collections.Generic;

namespace Poker.Client.Features.Lobby.Views
{
	public interface ILobbyView
	{
		public event Action<string> TableClicked;

		public void SetVisible(bool isVisible);

		public void SetStatus(string status);

		public void ShowTables(IReadOnlyList<LobbyEntry> entries);
	}
}