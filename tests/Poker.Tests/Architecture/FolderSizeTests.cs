using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Poker.Tests.Architecture
{
	// Folders stay small enough to read at a glance: past the limit, group the files by responsibility into subfolders
	// (the namespace follows the folder). See "Files and folders" in .claude/rules/csharp-style.md.
	public sealed class FolderSizeTests
	{
		private const int MaxSourceFilesPerFolder = 8;
		private static readonly string[] SourceFolders = { "shared", "server", "tests", "client/Assets" };

		[Fact]
		public void Folder_HoldsAtMostEightSourceFiles()
		{
			Dictionary<string, int> counts = new();
			foreach (string folder in SourceFolders)
			{
				foreach (string file in SourceTree.Files(folder, "*.cs"))
				{
					string directory = SourceTree.Relative(Path.GetDirectoryName(file)!);
					counts[directory] = counts.TryGetValue(directory, out int count) ? count + 1 : 1;
				}
			}

			List<string> violations = new();
			foreach (KeyValuePair<string, int> folder in counts)
			{
				if (folder.Value > MaxSourceFilesPerFolder) violations.Add(folder.Key + ": " + folder.Value + " files");
			}

			Assert.True(violations.Count == 0, "At most " + MaxSourceFilesPerFolder + " .cs files per folder; group these by responsibility into subfolders:\n" + string.Join("\n", violations));
		}
	}
}