using Poker.Client.Features.TurnTimer.Presentation;
using Poker.Client.Features.TurnTimer.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.TurnTimer
{
	public static class TurnTimerInstaller
	{
		public static void Install(IContainerBuilder builder, Transform uiRoot)
		{
			builder.RegisterComponent(AwayViewFactory.Create(uiRoot)).As<IAwayView>();
			builder.RegisterEntryPoint<SitOutPresenter>();
		}
	}
}