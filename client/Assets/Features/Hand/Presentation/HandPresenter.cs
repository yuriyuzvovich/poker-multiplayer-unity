using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Presentation.Commands.Betting;
using Poker.Client.Features.Hand.Presentation.Commands.Lifecycle;
using Poker.Client.Features.Hand.Presentation.Scene;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Table.Events;
using VContainer.Unity;

namespace Poker.Client.Features.Hand.Presentation
{
	public sealed class HandPresenter : IInitializable, ITickable, IDisposable
	{
		private readonly IServerConnection _connection;
		private readonly IMessageBus _bus;
		private readonly PlayerSession _session;
		private readonly IServerClock _clock;
		private readonly TableModel _table;
		private readonly HandModel _model;
		private readonly HandScene _scene;
		private readonly IHandView _view;
		private readonly PresentationQueue _queue;
		private readonly HandTimings _timings;
		private readonly DisplayLock _displayLock;
		private readonly CancellationTokenSource _lifetime = new();
		private readonly List<IDisposable> _subscriptions = new();
		private readonly List<DisplayHold> _displayHolds = new();
		private DisplayHold _handHold;

		public HandPresenter(
			IServerConnection connection,
			IMessageBus bus,
			PlayerSession session,
			IServerClock clock,
			TableModel table,
			HandModel model,
			HandScene scene,
			IHandView view,
			PresentationQueue queue,
			HandTimings timings,
			DisplayLock displayLock)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_scene = scene ?? throw new ArgumentNullException(nameof(scene));
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_timings = timings ?? throw new ArgumentNullException(nameof(timings));
			_displayLock = displayLock ?? throw new ArgumentNullException(nameof(displayLock));
		}

		public void Initialize()
		{
			_view.Attached += _scene.Replay;
			_view.ActionClicked += OnActionClicked;
			_view.PreActionClicked += OnPreActionClicked;
			_subscriptions.Add(_bus.Subscribe<TableJoined>(OnTableJoined));
			_subscriptions.Add(_bus.Subscribe<TableLeft>(OnTableLeft));
			_subscriptions.Add(_bus.Subscribe<SessionRestarted>(OnSessionRestarted));
			_subscriptions.Add(_connection.Subscribe<PlayerSeatedEvent>(OnPlayerSeated));
			_subscriptions.Add(_connection.Subscribe<HandStartedEvent>(OnHandStarted));
			_subscriptions.Add(_connection.Subscribe<HandSnapshotEvent>(OnHandSnapshot));
			_subscriptions.Add(_connection.Subscribe<HoleCardsEvent>(OnHoleCards));
			_subscriptions.Add(_connection.Subscribe<TurnStartedEvent>(OnTurnStarted));
			_subscriptions.Add(_connection.Subscribe<TurnActedEvent>(OnTurnActed));
			_subscriptions.Add(_connection.Subscribe<RoundEndedEvent>(OnRoundEnded));
			_subscriptions.Add(_connection.Subscribe<RoundStartedEvent>(OnRoundStarted));
			_subscriptions.Add(_connection.Subscribe<ShowdownEvent>(OnShowdown));
			_subscriptions.Add(_connection.Subscribe<HandEndedEvent>(OnHandEnded));
			_scene.Reset();
			_view.SetVisible(false);
		}

		public void Tick() => _scene.UpdateTimer(_clock.NowUnixMs);

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
			ReleaseDisplayHolds();
			_view.Attached -= _scene.Replay;
			_view.ActionClicked -= OnActionClicked;
			_view.PreActionClicked -= OnPreActionClicked;
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void OnTableJoined(TableJoined joined) => _view.SetVisible(true);

		private void OnTableLeft(TableLeft left)
		{
			ReleaseDisplayHolds();
			_queue.Clear();
			_model.Clear();
			_scene.Reset();
			_view.SetVisible(false);
		}

		private void OnSessionRestarted(SessionRestarted restarted)
		{
			ReleaseDisplayHolds();
			_queue.Clear();
			_model.Clear();
			_scene.Reset();
		}

		private void OnPlayerSeated(PlayerSeatedEvent seated)
		{
			if (!IsCurrentTable(seated.TableId)) return;
			if (!_session.IsMe(seated.Seat.PlayerId)) return;

			_queue.FastForward();
		}

		private void OnHandStarted(HandStartedEvent started)
		{
			if (!IsCurrentTable(started.TableId)) return;

			int mySeat = FindMySeat(started.Players);
			_model.Start(started.HandNumber, started.Players, mySeat);
			AcquireHandHold(mySeat);

			_queue.Enqueue(new HandStartedCommand(_scene, started, mySeat, _timings.HandStartedSeconds, _timings.CardFlightSeconds));
		}

		private void OnHandSnapshot(HandSnapshotEvent snapshot)
		{
			if (!IsCurrentTable(snapshot.TableId)) return;

			int mySeat = FindMySeat(snapshot.Players);
			_queue.Clear();
			_model.Load(snapshot, mySeat);
			_queue.Enqueue(new HandSnapshotCommand(_scene, snapshot, mySeat));
		}

		private void OnHoleCards(HoleCardsEvent cards)
		{
			if (!IsCurrentHand(cards.TableId, cards.HandNumber)) return;

			_model.SetMyCards(cards.Cards);
			_queue.Enqueue(new HoleCardsCommand(_scene, cards, _timings.HoleCardsSeconds));
		}

