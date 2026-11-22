using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Presentation;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Client.Features.Hand.Views;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.Hand
{
	public static class HandInstaller
	{
		public static void Install(IContainerBuilder builder)
		{
			builder.Register<HandModel>(Lifetime.Singleton);
			builder.Register<HandScene>(Lifetime.Singleton);
			builder.RegisterInstance(new HandTimings());
			builder.Register<HandLayer>(Lifetime.Singleton).As<IHandView>();
			builder.RegisterEntryPoint<HandPresenter>();
			builder.RegisterEntryPoint<HandDevActions>();
		}
	}
}