using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Lobby.Application;
using Poker.Client.Features.Lobby.Domain;
using Poker.Client.Features.Lobby.Presentation;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Tests.EditMode.Network;
using Poker.Shared.Features.Lobby.Models;
using Poker.Shared.Features.Lobby.Requests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poker.Client.Tests.EditMode.Lobby
{
	public sealed class LobbyTests
	{
		private FakeServerConnection _connection;
		private LobbyModel _model;
		private FakeLobbyView _view;
		private MessageBus _bus;
		private AppStateMachine _appState;
		private LobbyPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_connection = new FakeServerConnection();
			_model = new LobbyModel();
			_view = new FakeLobbyView();
			_bus = new MessageBus();
			_appState = new AppStateMachine();
			_presenter = new LobbyPresenter(_model, _view, _bus, _appState);
			_presenter.Initialize();
		}

		[TearDown]
		public void TearDown() => _presenter.Dispose();

		[UnityTest]
		public IEnumerator LoadLobby_FillsTheModel_FromTheTableList() => UniTask.ToCoroutine(async () =>
		{
			_connection.Responses[typeof(TableListResponse)] = new TableListResponse { Tables = new[] { Practice() } };
			LoadLobbyCommand command = new(_connection, _model);

			bool loaded = await command.ExecuteAsync(CancellationToken.None);

			Assert.IsTrue(loaded);
			Assert.AreEqual(1, _model.Tables.Count);
			Assert.AreEqual("practice", _model.Tables[0].TableId);
		});

		[UnityTest]
		public IEnumerator LoadLobby_WhenRejected_FailsAndKeepsTheModel() => UniTask.ToCoroutine(async () =>
		{
			_model.SetTables(new[] { Practice() });
			_connection.Failures[typeof(ListTablesRequest)] = new RequestRejectedException("session.required", "Say hello first.");
			LoadLobbyCommand command = new(_connection, _model);
			LogAssert.Expect(LogType.Warning, new Regex("Table list failed"));

			bool loaded = await command.ExecuteAsync(CancellationToken.None);

			Assert.IsFalse(loaded);
			Assert.AreEqual(1, _model.Tables.Count);
		});

		[Test]
		public void Presenter_ShowsEachTable_WithBlindsAndSeats()
		{
			_model.SetTables(new[] { Practice() });

			LobbyEntry entry = _view.Entries[0];
			Assert.AreEqual("practice", entry.TableId);
			Assert.AreEqual("Practice", entry.Name);
			Assert.AreEqual("Blinds 10/20", entry.Blinds);
			Assert.AreEqual("4/6", entry.Seats);
			Assert.AreEqual("Pick a table.", _view.Status);
		}

		[Test]
		public void ClickingATable_PublishesAJoinRequest_AndSaysSo()
		{
			List<JoinTableRequested> requests = new();
			using IDisposable subscription = _bus.Subscribe<JoinTableRequested>(requests.Add);

			_view.Click("practice");

			Assert.AreEqual(1, requests.Count);
			Assert.AreEqual("practice", requests[0].TableId);
			Assert.AreEqual("Joining...", _view.Status);
		}

		[Test]
		public void Lobby_HidesWhenATableIsJoined_AndComesBackWhenItIsLeft()
		{
			_bus.Publish(new TableJoined("practice"));
			Assert.IsFalse(_view.IsVisible);

			_bus.Publish(new TableLeft());

			Assert.IsTrue(_view.IsVisible);
			Assert.AreEqual("Pick a table.", _view.Status);
		}

		[Test]
		public void FailedJoin_ShowsTheReason()
		{
			_bus.Publish(new TableJoinFailed("practice", "table.not_found"));

			Assert.AreEqual("Could not join practice: table.not_found", _view.Status);
		}

		[Test]
		public void Status_FollowsTheConnection()
		{
			_appState.TransitionTo(AppState.Connecting, "ws://localhost:5080/ws");
			Assert.AreEqual("Connecting to ws://localhost:5080/ws...", _view.Status);

			_appState.TransitionTo(AppState.Failed, "refused");

			Assert.AreEqual("Server unreachable: refused", _view.Status);
		}

		[Test]
		public void Dispose_StopsReactingToTheView()
		{
			_presenter.Dispose();
			List<JoinTableRequested> requests = new();
			using IDisposable subscription = _bus.Subscribe<JoinTableRequested>(requests.Add);

			_view.Click("practice");

			Assert.AreEqual(0, requests.Count);
		}

		private static TableSummary Practice() => new() { TableId = "practice", Name = "Practice", SmallBlind = 10, BigBlind = 20, SeatCount = 6, OccupiedSeats = 4 };

		private sealed class FakeLobbyView : ILobbyView
		{
			public event Action<string> TableClicked;

			public bool IsVisible { get; private set; }
			public string Status { get; private set; } = "";
			public List<LobbyEntry> Entries { get; private set; } = new();

			public void SetVisible(bool isVisible) => IsVisible = isVisible;

			public void SetStatus(string status) => Status = status;

			public void ShowTables(IReadOnlyList<LobbyEntry> entries) => Entries = new List<LobbyEntry>(entries);

			public void Click(string tableId) => TableClicked?.Invoke(tableId);
		}
	}
}