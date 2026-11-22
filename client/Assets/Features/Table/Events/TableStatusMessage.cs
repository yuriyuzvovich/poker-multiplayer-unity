namespace Poker.Client.Features.Table.Events
{
	public sealed class TableStatusMessage
	{
		public TableStatusMessage(string text) => Text = text;

		public string Text { get; }
	}
}