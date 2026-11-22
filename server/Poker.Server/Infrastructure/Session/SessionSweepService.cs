using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Poker.Server.Infrastructure.Time;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Infrastructure.Session
{
	public sealed class SessionSweepService : BackgroundService
	{
		private readonly SessionManager _sessions;
		private readonly IClock _clock;
		private readonly IConnectionListener[] _listeners;
		private readonly ILogger<SessionSweepService> _logger;
		private readonly List<ClientSession> _expired = new();

		public SessionSweepService(SessionManager sessions, IClock clock, IEnumerable<IConnectionListener> listeners, ILogger<SessionSweepService> logger)
		{
			_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_listeners = listeners?.ToArray() ?? throw new ArgumentNullException(nameof(listeners));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task SweepAsync()
		{
			_expired.Clear();
			_sessions.TakeExpired(_clock.UtcNow, _expired);
			foreach (ClientSession session in _expired)
			{
				_logger.LogInformation("Session {SessionId} of {PlayerName} ended: {Reason}", session.Id, session.Player.Name, session.IsBroken ? "resend buffer overflow" : "zombie window passed");
				foreach (IConnectionListener listener in _listeners)
				{
					await listener.OnDisconnectedAsync(session);
				}
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(250));
			try
			{
				while (await timer.WaitForNextTickAsync(stoppingToken))
				{
					await SweepAsync();
				}
			}
			catch (OperationCanceledException) { }
		}
	}
}