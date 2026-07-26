using System.IO;

namespace Qenex.QInsight.AppConfig;

/// <summary>Resolves per-user file locations under %LOCALAPPDATA%\Qenex\QInsight. Files the user
/// modifies at runtime must not live next to the executable because the application directory
/// is not writable when installed under Program Files.</summary>
public static class AppDataPaths
{
	public static string Root { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Qenex", "QInsight");

	public static string AppSettingsFile => GetFilePath("QInsightAppSettings.xml");
	public static string RuntimeLayoutFile => GetFilePath("QInsightRuntimeSettings.xml");
	public static string EditLayoutFile => GetFilePath("QInsightEditSettings.xml");
	public static string PythonHistoryFile => GetFilePath("QInsightPythonHistory.csv");

	/// <summary>Returns the full path of a user file given by its path relative to <see cref="Root"/>.
	/// Ensures the target directory exists and one-time migrates a file left in the application
	/// directory by older versions.</summary>
	public static string GetFilePath(string relativePath)
	{
		var filePath = Path.Combine(Root, relativePath);
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			MigrateLegacyFile(Path.Combine(AppContext.BaseDirectory, relativePath), filePath);
		}
		catch
		{
			// A failed migration or directory creation must not crash the application;
			// callers already treat a missing file as "use defaults".
		}

		return filePath;
	}

	private static void MigrateLegacyFile(string legacyPath, string filePath)
	{
		if (File.Exists(filePath) || !File.Exists(legacyPath))
		{
			return;
		}

		File.Copy(legacyPath, filePath);
		try
		{
			File.Delete(legacyPath);
		}
		catch
		{
			// The application directory may be read-only (Program Files); the stale copy is
			// harmless because this location is only read when the new file is missing.
		}
	}
}
