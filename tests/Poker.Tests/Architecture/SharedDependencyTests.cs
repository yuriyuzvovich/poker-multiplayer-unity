using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Poker.Tests.Architecture
{
	// Poker.Shared is loaded by Unity and by the server, so it may use only the BCL subset both support.
	public sealed class SharedDependencyTests
	{
		private const string SharedFolder = "shared/Poker.Shared";
		private static readonly string[] ForbiddenNamespaces = { "UnityEngine", "UnityEditor", "Microsoft.AspNetCore", "Poker.Server", "Poker.Client" };

		[Fact]
		public void Shared_ReferencesNeitherUnityNorTheServerNorTheClient()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(SharedFolder, "*.cs"))
			{
				string text = File.ReadAllText(file);
				foreach (string forbidden in ForbiddenNamespaces)
				{
					if (text.Contains(forbidden + ".") || text.Contains("using " + forbidden + ";")) violations.Add(SourceTree.Relative(file) + " uses " + forbidden);
				}
			}

			Assert.True(violations.Count == 0, "Poker.Shared must stay plain netstandard2.1:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void SharedProject_HasNoPackageReferences()
		{
			string project = File.ReadAllText(Path.Combine(SourceTree.Root, SharedFolder, "Poker.Shared.csproj"));

			Assert.DoesNotContain("PackageReference", project);
		}
	}
}