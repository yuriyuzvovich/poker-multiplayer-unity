using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views.Data;

namespace Poker.Client.Features.Hand.Presentation.Scene
{
	// The texts and panels the hand layer shows right now. HandScene keeps it so a view loaded later (the table
	// reopened) is filled from the scene, the one place that knows the shown state.
	public sealed class HandShownState
	{
		public string Title { get; set; } = "";

		public string Pot { get; set; } = "";

		public string Status { get; set; } = "";

		public ActionOptions? Actions { get; set; }

		public PreAction? PreAction { get; set; }

		public bool CanPreCheck { get; set; }
	}
}