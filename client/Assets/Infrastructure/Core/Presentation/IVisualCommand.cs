using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Core.Presentation
{
	public interface IVisualCommand
	{
		public bool RunsOutOfOrder { get; }

		public UniTask PlayAsync(CancellationToken cancellationToken);

		public void Finish();
	}
}