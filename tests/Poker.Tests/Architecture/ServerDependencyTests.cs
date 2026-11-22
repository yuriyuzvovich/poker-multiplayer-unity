using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Poker.Tests.Architecture
{
	// The server is one assembly, so its dependency rules are checked on namespaces in the source (docs/architecture.md).
	public sealed class ServerDependencyTests
	{
		private const string FeaturesFolder = "server/Poker.Server/Features";
		private const string InfrastructureFolder = "server/Poker.Server/Infrastructure";
		private const string FeaturePrefix = "Poker.Server.Features.";
		private const string Kernel = "Table";
		private const string CompositionRoot = "server/Poker.Server/Infrastructure/Hosting/ServerHost.cs";

		// Infrastructure areas from the bottom up; an area may use only the areas before it.
		private static readonly string[] InfrastructureLayers = { "Time", "Transport", "Messaging", "Session", "Hosting" };

		[Fact]
		public void InfrastructureArea_UsesOnlyTheAreasBelowIt()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(InfrastructureFolder, "*.cs"))
			{
				string area = SourceTree.TopFolder(InfrastructureFolder, file);
				int level = Array.IndexOf(InfrastructureLayers, area);
				if (level < 0)
				{
					violations.Add(SourceTree.Relative(file) + " is in an area missing from InfrastructureLayers");
					continue;
				}

				foreach (string used in SourceTree.NamesAfter(File.ReadAllText(file), "Poker.Server.Infrastructure."))
				{
					if (Array.IndexOf(InfrastructureLayers, used) > level) violations.Add(SourceTree.Relative(file) + " (" + area + ") uses " + used);
				}
			}

			Assert.True(violations.Count == 0, "Infrastructure layers are " + string.Join(" < ", InfrastructureLayers) + ":\n" + string.Join("\n", violations));
		}

		[Fact]
		public void Feature_UsesOnlyItselfAndTheTableKernel()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(FeaturesFolder, "*.cs"))
			{
				string slice = SourceTree.TopFolder(FeaturesFolder, file);
				foreach (string used in SourceTree.NamesAfter(File.ReadAllText(file), FeaturePrefix))
				{
					if (used == slice || used == Kernel) continue;

					violations.Add(SourceTree.Relative(file) + " uses feature " + used);
				}
			}

			Assert.True(violations.Count == 0, "A feature may use only itself and the Table kernel:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void Infrastructure_UsesNoFeature_ExceptTheCompositionRoot()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(InfrastructureFolder, "*.cs"))
			{
				if (SourceTree.Relative(file) == CompositionRoot) continue;

				foreach (string used in SourceTree.NamesAfter(File.ReadAllText(file), FeaturePrefix))
				{
					violations.Add(SourceTree.Relative(file) + " uses feature " + used);
				}
			}

			Assert.True(violations.Count == 0, "Infrastructure never references features:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void CompositionRoot_InstallsEveryFeature()
		{
			string root = File.ReadAllText(Path.Combine(SourceTree.Root, CompositionRoot));
			List<string> missing = new();
			foreach (string folder in Directory.GetDirectories(Path.Combine(SourceTree.Root, FeaturesFolder)))
			{
				string slice = Path.GetFileName(folder);
				if (!root.Contains(slice + "Feature.Install(")) missing.Add(slice);
			}

			Assert.True(missing.Count == 0, "ServerHost must call <Slice>Feature.Install for: " + string.Join(", ", missing));
		}
	}
}