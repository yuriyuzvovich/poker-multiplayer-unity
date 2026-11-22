using System;
using Poker.Shared.Features.Table.Models;

namespace Poker.Shared.Features.Table.Requests
{
	public sealed class TableSnapshot
	{
		public string TableId { get; set; } = "";
		public string Name { get; set; } = "";
		public long SmallBlind { get; set; }
		public long BigBlind { get; set; }
		public long MinBuyIn { get; set; }
		public long MaxBuyIn { get; set; }
		public SeatSnapshot[] Seats { get; set; } = Array.Empty<SeatSnapshot>();
	}
}