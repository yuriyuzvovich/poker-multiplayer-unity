using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Poker.Tests.Architecture
{
	// The client's dependency rules, checked on the asmdef files and the source, so they hold without opening Unity.
	public sealed class ClientAssemblyTests
	{
		private const string AssetsFolder = "client/Assets";
		private const string FeaturesFolder = "client/Assets/Features";
		private const string InfrastructureFolder = "client/Assets/Infrastructure";
		private const string FeaturePrefix = "Features.";
		private const string InfrastructurePrefix = "Infrastructure.";
		private const string KernelAssembly = "Features.Table";
		private static readonly string[] CompositionRoots = { "App.Root", "App.Editor", "Tests.EditMode", "Tests.PlayMode" };

		[Fact]
		public void FeatureAssembly_ReferencesNoFeatureButTheKernel()
		{
			List<string> violations = new();
			foreach (KeyValuePair<string, List<string>> assembly in LoadAssemblies())
			{
				if (!assembly.Key.StartsWith(FeaturePrefix, StringComparison.Ordinal)) continue;

				foreach (string reference in assembly.Value)
				{
					if (reference.StartsWith(FeaturePrefix, StringComparison.Ordinal) && reference != KernelAssembly) violations.Add(assembly.Key + " -> " + reference);
				}
			}

			Assert.True(violations.Count == 0, "A feature assembly may reference only Features.Table among features:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void InfrastructureAssembly_ReferencesNoFeature()
		{
			List<string> violations = new();
			foreach (KeyValuePair<string, List<string>> assembly in LoadAssemblies())
			{
				if (!assembly.Key.StartsWith(InfrastructurePrefix, StringComparison.Ordinal)) continue;

				foreach (string reference in assembly.Value)
				{
					if (reference.StartsWith(FeaturePrefix, StringComparison.Ordinal)) violations.Add(assembly.Key + " -> " + reference);
				}
			}

			Assert.True(violations.Count == 0, "Infrastructure never references features:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void OnlyCompositionRoots_ReferenceSeveralFeatures()
		{
			List<string> violations = new();
			foreach (KeyValuePair<string, List<string>> assembly in LoadAssemblies())
			{
				if (Array.IndexOf(CompositionRoots, assembly.Key) >= 0) continue;

				int features = 0;
				foreach (string reference in assembly.Value)
				{
					if (reference.StartsWith(FeaturePrefix, StringComparison.Ordinal) && reference != KernelAssembly) features++;
				}

				if (features > 0) violations.Add(assembly.Key + " references " + features + " feature(s)");
			}

			Assert.True(violations.Count == 0, "Only App.Root, App.Editor and the test assemblies know several features:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void FeatureSource_UsesOnlyItselfAndTheTableKernel()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(FeaturesFolder, "*.cs"))
			{
				string slice = SourceTree.TopFolder(FeaturesFolder, file);
				foreach (string used in SourceTree.NamesAfter(File.ReadAllText(file), "Poker.Client.Features."))
				{
					if (used == slice || used == "Table") continue;

					violations.Add(SourceTree.Relative(file) + " uses feature " + used);
				}
			}

			Assert.True(violations.Count == 0, "A client feature may use only itself and the Table kernel:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void InfrastructureSource_UsesNoFeature()
		{
			List<string> violations = new();
			foreach (string file in SourceTree.Files(InfrastructureFolder, "*.cs"))
			{
				foreach (string used in SourceTree.NamesAfter(File.ReadAllText(file), "Poker.Client.Features."))
				{
					violations.Add(SourceTree.Relative(file) + " uses feature " + used);
				}
			}

			Assert.True(violations.Count == 0, "Client infrastructure never references features:\n" + string.Join("\n", violations));
		}

		[Fact]
		public void EveryFeatureFolder_HasItsOwnAssembly()
		{
			List<string> missing = new();
			foreach (string folder in Directory.GetDirectories(Path.Combine(SourceTree.Root, FeaturesFolder)))
			{
				string name = Path.GetFileName(folder);
				if (!File.Exists(Path.Combine(folder, FeaturePrefix + name + ".asmdef"))) missing.Add(name);
			}

			Assert.True(missing.Count == 0, "Each client feature folder needs Features.<Name>.asmdef: " + string.Join(", ", missing));
		}

		// Assembly name to referenced assembly names; GUID references are resolved through the .asmdef.meta files.
		private static Dictionary<string, List<string>> LoadAssemblies()
		{
			IReadOnlyList<string> files = SourceTree.Files(AssetsFolder, "*.asmdef");
			Dictionary<string, string> namesByGuid = new(StringComparer.Ordinal);
			Dictionary<string, JsonElement> definitions = new(StringComparer.Ordinal);
			foreach (string file in files)
			{
				using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
				JsonElement root = document.RootElement.Clone();
				string name = root.GetProperty("name").GetString() ?? "";
				definitions[name] = root;

				string meta = file + ".meta";
				if (!File.Exists(meta)) continue;

				Match guid = Regex.Match(File.ReadAllText(meta), @"guid:\s*([0-9a-f]+)");
				if (guid.Success) namesByGuid[guid.Groups[1].Value] = name;
			}

			Dictionary<string, List<string>> assemblies = new(StringComparer.Ordinal);
			foreach (KeyValuePair<string, JsonElement> definition in definitions)
			{
				List<string> references = new();
				if (definition.Value.TryGetProperty("references", out JsonElement array))
				{
					foreach (JsonElement item in array.EnumerateArray())
					{
						string reference = item.GetString() ?? "";
						if (reference.StartsWith("GUID:", StringComparison.Ordinal)) reference = namesByGuid.TryGetValue(reference.Substring(5), out string? resolved) ? resolved : reference;
						references.Add(reference);
					}
				}

				assemblies[definition.Key] = references;
			}

			Assert.NotEmpty(assemblies);
			return assemblies;
		}
	}
}