		private void OnTurnStarted(TurnStartedEvent turn)
		{
			if (!IsCurrentHand(turn.TableId, turn.HandNumber)) return;

			_model.StartTurn(turn);
			bool isMine = turn.SeatIndex == _model.MySeat;
			if (isMine) _queue.FastForward();

			_queue.Enqueue(new TurnStartedCommand(_scene, turn, isMine));
			if (!isMine) return;

			PlayerAction? preAction = _model.TakePreAction(turn);
			if (preAction == null) return;

			ActAsync(preAction.Value, 0).Forget();
		}

		private void OnTurnActed(TurnActedEvent acted)
		{
			if (!IsCurrentHand(acted.TableId, acted.HandNumber)) return;

			_model.EndTurn(acted.TurnId);
			_queue.Enqueue(new PlayerActedCommand(_scene, acted, _timings.PlayerActedSeconds, _timings.ChipFlightSeconds));
		}

		private void OnRoundEnded(RoundEndedEvent ended)
		{
			if (!IsCurrentHand(ended.TableId, ended.HandNumber)) return;

			_queue.Enqueue(new RoundEndedCommand(_scene, ended, _timings.RoundEndedSeconds, _timings.ChipFlightSeconds));
		}

		private void OnRoundStarted(RoundStartedEvent round)
		{
			if (!IsCurrentHand(round.TableId, round.HandNumber)) return;

			_model.SetBoard(round.Board);
			_queue.Enqueue(new RoundStartedCommand(_scene, round, _timings.BoardCardSeconds, _timings.CardFlightSeconds));
		}

		private void OnShowdown(ShowdownEvent showdown)
		{
			if (!IsCurrentHand(showdown.TableId, showdown.HandNumber)) return;

			_queue.Enqueue(new ShowdownCommand(_scene, showdown, _timings.ShowdownSeconds));
		}

		private void OnHandEnded(HandEndedEvent ended)
		{
			if (!IsCurrentHand(ended.TableId, ended.HandNumber)) return;

			foreach (SeatStack stack in ended.Stacks)
			{
				_table.ApplyStack(ended.TableId, stack.SeatIndex, _model.PlayerAt(stack.SeatIndex), stack.Stack);
			}

			_model.End();
			_queue.Enqueue(new HandEndedCommand(_scene, ended, _handHold, _timings.HandEndedSeconds, _timings.ChipFlightSeconds, _timings.CelebrationSeconds));
			_handHold = null;
		}

		private void OnActionClicked(ActionButton button)
		{
			TurnStartedEvent turn = _model.CurrentTurn;
			if (turn == null) return;

			PlayerAction action = button switch
			{
				ActionButton.Fold => PlayerAction.Fold,
				ActionButton.CheckOrCall => turn.CanCheck ? PlayerAction.Check : PlayerAction.Call,
				_ => PlayerAction.Raise,
			};
			long amount = button switch
			{
				ActionButton.MinRaise => turn.MinRaiseTo,
				ActionButton.AllIn => turn.MaxRaiseTo,
				_ => 0,
			};
			ActAsync(action, amount).Forget();
		}

		private void OnPreActionClicked(PreAction preAction)
		{
			_model.TogglePreAction(preAction);
			_scene.RefreshPreActions();
		}

		private async UniTaskVoid ActAsync(PlayerAction action, long amount)
		{
			if (!_model.IsInputOpen(_clock.NowUnixMs)) return;

			TurnStartedEvent turn = _model.CurrentTurn;
			TurnActRequest request = new() { TableId = _table.TableId, TurnId = turn.TurnId, Action = action, Amount = amount };
			_scene.HideActions();
			try
			{
				await _connection.RequestAsync<TurnActRequest, TurnActAcceptedResponse>(request, _lifetime.Token);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				_bus.Publish(new TableStatusMessage("Action refused: " + exception.Message));
				if (_model.CurrentTurn != turn) return;
				if (!_model.IsInputOpen(_clock.NowUnixMs)) return;

				_scene.ShowActions(turn);
			}
		}

		private void AcquireHandHold(int mySeat)
		{
			for (int i = _displayHolds.Count - 1; i >= 0; i--)
			{
				if (!_displayHolds[i].IsReleased) continue;

				_displayHolds.RemoveAt(i);
			}

			_handHold = null;
			if (mySeat < 0) return;

			_handHold = _displayLock.Acquire();
			_displayHolds.Add(_handHold);
		}

		private void ReleaseDisplayHolds()
		{
			foreach (DisplayHold hold in _displayHolds)
			{
				hold.Dispose();
			}

			_displayHolds.Clear();
			_handHold = null;
		}

		private int FindMySeat(HandPlayerInfo[] players)
		{
			foreach (HandPlayerInfo player in players)
			{
				if (!_session.IsMe(player.PlayerId)) continue;

				return player.SeatIndex;
			}

			return -1;
		}

		private bool IsCurrentTable(string tableId) => _table.IsJoined && tableId == _table.TableId;

		private bool IsCurrentHand(string tableId, long handNumber) => IsCurrentTable(tableId) && _model.Accepts(handNumber);
	}
}