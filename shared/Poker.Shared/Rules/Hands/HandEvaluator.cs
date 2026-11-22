using System;
using System.Collections.Generic;
using Poker.Shared.Rules.Cards;

namespace Poker.Shared.Rules.Hands
{
	public static class HandEvaluator
	{
		public static HandValue Evaluate(IReadOnlyList<Card> cards) => Evaluate(cards, null);

		public static HandValue Evaluate(IReadOnlyList<Card> cards, Card[]? bestFive)
		{
			if (cards == null) throw new ArgumentNullException(nameof(cards));
			if (cards.Count < 5 || cards.Count > 7) throw new ArgumentException("Between 5 and 7 cards are required.", nameof(cards));
			if (bestFive != null && bestFive.Length != 5) throw new ArgumentException("The best five need an array of five cards.", nameof(bestFive));

			Card[] five = new Card[5];
			HandValue best = new(-1);
			int count = cards.Count;
			for (int a = 0; a < count - 4; a++)
			{
				for (int b = a + 1; b < count - 3; b++)
				{
					for (int c = b + 1; c < count - 2; c++)
					{
						for (int d = c + 1; d < count - 1; d++)
						{
							for (int e = d + 1; e < count; e++)
							{
								five[0] = cards[a];
								five[1] = cards[b];
								five[2] = cards[c];
								five[3] = cards[d];
								five[4] = cards[e];
								HandValue value = EvaluateFive(five);
								if (value.Score <= best.Score) continue;

								best = value;
								if (bestFive != null) Array.Copy(five, bestFive, 5);
							}
						}
					}
				}
			}

			return best;
		}

		private static HandValue EvaluateFive(Card[] five)
		{
			int[] counts = new int[15];
			bool isFlush = true;
			foreach (Card card in five)
			{
				counts[(int)card.Rank]++;
				if (card.Suit != five[0].Suit) isFlush = false;
			}

			int straightHigh = FindStraightHigh(counts);
			if (isFlush && straightHigh > 0) return Score(HandCategory.StraightFlush, straightHigh);

			int[] ordered = OrderByCountThenRank(counts, out int topCount, out int secondCount);
			if (topCount == 4) return Score(HandCategory.FourOfAKind, ordered);
			if (topCount == 3 && secondCount == 2) return Score(HandCategory.FullHouse, ordered);
			if (isFlush) return Score(HandCategory.Flush, ordered);
			if (straightHigh > 0) return Score(HandCategory.Straight, straightHigh);
			if (topCount == 3) return Score(HandCategory.ThreeOfAKind, ordered);
			if (topCount == 2 && secondCount == 2) return Score(HandCategory.TwoPair, ordered);
			if (topCount == 2) return Score(HandCategory.OnePair, ordered);

			return Score(HandCategory.HighCard, ordered);
		}

		private static int FindStraightHigh(int[] counts)
		{
			for (int high = 14; high >= 6; high--)
			{
				if (counts[high] == 1 && counts[high - 1] == 1 && counts[high - 2] == 1 && counts[high - 3] == 1 && counts[high - 4] == 1) return high;
			}

			bool isWheel = counts[14] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1;
			return isWheel ? 5 : 0;
		}

		private static int[] OrderByCountThenRank(int[] counts, out int topCount, out int secondCount)
		{
			int[] ordered = new int[5];
			int filled = 0;
			for (int count = 4; count >= 1; count--)
			{
				for (int rank = 14; rank >= 2; rank--)
				{
					if (counts[rank] != count) continue;

					ordered[filled++] = rank;
				}
			}

			topCount = counts[ordered[0]];
			secondCount = filled > 1 ? counts[ordered[1]] : 0;
			return ordered;
		}

		private static HandValue Score(HandCategory category, int highRank) => new HandValue((int)category << 20 | highRank << 16);

		private static HandValue Score(HandCategory category, int[] ordered) =>
			new HandValue((int)category << 20 | ordered[0] << 16 | ordered[1] << 12 | ordered[2] << 8 | ordered[3] << 4 | ordered[4]);
	}
}