using System.Collections.Generic;
using Poker.Shared.Rules.Cards;
using Poker.Shared.Rules.Hands;

namespace Poker.Client.Features.Hand.Presentation.Scene
{
	// Reads the cards on the table with the shared evaluator: what a player holds with the board, and which five
	// cards made a winning hand. Reuses its buffers, so reading on every street allocates only the description.
	public sealed class HandReader
	{
		private readonly List<Card> _cards = new(7);
		private readonly Card[] _bestFive = new Card[5];

		public string Describe(string[] holeCards, IReadOnlyList<string> board) => TryCollect(holeCards, board) ? HandEvaluator.Evaluate(_cards).ToString() : "";

		public void AddBestFive(string[] holeCards, IReadOnlyList<string> board, List<string> destination)
		{
			if (!TryCollect(holeCards, board)) return;

			HandEvaluator.Evaluate(_cards, _bestFive);
			foreach (Card card in _bestFive)
			{
				string code = card.ToString();
				if (destination.Contains(code)) continue;

				destination.Add(code);
			}
		}

		private bool TryCollect(string[] holeCards, IReadOnlyList<string> board)
		{
			if (board.Count < 3) return false;

			_cards.Clear();
			foreach (string code in holeCards)
			{
				if (!Card.TryParse(code, out Card card)) return false;

				_cards.Add(card);
			}

			for (int i = 0; i < board.Count; i++)
			{
				if (!Card.TryParse(board[i], out Card card)) return false;

				_cards.Add(card);
			}

			return true;
		}
	}
}