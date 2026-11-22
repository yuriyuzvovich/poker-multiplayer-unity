using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Poker.Client.Infrastructure.Network.Messaging;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class MessageRouterTests
	{
		[Test]
		public void Dispatch_WhenOneSubscriberThrows_StillReachesTheOthers()
		{
			MessageRouter router = new();
			List<string> received = new();
			router.Subscribe<string>(_ => throw new InvalidOperationException("broken presenter"));
			router.Subscribe<string>(text => received.Add(text));
			LogAssert.Expect(LogType.Exception, new Regex("broken presenter"));

			router.Dispatch(typeof(string), "turn.acted");

			CollectionAssert.AreEqual(new[] { "turn.acted" }, received);
		}

		[Test]
		public void Dispatch_WhileAHandlerUnsubscribes_FinishesTheCurrentRound()
		{
			MessageRouter router = new();
			List<string> received = new();
			IDisposable first = null;
			first = router.Subscribe<string>(text =>
			{
				received.Add("first:" + text);
				first.Dispose();
			});
			router.Subscribe<string>(text => received.Add("second:" + text));

			router.Dispatch(typeof(string), "a");
			router.Dispatch(typeof(string), "b");

			CollectionAssert.AreEqual(new[] { "first:a", "second:a", "second:b" }, received);
		}

		[Test]
		public void Dispatch_OnlyReachesSubscribersOfThatType()
		{
			MessageRouter router = new();
			int calls = 0;
			router.Subscribe<string>(_ => calls++);

			router.Dispatch(typeof(Uri), new Uri("ws://localhost"));

			Assert.AreEqual(0, calls);
		}
	}
}