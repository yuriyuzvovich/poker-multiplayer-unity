using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Server.Features.Hand.Runtime
{
	public sealed class TurnActWork : ITableWork
	{
		private readonly MessageContext _request;
		private readonly TurnActRequest _act;
		private readonly HandEngine _engine;

		public TurnActWork(MessageContext request, TurnActRequest act, HandEngine engine)
		{
			_request = request;
			_act = act;
			_engine = engine;
		}

		public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
		{
			string error = _engine.Act(context, _request.Connection.Player!.PlayerId, _act);
			if (error.Length > 0) return _request.RejectAsync(error, "Action " + _act.Action + " refused.", cancellationToken);

			return _request.RespondAsync(new TurnActAcceptedResponse { TurnId = _act.TurnId }, cancellationToken);
		}
	}
}