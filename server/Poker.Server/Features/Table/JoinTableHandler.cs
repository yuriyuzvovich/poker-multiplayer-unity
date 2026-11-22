using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Table.Requests;

namespace Poker.Server.Features.Table
{
	public sealed class JoinTableHandler : MessageHandler<JoinTableRequest>
	{
		private readonly TableDirectory _directory;

		public JoinTableHandler(TableDirectory directory) => _directory = directory ?? throw new ArgumentNullException(nameof(directory));

		protected override Task HandleAsync(MessageContext context, JoinTableRequest message, CancellationToken cancellationToken) =>
			_directory.PostOrRejectAsync(context, message.TableId, new JoinTableWork(context), cancellationToken);
	}
}