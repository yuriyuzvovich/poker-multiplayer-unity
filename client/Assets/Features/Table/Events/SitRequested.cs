namespace Poker.Client.Features.Table.Events
{
	public sealed class SitRequested
	{
		public SitRequested(string tableId, int seatIndex)
		{
			TableId = tableId;
			SeatIndex = seatIndex;
		}

		public string TableId { get; }
		public int SeatIndex { get; }
	}
}