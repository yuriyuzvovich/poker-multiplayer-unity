using NUnit.Framework;
using Poker.Client.Infrastructure.Network.Clock;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class ServerClockTests
	{
		[Test]
		public void Offset_AssumesTheServerReadItsClockHalfwayThroughTheRoundTrip()
		{
			ServerClock clock = new();

			clock.AddSample(sentAtUnixMs: 1000, serverTimeUnixMs: 5050, receivedAtUnixMs: 1100);

			Assert.AreEqual(4000, clock.OffsetMs);
			Assert.AreEqual(100, clock.BestRoundTripMs);
		}

		[Test]
		public void TheSampleWithTheShortestRoundTrip_Wins()
		{
			ServerClock clock = new();
			clock.AddSample(1000, 5050, 1100);

			bool isSlowerTaken = clock.AddSample(2000, 9999, 2300);
			bool isFasterTaken = clock.AddSample(3000, 7010, 3020);

			Assert.IsFalse(isSlowerTaken);
			Assert.IsTrue(isFasterTaken);
			Assert.AreEqual(4000, clock.OffsetMs);
			Assert.AreEqual(20, clock.BestRoundTripMs);
		}

		[Test]
		public void TheBestSample_LeavesTheWindowAfterSixNewerOnes()
		{
			ServerClock clock = new();
			clock.AddSample(1000, 5010, 1020);
			for (int i = 1; i <= 5; i++)
			{
				clock.AddSample(1000 * (i + 1), 1000 * (i + 1) + 3050, 1000 * (i + 1) + 100);
			}

			Assert.AreEqual(4000, clock.OffsetMs, "the fast first sample still wins inside the window");

			clock.AddSample(7000, 10050, 7100);

			Assert.AreEqual(3000, clock.OffsetMs, "once it is six samples old the offset follows the newer ones");
			Assert.AreEqual(100, clock.BestRoundTripMs);
		}

		[Test]
		public void SampleReceivedBeforeItWasSent_IsIgnored()
		{
			ServerClock clock = new();

			Assert.IsFalse(clock.AddSample(2000, 5000, 1000));
			Assert.AreEqual(0, clock.OffsetMs);
		}
	}
}