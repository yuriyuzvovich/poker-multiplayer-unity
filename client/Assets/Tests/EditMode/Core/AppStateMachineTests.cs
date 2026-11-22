using System;
using System.Collections.Generic;
using NUnit.Framework;
using Poker.Client.Infrastructure.Core;

namespace Poker.Client.Tests.EditMode.Core
{
	public sealed class AppStateMachineTests
	{
		[Test]
		public void StartsInBooting()
		{
			Assert.AreEqual(AppState.Booting, new AppStateMachine().Current);
		}

		[Test]
		public void HappyPath_ConnectsAndRaisesChangedWithBothStates()
		{
			AppStateMachine machine = new();
			List<string> changes = new();
			machine.Changed += (previous, next) => changes.Add(previous + ">" + next);

			machine.TransitionTo(AppState.Connecting, "ws://test");
			machine.TransitionTo(AppState.Connected);
			machine.TransitionTo(AppState.Disconnected, "gone");

			CollectionAssert.AreEqual(new[] { "Booting>Connecting", "Connecting>Connected", "Connected>Disconnected" }, changes);
			Assert.AreEqual("gone", machine.Detail);
		}

		[Test]
		public void FailedConnect_CanRetry()
		{
			AppStateMachine machine = new();
			machine.TransitionTo(AppState.Connecting);
			machine.TransitionTo(AppState.Failed, "refused");

			machine.TransitionTo(AppState.Connecting);

			Assert.AreEqual(AppState.Connecting, machine.Current);
		}

		[TestCase(AppState.Booting, AppState.Connected)]
		[TestCase(AppState.Booting, AppState.Disconnected)]
		[TestCase(AppState.Connecting, AppState.Disconnected)]
		[TestCase(AppState.Connected, AppState.Connecting)]
		public void IllegalTransition_Throws(AppState from, AppState to)
		{
			Assert.IsFalse(AppStateMachine.IsAllowed(from, to));
		}

		[Test]
		public void IllegalTransition_LeavesStateUnchanged()
		{
			AppStateMachine machine = new();

			Assert.Throws<InvalidOperationException>(() => machine.TransitionTo(AppState.Connected));
			Assert.AreEqual(AppState.Booting, machine.Current);
		}
	}
}