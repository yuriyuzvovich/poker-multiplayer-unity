namespace Poker.Client.Infrastructure.Core
{
	public enum AppState
	{
		Booting = 0,
		Connecting = 1,
		Connected = 2,
		Disconnected = 3,
		Failed = 4,
	}
}