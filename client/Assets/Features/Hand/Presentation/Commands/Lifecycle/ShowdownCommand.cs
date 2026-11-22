using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;


namespace Poker.Client.Features.Hand.Presentation.Commands.Lifecycle
{
	public sealed class ShowdownCommand : HandStepCommand
	{
		private readonly ShowdownEvent _showdown;

		public ShowdownCommand(HandScene scene, ShowdownEvent showdown, float seconds) : base(scene, seconds) => _showdown = showdown;

		protected override void Apply() => Scene.ShowShowdown(_showdown);
	}
}