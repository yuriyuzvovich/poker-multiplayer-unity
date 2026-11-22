using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Poker.Server.Infrastructure.Session
{
	// Owns every write to one socket. Senders queue a packet and return at once, so a table actor never waits on a
	// slow client; the close handshake goes through the same queue, so it can never overlap a data send.
	public sealed class SocketOutbox
	{
		private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

		private readonly Channel<OutgoingFrame> _frames;
		private volatile bool _isClosed;

		public SocketOutbox(WebSocket socket, int capacity)
		{
			Socket = socket ?? throw new ArgumentNullException(nameof(socket));
			_frames = Channel.CreateBounded<OutgoingFrame>(new BoundedChannelOptions(capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
			Completion = WriteLoopAsync();
		}

		public WebSocket Socket { get; }

		public bool IsClosed => _isClosed;

		public Task Completion { get; }

		public bool TryEnqueue(byte[] packet) => !_isClosed && _frames.Writer.TryWrite(new OutgoingFrame(packet));

		public void Complete()
		{
			_isClosed = true;
			_frames.Writer.TryComplete();
		}

		public Task CloseAsync(WebSocketCloseStatus status, string description)
		{
			TaskCompletionSource closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
			bool isQueued = !_isClosed && _frames.Writer.TryWrite(new OutgoingFrame(status, description, closed));
			Complete();
			if (isQueued) return closed.Task;

			AbortQuietly();
			return Task.CompletedTask;
		}

		private async Task WriteLoopAsync()
		{
			try
			{
				await foreach (OutgoingFrame frame in _frames.Reader.ReadAllAsync())
				{
					if (frame.Closed != null)
					{
						await CloseOutputAsync(frame);
						return;
					}

					if (Socket.State != WebSocketState.Open) continue;

					await Socket.SendAsync(new ArraySegment<byte>(frame.Packet!), WebSocketMessageType.Binary, true, CancellationToken.None);
				}
			}
			catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or InvalidOperationException)
			{
				AbortQuietly();
			}
			finally
			{
				Complete();
				while (_frames.Reader.TryRead(out OutgoingFrame left)) left.Closed?.TrySetResult();
			}
		}

		private async Task CloseOutputAsync(OutgoingFrame frame)
		{
			using CancellationTokenSource timeout = new(CloseTimeout);
			try
			{
				if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived) await Socket.CloseOutputAsync(frame.CloseStatus, frame.Description, timeout.Token);
			}
			catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or OperationCanceledException)
			{
				AbortQuietly();
			}
			finally
			{
				frame.Closed!.TrySetResult();
			}
		}

		private void AbortQuietly()
		{
			try { Socket.Abort(); }
			catch (ObjectDisposedException) { }
		}

		private readonly struct OutgoingFrame
		{
			public OutgoingFrame(byte[] packet)
			{
				Packet = packet;
				CloseStatus = WebSocketCloseStatus.Empty;
				Description = "";
				Closed = null;
			}

			public OutgoingFrame(WebSocketCloseStatus closeStatus, string description, TaskCompletionSource closed)
			{
				Packet = null;
				CloseStatus = closeStatus;
				Description = description;
				Closed = closed;
			}

			public byte[]? Packet { get; }
			public WebSocketCloseStatus CloseStatus { get; }
			public string Description { get; }
			public TaskCompletionSource? Closed { get; }
		}
	}
}