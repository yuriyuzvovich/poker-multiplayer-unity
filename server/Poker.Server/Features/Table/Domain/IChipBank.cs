namespace Poker.Server.Features.Table.Domain
{
	public interface IChipBank
	{
		public bool TryWithdraw(string playerId, long amount);

		public void Deposit(string playerId, long amount);
	}
}