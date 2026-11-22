using System;
using System.Collections.Generic;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public sealed class DevPanelActions
	{
		private readonly List<DevPanelAction> _actions = new();

		public event Action<DevPanelAction> Added;

		public IReadOnlyList<DevPanelAction> Actions => _actions;

		public void Add(string label, Action run)
		{
			if (run == null) throw new ArgumentNullException(nameof(run));

			DevPanelAction action = new(label, run);
			_actions.Add(action);
			Added?.Invoke(action);
		}
	}
}