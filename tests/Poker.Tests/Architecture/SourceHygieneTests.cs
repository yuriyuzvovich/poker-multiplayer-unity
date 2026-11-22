using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Poker.Tests.Architecture
{
	// Text rules every file in the repository follows, whoever or whatever wrote it (AGENTS.md, .claude/rules).
	public sealed class SourceHygieneTests
	{
		private static readonly string[] TextFolders = { "shared", "server", "tests", "client/Assets", "tools", "docs", ".claude", ".github" };
		// Keep in step with the extensions in .claude/hooks/check-text.mjs.
		private static readonly string[] TextPatterns = { "*.cs", "*.md", "*.asmdef", "*.json", "*.ps1", "*.mjs", "*.yml", "*.props", "*.csproj" };
		private static readonly string[] RootFiles = { "AGENTS.md", "CLAUDE.md", "README.md", "LICENSE", "global.json", "Directory.Build.props" };
		private static readonly char[] Dashes = { (char)0x2013, (char)0x2014 };

		// Namespace roots per source folder; a file's namespace is its root plus its folder path below it.
		private static readonly (string Folder, string Namespace)[] NamespaceRoots =
		{
			("shared/Poker.Shared", "Poker.Shared"),
			("server/Poker.Server", "Poker.Server"),
			("tests/Poker.Tests", "Poker.Tests"),
			("client/Assets", "Poker.Client"),
		};

		[Fact]
		public void TextFiles_UseLfLineEndings_EndWithoutANewline_AndHaveNoDashes()
		{
			List<string> violations = new();
			foreach (string file in TextFiles())
			{
				string text = File.ReadAllText(file);
				string name = SourceTree.Relative(file);
				if (text.Contains('\r')) violations.Add(name + ": CRLF line endings");
				if (text.EndsWith("\n", StringComparison.Ordinal)) violations.Add(name + ": trailing newline at the end of the file");
				int dash = text.IndexOfAny(Dashes);
				if (dash >= 0) violations.Add(name + ": em or en dash on line " + (CountLines(text, dash) + 1));
			}

			Assert.True(violations.Count == 0, "Text hygiene (LF, no final newline, no em or en dashes):\n" + string.Join("\n", violations));
		}

		[Fact]
		public void Namespaces_MirrorFolders()
		{
			List<string> violations = new();
			foreach ((string folder, string root) in NamespaceRoots)
			{
				foreach (string file in SourceTree.Files(folder, "*.cs"))
				{
					string name = SourceTree.Relative(file);
					string expected = ExpectedNamespace(folder, root, file);
					Match declared = Regex.Match(File.ReadAllText(file), @"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
					if (!declared.Success) violations.Add(name + ": no namespace (expected " + expected + ")");
					else if (declared.Groups[1].Value != expected) violations.Add(name + ": namespace " + declared.Groups[1].Value + ", expected " + expected);
				}
			}

			Assert.True(violations.Count == 0, "Namespaces mirror folders:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void MarkdownLinks_PointToFilesThatExist()
		{
			List<string> violations = new();
			foreach (string file in TextFiles())
			{
				if (!file.EndsWith(".md", StringComparison.Ordinal)) continue;

				string name = SourceTree.Relative(file);
				// Templates link to files that exist only once a feature folder is created from them.
				if (name.EndsWith("-template.md", StringComparison.Ordinal)) continue;

				string text = Regex.Replace(File.ReadAllText(file), @"```.*?```|`[^`\n]*`", "", RegexOptions.Singleline);
				foreach (Match link in Regex.Matches(text, @"\]\(([^)#\s]+)(#[^)\s]*)?\)"))
				{
					string target = link.Groups[1].Value;
					if (Regex.IsMatch(target, "^[a-z]+:", RegexOptions.IgnoreCase)) continue;

					string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, target));
					if (!File.Exists(resolved) && !Directory.Exists(resolved)) violations.Add(name + " -> " + target);
				}
			}

			Assert.True(violations.Count == 0, "Relative links must resolve:\n" + string.Join("\n", violations));
		}

		private static string ExpectedNamespace(string folder, string root, string file)
		{
			string relativeFolder = Path.GetRelativePath(Path.Combine(SourceTree.Root, folder), Path.GetDirectoryName(file)!).Replace('\\', '/');
			if (relativeFolder == ".") return root;

			return root + "." + relativeFolder.Replace('/', '.');
		}

		private static IEnumerable<string> TextFiles()
		{
			foreach (string folder in TextFolders)
			{
				foreach (string pattern in TextPatterns)
				{
					foreach (string file in SourceTree.Files(folder, pattern))
					{
						yield return file;
					}
				}
			}

			foreach (string file in RootFiles)
			{
				string path = Path.Combine(SourceTree.Root, file);
				if (File.Exists(path)) yield return path;
			}
		}

		private static int CountLines(string text, int end)
		{
			int lines = 0;
			for (int i = 0; i < end; i++)
			{
				if (text[i] == '\n') lines++;
			}

			return lines;
		}
	}
}