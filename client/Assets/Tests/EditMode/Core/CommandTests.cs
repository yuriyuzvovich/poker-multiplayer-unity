using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Infrastructure.Core.Commands;
using UnityEngine.TestTools;

namespace Poker.Client.Tests.EditMode.Core
{
	public sealed class CommandTests
	{
		[UnityTest]
		public IEnumerator Sequence_StopsAtTheFirstFailure() => UniTask.ToCoroutine(async () =>
		{
			ScriptedCommand first = new("first", true);
			ScriptedCommand failing = new("failing", false);
			ScriptedCommand never = new("never", true);

			bool result = await new SequenceCommand("init", first, failing, never).ExecuteAsync(CancellationToken.None);

			Assert.IsFalse(result);
			Assert.AreEqual(1, first.Executions);
			Assert.AreEqual(1, failing.Executions);
			Assert.AreEqual(0, never.Executions);
		});

		[UnityTest]
		public IEnumerator Parallel_RunsEveryBranch_AndFailsIfAnyFails() => UniTask.ToCoroutine(async () =>
		{
			ScriptedCommand ok = new("ok", true);
			ScriptedCommand failing = new("failing", false);

			bool result = await new ParallelCommand("data", ok, failing).ExecuteAsync(CancellationToken.None);

			Assert.IsFalse(result);
			Assert.AreEqual(1, ok.Executions);
			Assert.AreEqual(1, failing.Executions);
		});

		[UnityTest]
		public IEnumerator Retry_StopsAtTheFirstSuccess() => UniTask.ToCoroutine(async () =>
		{
			ScriptedCommand flaky = new("connect", false, false, true, true);

			bool result = await new RetryCommand(flaky, 5, TimeSpan.Zero).ExecuteAsync(CancellationToken.None);

			Assert.IsTrue(result);
			Assert.AreEqual(3, flaky.Executions);
		});

		[UnityTest]
		public IEnumerator Retry_GivesUpAfterAllAttempts() => UniTask.ToCoroutine(async () =>
		{
			ScriptedCommand failing = new("connect", false);

			bool result = await new RetryCommand(failing, 3, TimeSpan.Zero).ExecuteAsync(CancellationToken.None);

			Assert.IsFalse(result);
			Assert.AreEqual(3, failing.Executions);
		});

		[UnityTest]
		public IEnumerator Timed_PassesTheResultThrough() => UniTask.ToCoroutine(async () =>
		{
			Assert.IsTrue(await new TimedCommand(new ScriptedCommand("ok", true)).ExecuteAsync(CancellationToken.None));
		});

		[Test]
		public void Retry_RequiresAtLeastOneAttempt()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new RetryCommand(new ScriptedCommand("x", true), 0, TimeSpan.Zero));
		}

		private sealed class ScriptedCommand : ICommand
		{
			private readonly Queue<bool> _results;
			private readonly bool _last;

			public ScriptedCommand(string name, params bool[] results)
			{
				Name = name;
				_results = new Queue<bool>(results);
				_last = results[results.Length - 1];
			}

			public string Name { get; }

			public int Executions { get; private set; }

			public UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
			{
				Executions++;
				bool result = _results.Count > 0 ? _results.Dequeue() : _last;
				return UniTask.FromResult(result);
			}
		}
	}
}