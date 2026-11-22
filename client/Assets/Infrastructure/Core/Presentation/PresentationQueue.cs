using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Poker.Client.Infrastructure.Core.Presentation
{
	public sealed class PresentationQueue : IDisposable
	{
		private readonly Queue<IVisualCommand> _pending = new();
		private CancellationTokenSource _pump;
		private IVisualCommand _playing;

		public bool IsIdle => _pump == null && _pending.Count == 0;

		public int PendingCount => _pending.Count;

		public void Enqueue(IVisualCommand command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));

			if (command.RunsOutOfOrder)
			{
				command.Finish();
				return;
			}

			_pending.Enqueue(command);
			if (_pump != null) return;

			_pump = new CancellationTokenSource();
			PumpAsync(_pump.Token).Forget();
		}

		public void FastForward()
		{
			IVisualCommand playing = _playing;
			StopPump();
			playing?.Finish();
			while (_pending.Count > 0) _pending.Dequeue().Finish();
		}

		public void Clear()
		{
			StopPump();
			_pending.Clear();
		}

		public void Dispose() => Clear();

		private async UniTaskVoid PumpAsync(CancellationToken cancellationToken)
		{
			while (_pending.Count > 0)
			{
				IVisualCommand command = _pending.Dequeue();
				_playing = command;
				try
				{
					await command.PlayAsync(cancellationToken);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				if (cancellationToken.IsCancellationRequested) return;
			}

			_playing = null;
			_pump.Dispose();
			_pump = null;
		}

		private void StopPump()
		{
			CancellationTokenSource pump = _pump;
			_pump = null;
			_playing = null;
			if (pump == null) return;

			pump.Cancel();
			pump.Dispose();
		}
	}
}