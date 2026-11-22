using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Server.Features.Hand
{
	public sealed class TurnActHandler : MessageHandler<TurnActRequest>
	{
		private readonly TableDirectory _directory;
		private readonly HandEngine _engine;

		public TurnActHandler(TableDirectory directory, HandEngine engine)
		{
			_directory = directory ?? throw new ArgumentNullException(nameof(directory));
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		protected override Task HandleAsync(MessageContext context, TurnActRequest message, CancellationToken cancellationToken) =>
			_directory.PostOrRejectAsync(context, message.TableId, new TurnActWork(context, message, _engine), cancellationToken);
	}
}