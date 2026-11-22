using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;

namespace Poker.Client.Features.Hand.Presentation.Commands.Lifecycle
{
	public sealed class HandEndedCommand : HandStepCommand
	{
		private readonly HandEndedEvent _ended;
		private readonly IDisposable _displayHold;
		private readonly float _flightSeconds;
		private readonly float _celebrationSeconds;

		public HandEndedCommand(HandScene scene, HandEndedEvent ended, IDisposable displayHold, float seconds, float flightSeconds, float celebrationSeconds) : base(scene, seconds)
		{
			_ended = ended;
			_displayHold = displayHold;
			_flightSeconds = flightSeconds;
			_celebrationSeconds = celebrationSeconds;
		}

		public override async UniTask PlayAsync(CancellationToken cancellationToken)
		{
			await Scene.CollectBetsAsync(_flightSeconds, cancellationToken);
			await Scene.PayWinnersAsync(_ended, _flightSeconds, cancellationToken);
			await Scene.CelebrateAsync(_ended, _celebrationSeconds, cancellationToken);
			await base.PlayAsync(cancellationToken);
		}

		protected override void Apply()
		{
			Scene.EndHand(_ended);
			_displayHold?.Dispose();
		}
	}
}