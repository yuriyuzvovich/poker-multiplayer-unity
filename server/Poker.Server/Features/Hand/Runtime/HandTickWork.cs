using System;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Actor;

namespace Poker.Server.Features.Hand.Runtime
{
	public sealed class HandTickWork : ITableTickWork
	{
		private readonly HandEngine _engine;

		public HandTickWork(HandEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			_engine.Tick(context);
			return Task.CompletedTask;
		}
	}
}