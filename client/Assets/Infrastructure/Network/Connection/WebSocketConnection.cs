using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Network.DevTools;

namespace Poker.Client.Infrastructure.Network.Connection
{
	public sealed class WebSocketConnection : IDisposable
	{
		private readonly ClientWebSocket _socket = new();
		private readonly CancellationTokenSource _receiveCts = new();
		private readonly UniTaskCompletionSource _receiveFinished = new();
		private readonly NetworkStats _stats;

		public WebSocketConnection(NetworkStats stats) => _stats = stats ?? throw new ArgumentNullException(nameof(stats));

		public event Action<ArraySegment<byte>> PacketReceived;
		public event Action<string> Closed;

		public bool IsOpen => _socket.State == WebSocketState.Open;

		public int CloseCode { get; private set; }

		public async UniTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			await _socket.ConnectAsync(uri, cancellationToken);
			ReceiveLoopAsync(_receiveCts.Token).Forget();
		}

		public UniTask SendAsync(byte[] packet, CancellationToken cancellationToken) =>
			_socket.SendAsync(new ArraySegment<byte>(packet), WebSocketMessageType.Binary, true, cancellationToken).AsUniTask();

		public async UniTask CloseAsync(CancellationToken cancellationToken)
		{
			if (!IsOpen) return;

			await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
			await _receiveFinished.Task.AttachExternalCancellation(cancellationToken);
		}

		public void Dispose()
		{
			_receiveCts.Cancel();
			_receiveCts.Dispose();
			_socket.Dispose();
		}

		private async UniTaskVoid ReceiveLoopAsync(CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[4096];
			using MemoryStream packet = new();
			string closeReason = "Server closed the connection.";
			try
			{
				while (_socket.State is WebSocketState.Open or WebSocketState.CloseSent)
				{
					WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
					if (result.MessageType == WebSocketMessageType.Close)
					{
						closeReason = string.IsNullOrEmpty(result.CloseStatusDescription) ? closeReason : result.CloseStatusDescription;
						CloseCode = (int)(result.CloseStatus ?? WebSocketCloseStatus.Empty);
						break;
					}

					packet.Write(buffer, 0, result.Count);
					if (!result.EndOfMessage) continue;

					long receivedAt = Stopwatch.GetTimestamp();
					await UniTask.SwitchToMainThread(cancellationToken);
					_stats.AddHop(NetworkStats.MillisecondsSince(receivedAt));
					PacketReceived?.Invoke(new ArraySegment<byte>(packet.GetBuffer(), 0, (int)packet.Length));
					packet.SetLength(0);
				}
			}
			catch (OperationCanceledException)
			{
				_receiveFinished.TrySetResult();
				return;
			}
			catch (WebSocketException exception)
			{
				closeReason = exception.Message;
			}
			catch (Exception exception)
			{
				// Anything else is a bug above the socket; close visibly instead of leaving a silent, half-dead link.
				UnityEngine.Debug.LogException(exception);
				closeReason = "receive loop failed: " + exception.Message;
			}

			_receiveFinished.TrySetResult();
			await UniTask.SwitchToMainThread();
			Closed?.Invoke(closeReason);
		}
	}
}