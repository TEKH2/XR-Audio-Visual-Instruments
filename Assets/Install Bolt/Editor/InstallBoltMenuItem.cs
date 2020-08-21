using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InstallBoltMenuItem
{
	private const string InstallFolder = "Install Bolt";

	[MenuItem("Tools/Install Bolt")]
	private static void Install()
	{
		var packageFiles = Directory.GetFiles(Path.Combine(Application.dataPath, InstallFolder), "*.unitypackage");

		if (packageFiles.Length == 0)
		{
			EditorUtility.DisplayDialog("Bolt Install Error", "Could not find any Bolt package file under '" + InstallFolder + "'.", "OK");
			return;
		}

		string matchingPackageFile = null;

		foreach (var packageFile in packageFiles)
		{
			if (PlayerSettings.scriptingRuntimeVersion == InferRuntimeVersion(Path.GetFileNameWithoutExtension(packageFile)))
			{
				matchingPackageFile = packageFile;
				break;
			}
		}

		if (matchingPackageFile == null)
		{
			EditorUtility.DisplayDialog("Bolt Install Error", "Could not find any Bolt package file that matches the current scripting runtime version: '" + PlayerSettings.scriptingRuntimeVersion + "'.", "OK");
		}

		if (EditorUtility.DisplayDialog("Install Bolt", "Import Bolt for " + GetRuntimeVersionStringPretty(PlayerSettings.scriptingRuntimeVersion) + "?", "Import", "Cancel"))
		{
			AssetDatabase.ImportPackage(matchingPackageFile, true);
		}
	}

	private static string GetRuntimeVersionString(ScriptingRuntimeVersion version)
	{
		switch (version)
		{
			case ScriptingRuntimeVersion.Latest:
				return "NET4";

			case ScriptingRuntimeVersion.Legacy:
				return "NET3";

			default:
				return version.ToString();
		}
	}

	private static string GetRuntimeVersionStringPretty(ScriptingRuntimeVersion version)
	{
		switch (version)
		{
			case ScriptingRuntimeVersion.Latest:
				return ".NET 4.x";

			case ScriptingRuntimeVersion.Legacy:
				return ".NET 3.x";

			default:
				return version.ToString();
		}
	}

	private static ScriptingRuntimeVersion? InferRuntimeVersion(string packageName)
	{
		foreach (var version in Enum.GetValues(typeof(ScriptingRuntimeVersion)).Cast<ScriptingRuntimeVersion>())
		{
			if (packageName.Contains(GetRuntimeVersionString(version)))
			{
				return version;
			}
		}

		return null;
	}
}
