using System;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public readonly struct DevPanelAction
	{
		public DevPanelAction(string label, Action run)
		{
			Label = label;
			Run = run;
		}

		public string Label { get; }
		public Action Run { get; }
	}
}