using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Actor;

namespace Poker.Server.Features.Table.Runtime.Work
{
	public sealed class ExpireReservationsWork : ITableTickWork
	{
		public static readonly ExpireReservationsWork Instance = new();

		private ExpireReservationsWork() { }

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			context.Table.ExpireReservations(context.Now);
			return Task.CompletedTask;
		}
	}
}