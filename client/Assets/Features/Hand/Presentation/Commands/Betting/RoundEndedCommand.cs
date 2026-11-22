using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;

namespace Poker.Client.Features.Hand.Presentation.Commands.Betting
{
	public sealed class RoundEndedCommand : HandStepCommand
	{
		private readonly RoundEndedEvent _ended;
		private readonly float _flightSeconds;

		public RoundEndedCommand(HandScene scene, RoundEndedEvent ended, float seconds, float flightSeconds) : base(scene, seconds)
		{
			_ended = ended;
			_flightSeconds = flightSeconds;
		}

		public override async UniTask PlayAsync(CancellationToken cancellationToken)
		{
			await Scene.CollectBetsAsync(_flightSeconds, cancellationToken);
			await base.PlayAsync(cancellationToken);
		}

		protected override void Apply() => Scene.EndRound(_ended);
	}
}