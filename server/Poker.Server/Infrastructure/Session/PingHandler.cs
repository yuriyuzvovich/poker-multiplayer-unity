using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Time;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Server.Infrastructure.Session
{
	public sealed class PingHandler : MessageHandler<PingRequest>
	{
		private readonly IClock _clock;

		public PingHandler(IClock clock) => _clock = clock ?? throw new ArgumentNullException(nameof(clock));

		protected override Task HandleAsync(MessageContext context, PingRequest message, CancellationToken cancellationToken) =>
			context.RespondAsync(new PongResponse
			{
				ClientTimeUnixMs = message.ClientTimeUnixMs,
				ServerTimeUnixMs = _clock.UtcNow.ToUnixTimeMilliseconds(),
			}, cancellationToken);
	}
}