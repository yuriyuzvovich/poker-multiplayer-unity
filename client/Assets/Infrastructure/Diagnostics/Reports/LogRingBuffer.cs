using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using VContainer.Unity;

namespace Poker.Client.Infrastructure.Diagnostics.Reports
{
	public sealed class LogRingBuffer : IInitializable, IDisposable
	{
		private readonly object _lock = new object();
		private readonly List<string> _head = new();
		private readonly Queue<string> _tail = new();
		private readonly int _headCapacity;
		private readonly int _tailCapacity;
		private long _skipped;
		private long _lineCount;
		private int _errorCount;
		private string _lastError = "";

		public LogRingBuffer(int headCapacity, int tailCapacity)
		{
			if (headCapacity < 0) throw new ArgumentOutOfRangeException(nameof(headCapacity));
			if (tailCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(tailCapacity));

			_headCapacity = headCapacity;
			_tailCapacity = tailCapacity;
		}

		public long LineCount
		{
			get
			{
				lock (_lock) return _lineCount;
			}
		}

		public int ErrorCount
		{
			get
			{
				lock (_lock) return _errorCount;
			}
		}

		public string LastError
		{
			get
			{
				lock (_lock) return _lastError;
			}
		}

		public void Initialize() => Application.logMessageReceivedThreaded += Add;

		public void Dispose() => Application.logMessageReceivedThreaded -= Add;

		public void Add(string message, string stackTrace, LogType type)
		{
			bool isError = type is LogType.Error or LogType.Exception or LogType.Assert;
			string line = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + Tag(type) + " " + message;
			if (isError && stackTrace.Length > 0) line += "\n    " + stackTrace.TrimEnd().Replace("\n", "\n    ");

			lock (_lock)
			{
				_lineCount++;
				if (isError)
				{
					_errorCount++;
					_lastError = message;
				}

				if (_head.Count < _headCapacity)
				{
					_head.Add(line);
					return;
				}

				_tail.Enqueue(line);
				if (_tail.Count <= _tailCapacity) return;

				_tail.Dequeue();
				_skipped++;
			}
		}

		public void WriteTo(TextWriter writer)
		{
			lock (_lock)
			{
				foreach (string line in _head)
				{
					writer.WriteLine(line);
				}

				if (_skipped > 0) writer.WriteLine("... " + _skipped + " lines skipped ...");

				foreach (string line in _tail)
				{
					writer.WriteLine(line);
				}
			}
		}

		private static string Tag(LogType type) => type switch
		{
			LogType.Warning => "WARN ",
			LogType.Error => "ERROR",
			LogType.Exception => "EXCPT",
			LogType.Assert => "ASSRT",
			_ => "INFO ",
		};
	}
}