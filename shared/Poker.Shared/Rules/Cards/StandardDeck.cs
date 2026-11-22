namespace Poker.Shared.Rules.Cards
{
	public static class StandardDeck
	{
		public const int Size = 52;

		public static Card[] CreateOrdered()
		{
			Card[] cards = new Card[Size];
			int index = 0;
			for (int suit = 0; suit < 4; suit++)
			{
				for (int rank = 2; rank <= 14; rank++)
				{
					cards[index++] = new Card((Rank)rank, (Suit)suit);
				}
			}

			return cards;
		}
	}
}