using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class PendingRequestsTests
	{
		private static readonly TimeSpan LongTimeout = TimeSpan.FromSeconds(5);

		[Test]
		public void Register_NeverReturnsZero_AndWrapsAround()
		{
			PendingRequests pending = new();
			for (int i = 1; i <= ushort.MaxValue; i++)
			{
				pending.Cancel(pending.Register());
			}

			Assert.AreEqual(1, pending.Register());
		}

		[Test]
		public void Register_SkipsIdsThatAreStillPending()
		{
			PendingRequests pending = new();
			ushort kept = pending.Register();
			for (int i = 2; i <= ushort.MaxValue; i++)
			{
				pending.Cancel(pending.Register());
			}

			Assert.AreNotEqual(kept, pending.Register());
		}

		[UnityTest]
		public IEnumerator WaitAsync_ResponseArrivesFirst_StillReturnsIt() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();

			Assert.IsTrue(pending.TryComplete(CreateMessage(requestId, "early")));
			IncomingMessage response = await pending.WaitAsync(requestId, LongTimeout, CancellationToken.None);

			Assert.AreEqual("early", response.Payload);
			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator WaitAsync_ResponseArrivesLater_ReturnsIt() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();

			UniTask<IncomingMessage> waiting = pending.WaitAsync(requestId, LongTimeout, CancellationToken.None);
			pending.TryComplete(CreateMessage(requestId, "late"));
			IncomingMessage response = await waiting;

			Assert.AreEqual("late", response.Payload);
			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator WaitAsync_NoResponse_TimesOutAndForgetsTheRequest() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();

			try
			{
				await pending.WaitAsync(requestId, TimeSpan.FromMilliseconds(50), CancellationToken.None);
				Assert.Fail("Expected a timeout.");
			}
			catch (TimeoutException) { }

			Assert.AreEqual(0, pending.Count);
			Assert.IsFalse(pending.TryComplete(CreateMessage(requestId, "too late")));
		});

		[UnityTest]
		public IEnumerator WaitAsync_Cancelled_ThrowsOperationCanceled() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();
			using CancellationTokenSource cancellation = new();
			cancellation.Cancel();

			try
			{
				await pending.WaitAsync(requestId, LongTimeout, cancellation.Token);
				Assert.Fail("Expected cancellation.");
			}
			catch (OperationCanceledException) { }

			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator FailAll_FailsEveryWaiter() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();
			UniTask<IncomingMessage> waiting = pending.WaitAsync(requestId, LongTimeout, CancellationToken.None);

			pending.FailAll(new ConnectionClosedException("test"));

			try
			{
				await waiting;
				Assert.Fail("Expected the connection-closed failure.");
			}
			catch (ConnectionClosedException) { }

			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator FailAll_BeforeTheWaitStarts_StillFailsWithTheConnectionError() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();

			pending.FailAll(new ConnectionClosedException("client shut down"));

			try
			{
				await pending.WaitAsync(requestId, LongTimeout, CancellationToken.None);
				Assert.Fail("Expected the connection-closed failure.");
			}
			catch (ConnectionClosedException) { }

			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator ShutdownFailingAndCancellingAWait_LeavesNoUnobservedError() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort requestId = pending.Register();
			using CancellationTokenSource owner = new();

			pending.FailAll(new ConnectionClosedException("client shut down"));
			owner.Cancel();

			try
			{
				await pending.WaitAsync(requestId, LongTimeout, owner.Token);
				Assert.Fail("Expected the wait to end with an error.");
			}
			catch (ConnectionClosedException) { }
			catch (OperationCanceledException) { }

			await UniTask.Yield();
			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator CancelAfterAFailure_LeavesNoUnobservedError() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			FailThenCancel(pending);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await UniTask.DelayFrame(3);

			Assert.AreEqual(0, pending.Count);
		});

		[UnityTest]
		public IEnumerator FailAllExcept_KeepsOneRequest_AndFailsTheRest() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort hello = pending.Register();
			ushort stale = pending.Register();
			UniTask<IncomingMessage> staleWait = pending.WaitAsync(stale, LongTimeout, CancellationToken.None);
			UniTask<IncomingMessage> helloWait = pending.WaitAsync(hello, LongTimeout, CancellationToken.None);

			pending.FailAllExcept(hello, new ConnectionClosedException("new session"));
			pending.TryComplete(CreateMessage(hello, "welcome"));

			try
			{
				await staleWait;
				Assert.Fail("Expected the stale request to fail.");
			}
			catch (ConnectionClosedException) { }

			Assert.AreEqual(hello, (await helloWait).Header.RequestId);
		});

		[UnityTest]
		public IEnumerator Fail_FailsOnlyThatRequest() => UniTask.ToCoroutine(async () =>
		{
			PendingRequests pending = new();
			ushort ping = pending.Register();
			ushort act = pending.Register();
			UniTask<IncomingMessage> pingWait = pending.WaitAsync(ping, LongTimeout, CancellationToken.None);

			pending.Fail(ping, new ConnectionClosedException("socket lost"));

			try
			{
				await pingWait;
				Assert.Fail("Expected the ping to fail.");
			}
			catch (ConnectionClosedException) { }

			Assert.AreEqual(1, pending.Count);
			Assert.IsTrue(pending.TryComplete(CreateMessage(act, "act")));
		});

		[Test]
		public void TryComplete_EventOrUnknownId_IsNotAResponse()
		{
			PendingRequests pending = new();
			pending.Register();

			Assert.IsFalse(pending.TryComplete(CreateMessage(0, "event")));
			Assert.IsFalse(pending.TryComplete(CreateMessage(999, "unknown")));
		}

		private static void FailThenCancel(PendingRequests pending)
		{
			ushort requestId = pending.Register();
			pending.Fail(requestId, new ConnectionClosedException("socket lost"));
			pending.Cancel(requestId);
		}

		private static IncomingMessage CreateMessage(ushort requestId, string payload) => new IncomingMessage(
			new PacketHeader(0, 0, 1, requestId, 0, PacketFlags.None),
			new MessageType(1, "test.message", typeof(string)),
			payload);
	}
}