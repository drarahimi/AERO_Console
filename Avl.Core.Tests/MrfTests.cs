using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>MRF (machine-readable, ES23.15) parity vs the bundled reference exe.
///
/// Note: full-precision (15-digit) output cannot byte-match across a different
/// language/compiler -- the last 1-2 digits differ by ULP-level roundoff from
/// gfortran. So we verify (a) the non-numeric skeleton (labels/structure/integer
/// counts) is identical, and (b) every ES value agrees to a tight relative
/// tolerance (1e-9) -- far beyond any consumer's needs.</summary>
public class MrfTests
{
    private static readonly Regex EsToken = new(@"[+-]?\d\.\d+E[+-]\d+", RegexOptions.Compiled);

    private static Geometry LoadAllegro() =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    private static string Skeleton(string line) => EsToken.Replace(line.TrimEnd(), "#");

    private static List<double> Numbers(string line) =>
        EsToken.Matches(line).Select(m => double.Parse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToList();

    private static void AssertClose(double golden, double actual)
    {
        if (Math.Abs(golden) < 1e-12 && Math.Abs(actual) < 1e-12) return; // both numerically zero
        double rel = Math.Abs(golden - actual) / Math.Max(Math.Abs(golden), 1e-300);
        Assert.True(rel < 1e-9, $"value mismatch: golden={golden:R} actual={actual:R} rel={rel:E2}");
    }

    [Fact]
    public void MrfTot_Allegro_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveCase.Solve(geo, new CaseInputs { AlfaDeg = 0, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });

        var actual = OutMrf.MrfTot(geo, r, null, " -unnamed-").Split('\n').Select(l => l.TrimEnd()).ToArray();
        var golden = GoldenText.SplitLines(TestPaths.ReadLocalGolden("allegro-mrf-alpha0.txt"))
            .Select(l => l.TrimEnd()).ToArray();

        Assert.True(golden.Length >= actual.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            Assert.Equal(Skeleton(golden[i]), Skeleton(actual[i]));
            var gn = Numbers(golden[i]);
            var an = Numbers(actual[i]);
            Assert.Equal(gn.Count, an.Count);
            for (int k = 0; k < gn.Count; k++) AssertClose(gn[k], an[k]);
        }
    }
}
