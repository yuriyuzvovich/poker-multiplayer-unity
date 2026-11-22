using Poker.Client.Features.Seating.Application;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.Seating
{
	public static class SeatingInstaller
	{
		public static void Install(IContainerBuilder builder) => builder.RegisterEntryPoint<SeatingFlow>();
	}
}