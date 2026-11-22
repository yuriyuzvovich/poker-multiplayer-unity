using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Core.Commands
{
	public sealed class SequenceCommand : ICommand
	{
		private readonly ICommand[] _steps;

		public SequenceCommand(string name, params ICommand[] steps)
		{
			Name = name;
			_steps = steps ?? throw new ArgumentNullException(nameof(steps));
		}

		public string Name { get; }

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			foreach (ICommand step in _steps)
			{
				bool succeeded = await step.ExecuteAsync(cancellationToken);
				if (succeeded) continue;

				return false;
			}

			return true;
		}
	}
}