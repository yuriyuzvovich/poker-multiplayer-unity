using NUnit.Framework;
using Poker.Client.Features.HandHistory.Presentation;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.HandHistory.Models;

namespace Poker.Client.Tests.EditMode.HandHistory
{
	public sealed class HandHistoryTextTests
	{
		[Test]
		public void Format_ShowsResultCardsShownHandsAndActionsByStreet()
		{
			HandRecord hand = new()
			{
				TableName = "Practice table",
				HandNumber = 7,
				NetChips = -20,
				Board = new[] { "2c", "7d", "9h", "Js", "3h" },
				Players = new[]
				{
					new HandRecordPlayer { Name = "Alice", IsMe = true, Cards = new[] { "As", "Ad" } },
					new HandRecordPlayer { Name = "Bob", Cards = new[] { "Kc", "Kd" }, Description = "Pair of kings", Won = 40 },
					new HandRecordPlayer { Name = "Ada" },
				},
				Actions = new[]
				{
					new HandRecordAction { Name = "Alice", Street = Street.Preflop, Action = PlayerAction.Call, Amount = 10 },
					new HandRecordAction { Name = "Bob", Street = Street.Preflop, Action = PlayerAction.Check },
					new HandRecordAction { Name = "Bob", Street = Street.Flop, Action = PlayerAction.Raise, Amount = 60 },
					new HandRecordAction { Name = "Alice", Street = Street.Flop, Action = PlayerAction.Fold },
				},
			};

			string[] lines = HandHistoryText.Format(hand).Split('\n');

			Assert.AreEqual("Hand #7, Practice table   -20", lines[0]);
			Assert.AreEqual("You: As Ad   Board: 2c 7d 9h Js 3h", lines[1]);
			Assert.AreEqual("Bob showed Kc Kd (Pair of kings) won 40", lines[2]);
			Assert.AreEqual("preflop: Alice call 10, Bob check; flop: Bob raise to 60, Alice fold", lines[3]);
			Assert.AreEqual(4, lines.Length, "a player who showed nothing and won nothing gets no line");
		}

		[Test]
		public void Format_AWinWithoutAShowdown_NamesTheWinnerAndAnEmptyBoard()
		{
			HandRecord hand = new()
			{
				TableName = "Heads-up table",
				HandNumber = 3,
				NetChips = 150,
				Players = new[]
				{
					new HandRecordPlayer { Name = "Alice", IsMe = true, Cards = new[] { "Qs", "Qd" }, Won = 300 },
					new HandRecordPlayer { Name = "Edsger" },
				},
				Actions = new[] { new HandRecordAction { Name = "Edsger", Street = Street.Preflop, Action = PlayerAction.Fold } },
			};

			string[] lines = HandHistoryText.Format(hand).Split('\n');

			Assert.AreEqual("Hand #3, Heads-up table   +150", lines[0]);
			Assert.AreEqual("You: Qs Qd   Board: none", lines[1]);
			Assert.AreEqual("You won 300", lines[2]);
			Assert.AreEqual("preflop: Edsger fold", lines[3]);
		}
	}
}