using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Client.Infrastructure.Network.Requests
{
	public sealed class PendingRequests
	{
		private readonly Dictionary<ushort, UniTaskCompletionSource<IncomingMessage>> _pending = new();
		private ushort _lastId;

		public int Count => _pending.Count;

		public ushort Register()
		{
			do
			{
				_lastId++;
			}
			while (_lastId == 0 || _pending.ContainsKey(_lastId));

			_pending.Add(_lastId, new UniTaskCompletionSource<IncomingMessage>());
			return _lastId;
		}

		public async UniTask<IncomingMessage> WaitAsync(ushort requestId, TimeSpan timeout, CancellationToken cancellationToken)
		{
			if (!_pending.TryGetValue(requestId, out UniTaskCompletionSource<IncomingMessage> source))
			{
				throw new InvalidOperationException("Request " + requestId + " is not pending.");
			}

			try
			{
				using CancellationTokenRegistration cancellation = cancellationToken.RegisterWithoutCaptureExecutionContext(static state => ((UniTaskCompletionSource<IncomingMessage>)state).TrySetCanceled(), source);
				return await source.Task.Timeout(timeout, DelayType.Realtime);
			}
			finally
			{
				_pending.Remove(requestId);
			}
		}

		public bool TryComplete(IncomingMessage message)
		{
			ushort requestId = message.Header.RequestId;
			if (requestId == 0) return false;
			if (!_pending.TryGetValue(requestId, out UniTaskCompletionSource<IncomingMessage> source)) return false;

			source.TrySetResult(message);
			return true;
		}

		public void Cancel(ushort requestId)
		{
			if (!_pending.TryGetValue(requestId, out UniTaskCompletionSource<IncomingMessage> source)) return;

			_pending.Remove(requestId);
			if (source.TrySetCanceled()) return;

			source.Task.Forget(static _ => { });
		}

		public void Fail(ushort requestId, Exception exception)
		{
			if (!_pending.TryGetValue(requestId, out UniTaskCompletionSource<IncomingMessage> source)) return;

			source.TrySetException(exception);
		}

		public void FailAllExcept(ushort keptRequestId, Exception exception)
		{
			if (_pending.Count == 0) return;

			ushort[] requestIds = new ushort[_pending.Count];
			_pending.Keys.CopyTo(requestIds, 0);
			foreach (ushort requestId in requestIds)
			{
				if (requestId == keptRequestId) continue;

				Fail(requestId, exception);
			}
		}

		public void FailAll(Exception exception)
		{
			if (_pending.Count == 0) return;

			UniTaskCompletionSource<IncomingMessage>[] sources = new UniTaskCompletionSource<IncomingMessage>[_pending.Count];
			_pending.Values.CopyTo(sources, 0);
			foreach (UniTaskCompletionSource<IncomingMessage> source in sources)
			{
				source.TrySetException(exception);
			}
		}
	}
}