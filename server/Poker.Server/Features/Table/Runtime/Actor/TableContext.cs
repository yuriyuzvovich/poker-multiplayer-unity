using System;
using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Time;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Features.Table.Runtime.Actor
{
	public sealed class TableContext
	{
		private readonly List<IClientConnection> _watchers = new();
		private readonly Dictionary<Type, object> _states = new();
		private readonly List<ISnapshotContributor> _snapshotContributors = new();
		private readonly IClock _clock;

		public TableContext(PokerTable table, IClock clock)
		{
			Table = table ?? throw new ArgumentNullException(nameof(table));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		}

		public PokerTable Table { get; }

		public DateTimeOffset Now => _clock.UtcNow;

		public IReadOnlyList<IClientConnection> Watchers => _watchers;

		public IReadOnlyList<ISnapshotContributor> SnapshotContributors => _snapshotContributors;

		public bool IsWatching(IClientConnection connection) => _watchers.Contains(connection);

		public void AddWatcher(IClientConnection connection)
		{
			if (IsWatching(connection)) return;

			_watchers.Add(connection);
		}

		public void RemoveWatcher(IClientConnection connection) => _watchers.Remove(connection);

		public TState GetState<TState>() where TState : class, new()
		{
			if (_states.TryGetValue(typeof(TState), out object? existing)) return (TState)existing;

			TState created = new();
			_states.Add(typeof(TState), created);
			if (created is ISnapshotContributor contributor) _snapshotContributors.Add(contributor);

			return created;
		}
	}
}