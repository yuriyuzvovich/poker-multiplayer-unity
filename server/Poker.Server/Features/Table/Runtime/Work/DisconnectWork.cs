using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Table.Models;

namespace Poker.Server.Features.Table.Runtime.Work
{
	public sealed class DisconnectWork : ITableWork
	{
		private readonly IClientConnection _connection;
		private readonly bool _keepsSeat;

		public DisconnectWork(IClientConnection connection, bool keepsSeat = false)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_keepsSeat = keepsSeat;
		}

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			context.RemoveWatcher(_connection);
			if (_keepsSeat) return Task.CompletedTask;

			PlayerIdentity? player = _connection.Player;
			if (player == null) return Task.CompletedTask;

			context.Table.ReleasePlayer(player.PlayerId, SeatReleaseReason.Disconnected);
			return Task.CompletedTask;
		}
	}
}