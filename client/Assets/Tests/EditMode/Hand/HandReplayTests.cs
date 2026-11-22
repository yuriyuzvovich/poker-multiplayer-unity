using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Presentation;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Features.Wallet.Domain;
using Poker.Client.Features.Wallet.Presentation;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Diagnostics.Reports;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Tests.EditMode.Network;
using Poker.Client.Tests.EditMode.Wallet;
using Poker.Shared;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Models;
using Poker.Shared.Features.Wallet.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using UnityEngine;

namespace Poker.Client.Tests.EditMode.Hand
{
	public sealed class HandReplayTests
	{
		private RecordedHand _recorded;
		private FakeServerConnection _connection;
		private MessageBus _bus;
		private TableModel _table;
		private FakeHandView _view;
		private PresentationQueue _queue;
		private FakeServerClock _clock;
		private DisplayLock _displayLock;
		private PlayerSession _session;
		private WalletPresenter _wallet;
		private FakeWalletView _walletView;
		private HandPresenter _presenter;
		private int _heroSeat;

		[SetUp]
		public void SetUp()
		{
			_recorded = RecordedHand.Load();
			_heroSeat = _recorded.Last<HoleCardsEvent>().SeatIndex;
		}

		[TearDown]
		public void TearDown()
		{
			_wallet?.Dispose();
			_wallet = null;
			_presenter?.Dispose();
			_queue?.Dispose();
		}

		[Test]
		public void RecordedHand_ReplaysToTheFinalTableState()
		{
			CreatePresenter(HandTimings.Instant());

			Replay(_recorded.Messages.Count);

			AssertReplayedToTheFinalState();
		}

		[Test]
		public void ReopenedView_IsFilledFromTheScene()
		{
			CreatePresenter(HandTimings.Instant());
			Replay(_recorded.Messages.Count);
			string title = _view.Title;
			string status = _view.Status;
			List<string> highlighted = _view.Highlighted;

			_view.Reattach();

			Assert.AreEqual(title, _view.Title);
			Assert.AreEqual(status, _view.Status);
			CollectionAssert.AreEqual(highlighted, _view.Highlighted);
			CollectionAssert.AreEqual(_recorded.Last<ShowdownEvent>().Board, _view.Board);
			CollectionAssert.AreEqual(_recorded.Last<HoleCardsEvent>().Cards, _view.SeatLine(_heroSeat).Cards);
			Assert.IsFalse(_view.AreActionsVisible);
			foreach (SeatStack stack in _recorded.Last<HandEndedEvent>().Stacks)
			{
				Assert.AreEqual(stack.Stack, _view.SeatLine(stack.SeatIndex).Stack, "replayed stack of seat " + stack.SeatIndex);
			}
		}

		[Test]
		public void BugReport_TranscriptReplaysToTheSameFinalState_AndTheReportKeepsTheLastLogLines()
		{
			string reportsFolder = Path.Combine(Path.GetTempPath(), "poker-bug-reports-" + Guid.NewGuid().ToString("N"));
			try
			{
				string reportFolder = WriteBugReportOf(_recorded, reportsFolder);
				_recorded = RecordedHand.LoadFrom(Path.Combine(reportFolder, "transcript.json"));
				CreatePresenter(HandTimings.Instant());

				Replay(_recorded.Messages.Count);

				AssertReplayedToTheFinalState();
				string report = File.ReadAllText(Path.Combine(reportFolder, "report.txt"));
				StringAssert.Contains("log line 0\n", report, "the start of the session is kept");
				StringAssert.Contains("lines skipped", report);
				StringAssert.Contains("log line 599\n", report, "the last lines are kept");
				StringAssert.DoesNotContain("log line 60\n", report, "the middle is dropped");
			}
			finally
			{
				if (Directory.Exists(reportsFolder)) Directory.Delete(reportsFolder, true);
			}
		}

