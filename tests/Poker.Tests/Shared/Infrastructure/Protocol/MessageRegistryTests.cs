using System;
using System.Collections.Generic;
using Poker.Shared;
using Poker.Shared.Features.Hand;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Lobby;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Features.Seating;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Xunit;

namespace Poker.Tests.Shared.Infrastructure.Protocol
{
	public sealed class MessageRegistryTests
	{
		[Theory]
		[InlineData("", 0x811C9DC5u)]
		[InlineData("a", 0xE40C292Cu)]
		[InlineData("foobar", 0xBF9CF968u)]
		public void MessageTypeId_MatchesFnv1aReferenceVectors(string name, uint expected)
		{
			Assert.Equal(expected, MessageTypeId.Compute(name));
		}

		[Fact]
		public void MessageTypeId_RejectsNonAsciiNames()
		{
			Assert.Throws<ArgumentException>(() => MessageTypeId.Compute("table.joiné"));
		}

		[Fact]
		public void Catalog_HasNoTypeIdCollisions()
		{
			MessageRegistry registry = MessageCatalog.CreateRegistry();

			HashSet<uint> ids = new();
			foreach (MessageType messageType in registry.All)
			{
				Assert.True(ids.Add(messageType.Id), messageType.Name);
				Assert.Same(messageType, registry.Get(messageType.ClrType));
			}

			Assert.NotEmpty(ids);
		}

		[Theory]
		[InlineData(typeof(ErrorResponse), ProtocolMessages.Error)]
		[InlineData(typeof(HelloRequest), ProtocolMessages.Hello)]
		[InlineData(typeof(WelcomeResponse), ProtocolMessages.Welcome)]
		[InlineData(typeof(SeatReservedEvent), TableMessageNames.SeatReserved)]
		[InlineData(typeof(PlayerSeatedEvent), TableMessageNames.PlayerSeated)]
		[InlineData(typeof(SeatReleasedEvent), TableMessageNames.SeatReleased)]
		[InlineData(typeof(ReserveSeatRequest), SeatingMessageNames.ReserveSeat)]
		[InlineData(typeof(SeatReservationResponse), SeatingMessageNames.SeatReservation)]
		[InlineData(typeof(BuyInRequest), SeatingMessageNames.BuyIn)]
		[InlineData(typeof(BuyInAcceptedResponse), SeatingMessageNames.BuyInAccepted)]
		[InlineData(typeof(StandUpRequest), SeatingMessageNames.StandUp)]
		[InlineData(typeof(StandUpAcceptedResponse), SeatingMessageNames.StandUpAccepted)]
		[InlineData(typeof(HandStartedEvent), HandMessageNames.HandStarted)]
		[InlineData(typeof(HoleCardsEvent), HandMessageNames.HoleCards)]
		[InlineData(typeof(HandSnapshotEvent), HandMessageNames.HandSnapshot)]
		[InlineData(typeof(TurnStartedEvent), HandMessageNames.TurnStarted)]
		[InlineData(typeof(TurnActRequest), HandMessageNames.TurnAct)]
		[InlineData(typeof(TurnActAcceptedResponse), HandMessageNames.TurnActAccepted)]
		[InlineData(typeof(TurnActedEvent), HandMessageNames.TurnActed)]
		[InlineData(typeof(RoundEndedEvent), HandMessageNames.RoundEnded)]
		[InlineData(typeof(RoundStartedEvent), HandMessageNames.RoundStarted)]
		[InlineData(typeof(ShowdownEvent), HandMessageNames.Showdown)]
		[InlineData(typeof(HandEndedEvent), HandMessageNames.HandEnded)]
		[InlineData(typeof(ListTablesRequest), LobbyMessageNames.ListTables)]
		[InlineData(typeof(TableListResponse), LobbyMessageNames.TableList)]
		[InlineData(typeof(JoinTableRequest), TableMessageNames.JoinTable)]
		[InlineData(typeof(TableSnapshot), TableMessageNames.Snapshot)]
		public void Catalog_RegistersEveryProtocolMessageUnderItsName(Type clrType, string name)
		{
			Assert.Equal(name, MessageCatalog.CreateRegistry().Get(clrType).Name);
		}

		[Fact]
		public void Register_SameNameTwice_Throws()
		{
			MessageRegistry registry = new();
			registry.Register<FirstMessage>("test.message");

			Assert.Throws<InvalidOperationException>(() => registry.Register<SecondMessage>("test.message"));
		}

		[Fact]
		public void Register_SameTypeTwice_Throws()
		{
			MessageRegistry registry = new();
			registry.Register<FirstMessage>("test.first");

			Assert.Throws<InvalidOperationException>(() => registry.Register<FirstMessage>("test.other"));
		}

		[Fact]
		public void Get_UnregisteredType_Throws()
		{
			Assert.Throws<KeyNotFoundException>(() => new MessageRegistry().Get(typeof(FirstMessage)));
		}

		private sealed class FirstMessage { }

		private sealed class SecondMessage { }
	}
}