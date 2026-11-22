using System;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Server.Infrastructure.Transport
{
	// A broadcast message serialized once and copied per receiver; each copy then gets that receiver's seq and ack.
	// Used by one table actor at a time, so the cached packet needs no lock.
	public sealed class OutgoingMessage
	{
		private byte[]? _packet;
		private MessageCodec? _encodedBy;

		public OutgoingMessage(object message) => Message = message ?? throw new ArgumentNullException(nameof(message));

		public object Message { get; }

		public byte[] CopyPacket(MessageCodec codec)
		{
			if (_packet == null || _encodedBy != codec)
			{
				_packet = codec.Encode(Message);
				_encodedBy = codec;
			}

			return (byte[])_packet.Clone();
		}
	}
}