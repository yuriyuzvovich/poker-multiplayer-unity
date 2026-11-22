using System.Collections.Generic;
using Poker.Server.Features.Hand.Domain;
using Poker.Shared.Rules.Cards;

namespace Poker.Tests.TestDoubles
{
	public static class ScriptedDeck
	{
		public static Deck Create(string firstCards)
		{
			List<Card> cards = new();
			foreach (string code in firstCards.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
			{
				cards.Add(Card.Parse(code));
			}

			foreach (Card card in StandardDeck.CreateOrdered())
			{
				if (cards.Contains(card)) continue;

				cards.Add(card);
			}

			return new Deck(cards.ToArray());
		}
	}
}