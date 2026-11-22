using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.TurnTimer
{
	public static class TurnTimerFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton(new TurnTimerOptions());
			services.AddSingleton<ITableEventListener, TimeoutStrikeListener>();
			services.AddSingleton<IMessageHandler, PlayerReturnHandler>();
		}
	}
}