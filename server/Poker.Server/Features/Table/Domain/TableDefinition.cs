using System;

namespace Poker.Server.Features.Table.Domain
{
	public sealed record TableDefinition(
		string TableId,
		string Name,
		int SeatCount,
		long SmallBlind,
		long BigBlind,
		long MinBuyIn,
		long MaxBuyIn,
		TimeSpan ReservationTime);
}