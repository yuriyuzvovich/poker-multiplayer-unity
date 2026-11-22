using System;
using System.Collections.Generic;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Features.Lobby.Presentation
{
	public sealed class LobbyScreen : LazyView<LobbyView>, ILobbyView
	{
		private readonly UiLayers _layers;
		private IReadOnlyList<LobbyEntry> _entries = Array.Empty<LobbyEntry>();
		private string _status = "";

		public LobbyScreen(ViewLoader loader, UiLayers layers) : base(loader, LobbyView.Address) => _layers = layers ?? throw new ArgumentNullException(nameof(layers));

		public event Action<string> TableClicked;

		public void SetVisible(bool isVisible)
		{
			if (isVisible) Open(_layers.Screens); else Close();
		}

		public void SetStatus(string status)
		{
			_status = status;
			Current?.SetStatus(status);
		}

		public void ShowTables(IReadOnlyList<LobbyEntry> entries)
		{
			_entries = entries;
			Current?.ShowTables(entries);
		}

		protected override void OnAttached(LobbyView view)
		{
			view.TableClicked += OnTableClicked;
			view.SetStatus(_status);
			view.ShowTables(_entries);
		}

		protected override void OnDetaching(LobbyView view) => view.TableClicked -= OnTableClicked;

		private void OnTableClicked(string tableId) => TableClicked?.Invoke(tableId);
	}
}