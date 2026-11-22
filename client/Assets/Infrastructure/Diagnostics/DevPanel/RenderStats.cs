using System;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public sealed class RenderStats : IDisposable
	{
		private const int FrameSamples = 30;

		private ProfilerRecorder _batches;
		private ProfilerRecorder _drawCalls;
		private ProfilerRecorder _setPassCalls;
		private ProfilerRecorder _textureMemory;
		private ProfilerRecorder _mainThread;

		public RenderStats()
		{
			_batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
			_drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
			_setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
			_textureMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
			_mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", FrameSamples);
		}

		public void AppendTo(StringBuilder text)
		{
			if (_batches.Valid)
			{
				text.Append("Render ").Append(_batches.LastValue).Append(" batches, ").Append(_drawCalls.LastValue).Append(" draw calls, ").Append(_setPassCalls.LastValue).Append(" SetPass\n");
			}
			else
			{
				text.Append("Render counters need the editor or a development build\n");
			}

			if (_mainThread.Valid && _mainThread.Count > 0) AppendFrameTime(text);

			if (!_textureMemory.Valid) return;

			text.Append("Textures ").Append((_textureMemory.LastValue / (1024f * 1024f)).ToString("F1")).Append(" MB\n");
		}

		public void Dispose()
		{
			_batches.Dispose();
			_drawCalls.Dispose();
			_setPassCalls.Dispose();
			_textureMemory.Dispose();
			_mainThread.Dispose();
		}

		private void AppendFrameTime(StringBuilder text)
		{
			double total = 0;
			int count = _mainThread.Count;
			for (int i = 0; i < count; i++)
			{
				total += _mainThread.GetSample(i).Value;
			}

			double milliseconds = total / count / 1_000_000.0;
			text.Append("Frame ").Append(milliseconds.ToString("F1")).Append(" ms (").Append((1000.0 / milliseconds).ToString("F0")).Append(" fps), target ").Append(Application.targetFrameRate).Append('\n');
		}
	}
}