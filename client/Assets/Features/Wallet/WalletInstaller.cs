using Poker.Client.Features.Wallet.Domain;
using Poker.Client.Features.Wallet.Presentation;
using Poker.Client.Features.Wallet.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.Wallet
{
	public static class WalletInstaller
	{
		public static void Install(IContainerBuilder builder, Transform uiRoot)
		{
			builder.Register<WalletModel>(Lifetime.Singleton);
			builder.RegisterComponent(WalletViewFactory.Create(uiRoot)).As<IWalletView>();
			builder.RegisterEntryPoint<WalletPresenter>();
		}
	}
}