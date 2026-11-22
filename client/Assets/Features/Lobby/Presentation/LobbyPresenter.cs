using System;
using System.Collections.Generic;
using Poker.Client.Features.Lobby.Domain;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Shared.Features.Lobby.Models;
using VContainer.Unity;

namespace Poker.Client.Features.Lobby.Presentation
{
	public sealed class LobbyPresenter : IInitializable, IDisposable
	{
		private readonly LobbyModel _model;
		private readonly ILobbyView _view;
		private readonly IMessageBus _bus;
		private readonly AppStateMachine _appState;
		private readonly List<IDisposable> _subscriptions = new();

		public LobbyPresenter(LobbyModel model, ILobbyView view, IMessageBus bus, AppStateMachine appState)
		{
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_appState = appState ?? throw new ArgumentNullException(nameof(appState));
		}

		public void Initialize()
		{
			_model.Changed += Render;
			_view.TableClicked += OnTableClicked;
			_appState.Changed += OnAppStateChanged;
			_subscriptions.Add(_bus.Subscribe<TableJoined>(OnTableJoined));
			_subscriptions.Add(_bus.Subscribe<TableJoinFailed>(OnTableJoinFailed));
			_subscriptions.Add(_bus.Subscribe<TableLeft>(OnTableLeft));
			_view.SetVisible(true);
			ShowAppState(_appState.Current);
			Render();
		}

		public void Dispose()
		{
			_model.Changed -= Render;
			_view.TableClicked -= OnTableClicked;
			_appState.Changed -= OnAppStateChanged;
			foreach (IDisposable subscription in _subscriptions)
			{
				subscription.Dispose();
			}

			_subscriptions.Clear();
		}

		private void Render()
		{
			IReadOnlyList<TableSummary> tables = _model.Tables;
			List<LobbyEntry> entries = new(tables.Count);
			for (int i = 0; i < tables.Count; i++)
			{
				TableSummary table = tables[i];
				string blinds = "Blinds " + table.SmallBlind + "/" + table.BigBlind;
				string seats = table.OccupiedSeats + "/" + table.SeatCount;
				entries.Add(new LobbyEntry(table.TableId, table.Name, blinds, seats));
			}

			_view.ShowTables(entries);
			if (entries.Count > 0) _view.SetStatus("Pick a table.");
		}

		private void OnTableClicked(string tableId)
		{
			_view.SetStatus("Joining...");
			_bus.Publish(new JoinTableRequested(tableId));
		}

		private void OnTableJoined(TableJoined joined) => _view.SetVisible(false);

		private void OnTableJoinFailed(TableJoinFailed failed) => _view.SetStatus("Could not join " + failed.TableId + ": " + failed.Reason);

		private void OnTableLeft(TableLeft left)
		{
			_view.SetVisible(true);
			_view.SetStatus("Pick a table.");
		}

		private void OnAppStateChanged(AppState previous, AppState next) => ShowAppState(next);

		private void ShowAppState(AppState state) => _view.SetStatus(state switch
		{
			AppState.Booting => "Starting...",
			AppState.Connecting => "Connecting to " + _appState.Detail + "...",
			AppState.Connected => "Loading tables...",
			AppState.Disconnected => "Disconnected: " + _appState.Detail,
			AppState.Failed => "Server unreachable: " + _appState.Detail,
			_ => state.ToString(),
		});
	}
}