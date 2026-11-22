using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;

namespace Poker.Client.Features.Hand.Presentation.Commands.Betting
{
	public sealed class RoundStartedCommand : HandStepCommand
	{
		private readonly RoundStartedEvent _round;
		private readonly float _flightSeconds;

		public RoundStartedCommand(HandScene scene, RoundStartedEvent round, float secondsPerCard, float flightSeconds) : base(scene, secondsPerCard)
		{
			_round = round;
			_flightSeconds = flightSeconds;
		}

		public override async UniTask PlayAsync(CancellationToken cancellationToken)
		{
			int firstCount = _round.Street == Street.Flop ? 1 : _round.Board.Length;
			for (int count = firstCount; count <= _round.Board.Length; count++)
			{
				await Scene.DealBoardCardAsync(count - 1, _flightSeconds, cancellationToken);
				Scene.ShowBoard(_round.Street, _round.Board, count);
				await HoldAsync(Seconds, cancellationToken);
			}
		}

		protected override void Apply() => Scene.ShowBoard(_round.Street, _round.Board, _round.Board.Length);
	}
}