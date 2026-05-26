using System.Text;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class PythonOutputBufferWriter
{
    private readonly StringBuilder buffer = new();

    public string Text => buffer.ToString();

    public void write(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        buffer.Append(text);
    }

    public void flush()
    {
    }
}
