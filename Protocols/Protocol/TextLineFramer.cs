using System.Text;

namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Reassembles newline-delimited text from raw byte chunks for protocols hosted on byte-stream
/// drivers (TCP, serial). Text decoding and line splitting are protocol concerns — the drivers
/// only transport bytes. Uses a stateful decoder, so multi-byte characters split across chunks
/// decode correctly. Lines are separated by LF; a trailing CR is stripped (CRLF tolerant).
/// Not thread-safe; the owning protocol serializes access.
/// </summary>
public sealed class TextLineFramer(Encoding? encoding = null)
{
    private readonly Encoding textEncoding = encoding ?? Encoding.UTF8;
    private readonly Decoder decoder = (encoding ?? Encoding.UTF8).GetDecoder();
    private readonly StringBuilder currentLine = new();

    /// <summary>Appends a received chunk and returns every line completed by it.</summary>
    public IReadOnlyList<string> Append(ReadOnlySpan<byte> data)
    {
        var chars = new char[textEncoding.GetMaxCharCount(data.Length)];
        var charCount = decoder.GetChars(data, chars, flush: false);

        List<string>? lines = null;
        for (var i = 0; i < charCount; i++)
        {
            var character = chars[i];
            if (character != '\n')
            {
                currentLine.Append(character);
                continue;
            }

            if (currentLine.Length > 0 && currentLine[^1] == '\r')
            {
                currentLine.Length--;
            }

            (lines ??= []).Add(currentLine.ToString());
            currentLine.Clear();
        }

        return lines ?? (IReadOnlyList<string>)[];
    }

    /// <summary>Drops any partially received line (e.g. after a reconnect).</summary>
    public void Reset()
    {
        currentLine.Clear();
        decoder.Reset();
    }
}
