using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Poker.Tests.TestDoubles
{
	// A socket whose peer stopped reading: the first send never completes until the socket is aborted.
	public sealed class StalledWebSocket : WebSocket
	{
		private readonly TaskCompletionSource _aborted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly List<byte[]> _sent = new();
		private WebSocketState _state = WebSocketState.Open;

		public bool IsStalled { get; set; } = true;

		public bool WasAborted => _state == WebSocketState.Aborted;

		public IReadOnlyList<byte[]> Sent
		{
			get
			{
				lock (_sent) return _sent.ToArray();
			}
		}

		public override WebSocketCloseStatus? CloseStatus => null;
		public override string? CloseStatusDescription => null;
		public override WebSocketState State => _state;
		public override string? SubProtocol => null;

		public override void Abort()
		{
			_state = WebSocketState.Aborted;
			_aborted.TrySetResult();
		}

		public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

		public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
		{
			_state = WebSocketState.CloseSent;
			return Task.CompletedTask;
		}

		public override void Dispose() { }

		public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

		public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
		{
			if (IsStalled)
			{
				await _aborted.Task;
				throw new WebSocketException("aborted");
			}

			lock (_sent) _sent.Add(buffer.ToArray());
		}
	}
}