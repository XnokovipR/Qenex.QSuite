using System.Diagnostics;
using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Qenex.QInsight.Models;

public static class SyntaxHighlighting
{
    private const string ResourceNamespace = "Qenex.QInsight.Resources";
    private const string UserHighlightingDirectoryName = "SyntaxHighlighting";
    private const string DarkPythonHighlightingFileName = "PythonScriptDark.xshd";
    private const string LightPythonHighlightingFileName = "PythonScriptLight.xshd";

    public static IHighlightingDefinition LoadPythonHighlighting(bool isDark)
    {
        var fileName = isDark ? DarkPythonHighlightingFileName : LightPythonHighlightingFileName;
        var resourceName = $"{ResourceNamespace}.{fileName}";
        var userHighlightingFilePath = Path.Combine(AppContext.BaseDirectory, UserHighlightingDirectoryName, fileName);

        EnsureUserHighlightingFile(userHighlightingFilePath, resourceName);

        if (File.Exists(userHighlightingFilePath))
        {
            try
            {
                return LoadFromFile(userHighlightingFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cannot load AvalonEdit highlighting from '{userHighlightingFilePath}'. Embedded default will be used. {ex}");
            }
        }

        return LoadFromResource(resourceName);
    }

    private static IHighlightingDefinition LoadFromFile(string filePath)
    {
        using var reader = new XmlTextReader(filePath);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    private static IHighlightingDefinition LoadFromResource(string resourceName)
    {
        using var stream = typeof(SyntaxHighlighting).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded AvalonEdit highlighting resource '{resourceName}' was not found.");
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    private static void EnsureUserHighlightingFile(string filePath, string resourceName)
    {
        if (File.Exists(filePath)) return;

        try
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using var stream = typeof(SyntaxHighlighting).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null) return;

            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cannot create user AvalonEdit highlighting file '{filePath}'. {ex}");
        }
    }
}
