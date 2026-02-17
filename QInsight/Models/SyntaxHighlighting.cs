using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Qenex.QInsight.Models;

public static class SyntaxHighlighting
{
    public static IHighlightingDefinition LoadPythonHighlighting(bool isDark)
    {
        var name = isDark ? "PythonScriptDark.xshd" : "PythonScriptLight.xshd";
        using var stream = typeof(SyntaxHighlighting).Assembly
            .GetManifestResourceStream($"Qenex.QInsight.Resources.{name}");
        using var reader = new System.Xml.XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}