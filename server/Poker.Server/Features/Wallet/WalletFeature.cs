using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Messaging;

namespace Poker.Server.Features.Wallet
{
	public static class WalletFeature
	{
		public static void Install(IServiceCollection services)
		{
			services.AddSingleton(new WalletOptions());
			services.AddSingleton<WalletBook>();
			services.AddSingleton<IChipBank>(provider => provider.GetRequiredService<WalletBook>());
			services.AddSingleton<IMessageHandler, GetBalanceHandler>();
			services.AddSingleton<IMessageHandler, AddChipsHandler>();
		}
	}
}