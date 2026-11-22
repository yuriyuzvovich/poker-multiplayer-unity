using Poker.Client.Features.HandHistory.Presentation;
using VContainer;
using VContainer.Unity;

namespace Poker.Client.Features.HandHistory
{
	public static class HandHistoryInstaller
	{
		public static void Install(IContainerBuilder builder) => builder.RegisterEntryPoint<HandHistoryFlow>();
	}
}