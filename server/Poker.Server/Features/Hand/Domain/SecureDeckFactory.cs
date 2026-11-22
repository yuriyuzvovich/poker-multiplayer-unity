using System.Security.Cryptography;
using Poker.Shared.Rules.Cards;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed class SecureDeckFactory : IDeckFactory
	{
		public Deck CreateShuffled()
		{
			Card[] cards = StandardDeck.CreateOrdered();
			for (int i = cards.Length - 1; i > 0; i--)
			{
				int j = RandomNumberGenerator.GetInt32(i + 1);
				(cards[i], cards[j]) = (cards[j], cards[i]);
			}

			return new Deck(cards);
		}
	}
}