using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Core.Commands
{
	public sealed class RetryCommand : ICommand
	{
		private readonly ICommand _inner;
		private readonly int _attempts;
		private readonly TimeSpan _delay;

		public RetryCommand(ICommand inner, int attempts, TimeSpan delay)
		{
			if (attempts < 1) throw new ArgumentOutOfRangeException(nameof(attempts), attempts, "At least one attempt is required.");

			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_attempts = attempts;
			_delay = delay;
		}

		public string Name => _inner.Name;

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			for (int attempt = 1; attempt <= _attempts; attempt++)
			{
				if (attempt > 1) await UniTask.Delay(_delay, DelayType.Realtime, cancellationToken: cancellationToken);

				bool succeeded = await _inner.ExecuteAsync(cancellationToken);
				if (!succeeded) continue;

				return true;
			}

			return false;
		}
	}
}