using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Core.Presentation;
using VContainer;

namespace Poker.Client.Infrastructure.Core
{
	public static class CoreInstaller
	{
		public static void Install(IContainerBuilder builder)
		{
			builder.Register<AppStateMachine>(Lifetime.Singleton);
			builder.Register<IMessageBus, MessageBus>(Lifetime.Singleton);
			builder.Register<PresentationQueue>(Lifetime.Singleton);
			builder.Register<DisplayLock>(Lifetime.Singleton);
		}
	}
}