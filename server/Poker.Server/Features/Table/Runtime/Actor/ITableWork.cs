using System.Threading;
using System.Threading.Tasks;

namespace Poker.Server.Features.Table.Runtime.Actor
{
	public interface ITableWork
	{
		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken);
	}
}