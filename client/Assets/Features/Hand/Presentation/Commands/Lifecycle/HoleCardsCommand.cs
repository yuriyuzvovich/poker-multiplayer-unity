using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;


namespace Poker.Client.Features.Hand.Presentation.Commands.Lifecycle
{
	public sealed class HoleCardsCommand : HandStepCommand
	{
		private readonly HoleCardsEvent _cards;

		public HoleCardsCommand(HandScene scene, HoleCardsEvent cards, float seconds) : base(scene, seconds) => _cards = cards;

		protected override void Apply() => Scene.ShowHoleCards(_cards);
	}
}