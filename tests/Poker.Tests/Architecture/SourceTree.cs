using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Poker.Tests.Architecture
{
	// Reads the repository's source files, so architecture and hygiene rules can be checked as plain tests.
	public static class SourceTree
	{
		private static readonly string[] SkippedFolders = { "bin", "obj", "Library", "Temp", "Logs", "UserSettings", "Plugins", "TextMesh Pro", ".git", ".vs", ".idea" };
		private static readonly Lazy<string> LazyRoot = new(FindRoot);

		public static string Root => LazyRoot.Value;

		public static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

		public static IReadOnlyList<string> Files(string relativeFolder, string searchPattern)
		{
			List<string> files = new();
			string folder = Path.Combine(Root, relativeFolder);
			if (Directory.Exists(folder)) Collect(folder, searchPattern, files);
			files.Sort(StringComparer.Ordinal);
			return files;
		}

		// Every distinct "<prefix><Name>" token in the text, for example the slice names after "Poker.Server.Features.".
		public static ISet<string> NamesAfter(string text, string prefix)
		{
			HashSet<string> names = new(StringComparer.Ordinal);
			foreach (Match match in Regex.Matches(text, Regex.Escape(prefix) + @"(\w+)"))
			{
				names.Add(match.Groups[1].Value);
			}

			return names;
		}

		// The first folder below baseFolder that holds the file, for example "Hand" for Features/Hand/Runtime/HandEngine.cs.
		public static string TopFolder(string baseFolder, string file)
		{
			string relative = Path.GetRelativePath(Path.Combine(Root, baseFolder), file).Replace('\\', '/');
			int slash = relative.IndexOf('/');
			return slash < 0 ? "" : relative.Substring(0, slash);
		}

		private static void Collect(string folder, string searchPattern, List<string> files)
		{
			files.AddRange(Directory.GetFiles(folder, searchPattern));
			foreach (string child in Directory.GetDirectories(folder))
			{
				if (Array.IndexOf(SkippedFolders, Path.GetFileName(child)) >= 0) continue;

				Collect(child, searchPattern, files);
			}
		}

		private static string FindRoot()
		{
			DirectoryInfo? directory = new(AppContext.BaseDirectory);
			while (directory != null)
			{
				if (File.Exists(Path.Combine(directory.FullName, "mp-poker.sln"))) return directory.FullName;

				directory = directory.Parent;
			}

			throw new DirectoryNotFoundException("mp-poker.sln not found above " + AppContext.BaseDirectory);
		}
	}
}