using NUnit.Framework;
using Poker.Client.Infrastructure.Network.DevTools;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class NetworkStatsTests
	{
		[Test]
		public void Counters_AddUpTrafficHopsAndResends()
		{
			NetworkStats stats = new();

			stats.AddSent(100);
			stats.AddSent(50);
			stats.AddReceived(300);
			stats.AddResent(2);
			stats.AddHop(4);
			stats.AddHop(10);

			Assert.AreEqual(150, stats.BytesOut);
			Assert.AreEqual(2, stats.PacketsOut);
			Assert.AreEqual(300, stats.BytesIn);
			Assert.AreEqual(1, stats.PacketsIn);
			Assert.AreEqual(2, stats.Resent);
			Assert.AreEqual(2, stats.HopCount);
			Assert.AreEqual(14, stats.HopTotalMs, 0.001);
			Assert.AreEqual(10, stats.HopMaxMs, 0.001);
		}

		[Test]
		public void TopByBytes_KeepsTheLargestTypesInOrder()
		{
			NetworkStats stats = new();
			stats.AddHandled("a", 100, 1);
			stats.AddHandled("b", 400, 1);
			stats.AddHandled("c", 50, 1);
			stats.AddHandled("d", 300, 1);
			stats.AddHandled("a", 250, 1);
			MessageTypeStats[] top = new MessageTypeStats[3];

			int count = stats.CopyTopByBytes(top);

			Assert.AreEqual(3, count);
			Assert.AreEqual("b", top[0].Name);
			Assert.AreEqual("a", top[1].Name);
			Assert.AreEqual(350, top[1].Bytes);
			Assert.AreEqual(2, top[1].Count);
			Assert.AreEqual("d", top[2].Name);
		}

		[Test]
		public void TopByBytes_WithFewerTypesThanSlots_FillsOnlyThoseTypes()
		{
			NetworkStats stats = new();
			stats.AddHandled("a", 10, 1);
			stats.AddHandled("b", 20, 1);
			MessageTypeStats[] top = new MessageTypeStats[3];

			Assert.AreEqual(2, stats.CopyTopByBytes(top));
			Assert.AreEqual("b", top[0].Name);
			Assert.AreEqual("a", top[1].Name);
		}

		[Test]
		public void Slowest_IsTheTypeWithTheLongestSingleHandling()
		{
			NetworkStats stats = new();
			Assert.IsNull(stats.FindSlowest());

			stats.AddHandled("a", 10, 2);
			stats.AddHandled("b", 10, 7);
			stats.AddHandled("a", 10, 5);

			MessageTypeStats slowest = stats.FindSlowest();
			Assert.AreEqual("b", slowest.Name);
			Assert.AreEqual(7, slowest.MaxHandleMs, 0.001);
		}
	}
}