using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Tests.TestDoubles
{
	public sealed class RecordingConnection : IClientConnection
	{
		private readonly List<SentMessage> _sent = new();

		public RecordingConnection(PlayerIdentity? player = null, string id = "test-connection")
		{
			Player = player;
			Id = id;
		}

		public string Id { get; }

		public PlayerIdentity? Player { get; }

		public Exception? FailSendsWith { get; set; }

		public IReadOnlyList<SentMessage> Sent => _sent;

		public Task SendAsync<TMessage>(TMessage message, ushort requestId, CancellationToken cancellationToken) where TMessage : class
		{
			if (FailSendsWith != null) throw FailSendsWith;

			_sent.Add(new SentMessage(message, requestId));
			return Task.CompletedTask;
		}

		public void Send(OutgoingMessage message)
		{
			if (FailSendsWith != null) throw FailSendsWith;

			_sent.Add(new SentMessage(message.Message, 0));
		}

		public List<T> SentOfType<T>()
		{
			List<T> matches = new();
			foreach (SentMessage sent in _sent)
			{
				if (sent.Message is not T typed) continue;

				matches.Add(typed);
			}

			return matches;
		}

		public void Clear() => _sent.Clear();
	}
}