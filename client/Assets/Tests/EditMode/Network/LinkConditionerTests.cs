using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Poker.Client.Infrastructure.Network.DevTools;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class LinkConditionerTests
	{
		private readonly List<byte> _delivered = new();

		[SetUp]
		public void SetUp() => _delivered.Clear();

		[Test]
		public void WithoutDelay_PacketsPassStraightThrough()
		{
			using LinkConditioner link = new(TimeSpan.Zero);
			link.Bind(Deliver);

			link.Receive(Packet(7));

			CollectionAssert.AreEqual(new byte[] { 7 }, _delivered);
		}

		[UnityTest]
		public IEnumerator WithDelay_PacketsArriveLater_InTheirOriginalOrder() => UniTask.ToCoroutine(async () =>
		{
			using LinkConditioner link = new(TimeSpan.FromMilliseconds(200));
			link.Bind(Deliver);
			byte[] buffer = { 1 };
			Stopwatch watch = Stopwatch.StartNew();

			link.Receive(new ArraySegment<byte>(buffer));
			buffer[0] = 2;
			link.Receive(new ArraySegment<byte>(buffer));
			Assert.IsEmpty(_delivered, "nothing arrives before the one-way delay");

			while (_delivered.Count < 2 && watch.ElapsedMilliseconds < 2000) await UniTask.Yield();

			CollectionAssert.AreEqual(new byte[] { 1, 2 }, _delivered, "copies taken at receive time, delivered in order");
			Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 90);
		});

		[Test]
		public void Reset_DropsPacketsStillInFlight()
		{
			using LinkConditioner link = new(TimeSpan.FromSeconds(10));
			link.Bind(Deliver);
			link.Receive(Packet(1));

			link.Reset();

			Assert.IsEmpty(_delivered);
		}

		private void Deliver(ArraySegment<byte> packet) => _delivered.Add(packet.Array![packet.Offset]);

		private static ArraySegment<byte> Packet(byte value) => new ArraySegment<byte>(new[] { value });
	}
}