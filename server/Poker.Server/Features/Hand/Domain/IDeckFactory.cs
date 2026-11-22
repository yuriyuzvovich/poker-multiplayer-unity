namespace Poker.Server.Features.Hand.Domain
{
	public interface IDeckFactory
	{
		public Deck CreateShuffled();
	}
}