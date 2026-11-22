using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Core.Commands
{
	public sealed class ParallelCommand : ICommand
	{
		private readonly ICommand[] _branches;

		public ParallelCommand(string name, params ICommand[] branches)
		{
			Name = name;
			_branches = branches ?? throw new ArgumentNullException(nameof(branches));
		}

		public string Name { get; }

		public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
		{
			UniTask<bool>[] running = new UniTask<bool>[_branches.Length];
			for (int i = 0; i < _branches.Length; i++)
			{
				running[i] = _branches[i].ExecuteAsync(cancellationToken);
			}

			bool[] results = await UniTask.WhenAll(running);
			foreach (bool result in results)
			{
				if (result) continue;

				return false;
			}

			return true;
		}
	}
}