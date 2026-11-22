using VContainer;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Client.Infrastructure.Network
{
	public static class NetworkInstaller
	{
		public static void Install(IContainerBuilder builder)
		{
			builder.Register<IPayloadSerializer, NewtonsoftPayloadSerializer>(Lifetime.Singleton);
			builder.Register<MessageCodec>(Lifetime.Singleton);
			builder.Register<NetworkStats>(Lifetime.Singleton);
			builder.Register<ServerConnection>(Lifetime.Singleton).As<IServerConnection>().AsSelf();
			builder.Register<PlayerSession>(Lifetime.Singleton);
			builder.Register<ServerClock>(Lifetime.Singleton).As<IServerClock>().AsSelf();
		}
	}
}