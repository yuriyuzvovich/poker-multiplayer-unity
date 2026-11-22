using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.Hand
{
	public static class HandFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton<IDeckFactory, SecureDeckFactory>();
			services.AddSingleton(new HandOptions());
			services.AddSingleton<HandEngine>();
			services.AddSingleton<ITableTickWork, HandTickWork>();
			services.AddSingleton<IMessageHandler, TurnActHandler>();
		}
	}
}