namespace Poker.Client.Features.Lobby.Views
{
	public readonly struct LobbyEntry
	{
		public LobbyEntry(string tableId, string name, string blinds, string seats)
		{
			TableId = tableId;
			Name = name;
			Blinds = blinds;
			Seats = seats;
		}

		public string TableId { get; }
		public string Name { get; }
		public string Blinds { get; }
		public string Seats { get; }
	}
}