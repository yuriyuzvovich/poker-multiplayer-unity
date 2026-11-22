using System;
using NUnit.Framework;
using Poker.Client.Features.Hand.Domain;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Tests.EditMode.Hand
{
	public sealed class PreActionTests
	{
		[TestCase(PreAction.CheckFold, true, PlayerAction.Check)]
		[TestCase(PreAction.CheckFold, false, PlayerAction.Fold)]
		[TestCase(PreAction.Check, true, PlayerAction.Check)]
		[TestCase(PreAction.CallAny, true, PlayerAction.Check)]
		[TestCase(PreAction.CallAny, false, PlayerAction.Call)]
		public void TakePreAction_TurnsTheChoiceIntoAMove(PreAction chosen, bool canCheck, PlayerAction expected)
		{
			HandModel hand = new();
			hand.TogglePreAction(chosen);

			Assert.AreEqual(expected, hand.TakePreAction(new TurnStartedEvent { CanCheck = canCheck }));
			Assert.AreEqual(PreAction.None, hand.PreAction, "a choice is used once");
		}

		[Test]
		public void Check_FacingABet_LeavesTheTurnToThePlayer()
		{
			HandModel hand = new();
			hand.TogglePreAction(PreAction.Check);

			Assert.IsNull(hand.TakePreAction(new TurnStartedEvent { CanCheck = false }));
			Assert.AreEqual(PreAction.None, hand.PreAction);
		}

		[Test]
		public void SecondTap_ClearsTheChoice_AndANewHandStartsWithoutOne()
		{
			HandModel hand = new();
			hand.TogglePreAction(PreAction.CallAny);
			hand.TogglePreAction(PreAction.CallAny);
			Assert.AreEqual(PreAction.None, hand.PreAction, "a second tap clears the choice");

			hand.TogglePreAction(PreAction.CheckFold);
			hand.Start(2, Array.Empty<HandPlayerInfo>(), 0);
			Assert.AreEqual(PreAction.None, hand.PreAction, "a choice does not carry into the next hand");
		}
	}
}