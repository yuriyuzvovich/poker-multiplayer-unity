namespace Poker.Server.Infrastructure.Session
{
	public interface IPlayerLocator
	{
		public string FindSeatedTableId(string playerId);
	}
}