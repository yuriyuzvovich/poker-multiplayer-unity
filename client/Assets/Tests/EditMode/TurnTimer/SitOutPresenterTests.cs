using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.TurnTimer.Presentation;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Tests.EditMode.Network;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Features.TurnTimer.Events;
using Poker.Shared.Features.TurnTimer.Requests;

namespace Poker.Client.Tests.EditMode.TurnTimer
{
	public sealed class SitOutPresenterTests
	{
		private const string TableId = "heads-up";

		private FakeServerConnection _connection;
		private TableModel _table;
		private PresentationQueue _queue;
		private FakeAwayView _view;
		private SitOutPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_connection = new FakeServerConnection();
			PlayerSession session = new();
			session.Establish("session-1", "p-hero", "Hero");
			_table = new TableModel();
			_table.Load(new TableSnapshot
			{
				TableId = TableId,
				Seats = new[]
				{
					new SeatSnapshot { Index = 0, Status = SeatStatus.Seated, PlayerId = "bot-edsger", PlayerName = "Edsger", Stack = 2500 },
					new SeatSnapshot { Index = 1, Status = SeatStatus.Seated, PlayerId = "p-hero", PlayerName = "Hero", Stack = 2000 },
				},
			});
			_queue = new PresentationQueue();
			_queue.Enqueue(new EndlessAnimation());
			_view = new FakeAwayView();
			_presenter = new SitOutPresenter(_connection, new MessageBus(), session, _table, _queue, _view);
			_presenter.Initialize();
		}

		[TearDown]
		public void TearDown()
		{
			_presenter.Dispose();
			_queue.Dispose();
		}

		[Test]
		public void ISatOut_ShowsTheBannerAtOnce_EvenBehindARunningAnimation()
		{
			_connection.Deliver(new PlayerSatOutEvent { TableId = TableId, SeatIndex = 1, PlayerId = "p-hero" });

			Assert.IsTrue(_table.Seats[1].IsSittingOut);
			Assert.IsTrue(_view.IsVisible);
			Assert.IsFalse(_queue.IsIdle, "the running animation is neither waited for nor skipped");
		}

		[Test]
		public void SomeoneElseSatOut_MarksTheirSeat_WithoutMyBanner()
		{
			_connection.Deliver(new PlayerSatOutEvent { TableId = TableId, SeatIndex = 0, PlayerId = "bot-edsger" });

			Assert.IsTrue(_table.Seats[0].IsSittingOut);
			Assert.IsFalse(_view.IsVisible);
		}

		[Test]
		public void IAmBack_SendsTheRequest_AndTheBannerWaitsForTheServerEvent()
		{
			_connection.Deliver(new PlayerSatOutEvent { TableId = TableId, SeatIndex = 1, PlayerId = "p-hero" });

			_view.ClickReturn();
			Assert.AreEqual(TableId, ((PlayerReturnRequest)_connection.Requests[0]).TableId);
			Assert.IsTrue(_view.IsVisible);

			_connection.Deliver(new PlayerReturnedEvent { TableId = TableId, SeatIndex = 1, PlayerId = "p-hero" });
			Assert.IsFalse(_table.Seats[1].IsSittingOut);
			Assert.IsFalse(_view.IsVisible);
		}

		private sealed class EndlessAnimation : IVisualCommand
		{
			public bool RunsOutOfOrder => false;

			public UniTask PlayAsync(CancellationToken cancellationToken) => UniTask.Never(cancellationToken);

			public void Finish() { }
		}
	}
}