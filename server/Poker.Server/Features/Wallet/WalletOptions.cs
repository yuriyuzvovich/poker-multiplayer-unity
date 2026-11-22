namespace Poker.Server.Features.Wallet
{
	public sealed class WalletOptions
	{
		public long StartingBalance { get; init; } = 5000;
		public long FreeChipsBelow { get; init; } = 1000;
		public long FreeChipsUpTo { get; init; } = 5000;
	}
}