namespace Poker.Client.Features.Table.Events
{
	public sealed class JoinTableRequested
	{
		public JoinTableRequested(string tableId) => TableId = tableId;

		public string TableId { get; }
	}
}