using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Features.Table.Runtime.Actor
{
	public interface ISnapshotContributor
	{
		public Task SendSnapshotAsync(TableContext context, IClientConnection viewer, CancellationToken cancellationToken);
	}
}