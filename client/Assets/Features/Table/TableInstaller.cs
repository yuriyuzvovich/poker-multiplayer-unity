using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Presentation;
using Poker.Client.Features.Table.Views;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.Table
{
	public static class TableInstaller
	{
		public static void Install(IContainerBuilder builder)
		{
			builder.Register<TableModel>(Lifetime.Singleton);
			builder.Register<TableToolbar>(Lifetime.Singleton);
			builder.Register<TableScreen>(Lifetime.Singleton).As<ITableView>().AsSelf();
			builder.RegisterEntryPoint<TablePresenter>();
		}
	}
}