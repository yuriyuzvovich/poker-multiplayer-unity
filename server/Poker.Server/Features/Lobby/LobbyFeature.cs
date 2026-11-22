using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.Lobby
{
	public static class LobbyFeature
	{
		public static void Install(IServiceCollection services) => services.AddSingleton<IMessageHandler, ListTablesHandler>();
	}
}