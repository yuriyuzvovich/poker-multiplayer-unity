using System;
using Poker.Client.Features.TurnTimer.Views;

namespace Poker.Client.Tests.EditMode.TurnTimer
{
	public sealed class FakeAwayView : IAwayView
	{
		public event Action ReturnClicked;

		public bool IsVisible { get; private set; }

		public void SetVisible(bool isVisible) => IsVisible = isVisible;

		public void ClickReturn() => ReturnClicked?.Invoke();
	}
}