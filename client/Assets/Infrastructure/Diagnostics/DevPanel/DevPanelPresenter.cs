using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using VContainer.Unity;
using Poker.Client.Infrastructure.Diagnostics.Reports;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Session;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public sealed class DevPanelPresenter : IInitializable, ITickable, IDisposable
	{
		private const float RefreshSeconds = 0.5f;

		private readonly IDevPanelView _view;
		private readonly BugReportWriter _reports;
		private readonly LogRingBuffer _log;
		private readonly TranscriptRecorder _transcript;
		private readonly ServerConnection _connection;
		private readonly ServerClock _clock;
		private readonly PlayerSession _session;
		private readonly DevPanelActions _actions;
		private readonly NetworkStats _network;
		private readonly MessageTypeStats[] _topTypes = new MessageTypeStats[3];
		private readonly StringBuilder _info = new();
		private readonly RenderStats _render = new();
		private float _nextRefreshAt;
		private float _sampledAt;
		private long _sampledBytesIn;
		private long _sampledBytesOut;
		private long _sampledPacketsIn;
		private long _sampledPacketsOut;

		public DevPanelPresenter(IDevPanelView view, BugReportWriter reports, LogRingBuffer log, TranscriptRecorder transcript, ServerConnection connection, ServerClock clock, PlayerSession session, DevPanelActions actions, NetworkStats network)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_reports = reports ?? throw new ArgumentNullException(nameof(reports));
			_log = log ?? throw new ArgumentNullException(nameof(log));
			_transcript = transcript ?? throw new ArgumentNullException(nameof(transcript));
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_actions = actions ?? throw new ArgumentNullException(nameof(actions));
			_network = network ?? throw new ArgumentNullException(nameof(network));
		}

		public void Initialize()
		{
			_view.ToggleClicked += OnToggleClicked;
			_view.BugReportClicked += OnBugReportClicked;
			_view.ShortDropClicked += OnShortDropClicked;
			_view.LongDropClicked += OnLongDropClicked;
			_actions.Added += _view.AddAction;
			IReadOnlyList<DevPanelAction> actions = _actions.Actions;
			for (int i = 0; i < actions.Count; i++)
			{
				_view.AddAction(actions[i]);
			}
		}

		public void Tick()
		{
			if (!_view.IsPanelVisible) return;
			if (Time.unscaledTime < _nextRefreshAt) return;

			Refresh();
		}

		public void Dispose()
		{
			_view.ToggleClicked -= OnToggleClicked;
			_view.BugReportClicked -= OnBugReportClicked;
			_view.ShortDropClicked -= OnShortDropClicked;
			_view.LongDropClicked -= OnLongDropClicked;
			_actions.Added -= _view.AddAction;
			_render.Dispose();
		}

		private void OnToggleClicked()
		{
			_view.SetPanelVisible(!_view.IsPanelVisible);
			Refresh();
		}

		private void OnBugReportClicked()
		{
			_reports.Write();
			Refresh();
		}

		private static string Kilobytes(double bytes) => (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture);

		private static string Rate(double perSecond) => perSecond.ToString("0", CultureInfo.InvariantCulture);

		private static string Milliseconds(double milliseconds) => milliseconds.ToString("0.0", CultureInfo.InvariantCulture);

		private void OnShortDropClicked() => _connection.SimulateDrop(TimeSpan.FromSeconds(3));

		private void OnLongDropClicked() => _connection.SimulateDrop(TimeSpan.FromSeconds(40));

		private void AppendNetwork()
		{
			float now = Time.unscaledTime;
			float seconds = Mathf.Max(now - _sampledAt, 0.001f);
			_info.Append("Net in ").Append(Kilobytes((_network.BytesIn - _sampledBytesIn) / seconds)).Append(" KB/s, ").Append(Rate((_network.PacketsIn - _sampledPacketsIn) / seconds)).Append(" pkt/s; out ");
			_info.Append(Kilobytes((_network.BytesOut - _sampledBytesOut) / seconds)).Append(" KB/s, ").Append(Rate((_network.PacketsOut - _sampledPacketsOut) / seconds)).Append(" pkt/s; resent ").Append(_network.Resent).Append('\n');
			_sampledAt = now;
			_sampledBytesIn = _network.BytesIn;
			_sampledBytesOut = _network.BytesOut;
			_sampledPacketsIn = _network.PacketsIn;
			_sampledPacketsOut = _network.PacketsOut;

			if (_network.HopCount > 0)
			{
				_info.Append("Main-thread hop avg ").Append(Milliseconds(_network.HopTotalMs / _network.HopCount)).Append(" ms, max ").Append(Milliseconds(_network.HopMaxMs)).Append(" ms\n");
			}

			int topCount = _network.CopyTopByBytes(_topTypes);
			if (topCount > 0) _info.Append("Most bytes in:");
			for (int i = 0; i < topCount; i++)
			{
				MessageTypeStats stats = _topTypes[i];
				_info.Append(i == 0 ? " " : ", ").Append(stats.Name).Append(' ').Append(Kilobytes(stats.Bytes)).Append(" KB x").Append(stats.Count);
			}

			if (topCount > 0) _info.Append('\n');

			MessageTypeStats slowest = _network.FindSlowest();
			if (slowest == null) return;

			_info.Append("Slowest handler: ").Append(slowest.Name).Append(' ').Append(Milliseconds(slowest.MaxHandleMs)).Append(" ms\n");
		}

		private void Refresh()
		{
			_nextRefreshAt = Time.unscaledTime + RefreshSeconds;
			_info.Clear();
			_info.Append("Player ").Append(_session.PlayerName).Append(" (").Append(_session.PlayerId).Append(")\n");
			_info.Append("Socket ").Append(_connection.IsOpen ? "open" : "closed").Append(", session ").Append(_connection.IsEstablished ? "established" : "none").Append('\n');
			_info.Append("Clock offset ").Append(_clock.OffsetMs).Append(" ms, best round trip ");
			if (_clock.BestRoundTripMs == long.MaxValue) _info.Append("none"); else _info.Append(_clock.BestRoundTripMs).Append(" ms");

			_info.Append('\n');
			_info.Append("Transcript ").Append(_transcript.Count).Append(" messages").Append(_transcript.StartsAtBaseline ? " from a snapshot" : "").Append('\n');
			_render.AppendTo(_info);
			AppendNetwork();
			_info.Append("Log ").Append(_log.LineCount).Append(" lines, ").Append(_log.ErrorCount).Append(" errors\n");
			if (_log.ErrorCount > 0) _info.Append("Last error: ").Append(_log.LastError).Append('\n');
			if (_reports.LastReportFolder.Length > 0) _info.Append("Report: ").Append(_reports.LastReportFolder);

			_view.SetInfo(_info.ToString());
		}
	}
}