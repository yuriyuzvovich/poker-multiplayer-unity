using System.Collections.Generic;
using NUnit.Framework;
using Poker.Client.Infrastructure.Network.Reliability;
using Poker.Shared;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class ReliableLinkTests
	{
		private readonly MessageRegistry _registry = MessageCatalog.CreateRegistry();
		private readonly List<byte[]> _resend = new();

		[Test]
		public void Stamp_BeforeTheFirstWelcome_SendsGameMessagesUnnumbered()
		{
			ReliableLink link = new();

			bool isNumbered = link.Stamp(Packet(), _registry.Get(typeof(ListTablesRequest)));

			Assert.IsFalse(isNumbered);
		}

		[Test]
		public void FirstWelcome_StartsNumbering_AndASecondFreshWelcomeRestartsIt()
		{
			ReliableLink link = new();

			Assert.AreEqual(WelcomeKind.Started, link.ApplyWelcome(new WelcomeResponse { ResumeKey = "k1" }, default, _resend));
			Assert.IsTrue(link.Stamp(Packet(), _registry.Get(typeof(ListTablesRequest))));
			Assert.AreEqual(WelcomeKind.Restarted, link.ApplyWelcome(new WelcomeResponse { ResumeKey = "k2" }, default, _resend));
			Assert.AreEqual("k2", link.ResumeKey);
			Assert.IsTrue(link.IsEstablished);
		}

		[Test]
		public void ResumedWelcome_HandsBackTheUnacknowledgedPackets()
		{
			ReliableLink link = new();
			link.ApplyWelcome(new WelcomeResponse(), default, _resend);
			link.Stamp(Packet(), _registry.Get(typeof(ListTablesRequest)));
			link.Stamp(Packet(), _registry.Get(typeof(ListTablesRequest)));

			WelcomeKind kind = link.ApplyWelcome(new WelcomeResponse { Resumed = true }, default, _resend);

			Assert.AreEqual(WelcomeKind.Resumed, kind);
			Assert.AreEqual(2, _resend.Count);
		}

		[Test]
		public void AckIsDue_AfterTheServersUnackedLimit()
		{
			ReliableLink link = new();
			link.ApplyWelcome(new WelcomeResponse { MaxUnacked = 2 }, default, _resend);

			link.Receive(new PacketHeader(1, 0, 0, 0, 0, PacketFlags.None));
			Assert.IsFalse(link.IsAckDue);
			link.Receive(new PacketHeader(2, 0, 0, 0, 0, PacketFlags.None));

			Assert.IsTrue(link.IsAckDue);
		}

		private static byte[] Packet() => PacketCodec.Encode(new PacketHeader(0, 0, 1, 0, 0, PacketFlags.None), new byte[] { 1 });
	}
}