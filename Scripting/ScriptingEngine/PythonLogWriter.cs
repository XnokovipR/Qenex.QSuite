using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class PythonLogWriter
{
    private readonly ILogger logger;
    private readonly LogLevel level;
    private readonly System.Text.StringBuilder buffer = new();

    public PythonLogWriter(ILogger logger, LogLevel level)
    {
        this.logger = logger;
        this.level  = level;
    }

    // Python calls this — possibly multiple times per print() call,
    // and the final newline arrives as a separate write("\n").
    public void write(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        buffer.Append(text);

        // Flush on every newline so multi-line output becomes multiple log entries.
        int newlineIndex;
        while ((newlineIndex = IndexOfNewline(buffer)) >= 0)
        {
            var line = buffer.ToString(0, newlineIndex);
            buffer.Remove(0, newlineIndex + 1);
            if (line.Length > 0)
                logger.Log(level, line);
        }
    }

    // Python may call sys.stdout.flush() — must exist, can be a no-op.
    public void flush()
    {
        if (buffer.Length == 0) return;
        logger.Log(level, buffer.ToString());
        buffer.Clear();
    }

    private static int IndexOfNewline(System.Text.StringBuilder sb)
    {
        for (int i = 0; i < sb.Length; i++)
            if (sb[i] == '\n') return i;
        return -1;
    }
}