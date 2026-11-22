using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Popups
{
	public abstract class Popup<TResult> : MonoBehaviour
	{
		private readonly UniTaskCompletionSource<TResult> _result = new();

		public bool IsClosed { get; private set; }

		public UniTask<TResult> Result => _result.Task;

		public void Close(TResult result)
		{
			if (IsClosed) return;

			IsClosed = true;
			_result.TrySetResult(result);
		}
	}
}