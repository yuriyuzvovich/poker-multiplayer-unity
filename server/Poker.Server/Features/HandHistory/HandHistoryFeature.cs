using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.HandHistory
{
	// A read model: it only listens to table events and answers history requests; the Hand slice knows nothing of it.
	public static class HandHistoryFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton<HandHistoryBook>();
			services.AddSingleton<ITableEventListener, HandHistoryListener>();
			services.AddSingleton<IMessageHandler, GetHandHistoryHandler>();
		}
	}
}