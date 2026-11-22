namespace Poker.Client.Features.Table.Views.Seats
{
	public readonly struct SeatRow
	{
		public SeatRow(string name, string stack, string badge, string actionLabel)
		{
			Name = name;
			Stack = stack;
			Badge = badge;
			ActionLabel = actionLabel;
		}

		public string Name { get; }
		public string Stack { get; }
		public string Badge { get; }
		public string ActionLabel { get; }
	}
}