using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.Seating
{
	public static class SeatingFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton<IMessageHandler, ReserveSeatHandler>();
			services.AddSingleton<IMessageHandler, BuyInHandler>();
			services.AddSingleton<IMessageHandler, StandUpHandler>();
		}
	}
}