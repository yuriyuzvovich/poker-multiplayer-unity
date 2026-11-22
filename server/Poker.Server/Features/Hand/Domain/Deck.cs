using System;
using Poker.Shared.Rules.Cards;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed class Deck
	{
		private readonly Card[] _cards;
		private int _next;

		public Deck(Card[] cards) => _cards = cards ?? throw new ArgumentNullException(nameof(cards));

		public int Remaining => _cards.Length - _next;

		public Card Draw()
		{
			if (_next >= _cards.Length) throw new InvalidOperationException("The deck is empty.");

			return _cards[_next++];
		}
	}
}