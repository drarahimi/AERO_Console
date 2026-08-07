using System.Text.RegularExpressions;

namespace Avl.Core.Tests;

/// <summary>Golden-transcript block extraction + comparison helpers, mirroring the
/// TypeScript tests (packages/avl-core/test/**).</summary>
public static class GoldenText
{
    public static string[] SplitLines(string text) => Regex.Split(text, "\r\n|\r|\n");

    /// <summary>Extracts the first "----...\nVortex Lattice Output...----" block
    /// (inclusive) from a captured avl.exe transcript.</summary>
    public static string[] ExtractFirstOutTotalBlock(string goldenText)
    {
        var lines = SplitLines(goldenText);
        int idx = Array.FindIndex(lines, l => l.Contains("Vortex Lattice Output"));
        int start = idx - 1; // leading "----" separator
        int end = start + 1;
        while (!lines[end].TrimStart().StartsWith("---")) end++;
        return lines.Skip(start).Take(end - start + 1).Select(l => l.TrimEnd()).ToArray();
    }

    /// <summary>Extracts the block between the header line containing <paramref name="header"/>
    /// (kept, with its preceding "----" separator) and the following "----" line.</summary>
    public static string[] ExtractBlock(string goldenText, string header)
    {
        var lines = SplitLines(goldenText);
        int idx = Array.FindIndex(lines, l => l.Contains(header));
        int start = idx - 1;
        int end = start + 1;
        while (end < lines.Length && !lines[end].TrimStart().StartsWith("---")) end++;
        return lines.Skip(start).Take(end - start + 1).Select(l => l.TrimEnd()).ToArray();
    }

    public static string NormalizeNegativeZero(string s) => Regex.Replace(s, @"-(0\.0+)\b", " $1");

    /// <summary>Collapses near-zero exponential tokens (|v| ~1e-15 or smaller) to a
    /// placeholder: pure double-precision roundoff on a quantity that's exactly zero
    /// by symmetry, whose last digits/sign carry no correctness signal.</summary>
    public static string NormalizeNearZero(string s) =>
        Regex.Replace(s, @"[- ]0\.\d+E[+-](1[5-9]|[2-9]\d)", "~ZERO~");

    public static string[] Normalize(IEnumerable<string> lines) =>
        lines.Select(l => l.TrimEnd()).ToArray();

    public const string Sep = " ---------------------------------------------------------------";

    /// <summary>[Sep] + the block from the first line containing <paramref name="header"/>
    /// through (inclusive) the next line whose TrimStart starts with "---".</summary>
    public static string[] ExtractHeaderToDashes(string goldenText, string header)
    {
        var lines = SplitLines(goldenText);
        int titleIdx = Array.FindIndex(lines, l => l.Contains(header));
        int end = titleIdx;
        while (!lines[end].TrimStart().StartsWith("---")) end++;
        return Prepend(lines, titleIdx, end);
    }

    /// <summary>[Sep] + the block from <paramref name="header"/> through the next line
    /// equal (trimmed) to the separator.</summary>
    public static string[] ExtractHeaderToSep(string goldenText, string header)
    {
        var lines = SplitLines(goldenText);
        int titleIdx = Array.FindIndex(lines, l => l.Contains(header));
        int end = titleIdx;
        while (lines[end].Trim() != Sep.Trim()) end++;
        return Prepend(lines, titleIdx, end);
    }

    /// <summary>ST/SM: [Sep] + block from the 2nd "Vortex Lattice Output" occurrence
    /// through the "Clb Cnr" spiral-stability line.</summary>
    public static string[] ExtractStabilityBlock(string goldenText)
    {
        var lines = SplitLines(goldenText);
        var occ = Occurrences(lines, "Vortex Lattice Output");
        int titleIdx = occ[1];
        int endIdx = Array.FindIndex(lines, titleIdx + 1, l => l.Contains("Clb Cnr"));
        return Prepend(lines, titleIdx, endIdx);
    }

    /// <summary>SB: [Sep] + block from the 2nd "Vortex Lattice Output" occurrence
    /// through 2 lines past the last "Cnd##" control-derivative row.</summary>
    public static string[] ExtractSbBlock(string goldenText)
    {
        var lines = SplitLines(goldenText);
        var occ = Occurrences(lines, "Vortex Lattice Output");
        int titleIdx = occ[1];
        int lastDataIdx = -1;
        for (int i = titleIdx + 1; i < lines.Length; i++)
            if (Regex.IsMatch(lines[i], @"Cnd\d\d")) lastDataIdx = i;
        int endIdx = lastDataIdx + 2;
        return Prepend(lines, titleIdx, endIdx);
    }

    /// <summary>FS's ill-conditioned C.P.x/c blanking: on a strip data row whose |cl|
    /// &lt; 5e-5, blank the trailing 9-char C.P.x/c slot before comparing.</summary>
    public static string NormalizeIllConditionedCp(string s)
    {
        var tokens = s.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 15 || !Regex.IsMatch(tokens[0], @"^\d+$")) return s;
        if (double.TryParse(tokens[9], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double cl)
            && Math.Abs(cl) < 5e-5)
        {
            return s.Substring(0, s.Length - 9) + "  ~CL0~  ";
        }
        return s;
    }

    private static string[] Prepend(string[] lines, int start, int endInclusive)
    {
        var block = new List<string> { Sep };
        for (int i = start; i <= endInclusive; i++) block.Add(lines[i].TrimEnd());
        return block.ToArray();
    }

    private static List<int> Occurrences(string[] lines, string needle)
    {
        var acc = new List<int>();
        for (int i = 0; i < lines.Length; i++) if (lines[i].Contains(needle)) acc.Add(i);
        return acc;
    }
}
