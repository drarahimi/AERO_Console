// Small text-parsing helpers mirroring userio.f's GETFLT and ainput.f's RDLINE.
//
// Port of packages/avl-core/src/geometry/fortranText.ts.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Avl.Core.Model;

public static partial class FortranText
{
    [GeneratedRegex(@"[\s,]+")]
    private static partial Regex SplitRegex();

    /// <summary>
    /// Approximates userio.f SUBROUTINE GETFLT: splits a line on whitespace/commas
    /// and parses up to <paramref name="n"/> (or all, if n&lt;=0) numbers via Fortran
    /// list-directed read semantics. Trailing "!" comment is stripped first.
    /// </summary>
    public static List<double> GetFlt(string line, int n = 0)
    {
        int bang = line.IndexOf('!');
        string text = bang >= 0 ? line.Substring(0, bang) : line;
        var tokens = SplitRegex().Split(text.Trim())
            .Where(t => t.Length > 0)
            .ToArray();
        int count = n > 0 ? Math.Min(n, tokens.Length) : tokens.Length;
        var outList = new List<double>();
        for (int i = 0; i < count; i++)
        {
            if (!double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                throw new FormatException($"GETFLT: cannot parse number '{tokens[i]}'");
            outList.Add(v);
        }
        return outList;
    }

    /// <summary>Like GetFlt, but returns null instead of throwing on a parse failure --
    /// used for lookahead loops (e.g. AIRFOIL's inline-coordinate reading) that
    /// need to detect "this line isn't numeric" without an exception.</summary>
    public static List<double>? TryGetFlt(string line, int n = 0)
    {
        try
        {
            return GetFlt(line, n);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Port of ainput.f SUBROUTINE RDLINE: strips comments (# or ! as first
/// char = full-line comment; trailing "!..." stripped), skips blank lines,
/// trims leading whitespace.</summary>
public sealed class LineIterator
{
    private readonly string[] _rawLines;
    private int _idx;
    private string? _pending;

    /// <summary>Number of the most recently returned significant line (1-based over raw lines).</summary>
    public int LineNumber { get; private set; }

    public LineIterator(string text)
    {
        _rawLines = Regex.Split(text, "\r\n|\r|\n");
    }

    /// <summary>Pushes a line back so the next Next() call returns it again (single-slot).</summary>
    public void PushBack(string line) => _pending = line;

    /// <summary>Returns the next significant line, or null at end of file.</summary>
    public string? Next()
    {
        if (_pending != null)
        {
            string l = _pending;
            _pending = null;
            return l;
        }
        while (_idx < _rawLines.Length)
        {
            string line = _rawLines[_idx++];
            LineNumber++;
            if (line.Length > 0 && (line[0] == '!' || line[0] == '#')) continue; // full-line comment (RDLINE checks col 1 before stripping)
            if (line.Trim().Length == 0) continue; // blank line
            line = line.TrimStart();
            int bang = line.IndexOf('!');
            if (bang > 0) line = line.Substring(0, bang);
            return line.TrimEnd();
        }
        return null;
    }
}
