using System;
using Poker.Shared.Rules.Cards;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed class SeededDeckFactory : IDeckFactory
	{
		private readonly Random _random;

		public SeededDeckFactory(int seed) => _random = new Random(seed);

		public Deck CreateShuffled()
		{
			Card[] cards = StandardDeck.CreateOrdered();
			for (int i = cards.Length - 1; i > 0; i--)
			{
				int j = _random.Next(i + 1);
				(cards[i], cards[j]) = (cards[j], cards[i]);
			}

			return new Deck(cards);
		}
	}
}