using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Poker.Client.Infrastructure.Core.Messaging;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poker.Client.Tests.EditMode.Core
{
	public sealed class MessageBusTests
	{
		[Test]
		public void Publish_ReachesEverySubscriberOfThatType_Only()
		{
			MessageBus bus = new();
			List<string> received = new();
			bus.Subscribe<string>(text => received.Add("a:" + text));
			bus.Subscribe<string>(text => received.Add("b:" + text));
			bus.Subscribe<int>(number => received.Add("int:" + number));

			bus.Publish("hello");

			CollectionAssert.AreEqual(new[] { "a:hello", "b:hello" }, received);
		}

		[Test]
		public void Publish_WhenOneSubscriberThrows_StillReachesTheOthers()
		{
			MessageBus bus = new();
			List<string> received = new();
			bus.Subscribe<string>(_ => throw new InvalidOperationException("broken subscriber"));
			bus.Subscribe<string>(text => received.Add(text));
			LogAssert.Expect(LogType.Exception, new Regex("broken subscriber"));

			bus.Publish("hello");

			CollectionAssert.AreEqual(new[] { "hello" }, received);
		}

		[Test]
		public void DisposedSubscription_NoLongerReceives_AndDisposeIsIdempotent()
		{
			MessageBus bus = new();
			int calls = 0;
			IDisposable subscription = bus.Subscribe<string>(_ => calls++);

			subscription.Dispose();
			subscription.Dispose();
			bus.Publish("ignored");

			Assert.AreEqual(0, calls);
		}

		[Test]
		public void Publish_WithoutSubscribers_DoesNothing()
		{
			Assert.DoesNotThrow(() => new MessageBus().Publish(42));
		}
	}
}