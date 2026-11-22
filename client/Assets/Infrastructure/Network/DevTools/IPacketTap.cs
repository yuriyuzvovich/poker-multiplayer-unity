using System;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Client.Infrastructure.Network.DevTools
{
	// Sees every packet that passed the reliability checks; debug tools such as the transcript recorder plug in here.
	public interface IPacketTap
	{
		public void OnReceived(MessageType type, ushort requestId, ArraySegment<byte> payload);
	}
}