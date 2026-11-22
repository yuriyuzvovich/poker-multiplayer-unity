namespace Poker.Client.Features.Table.Events
{
	public sealed class TableJoined
	{
		public TableJoined(string tableId) => TableId = tableId;

		public string TableId { get; }
	}
}