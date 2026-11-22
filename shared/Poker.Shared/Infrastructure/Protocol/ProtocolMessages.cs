using System;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Shared.Infrastructure.Protocol
{
	public static class ProtocolMessages
	{
		public const string Error = "error";
		public const string Hello = "sys.hello";
		public const string Welcome = "sys.welcome";
		public const string Ping = "sys.ping";
		public const string Pong = "sys.pong";
		public const string Ack = "sys.ack";

		public static bool IsUnnumbered(MessageType messageType) => messageType.Name.StartsWith("sys.", StringComparison.Ordinal);

		public static void Register(MessageRegistry registry)
		{
			registry.Register<ErrorResponse>(Error);
			registry.Register<HelloRequest>(Hello);
			registry.Register<WelcomeResponse>(Welcome);
			registry.Register<PingRequest>(Ping);
			registry.Register<PongResponse>(Pong);
			registry.Register<AckMessage>(Ack);
		}
	}
}