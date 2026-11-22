using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Client.Infrastructure.Core.Presentation;

namespace Poker.Client.Features.Hand.Presentation.Commands
{
	public abstract class HandStepCommand : IVisualCommand
	{
		protected HandStepCommand(HandScene scene, float seconds)
		{
			Scene = scene ?? throw new ArgumentNullException(nameof(scene));
			Seconds = seconds;
		}

		protected HandScene Scene { get; }
		protected float Seconds { get; }

		public bool RunsOutOfOrder => false;

		public virtual UniTask PlayAsync(CancellationToken cancellationToken)
		{
			Apply();
			return HoldAsync(Seconds, cancellationToken);
		}

		public void Finish()
		{
			Scene.StopFlights();
			Apply();
		}

		protected abstract void Apply();

		protected static UniTask HoldAsync(float seconds, CancellationToken cancellationToken) =>
			seconds > 0f ? UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.Realtime, cancellationToken: cancellationToken, cancelImmediately: true) : UniTask.CompletedTask;
	}
}