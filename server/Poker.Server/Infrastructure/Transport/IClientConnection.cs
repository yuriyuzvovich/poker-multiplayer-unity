using System.Threading;
using System.Threading.Tasks;

namespace Poker.Server.Infrastructure.Transport
{
	public interface IClientConnection
	{
		public string Id { get; }

		public PlayerIdentity? Player { get; }

		public Task SendAsync<TMessage>(TMessage message, ushort requestId, CancellationToken cancellationToken) where TMessage : class;

		public void Send(OutgoingMessage message);
	}
}