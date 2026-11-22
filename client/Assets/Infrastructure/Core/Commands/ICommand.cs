using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Core.Commands
{
	public interface ICommand
	{
		public string Name { get; }

		public UniTask<bool> ExecuteAsync(CancellationToken cancellationToken);
	}
}