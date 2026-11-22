using System.IO;
using NUnit.Framework;
using UnityEngine;
using Poker.Client.Infrastructure.Diagnostics.Reports;

namespace Poker.Client.Tests.EditMode.Diagnostics
{
	public sealed class LogRingBufferTests
	{
		[Test]
		public void KeepsTheFirstAndTheLastLines_AndMarksWhatItSkipped()
		{
			LogRingBuffer log = new(2, 3);
			for (int i = 0; i < 10; i++)
			{
				log.Add("line " + i, "", LogType.Log);
			}

			string[] written = Write(log);

			Assert.AreEqual(6, written.Length);
			StringAssert.EndsWith("line 0", written[0]);
			StringAssert.EndsWith("line 1", written[1]);
			Assert.AreEqual("... 5 lines skipped ...", written[2]);
			StringAssert.EndsWith("line 7", written[3]);
			StringAssert.EndsWith("line 9", written[5]);
			Assert.AreEqual(10, log.LineCount);
		}

		[Test]
		public void Errors_AreCounted_AndKeepTheirStackTrace()
		{
			LogRingBuffer log = new(10, 10);

			log.Add("all good", "", LogType.Log);
			log.Add("boom", "at Poker.Something()\nat Poker.Caller()", LogType.Exception);

			Assert.AreEqual(1, log.ErrorCount);
			Assert.AreEqual("boom", log.LastError);
			string[] written = Write(log);
			StringAssert.Contains("EXCPT boom", written[1]);
			Assert.AreEqual("    at Poker.Caller()", written[3]);
		}

		private static string[] Write(LogRingBuffer log)
		{
			using StringWriter writer = new() { NewLine = "\n" };
			log.WriteTo(writer);
			return writer.ToString().TrimEnd('\n').Split('\n');
		}
	}
}