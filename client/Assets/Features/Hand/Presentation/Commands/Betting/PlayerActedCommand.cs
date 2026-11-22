using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Features.Hand.Presentation.Commands.Betting
{
	public sealed class PlayerActedCommand : HandStepCommand
	{
		private readonly TurnActedEvent _acted;
		private readonly float _flightSeconds;

		public PlayerActedCommand(HandScene scene, TurnActedEvent acted, float seconds, float flightSeconds) : base(scene, seconds)
		{
			_acted = acted;
			_flightSeconds = flightSeconds;
		}

		public override async UniTask PlayAsync(CancellationToken cancellationToken)
		{
			await Scene.FlyBetAsync(_acted, _flightSeconds, cancellationToken);
			await base.PlayAsync(cancellationToken);
		}

		protected override void Apply() => Scene.ApplyActed(_acted);
	}
}