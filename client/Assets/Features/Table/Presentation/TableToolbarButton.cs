using System;

namespace Poker.Client.Features.Table.Presentation
{
	public readonly struct TableToolbarButton
	{
		public TableToolbarButton(string name, string label, Action click)
		{
			Name = name;
			Label = label;
			Click = click;
		}

		public string Name { get; }
		public string Label { get; }
		public Action Click { get; }
	}
}