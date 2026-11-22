using System;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Server.Features.Hand.Bots
{
	public static class BotPolicy
	{
		public static PlayerAction Decide(TurnStartedEvent turn, long stack, Random random, out long raiseTo)
		{
			raiseTo = turn.MinRaiseTo;
			int roll = random.Next(100);
			if (turn.CanCheck) return turn.CanRaise && roll < 20 ? PlayerAction.Raise : PlayerAction.Check;

			bool isCheap = turn.ToCall * 5 <= stack;
			if (isCheap && turn.CanRaise && roll < 10) return PlayerAction.Raise;
			if (isCheap && roll < 80) return PlayerAction.Call;
			if (!isCheap && roll < 30) return PlayerAction.Call;

			return PlayerAction.Fold;
		}
	}
}