using System;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public interface IDevPanelView
	{
		public event Action ToggleClicked;
		public event Action BugReportClicked;
		public event Action ShortDropClicked;
		public event Action LongDropClicked;

		public bool IsPanelVisible { get; }

		public void SetPanelVisible(bool isVisible);

		public void SetInfo(string info);

		public void AddAction(DevPanelAction action);
	}
}