// Port of XFOIL's aread.f (SUBROUTINE AREAD): reads airfoil coordinate files in
// the formats XFOIL accepts -- plain (type 1), labeled generic (type 2), and MSES
// single/multi-element (types 3/4). Operates on in-memory text (the host supplies
// file contents) rather than a Fortran logical unit. GETFLT's "parse the leading
// floats out of a line" behavior is reproduced by ParseFloats.

namespace Xfoil.Core.Geometry;

/// <summary>Result of reading an airfoil file. Type mirrors AREAD's ITYPE:
/// 0 = read error, 1 = plain, 2 = labeled generic, 3 = MSES single element.</summary>
public sealed record AirfoilFileResult(double[] X, double[] Y, string Name, int Type)
{
    public bool Ok => Type != 0;
    public int N => X.Length;
    public static AirfoilFileResult Error { get; } = new(Array.Empty<double>(), Array.Empty<double>(), "", 0);
}

public static class AirfoilFile
{
    /// <summary>Port of aread.f AREAD for the single-element (or first-element) case.
    /// nmax caps the coordinate count (XFOIL's IBX buffer limit).</summary>
    public static AirfoilFileResult Read(string contents, int nmax = 2 * 500)
    {
        // Split into lines, dropping trailing empties; keep '#'/'!' comment handling
        // per-line as AREAD does (it skips comment lines wherever they appear).
        var rawLines = contents.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        // Reader that yields non-comment lines on demand (AREAD skips '#'/'!' lines).
        int cursor = 0;
        string? NextLine()
        {
            while (cursor < rawLines.Length)
            {
                string line = rawLines[cursor++];
                if (line.Length > 0 && (line[0] == '#' || line[0] == '!')) continue;
                return line;
            }
            return null;
        }

        string? line1 = NextLine();
        if (line1 == null) return AirfoilFileResult.Error;
        string? line2 = NextLine();
        if (line2 == null) return AirfoilFileResult.Error;

        int itype;
        string name = "";
        var seeds = new List<string>(); // already-read lines that are coordinates

        // Try to read numbers from the first line. If it isn't at least two numbers,
        // it's a name/title line (labeled or MSES). Otherwise the file is plain and
        // both line1 and line2 are already coordinates.
        var a1 = ParseFloats(line1, 10);
        if (a1.Count < 2)
        {
            name = line1.Trim();
            // Second line: >= 4 numbers => MSES domain-size (ISPARS) line, coordinates
            // follow it (type 3). Otherwise it's a labeled generic file (type 2) whose
            // coordinates begin at line2.
            var a2 = ParseFloats(line2, 10);
            if (a2.Count >= 4)
                itype = 3; // MSES: line2 consumed as ISPARS, nothing to seed
            else
            {
                itype = 2;
                seeds.Add(line2); // labeled: line2 is the first coordinate
            }
        }
        else
        {
            itype = 1;        // plain: line1 and line2 are both coordinates
            seeds.Add(line1);
            seeds.Add(line2);
        }

        var coords = ReadCoords(NextLine, seeds, nmax);
        return coords == null
            ? AirfoilFileResult.Error
            : new AirfoilFileResult(coords.Value.x, coords.Value.y, name, itype);
    }

    // Reads coordinate pairs until a 999.0/999.0 element terminator, end of input, or
    // nmax is exceeded. `seeds` are coordinate lines already read that should be
    // consumed first. Returns null only if nothing at all was read (mirrors AREAD's
    // error bail).
    private static (double[] x, double[] y)? ReadCoords(Func<string?> nextLine, IEnumerable<string> seeds, int nmax)
    {
        var xs = new List<double>();
        var ys = new List<double>();

        bool Consume(string line)
        {
            var a = ParseFloats(line, 2);
            if (a.Count < 2) return true;      // skip lines without two numbers (continue)
            double x = a[0], y = a[1];
            if (x == 999.0 && y == 999.0) return false; // element terminator -> stop
            xs.Add(x);
            ys.Add(y);
            return true;
        }

        foreach (var seed in seeds)
            if (!Consume(seed))
                return xs.Count == 0 ? null : (xs.ToArray(), ys.ToArray());

        while (xs.Count < nmax)
        {
            string? line = nextLine();
            if (line == null) break;           // end of input
            if (!Consume(line)) break;         // hit 999/999 terminator
        }

        if (xs.Count == 0) return null;
        return (xs.ToArray(), ys.ToArray());
    }

    /// <summary>Port of userio.f GETFLT semantics used by AREAD: parse up to `max`
    /// leading whitespace-separated floats from a line. Stops at the first token that
    /// isn't a number (returning what was parsed so far), which is how AREAD tells a
    /// numeric coordinate line from a name/title line.</summary>
    public static List<double> ParseFloats(string line, int max)
    {
        var result = new List<double>();
        foreach (var tok in line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (result.Count >= max) break;
            if (double.TryParse(tok, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                result.Add(v);
            else
                break; // first non-numeric token ends the numeric run
        }
        return result;
    }
}
