using System;
using System.Collections.Generic;

namespace Poker.Client.Features.Table.Presentation
{
	// Buttons that other slices put on the table screen (for example hand history). The kernel lays them out
	// and knows nothing of what they open, so no slice has to reach into another slice's view.
	public sealed class TableToolbar
	{
		private readonly List<TableToolbarButton> _buttons = new();

		public event Action<TableToolbarButton> Added;

		public IReadOnlyList<TableToolbarButton> Buttons => _buttons;

		public void Add(string name, string label, Action click)
		{
			if (click == null) throw new ArgumentNullException(nameof(click));

			TableToolbarButton button = new(name, label, click);
			_buttons.Add(button);
			Added?.Invoke(button);
		}
	}
}