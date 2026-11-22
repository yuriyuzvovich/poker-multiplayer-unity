using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Poker.Client.App.Editor.Build
{
	public static class AndroidBuild
	{
		private static string Output => Environment.GetEnvironmentVariable("POKER_APK") ?? "Builds/Android/mp-poker-dev.apk";

		[MenuItem("mp-poker/Build Android Development APK")]
		public static void Build()
		{
			PlayerSettings.productName = "mp-poker";
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "dev.mppoker.client");
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			EditorUserBuildSettings.buildAppBundle = false;

			AddressableAssetSettings.CleanPlayerContent();
			AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
			if (!string.IsNullOrEmpty(content.Error)) throw new InvalidOperationException("Addressables build failed: " + content.Error);

			BuildPlayerOptions options = new()
			{
				scenes = EnabledScenes(),
				locationPathName = Output,
				target = BuildTarget.Android,
				options = BuildOptions.Development | BuildOptions.AllowDebugging,
			};
			BuildReport report = BuildPipeline.BuildPlayer(options);
			if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android build " + report.summary.result + " with " + report.summary.totalErrors + " errors");

			Debug.Log("[BUILD] " + Output + ": " + report.summary.totalSize / (1024 * 1024) + " MB in " + report.summary.totalTime.TotalMinutes.ToString("F1") + " min");
		}

		private static string[] EnabledScenes()
		{
			List<string> scenes = new();
			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (!scene.enabled) continue;

				scenes.Add(scene.path);
			}

			return scenes.ToArray();
		}
	}
}