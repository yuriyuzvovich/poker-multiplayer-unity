using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Infrastructure.Core.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poker.Client.Tests.EditMode.Core
{
	public sealed class PresentationQueueTests
	{
		private readonly List<string> _log = new();
		private PresentationQueue _queue;

		[SetUp]
		public void SetUp()
		{
			_log.Clear();
			_queue = new PresentationQueue();
		}

		[TearDown]
		public void TearDown() => _queue.Dispose();

		[Test]
		public void Commands_PlayOneAtATime_InTheOrderTheyArrived()
		{
			BlockingCommand first = Enqueue("A");
			BlockingCommand second = Enqueue("B");

			CollectionAssert.AreEqual(new[] { "play A" }, _log);
			Assert.AreEqual(1, _queue.PendingCount);

			first.Complete();
			CollectionAssert.AreEqual(new[] { "play A", "play B" }, _log);

			second.Complete();
			Assert.IsTrue(_queue.IsIdle);
		}

		[Test]
		public void FastForward_CancelsThePlayingCommand_AndFinishesEveryCommandInOrder()
		{
			BlockingCommand playing = Enqueue("A");
			Enqueue("B");
			Enqueue("C");

			_queue.FastForward();

			Assert.IsTrue(playing.Token.IsCancellationRequested);
			CollectionAssert.AreEqual(new[] { "play A", "finish A", "finish B", "finish C" }, _log);
			Assert.IsTrue(_queue.IsIdle);
		}

		[Test]
		public void AfterFastForward_NewCommandsPlayNormally()
		{
			Enqueue("A");
			_queue.FastForward();

			Enqueue("B");

			CollectionAssert.AreEqual(new[] { "play A", "finish A", "play B" }, _log);
			Assert.IsFalse(_queue.IsIdle);
		}

		[Test]
		public void OutOfOrderCommand_AppliesAtOnce_WithoutWaitingForThePlayingOne()
		{
			Enqueue("A");
			Enqueue("B");

			_queue.Enqueue(new BlockingCommand("away", _log, runsOutOfOrder: true));

			CollectionAssert.AreEqual(new[] { "play A", "finish away" }, _log);
			Assert.AreEqual(1, _queue.PendingCount);
		}

		[Test]
		public void Clear_DropsQueuedCommandsWithoutFinishingThem()
		{
			BlockingCommand playing = Enqueue("A");
			Enqueue("B");

			_queue.Clear();

			Assert.IsTrue(playing.Token.IsCancellationRequested);
			CollectionAssert.AreEqual(new[] { "play A" }, _log);
			Assert.IsTrue(_queue.IsIdle);
		}

		[Test]
		public void FailingCommand_IsLogged_AndTheQueueMovesOn()
		{
			BlockingCommand failing = Enqueue("A");
			Enqueue("B");
			LogAssert.Expect(LogType.Exception, new Regex("broken animation"));

			failing.Fail(new InvalidOperationException("broken animation"));

			CollectionAssert.AreEqual(new[] { "play A", "play B" }, _log);
		}

		private BlockingCommand Enqueue(string name)
		{
			BlockingCommand command = new(name, _log);
			_queue.Enqueue(command);
			return command;
		}

		private sealed class BlockingCommand : IVisualCommand
		{
			private readonly string _name;
			private readonly List<string> _log;
			private readonly UniTaskCompletionSource _completion = new();

			public BlockingCommand(string name, List<string> log, bool runsOutOfOrder = false)
			{
				_name = name;
				_log = log;
				RunsOutOfOrder = runsOutOfOrder;
			}

			public bool RunsOutOfOrder { get; }

			public CancellationToken Token { get; private set; }

			public UniTask PlayAsync(CancellationToken cancellationToken)
			{
				_log.Add("play " + _name);
				Token = cancellationToken;
				return _completion.Task.AttachExternalCancellation(cancellationToken);
			}

			public void Finish() => _log.Add("finish " + _name);

			public void Complete() => _completion.TrySetResult();

			public void Fail(Exception exception) => _completion.TrySetException(exception);
		}
	}
}