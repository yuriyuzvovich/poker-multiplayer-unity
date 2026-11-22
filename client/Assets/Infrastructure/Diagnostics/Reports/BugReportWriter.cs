using System;
using System.Globalization;
using System.IO;
using System.Text;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Shared.Infrastructure.Protocol;
using UnityEngine;

namespace Poker.Client.Infrastructure.Diagnostics.Reports
{
	public sealed class BugReportWriter
	{
		private static readonly UTF8Encoding Utf8 = new(false);

		private readonly LogRingBuffer _log;
		private readonly TranscriptRecorder _transcript;
		private readonly PlayerSession _session;
		private readonly ServerClock _clock;
		private readonly ServerConnection _connection;
		private readonly string _reportsFolder;

		public BugReportWriter(LogRingBuffer log, TranscriptRecorder transcript, PlayerSession session, ServerClock clock, ServerConnection connection, string reportsFolder)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
			_transcript = transcript ?? throw new ArgumentNullException(nameof(transcript));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_reportsFolder = reportsFolder ?? throw new ArgumentNullException(nameof(reportsFolder));
		}

		public string LastReportFolder { get; private set; } = "";

		public string Write()
		{
			DateTime now = DateTime.UtcNow;
			string folder = Path.Combine(_reportsFolder, "bug-" + now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));
			Directory.CreateDirectory(folder);

			using (StreamWriter report = CreateWriter(Path.Combine(folder, "report.txt")))
			{
				report.WriteLine("Created (UTC): " + now.ToString("O", CultureInfo.InvariantCulture));
				report.WriteLine("App: " + Application.version + ", Unity " + Application.unityVersion + ", " + Application.platform);
				report.WriteLine("Protocol: " + ProtocolInfo.Version);
				report.WriteLine("Player: " + _session.PlayerName + " (" + _session.PlayerId + "), session " + _session.SessionId);
				report.WriteLine("Connection: open " + _connection.IsOpen + ", session established " + _connection.IsEstablished);
				report.WriteLine("Clock: offset " + _clock.OffsetMs + " ms, best round trip " + FormatRoundTrip());
				report.WriteLine("Transcript: " + _transcript.Count + " messages, starts at a table snapshot " + _transcript.StartsAtBaseline);
				report.WriteLine("Log: " + _log.LineCount + " lines, " + _log.ErrorCount + " errors");
				report.WriteLine();
				_log.WriteTo(report);
			}

			using (StreamWriter transcript = CreateWriter(Path.Combine(folder, "transcript.json")))
			{
				_transcript.WriteTo(transcript, _session.PlayerId);
			}

			LastReportFolder = folder;
			Debug.Log("[DIAG] Bug report written to " + folder);
			return folder;
		}

		private string FormatRoundTrip() => _clock.BestRoundTripMs == long.MaxValue ? "none yet" : _clock.BestRoundTripMs + " ms";

		private static StreamWriter CreateWriter(string path) => new StreamWriter(path, false, Utf8) { NewLine = "\n" };
	}
}