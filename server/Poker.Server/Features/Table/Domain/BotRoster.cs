namespace Poker.Server.Features.Table.Domain
{
	public static class BotRoster
	{
		public static bool IsBot(string playerId) => playerId.StartsWith("bot-", System.StringComparison.Ordinal);
	}
}