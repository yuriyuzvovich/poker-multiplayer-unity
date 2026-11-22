using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Time;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Lobby.Models;

namespace Poker.Server.Features.Table.Runtime.Actor
{
	public sealed class TableActor
	{
		private readonly Channel<ITableWork> _mailbox = Channel.CreateUnbounded<ITableWork>(new UnboundedChannelOptions { SingleReader = true });
		private readonly TableContext _context;
		private readonly List<TableEvent> _events = new();
		private readonly List<TableEvent> _reactions = new();
		private readonly List<IClientConnection> _deadWatchers = new();
		private readonly ILogger<TableActor> _logger;
		private readonly IReadOnlyList<ITableEventListener> _listeners;
		private TableSummary _summary;
		private string[] _holderIds;

		public TableActor(PokerTable table, IClock clock, ILogger<TableActor> logger, IReadOnlyList<ITableEventListener>? listeners = null)
		{
			_context = new TableContext(table, clock);
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_listeners = listeners ?? Array.Empty<ITableEventListener>();
			_summary = table.CreateSummary();
			_holderIds = table.CollectHolderIds();
		}

		public string TableId => _context.Table.Definition.TableId;

		public TableSummary Summary => Volatile.Read(ref _summary);

		public bool IsSeatHeldBy(string playerId) => Array.IndexOf(Volatile.Read(ref _holderIds), playerId) >= 0;

		public void Post(ITableWork work)
		{
			if (_mailbox.Writer.TryWrite(work)) return;

			_logger.LogWarning("{TableId} is stopped; dropped {Work}", TableId, work.GetType().Name);
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			await foreach (ITableWork work in _mailbox.Reader.ReadAllAsync(cancellationToken))
			{
				await ExecuteAsync(work, cancellationToken);
			}
		}

		internal async Task ExecuteAsync(ITableWork work, CancellationToken cancellationToken)
		{
			try
			{
				await work.ExecuteAsync(_context, cancellationToken);
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				_logger.LogError(exception, "{Work} failed on {TableId}", work.GetType().Name, TableId);
			}

			BroadcastEvents();
			RefreshDirectoryView();
		}

		// Ticks run four times a second and rarely change seating, so rebuild the lobby view only when it changed.
		private void RefreshDirectoryView()
		{
			PokerTable table = _context.Table;
			if (table.CountOccupiedSeats() != _summary.OccupiedSeats) Volatile.Write(ref _summary, table.CreateSummary());
			if (!table.HoldersMatch(_holderIds)) Volatile.Write(ref _holderIds, table.CollectHolderIds());
		}

		private void BroadcastEvents()
		{
			_context.Table.TakeEvents(_events);
			for (int i = 0; i < _events.Count; i++)
			{
				NotifyListeners(_events[i]);
				_context.Table.TakeEvents(_reactions);
				_events.InsertRange(i + 1, _reactions);
				_reactions.Clear();
			}

			if (_events.Count == 0) return;

			foreach (TableEvent tableEvent in _events)
			{
				OutgoingMessage outgoing = new(tableEvent.Message);
				foreach (IClientConnection watcher in _context.Watchers)
				{
					if (_deadWatchers.Contains(watcher)) continue;
					if (!tableEvent.IsPublic && watcher.Player?.PlayerId != tableEvent.RecipientPlayerId) continue;

					TrySend(watcher, outgoing);
				}
			}

			_events.Clear();
			foreach (IClientConnection watcher in _deadWatchers)
			{
				_context.RemoveWatcher(watcher);
			}

			_deadWatchers.Clear();
		}

		private void NotifyListeners(TableEvent tableEvent)
		{
			foreach (ITableEventListener listener in _listeners)
			{
				try
				{
					listener.OnEvent(_context, tableEvent);
				}
				catch (Exception exception)
				{
					_logger.LogError(exception, "{Listener} failed on {Event} at {TableId}", listener.GetType().Name, tableEvent.Message.GetType().Name, TableId);
				}
			}
		}

		// Sessions queue the packet and return; a slow client never holds up the table.
		private void TrySend(IClientConnection watcher, OutgoingMessage message)
		{
			try
			{
				watcher.Send(message);
			}
			catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException)
			{
				_logger.LogInformation("{TableId} dropped watcher {ConnectionId}: {Reason}", TableId, watcher.Id, exception.Message);
				_deadWatchers.Add(watcher);
			}
		}
	}
}