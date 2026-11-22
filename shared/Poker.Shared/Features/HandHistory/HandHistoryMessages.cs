using Poker.Shared.Features.HandHistory.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.HandHistory
{
	public static class HandHistoryMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<GetHandHistoryRequest>(HandHistoryMessageNames.GetHistory);
			registry.Register<HandHistoryResponse>(HandHistoryMessageNames.History);
		}
	}
}