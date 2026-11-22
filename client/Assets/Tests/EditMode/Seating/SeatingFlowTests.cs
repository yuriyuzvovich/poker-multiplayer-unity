using System.Collections.Generic;
using NUnit.Framework;
using Poker.Client.Features.Seating.Application;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.UI.Popups;
using Poker.Client.Infrastructure.UI.Views;
using Poker.Client.Tests.EditMode.Network;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using UnityEngine;

namespace Poker.Client.Tests.EditMode.Seating
{
	// The paths that end before the buy-in popup; the popup itself loads through Addressables and is covered in Play Mode.
	public sealed class SeatingFlowTests
	{
		private const string TableId = "practice";

		private GameObject _host;
		private FakeServerConnection _connection;
		private MessageBus _bus;
		private SeatingFlow _flow;
		private List<string> _notices;

		[SetUp]
		public void SetUp()
		{
			_host = new GameObject("seating-test");
			_connection = new FakeServerConnection();
			_bus = new MessageBus();
			TableModel table = new();
			table.Load(new TableSnapshot { TableId = TableId, Seats = new[] { new SeatSnapshot { Index = 0 }, new SeatSnapshot { Index = 1 } } });
			PopupService popups = new(UiLayers.Create(_host.transform), new ViewLoader());
			_flow = new SeatingFlow(_connection, _bus, popups, new FakeServerClock(), table);
			_flow.Initialize();
			_notices = new List<string>();
			_bus.Subscribe<TableStatusMessage>(message => _notices.Add(message.Text));
		}

		[TearDown]
		public void TearDown()
		{
			_flow.Dispose();
			Object.DestroyImmediate(_host);
		}

		[Test]
		public void Sit_AsksTheServerToReserveThatSeat_AtThatTable()
		{
			_connection.Failures[typeof(ReserveSeatRequest)] = new RequestRejectedException(SeatErrorCodes.SeatTaken, "Seat 1 is taken.");

			_bus.Publish(new SitRequested(TableId, 1));

			ReserveSeatRequest request = (ReserveSeatRequest)_connection.Requests[0];
			Assert.AreEqual(TableId, request.TableId);
			Assert.AreEqual(1, request.SeatIndex);
		}

		[Test]
		public void RefusedReservation_TellsThePlayerWhy()
		{
			_connection.Failures[typeof(ReserveSeatRequest)] = new RequestRejectedException(SeatErrorCodes.SeatTaken, "Seat 1 is taken.");

			_bus.Publish(new SitRequested(TableId, 1));

			Assert.AreEqual(new[] { "Cannot sit there: " + SeatErrorCodes.SeatTaken + ": Seat 1 is taken." }, _notices);
		}

		[Test]
		public void StandingUpDuringAHand_SaysTheSeatIsKeptUntilItEnds()
		{
			_connection.Responses[typeof(StandUpAcceptedResponse)] = new StandUpAcceptedResponse { SeatIndex = 1, LeavesAfterHand = true };

			_bus.Publish(new StandUpRequested(TableId));

			Assert.AreEqual(TableId, ((StandUpRequest)_connection.Requests[0]).TableId);
			Assert.AreEqual(new[] { "You leave the seat when this hand ends." }, _notices);
		}

		[Test]
		public void StandingUpBetweenHands_SaysNothing()
		{
			_connection.Responses[typeof(StandUpAcceptedResponse)] = new StandUpAcceptedResponse { SeatIndex = 1, LeavesAfterHand = false };

			_bus.Publish(new StandUpRequested(TableId));

			Assert.IsEmpty(_notices);
		}

		[Test]
		public void RefusedStandUp_TellsThePlayerWhy()
		{
			_connection.Failures[typeof(StandUpRequest)] = new RequestRejectedException(SeatErrorCodes.NotSeated, "You have no seat.");

			_bus.Publish(new StandUpRequested(TableId));

			Assert.AreEqual(new[] { "Cannot stand up: " + SeatErrorCodes.NotSeated + ": You have no seat." }, _notices);
		}

		[Test]
		public void ReleasedSeat_ThatWasNotReservedByMe_SaysNothing()
		{
			_connection.Deliver(new SeatReleasedEvent { TableId = TableId, SeatIndex = 1, Reason = SeatReleaseReason.ReservationExpired });

			Assert.IsEmpty(_notices);
		}
	}
}