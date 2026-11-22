using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Features.Table
{
	public static class TableFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton<TableDirectory>();
			services.AddSingleton<IConnectionListener>(provider => provider.GetRequiredService<TableDirectory>());
			services.AddSingleton<IPlayerLocator>(provider => provider.GetRequiredService<TableDirectory>());
			services.AddSingleton<ITableTickWork>(ExpireReservationsWork.Instance);
			services.AddHostedService<TableHostService>();
			services.AddSingleton<IMessageHandler, JoinTableHandler>();
		}
	}
}