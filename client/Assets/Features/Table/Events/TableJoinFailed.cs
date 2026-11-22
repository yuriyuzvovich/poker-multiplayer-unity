namespace Poker.Client.Features.Table.Events
{
	public sealed class TableJoinFailed
	{
		public TableJoinFailed(string tableId, string reason)
		{
			TableId = tableId;
			Reason = reason;
		}

		public string TableId { get; }
		public string Reason { get; }
	}
}