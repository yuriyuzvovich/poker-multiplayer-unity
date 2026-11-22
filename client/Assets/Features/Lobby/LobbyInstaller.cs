using Poker.Client.Features.Lobby.Application;
using Poker.Client.Features.Lobby.Domain;
using Poker.Client.Features.Lobby.Presentation;
using Poker.Client.Features.Lobby.Views;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.Lobby
{
	public static class LobbyInstaller
	{
		public static void Install(IContainerBuilder builder)
		{
			builder.Register<LobbyModel>(Lifetime.Singleton);
			builder.Register<LoadLobbyCommand>(Lifetime.Singleton);
			builder.Register<LobbyScreen>(Lifetime.Singleton).As<ILobbyView>();
			builder.RegisterEntryPoint<LobbyPresenter>();
		}
	}
}