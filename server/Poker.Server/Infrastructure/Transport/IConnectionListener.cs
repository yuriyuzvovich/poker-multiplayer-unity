using System.Threading.Tasks;

namespace Poker.Server.Infrastructure.Transport
{
	public interface IConnectionListener
	{
		public Task OnDisconnectedAsync(IClientConnection connection);

		public Task OnReplacedAsync(IClientConnection connection);
	}
}