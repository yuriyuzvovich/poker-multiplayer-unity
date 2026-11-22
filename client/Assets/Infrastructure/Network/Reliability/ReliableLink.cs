using System;
using System.Collections.Generic;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.Reliability;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Client.Infrastructure.Network.Reliability
{
	// The client side of reliable messaging above TCP: numbering and acks once a session exists, the resend after a
	// resumed welcome, and fresh numbering when the server starts a new session.
	public sealed class ReliableLink
	{
		private ReliableChannel _channel = new();
		private int _maxUnacked = 30;

		public bool IsEstablished { get; private set; }

		public string ResumeKey { get; private set; } = "";

		public TimeSpan HeartbeatInterval { get; private set; } = TimeSpan.FromSeconds(10);

		public ushort LastReceived => _channel.LastReceived;

		public bool IsAckDue => IsEstablished && _channel.ReceivedSinceAck >= _maxUnacked;

		// Returns whether the packet got a sequence number and is kept for a resend.
		public bool Stamp(byte[] packet, MessageType messageType)
		{
			bool isNumbered = IsEstablished && !ProtocolMessages.IsUnnumbered(messageType);
			if (!isNumbered)
			{
				_channel.StampUnnumbered(packet);
				return false;
			}

			if (!_channel.TryStamp(packet)) throw new ConnectionClosedException("the resend buffer is full; the server stopped acknowledging");

			return true;
		}

		public ReceiveResult Receive(in PacketHeader header) => _channel.Receive(header);

		public WelcomeKind ApplyWelcome(WelcomeResponse welcome, in PacketHeader header, List<byte[]> resend)
		{
			ResumeKey = welcome.ResumeKey;
			if (welcome.HeartbeatMs > 0) HeartbeatInterval = TimeSpan.FromMilliseconds(welcome.HeartbeatMs);
			if (welcome.MaxUnacked > 0) _maxUnacked = welcome.MaxUnacked;

			if (welcome.Resumed)
			{
				_channel.CollectResend(header.Ack, resend);
				return WelcomeKind.Resumed;
			}

			if (!IsEstablished)
			{
				IsEstablished = true;
				return WelcomeKind.Started;
			}

			_channel = new ReliableChannel();
			return WelcomeKind.Restarted;
		}
	}
}