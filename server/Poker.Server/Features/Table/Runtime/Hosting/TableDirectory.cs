using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Time;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Lobby.Models;
using Poker.Shared.Features.Table;

namespace Poker.Server.Features.Table.Runtime.Hosting
{
	public sealed class TableDirectory : IConnectionListener, IPlayerLocator
	{
		private readonly List<TableActor> _actors = new();

		public TableDirectory(IClock clock, ILoggerFactory loggerFactory, IEnumerable<ITableEventListener> listeners, IChipBank bank)
		{
			if (clock == null) throw new ArgumentNullException(nameof(clock));
			if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
			if (listeners == null) throw new ArgumentNullException(nameof(listeners));
			if (bank == null) throw new ArgumentNullException(nameof(bank));

			List<ITableEventListener> tableListeners = new(listeners);

			foreach (PokerTable table in TableSeed.CreateTables(bank))
			{
				_actors.Add(new TableActor(table, clock, loggerFactory.CreateLogger<TableActor>(), tableListeners));
			}
		}

		public IReadOnlyList<TableActor> Actors => _actors;

		public bool TryGet(string tableId, [NotNullWhen(true)] out TableActor? actor)
		{
			foreach (TableActor candidate in _actors)
			{
				if (candidate.TableId != tableId) continue;

				actor = candidate;
				return true;
			}

			actor = null;
			return false;
		}

		public Task PostOrRejectAsync(MessageContext context, string tableId, ITableWork work, CancellationToken cancellationToken)
		{
			if (!TryGet(tableId, out TableActor? actor))
			{
				return context.RejectAsync(TableErrorCodes.TableNotFound, "No table with id '" + tableId + "'.", cancellationToken);
			}

			actor.Post(work);
			return Task.CompletedTask;
		}

		public TableSummary[] GetSummaries()
		{
			TableSummary[] summaries = new TableSummary[_actors.Count];
			for (int i = 0; i < _actors.Count; i++)
			{
				summaries[i] = _actors[i].Summary;
			}

			return summaries;
		}

		public string FindSeatedTableId(string playerId)
		{
			foreach (TableActor actor in _actors)
			{
				if (!actor.IsSeatHeldBy(playerId)) continue;

				return actor.TableId;
			}

			return "";
		}

		public Task OnDisconnectedAsync(IClientConnection connection) => PostToAll(connection, false);

		public Task OnReplacedAsync(IClientConnection connection) => PostToAll(connection, true);

		private Task PostToAll(IClientConnection connection, bool keepsSeat)
		{
			foreach (TableActor actor in _actors)
			{
				actor.Post(new DisconnectWork(connection, keepsSeat));
			}

			return Task.CompletedTask;
		}
	}
}