using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Poker.Client.Infrastructure.Network.DevTools
{
	// Development network faults: added latency in both directions and a blackout that refuses new connections.
	public sealed class LinkConditioner : IDisposable
	{
		private readonly Queue<DelayedPacket> _incoming = new();
		private readonly TimeSpan _oneWayDelay;
		private Action<ArraySegment<byte>> _deliver;
		private CancellationTokenSource _pump;
		private DateTime _blackoutUntil;

		public LinkConditioner(TimeSpan roundTrip) => _oneWayDelay = TimeSpan.FromTicks(roundTrip.Ticks / 2);

		public bool IsActive => _oneWayDelay > TimeSpan.Zero;

		public bool IsBlackedOut => DateTime.UtcNow < _blackoutUntil;

		public void BlackOut(TimeSpan duration) => _blackoutUntil = DateTime.UtcNow + duration;

		public void Bind(Action<ArraySegment<byte>> deliver) => _deliver = deliver ?? throw new ArgumentNullException(nameof(deliver));

		public DateTime OutgoingDueAt() => DateTime.UtcNow + _oneWayDelay;

		public UniTask WaitUntilAsync(DateTime dueAt, CancellationToken cancellationToken)
		{
			TimeSpan wait = dueAt - DateTime.UtcNow;
			return IsActive && wait > TimeSpan.Zero ? UniTask.Delay(wait, DelayType.Realtime, cancellationToken: cancellationToken) : UniTask.CompletedTask;
		}

		public void Receive(ArraySegment<byte> packet)
		{
			if (!IsActive)
			{
				_deliver(packet);
				return;
			}

			byte[] copy = new byte[packet.Count];
			Array.Copy(packet.Array!, packet.Offset, copy, 0, packet.Count);
			_incoming.Enqueue(new DelayedPacket(DateTime.UtcNow + _oneWayDelay, copy));
			if (_pump != null) return;

			_pump = new CancellationTokenSource();
			PumpAsync(_pump.Token).Forget();
		}

		public void Reset()
		{
			_incoming.Clear();
			if (_pump == null) return;

			_pump.Cancel();
			_pump.Dispose();
			_pump = null;
		}

		public void Dispose() => Reset();

		private async UniTaskVoid PumpAsync(CancellationToken cancellationToken)
		{
			while (_incoming.Count > 0)
			{
				TimeSpan wait = _incoming.Peek().DueAt - DateTime.UtcNow;
				if (wait > TimeSpan.Zero)
				{
					bool isCancelled = await UniTask.Delay(wait, DelayType.Realtime, cancellationToken: cancellationToken).SuppressCancellationThrow();
					if (isCancelled) return;
				}

				DelayedPacket packet = _incoming.Dequeue();
				try
				{
					_deliver(new ArraySegment<byte>(packet.Bytes));
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				if (cancellationToken.IsCancellationRequested) return;
			}

			_pump.Dispose();
			_pump = null;
		}

		private readonly struct DelayedPacket
		{
			public DelayedPacket(DateTime dueAt, byte[] bytes)
			{
				DueAt = dueAt;
				Bytes = bytes;
			}

			public DateTime DueAt { get; }
			public byte[] Bytes { get; }
		}
	}
}