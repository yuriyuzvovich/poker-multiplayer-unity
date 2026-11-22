using NUnit.Framework;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Tests.EditMode.Hand
{
	public sealed class TurnClockTests
	{
		[Test]
		public void Advance_CountsWholeSecondsAgainstTheDeadlineMinusLeeway()
		{
			TurnClock clock = new();
			clock.Start(new TurnStartedEvent { SeatIndex = 2, DeadlineUnixMs = 21_000, LeewayMs = 1_000 }, false);

			clock.Advance(10_000);
			Assert.AreEqual("10", clock.SecondsText);
			Assert.AreEqual(1f, clock.Remaining, 0.001f);

			clock.Advance(15_500);
			Assert.AreEqual("5", clock.SecondsText);
			Assert.AreEqual(0.45f, clock.Remaining, 0.001f);
		}

		[Test]
		public void Advance_ReportsTimeUpOnce_WhenTheShownSecondsReachZero()
		{
			TurnClock clock = new();
			clock.Start(new TurnStartedEvent { DeadlineUnixMs = 2_000 }, true);

			Assert.IsFalse(clock.Advance(1_500));
			Assert.IsTrue(clock.Advance(2_000));
			Assert.IsFalse(clock.Advance(2_500));
			Assert.AreEqual("", clock.SecondsText);
		}

		[Test]
		public void Clear_StopsTheClock()
		{
			TurnClock clock = new();
			clock.Start(new TurnStartedEvent { DeadlineUnixMs = 5_000 }, true);

			clock.Clear();

			Assert.IsFalse(clock.IsRunning);
			Assert.IsFalse(clock.IsMine);
		}
	}
}