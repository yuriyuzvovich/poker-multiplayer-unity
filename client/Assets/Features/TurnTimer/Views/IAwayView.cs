using System;

namespace Poker.Client.Features.TurnTimer.Views
{
	public interface IAwayView
	{
		public event Action ReturnClicked;

		public void SetVisible(bool isVisible);
	}
}