using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Poker.Shared.Rules.Cards;
using Poker.Shared.Rules.Hands;

namespace Poker.Tests.Shared.Rules
{
	public sealed class HandEvaluatorTests
	{
		[Theory]
		[InlineData("As Ks Qs Js Ts", HandCategory.StraightFlush)]
		[InlineData("5d 4d 3d 2d Ad", HandCategory.StraightFlush)]
		[InlineData("9c 9d 9h 9s 2c", HandCategory.FourOfAKind)]
		[InlineData("Kc Kd Kh 3s 3c", HandCategory.FullHouse)]
		[InlineData("Ah 9h 7h 4h 2h", HandCategory.Flush)]
		[InlineData("Tc 9d 8h 7s 6c", HandCategory.Straight)]
		[InlineData("5c 4d 3h 2s Ac", HandCategory.Straight)]
		[InlineData("Qc Qd Qh 7s 2c", HandCategory.ThreeOfAKind)]
		[InlineData("Jc Jd 4h 4s Ac", HandCategory.TwoPair)]
		[InlineData("Tc Td 8h 4s 2c", HandCategory.OnePair)]
		[InlineData("Ac Jd 8h 4s 2c", HandCategory.HighCard)]
		public void Evaluate_FiveCards_FindsTheCategory(string cards, HandCategory expected)
		{
			Assert.Equal(expected, HandEvaluator.Evaluate(Parse(cards)).Category);
		}

		[Theory]
		[InlineData("As Ks Qs Js Ts", "Ks Qs Js Ts 9s")]
		[InlineData("6c 5d 4h 3s 2c", "5c 4d 3h 2s Ac")]
		[InlineData("Ac Ad 9h 9s 2c", "Kc Kd Qh Qs Jc")]
		[InlineData("Ac Ad 9h 9s 3c", "Ah As 9c 9d 2c")]
		[InlineData("Kc Kd Kh 2s 2c", "Qc Qd Qh As Ac")]
		[InlineData("Tc Td 8h 4s 3c", "Th Ts 8c 4d 2c")]
		[InlineData("2c 3c 4c 5c 7c", "As Kd Qh Js 9c")]
		public void Evaluate_StrongerHandScoresHigher(string stronger, string weaker)
		{
			Assert.True(HandEvaluator.Evaluate(Parse(stronger)) > HandEvaluator.Evaluate(Parse(weaker)));
		}

		[Fact]
		public void Evaluate_SevenCards_PicksTheBestFive()
		{
			HandValue value = HandEvaluator.Evaluate(Parse("2c 7d Ah Kh Qh Jh Th"));

			Assert.Equal(HandCategory.StraightFlush, value.Category);
		}

		[Fact]
		public void Evaluate_WithBestFive_ReportsTheFiveCardsThatMakeTheHand()
		{
			Card[] bestFive = new Card[5];

			HandValue value = HandEvaluator.Evaluate(Parse("2c 7d Ah Kh Qh Jh Th"), bestFive);

			Assert.Equal(HandCategory.StraightFlush, value.Category);
			Assert.Equal(Parse("Ah Kh Qh Jh Th"), bestFive.OrderByDescending(card => card.Rank));
		}

		[Fact]
		public void Evaluate_WithBestFive_KeepsTheKickerThatPlays()
		{
			Card[] bestFive = new Card[5];

			HandEvaluator.Evaluate(Parse("Ac 9d Kh Ks 7c 4d 2h"), bestFive);

			Assert.Equal(Parse("Ac Kh Ks 9d 7c"), bestFive.OrderByDescending(card => card.Rank).ThenBy(card => card.Suit));
		}

		[Fact]
		public void Evaluate_BoardPlays_IsATie()
		{
			HandValue first = HandEvaluator.Evaluate(Parse("2c 3d Ah Kd Qs Jc Th"));
			HandValue second = HandEvaluator.Evaluate(Parse("4c 5d Ah Kd Qs Jc Th"));

			Assert.Equal(first, second);
		}

		[Fact]
		public void Evaluate_KickerDecides()
		{
			HandValue aceKicker = HandEvaluator.Evaluate(Parse("Ac 9d Kh Ks 7c 4d 2h"));
			HandValue queenKicker = HandEvaluator.Evaluate(Parse("Qc 9d Kh Ks 7c 4d 2h"));

			Assert.True(aceKicker > queenKicker);
		}

		[Theory]
		[InlineData(4)]
		[InlineData(8)]
		public void Evaluate_WrongCardCount_Throws(int count)
		{
			List<Card> cards = new(StandardDeck.CreateOrdered()[..count]);

			Assert.Throws<ArgumentException>(() => HandEvaluator.Evaluate(cards));
		}

		[Fact]
		public void Card_RoundTripsItsCode_AndTheDeckIsUnique()
		{
			HashSet<string> codes = new();
			foreach (Card card in StandardDeck.CreateOrdered())
			{
				Assert.Equal(card, Card.Parse(card.ToString()));
				Assert.True(codes.Add(card.ToString()));
			}

			Assert.Equal(52, codes.Count);
			Assert.False(Card.TryParse("1x", out Card _));
		}

		private static List<Card> Parse(string cards)
		{
			List<Card> parsed = new();
			foreach (string code in cards.Split(' '))
			{
				parsed.Add(Card.Parse(code));
			}

			return parsed;
		}
	}
}