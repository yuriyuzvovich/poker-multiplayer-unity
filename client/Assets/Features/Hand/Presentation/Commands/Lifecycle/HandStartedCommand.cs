using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;

namespace Poker.Client.Features.Hand.Presentation.Commands.Lifecycle
{
	public sealed class HandStartedCommand : HandStepCommand
	{
		private readonly HandStartedEvent _started;
		private readonly int _mySeat;
		private readonly float _flightSeconds;

		public HandStartedCommand(HandScene scene, HandStartedEvent started, int mySeat, float seconds, float flightSeconds) : base(scene, seconds)
		{
			_started = started;
			_mySeat = mySeat;
			_flightSeconds = flightSeconds;
		}

		public override async UniTask PlayAsync(CancellationToken cancellationToken)
		{
			await Scene.DealAsync(_started, _flightSeconds, cancellationToken);
			await base.PlayAsync(cancellationToken);
		}

		protected override void Apply() => Scene.StartHand(_started, _mySeat);
	}
}