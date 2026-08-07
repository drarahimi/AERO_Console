using System.IO;
using Avl.Core.Model;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Numeric parity of the solver against reference avl.exe totals captured
/// in test/golden/allegro-ft-alpha0.txt (before the text formatters exist).</summary>
public class SolverTests
{
    private static Geometry LoadAllegro()
    {
        return AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var path = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            },
        });
    }

    [Fact]
    public void Allegro_Alpha0_TotalsMatchReference()
    {
        var geo = LoadAllegro();
        var result = SolveCase.Solve(geo, new CaseInputs
        {
            AlfaDeg = 0,
            BetaDeg = 0,
            Wrot = new double[] { 0, 0, 0 },
        });

        // Reference (avl3.51-32.exe): allegro-ft-alpha0.txt
        Assert.Equal(0.43551, result.Totals.ClTot, 4);
        Assert.Equal(0.02509, result.Totals.CdTot, 4);
        Assert.Equal(0.02973, result.Totals.CmTot[1], 4);
        Assert.Equal(0.43397, result.Trefftz.Clff, 4);
        Assert.Equal(0.0051452, result.Trefftz.Cdff, 5);
        Assert.Equal(0.9995, result.Trefftz.Spanef, 3);
    }
}
