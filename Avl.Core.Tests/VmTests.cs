using System.Globalization;
using System.IO;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>VM (strip shear/moment) parity vs the bundled reference exe. Only the
/// fixed-format numeric data rows (F10.4,G14.6,3X,G14.6) are byte-comparable; the
/// list-directed header lines are compiler-specific and not machine-parsed.</summary>
public class VmTests
{
    private static Geometry LoadAllegro() =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    /// <summary>Lines that are exactly three numeric tokens -- the shear/moment data rows.</summary>
    private static List<string> DataRows(IEnumerable<string> lines)
    {
        var rows = new List<string>();
        foreach (var l in lines)
        {
            var t = l.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length != 3) continue;
            if (t.All(x => double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                rows.Add(l.TrimEnd());
        }
        return rows;
    }

    [Fact]
    public void Vm_Allegro_DataRows_MatchReference()
    {
        var geo = LoadAllegro();
        var r = SolveCase.Solve(geo, new CaseInputs { AlfaDeg = 0, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });

        var actual = DataRows(OutVm.FormatOutVm(geo, r).Split('\n'));
        var golden = DataRows(GoldenText.SplitLines(TestPaths.ReadLocalGolden("allegro-vm-alpha0.txt")));

        Assert.NotEmpty(golden);
        Assert.Equal(golden.Count, actual.Count);
        string Norm(string s) => GoldenText.NormalizeNearZero(GoldenText.NormalizeNegativeZero(s));
        for (int i = 0; i < golden.Count; i++)
            Assert.Equal(Norm(golden[i]), Norm(actual[i]));
    }
}
