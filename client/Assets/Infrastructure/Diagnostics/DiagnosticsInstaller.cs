using UnityEngine;
using VContainer;
using VContainer.Unity;
using Poker.Client.Infrastructure.Diagnostics.DevPanel;
using Poker.Client.Infrastructure.Diagnostics.Reports;

namespace Poker.Client.Infrastructure.Diagnostics
{
	public static class DiagnosticsInstaller
	{
		public static void Install(IContainerBuilder builder, Transform uiRoot, string reportsFolder)
		{
			builder.RegisterEntryPoint<LogRingBuffer>().WithParameter("headCapacity", 50).WithParameter("tailCapacity", 500).AsSelf();
			builder.Register<BugReportWriter>(Lifetime.Singleton).WithParameter(reportsFolder);
			builder.RegisterComponent(DevPanelViewFactory.Create(uiRoot)).As<IDevPanelView>();
			builder.Register<DevPanelActions>(Lifetime.Singleton);
			builder.RegisterEntryPoint<DevPanelPresenter>();
		}
	}
}