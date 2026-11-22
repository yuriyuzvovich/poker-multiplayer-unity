using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Lobby.Domain;
using Poker.Client.Infrastructure.Core.Commands;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Shared.Features.Lobby.Requests;
using UnityEngine;

namespace Poker.Client.Features.Lobby.Application
{
	public sealed class LoadLobbyCommand : ICommand
	{
		private readonly IServerConnection _connection;
		private readonly LobbyModel _model;

		public LoadLobbyCommand(IServerConnection connection, LobbyModel model)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_model = model ?? throw new ArgumentNullException(nameof(model));
		}

		public string Name => "LoadLobby";

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			TableListResponse response;
			try
			{
				response = await _connection.RequestAsync<ListTablesRequest, TableListResponse>(new ListTablesRequest(), cancellationToken);
			}
			catch (Exception exception) when (RequestErrors.IsExpected(exception))
			{
				Debug.LogWarning("[LOBBY] Table list failed: " + exception.Message);
				return false;
			}

			_model.SetTables(response.Tables);
			return true;
		}
	}
}