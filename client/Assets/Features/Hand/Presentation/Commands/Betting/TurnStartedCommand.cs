using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Turns;


namespace Poker.Client.Features.Hand.Presentation.Commands.Betting
{
	public sealed class TurnStartedCommand : HandStepCommand
	{
		private readonly TurnStartedEvent _turn;
		private readonly bool _isMine;

		public TurnStartedCommand(HandScene scene, TurnStartedEvent turn, bool isMine) : base(scene, 0f)
		{
			_turn = turn;
			_isMine = isMine;
		}

		protected override void Apply() => Scene.StartTurn(_turn, _isMine);
	}
}