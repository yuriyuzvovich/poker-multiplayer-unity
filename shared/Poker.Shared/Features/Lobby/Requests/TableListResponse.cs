using System;
using Poker.Shared.Features.Lobby.Models;

namespace Poker.Shared.Features.Lobby.Requests
{
	public sealed class TableListResponse
	{
		public TableSummary[] Tables { get; set; } = Array.Empty<TableSummary>();
	}
}