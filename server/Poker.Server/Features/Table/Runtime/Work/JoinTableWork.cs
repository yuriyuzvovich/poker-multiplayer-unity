using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.Table.Runtime.Work
{
	public sealed class JoinTableWork : ITableWork
	{
		private readonly MessageContext _request;

		public JoinTableWork(MessageContext request) => _request = request;

		public async Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			context.AddWatcher(_request.Connection);
			await _request.RespondAsync(context.Table.CreateSnapshot(), cancellationToken);
			foreach (ISnapshotContributor contributor in context.SnapshotContributors)
			{
				await contributor.SendSnapshotAsync(context, _request.Connection, cancellationToken);
			}
		}
	}
}