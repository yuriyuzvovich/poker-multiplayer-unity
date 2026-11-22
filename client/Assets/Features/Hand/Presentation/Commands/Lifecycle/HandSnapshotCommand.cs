using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Shared.Features.Hand.Events;


namespace Poker.Client.Features.Hand.Presentation.Commands.Lifecycle
{
	public sealed class HandSnapshotCommand : HandStepCommand
	{
		private readonly HandSnapshotEvent _snapshot;
		private readonly int _mySeat;

		public HandSnapshotCommand(HandScene scene, HandSnapshotEvent snapshot, int mySeat) : base(scene, 0f)
		{
			_snapshot = snapshot;
			_mySeat = mySeat;
		}

		protected override void Apply() => Scene.LoadSnapshot(_snapshot, _mySeat);
	}
}