		private void AssertReplayedToTheFinalState()
		{
			ShowdownEvent showdown = _recorded.Last<ShowdownEvent>();
			HandEndedEvent ended = _recorded.Last<HandEndedEvent>();
			Assert.IsTrue(_queue.IsIdle);
			Assert.IsTrue(_view.IsVisible);
			CollectionAssert.AreEqual(showdown.Board, _view.Board);
			CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 4, 5, 5 }, _view.BoardHistory, "the board grows street by street, in network order");
			StringAssert.Contains(" wins ", _view.Status);
			StringAssert.Contains(" with ", _view.Status, "a showdown win names the winning hand");
			AssertWinningCardsHighlighted(showdown, ended);
			Assert.IsFalse(_view.AreActionsVisible);
			Assert.AreEqual(CountHeroTurns(_recorded.Messages.Count), _view.ShowActionsCount);
			CollectionAssert.AreEqual(_recorded.Last<HoleCardsEvent>().Cards, _view.SeatLine(_heroSeat).Cards);
			Assert.IsTrue(_view.SeatLine(_heroSeat).IsMine);
			foreach (SeatStack stack in ended.Stacks)
			{
				Assert.AreEqual(stack.Stack, _view.SeatLine(stack.SeatIndex).Stack, "hand view stack of seat " + stack.SeatIndex);
				Assert.AreEqual(stack.Stack, _table.Seats[stack.SeatIndex].Stack, "table seat stack of seat " + stack.SeatIndex);
			}
		}

		private void AssertWinningCardsHighlighted(ShowdownEvent showdown, HandEndedEvent ended)
		{
			List<string> allowed = new(showdown.Board);
			foreach (CardReveal reveal in showdown.Reveals)
			{
				foreach (PotAward award in ended.Awards)
				{
					if (award.SeatIndex != reveal.SeatIndex) continue;

					allowed.AddRange(reveal.Cards);
				}
			}

			Assert.GreaterOrEqual(_view.Highlighted.Count, 5, "the winner's best five cards are highlighted");
			foreach (string code in _view.Highlighted)
			{
				CollectionAssert.Contains(allowed, code, "only the board and the winners' own cards can be highlighted");
			}
		}

		[Test]
		public void MyTurn_ActionPanelSendsTheCurrentTurnId()
		{
			CreatePresenter(HandTimings.Instant());
			int heroTurnIndex = _recorded.IndexOf(m => m is TurnStartedEvent turn && turn.SeatIndex == _heroSeat);
			TurnStartedEvent heroTurn = (TurnStartedEvent)_recorded.Messages[heroTurnIndex].Payload;

			Replay(heroTurnIndex + 1);
			Assert.IsTrue(_view.AreActionsVisible);
			_view.Click(ActionButton.CheckOrCall);

			TurnActRequest request = (TurnActRequest)_connection.Requests[_connection.Requests.Count - 1];
			Assert.AreEqual(heroTurn.TurnId, request.TurnId);
			Assert.AreEqual(heroTurn.CanCheck ? PlayerAction.Check : PlayerAction.Call, request.Action);
			Assert.AreEqual("heads-up", request.TableId);
			Assert.IsFalse(_view.AreActionsVisible, "the panel hides until the server answers with turn.acted");
		}

		[Test]
		public void PreSelectedCheckFold_ActsOnMyTurnWithoutShowingTheButtons()
		{
			CreatePresenter(HandTimings.Instant());
			int heroTurnIndex = HeroFirstTurnIndex();
			TurnStartedEvent heroTurn = (TurnStartedEvent)_recorded.Messages[heroTurnIndex].Payload;
			Replay(heroTurnIndex);
			Assert.IsTrue(_view.ArePreActionsVisible, "pre-actions show while I wait for my turn");

			_view.Click(PreAction.CheckFold);
			Assert.AreEqual(PreAction.CheckFold, _view.ChosenPreAction, "the chosen pre-action is marked");
			int requestsBefore = _connection.Requests.Count;
			_connection.Deliver(heroTurn);

			Assert.AreEqual(requestsBefore + 1, _connection.Requests.Count, "the pre-action is sent when my turn starts");
			TurnActRequest request = (TurnActRequest)_connection.Requests[_connection.Requests.Count - 1];
			Assert.AreEqual(heroTurn.TurnId, request.TurnId);
			Assert.AreEqual(heroTurn.CanCheck ? PlayerAction.Check : PlayerAction.Fold, request.Action);
			Assert.IsFalse(_view.AreActionsVisible, "the action buttons stay hidden");
			Assert.IsFalse(_view.ArePreActionsVisible, "pre-actions hide on my turn");
		}

		[Test]
		public void SittingDown_FastForwardsTheAnimationBacklog()
		{
			CreatePresenter(SlowTimings());
			Replay(HeroFirstTurnIndex(), skipHeroSeated: true);
			Assert.IsFalse(_queue.IsIdle, "animations lag behind the network");
			Assert.AreEqual(0, _view.ShowActionsCount);

			_connection.Deliver(SeatedEvent(_heroSeat, _recorded.HeroPlayerId));

			Assert.IsTrue(_queue.IsIdle);
			CollectionAssert.AreEqual(_recorded.Last<HoleCardsEvent>().Cards, _view.SeatLine(_heroSeat).Cards);
			Assert.AreEqual(2, _view.Seats.Count);
		}

		[Test]
		public void Flights_DealTheCards_MoveEveryBet_AndPayTheWinner()
		{
			HandTimings timings = HandTimings.Instant();
			timings.ChipFlightSeconds = 0.4f;
			timings.CardFlightSeconds = 0.25f;
			timings.CelebrationSeconds = 0.9f;
			CreatePresenter(timings);

			Replay(_recorded.Messages.Count);

			AssertReplayedToTheFinalState();
			Assert.AreEqual("Deal:" + DealtSeats(), _view.Flights[0]);
			CollectionAssert.AreEqual(new[] { "Board:0", "Board:1", "Board:2", "Board:3", "Board:4" }, FlightsOf("Board:"));
			Assert.AreEqual(CountChipActions(), FlightsOf("AvatarToBet:").Count, "one flight per call or raise");
			CollectionAssert.IsNotEmpty(FlightsOf("BetToPot:"));
			Assert.AreEqual("PotToAvatar:" + WinnerSeats(), _view.Flights[_view.Flights.Count - 2]);
			Assert.AreEqual("Win:" + WinnerAmounts(), _view.Flights[_view.Flights.Count - 1], "the winner is celebrated with the amount won");
		}

		[Test]
		public void FastForward_StopsTheFlightInTheAir_AndShowsTheEndState()
		{
			CreatePresenter(SlowTimings());
			_view.HoldsFlights = true;
			Replay(HeroFirstTurnIndex(), skipHeroSeated: true);
			Assert.IsFalse(_queue.IsIdle);
			StringAssert.StartsWith("Deal:", _view.Flights[_view.Flights.Count - 1], "the deal is in the air");
			int stopsBefore = _view.StopFlightsCount;

			_connection.Deliver(SeatedEvent(_heroSeat, _recorded.HeroPlayerId));

			Assert.IsTrue(_queue.IsIdle);
			Assert.Greater(_view.StopFlightsCount, stopsBefore, "the flight in the air is stopped");
			CollectionAssert.AreEqual(_recorded.Last<HoleCardsEvent>().Cards, _view.SeatLine(_heroSeat).Cards);
		}

		[Test]
		public void SomeoneElseSittingDown_DoesNotFastForward()
		{
			CreatePresenter(SlowTimings());
			Replay(HeroFirstTurnIndex(), skipHeroSeated: true);

			_connection.Deliver(SeatedEvent(1 - _heroSeat, "bot-edsger"));

			Assert.IsFalse(_queue.IsIdle);
			CollectionAssert.AreEqual(new[] { "", "" }, _view.SeatLine(_heroSeat).Cards, "hole cards still face down behind the deal");
		}

		[Test]
		public void MyTurn_FastForwardsTheBacklog_SoThePanelShowsAtOnce()
		{
			CreatePresenter(SlowTimings());

			Replay(HeroFirstTurnIndex() + 1, skipHeroSeated: true);

			Assert.IsTrue(_queue.IsIdle);
			Assert.IsTrue(_view.AreActionsVisible);
			Assert.AreEqual("Your turn", _view.Status);
		}

		[Test]
		public void Countdown_ShowsTheVisibleTime_AndClosesInputWhenItEnds()
		{
			CreatePresenter(HandTimings.Instant());
			int heroTurnIndex = HeroFirstTurnIndex();
			TurnStartedEvent heroTurn = (TurnStartedEvent)_recorded.Messages[heroTurnIndex].Payload;
			long visibleEnd = heroTurn.DeadlineUnixMs - heroTurn.LeewayMs;
			Replay(heroTurnIndex + 1);

			_clock.NowUnixMs = visibleEnd - 4500;
			_presenter.Tick();
			Assert.AreEqual("5", _view.Timer);
			Assert.AreEqual(_heroSeat, _view.TimerSeat, "the ring sits on the acting seat");
			Assert.AreEqual(1f, _view.TimerRemaining, 0.001f, "the ring starts full");
			Assert.IsTrue(_view.AreActionsVisible);

			_clock.NowUnixMs = visibleEnd - 2250;
			_presenter.Tick();
			Assert.AreEqual(0.5f, _view.TimerRemaining, 0.001f, "the ring drains with the visible time");

			_clock.NowUnixMs = visibleEnd;
			_presenter.Tick();
			_view.Click(ActionButton.CheckOrCall);

			Assert.AreEqual("", _view.Timer);
			Assert.AreEqual(0f, _view.TimerRemaining, 0.001f);
			Assert.AreEqual("Time is up", _view.Status);
			Assert.IsFalse(_view.AreActionsVisible);
			Assert.IsEmpty(_connection.Requests, "nothing is sent once the visible time is over; the leeway belongs to the network");
		}

		[Test]
		public void BalanceChangedDuringMyHand_StaysOffScreenUntilHandEndedIsShown()
		{
			CreatePresenter(SlowTimings());
			CreateWallet(3000);
			int handEndedIndex = _recorded.IndexOf(m => m is HandEndedEvent);
			Replay(handEndedIndex);
			Assert.AreEqual("Balance 3,000", _walletView.Balance);

			_connection.Deliver(new BalanceChangedEvent { Balance = 4321, Delta = 1321, Reason = BalanceChangeReason.CashOut });
			_connection.Deliver(_recorded.Messages[handEndedIndex].Payload);

			Assert.AreEqual("Balance 3,000", _walletView.Balance, "the network already said hand.ended, the screen has not shown it yet");
			_queue.FastForward();
			Assert.AreEqual("Balance 4,321", _walletView.Balance);
		}

		[Test]
		public void BalanceChangedOutsideMyHand_ShowsAtOnce()
		{
			CreatePresenter(SlowTimings());
			CreateWallet(3000);

			_connection.Deliver(new BalanceChangedEvent { Balance = 2500, Delta = -500, Reason = BalanceChangeReason.BuyIn });

			Assert.AreEqual("Balance 2,500", _walletView.Balance);
		}

		[Test]
		public void LeavingTheTableMidHand_ReleasesTheBalanceDisplay()
		{
			CreatePresenter(SlowTimings());
			CreateWallet(3000);
			Replay(_recorded.IndexOf(m => m is HandEndedEvent));
			_connection.Deliver(new BalanceChangedEvent { Balance = 4000, Delta = 1000, Reason = BalanceChangeReason.CashOut });

			_bus.Publish(new TableLeft());

			Assert.AreEqual("Balance 4,000", _walletView.Balance);
		}

		private void CreatePresenter(HandTimings timings)
		{
			_connection = new FakeServerConnection();
			_bus = new MessageBus();
			_session = new PlayerSession();
			_session.Establish("session-1", _recorded.HeroPlayerId, "Hero");
			_displayLock = new DisplayLock();
			_clock = new FakeServerClock();
			_table = new TableModel();
			_view = new FakeHandView();
			_queue = new PresentationQueue();
			HandModel hand = new();
			_presenter = new HandPresenter(_connection, _bus, _session, _clock, _table, hand, new HandScene(_view, _table, hand), _view, _queue, timings, _displayLock);
			_presenter.Initialize();
		}

		private void Replay(int count, bool skipHeroSeated = false)
		{
			for (int i = 0; i < count; i++)
			{
				RecordedMessage message = _recorded.Messages[i];
				if (message.Payload is TableSnapshot snapshot)
				{
					_table.Load(snapshot);
					_bus.Publish(new TableJoined(snapshot.TableId));
					continue;
				}

				if (message.RequestId != 0) continue;

				if (message.Payload is SeatReservedEvent reserved) _table.ApplySeat(reserved.TableId, reserved.Seat);
				if (message.Payload is PlayerSeatedEvent seated)
				{
					_table.ApplySeat(seated.TableId, seated.Seat);
					if (skipHeroSeated && seated.Seat.PlayerId == _recorded.HeroPlayerId) continue;
				}

				_connection.Deliver(message.Payload);
			}
		}

		private static string WriteBugReportOf(RecordedHand hand, string reportsFolder)
		{
			MessageRegistry registry = MessageCatalog.CreateRegistry();
			MessageCodec codec = new(registry, new NewtonsoftPayloadSerializer());
			TranscriptRecorder transcript = new(TableMessageNames.Snapshot);
			foreach (RecordedMessage message in hand.Messages)
			{
				PacketCodec.TryDecode(new ArraySegment<byte>(codec.Encode(message.Payload, requestId: message.RequestId)), out PacketHeader header, out ArraySegment<byte> payload);
				registry.TryGet(header.TypeId, out MessageType type);
				transcript.Record(type, header.RequestId, payload);
			}

			LogRingBuffer log = new(50, 500);
			for (int i = 0; i < 600; i++)
			{
				log.Add("log line " + i, "", LogType.Log);
			}

			PlayerSession session = new();
			session.Establish("session-1", hand.HeroPlayerId, "Hero");
			NetworkSettings settings = new(new Uri("ws://localhost:1/ws"), TimeSpan.FromSeconds(1), "Hero", "", TimeSpan.Zero);
			ServerConnection connection = new(codec, registry, settings, new NetworkStats(), new IPacketTap[] { transcript });
			return new BugReportWriter(log, transcript, session, new ServerClock(), connection, reportsFolder).Write();
		}

		private void CreateWallet(long balance)
		{
			_connection.Responses[typeof(BalanceResponse)] = new BalanceResponse { Balance = balance };
			_walletView = new FakeWalletView();
			_wallet = new WalletPresenter(_connection, _session, _displayLock, new WalletModel(), _walletView);
			_wallet.Initialize();
		}

		private List<string> FlightsOf(string prefix)
		{
			List<string> flights = new();
			foreach (string flight in _view.Flights)
			{
				if (!flight.StartsWith(prefix, StringComparison.Ordinal)) continue;

				flights.Add(flight);
			}

			return flights;
		}

		private string DealtSeats()
		{
			List<int> seats = new();
			foreach (HandPlayerInfo player in _recorded.Last<HandStartedEvent>().Players)
			{
				if (player.IsFolded) continue;

				seats.Add(player.SeatIndex);
			}

			return string.Join(",", seats);
		}

		private string WinnerAmounts()
		{
			List<int> seats = new();
			List<long> amounts = new();
			foreach (PotAward award in _recorded.Last<HandEndedEvent>().Awards)
			{
				if (award.Amount <= 0) continue;

				int index = seats.IndexOf(award.SeatIndex);
				if (index >= 0)
				{
					amounts[index] += award.Amount;
					continue;
				}

				seats.Add(award.SeatIndex);
				amounts.Add(award.Amount);
			}

			List<string> parts = new();
			for (int i = 0; i < seats.Count; i++)
			{
				parts.Add(seats[i] + "+" + amounts[i]);
			}

			return string.Join(",", parts);
		}

		private string WinnerSeats()
		{
			List<int> seats = new();
			foreach (PotAward award in _recorded.Last<HandEndedEvent>().Awards)
			{
				if (award.Amount <= 0) continue;
				if (seats.Contains(award.SeatIndex)) continue;

				seats.Add(award.SeatIndex);
			}

			return string.Join(",", seats);
		}

		private int CountChipActions()
		{
			int actions = 0;
			foreach (RecordedMessage message in _recorded.Messages)
			{
				if (message.Payload is not TurnActedEvent acted) continue;
				if (acted.Action is not (PlayerAction.Call or PlayerAction.Raise)) continue;

				actions++;
			}

			return actions;
		}

		private int HeroFirstTurnIndex() =>_recorded.IndexOf(m => m is TurnStartedEvent turn && turn.SeatIndex == _heroSeat);

		private int CountHeroTurns(int count)
		{
			int turns = 0;
			for (int i = 0; i < count; i++)
			{
				if (_recorded.Messages[i].Payload is not TurnStartedEvent turn) continue;
				if (turn.SeatIndex != _heroSeat) continue;

				turns++;
			}

			return turns;
		}

		private static PlayerSeatedEvent SeatedEvent(int seatIndex, string playerId) => new PlayerSeatedEvent
		{
			TableId = "heads-up",
			Seat = new SeatSnapshot { Index = seatIndex, Status = SeatStatus.Seated, PlayerId = playerId, PlayerName = playerId, Stack = 2000 },
		};

		private static HandTimings SlowTimings() => new HandTimings
		{
			HandStartedSeconds = 30f,
			HoleCardsSeconds = 30f,
			PlayerActedSeconds = 30f,
			RoundEndedSeconds = 30f,
			BoardCardSeconds = 30f,
			ShowdownSeconds = 30f,
			HandEndedSeconds = 30f,
		};
	}
}