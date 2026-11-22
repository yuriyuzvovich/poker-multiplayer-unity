using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Table.Events;
using Poker.Client.Infrastructure.Core.Commands;
using Poker.Client.Infrastructure.Core.Messaging;
using UnityEngine;

namespace Poker.Client.App.Startup
{
	public sealed class ReturnToTableCommand : ICommand
	{
		private readonly HelloCommand _hello;
		private readonly IMessageBus _bus;

		public ReturnToTableCommand(HelloCommand hello, IMessageBus bus)
		{
			_hello = hello ?? throw new ArgumentNullException(nameof(hello));
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
		}

		public string Name => "ReturnToTable";

		public UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			string tableId = _hello.SeatedTableId;
			if (tableId.Length == 0) return UniTask.FromResult(true);

			Debug.Log("[INIT] Still seated at " + tableId + "; returning to it");
			_bus.Publish(new JoinTableRequested(tableId));
			return UniTask.FromResult(true);
		}
	}
}