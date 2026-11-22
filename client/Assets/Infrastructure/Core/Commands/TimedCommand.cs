using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Poker.Client.Infrastructure.Core.Commands
{
	public sealed class TimedCommand : ICommand
	{
		private readonly ICommand _inner;

		public TimedCommand(ICommand inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

		public string Name => _inner.Name;

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			bool succeeded = await _inner.ExecuteAsync(cancellationToken);
			Debug.Log("[INIT] " + Name + (succeeded ? " done" : " FAILED") + " in " + stopwatch.ElapsedMilliseconds + " ms");
			return succeeded;
		}
	}
}