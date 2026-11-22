namespace Poker.Server.Features.Table.Domain
{
	public sealed class UnlimitedChipBank : IChipBank
	{
		public bool TryWithdraw(string playerId, long amount) => true;

		public void Deposit(string playerId, long amount) { }
	}
}