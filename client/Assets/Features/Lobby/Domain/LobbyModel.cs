using System;
using System.Collections.Generic;
using Poker.Shared.Features.Lobby.Models;

namespace Poker.Client.Features.Lobby.Domain
{
	public sealed class LobbyModel
	{
		private TableSummary[] _tables = Array.Empty<TableSummary>();

		public event Action Changed;

		public IReadOnlyList<TableSummary> Tables => _tables;

		public void SetTables(TableSummary[] tables)
		{
			_tables = tables ?? throw new ArgumentNullException(nameof(tables));
			Changed?.Invoke();
		}
	}
}