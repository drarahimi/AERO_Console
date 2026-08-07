using System.IO;
using Avl.Core.Model;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>DE (design-variable changes) vs the bundled reference exe: perturbing a
/// design variable rotates the declaring sections' incidence and changes the solved
/// forces. testdes.avl with Des1 = 2.0 at alpha=0.</summary>
public class DesignChangeTests
{
    private static Geometry Load(string file) =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun(file), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    [Fact]
    public void DesignChange_Des1_MatchesReference()
    {
        var geo = Load("testdes.avl");
        Assert.True(geo.DesignNames.Count >= 1);

        var deldes = new double[geo.DesignNames.Count];
        deldes[0] = 2.0; // Des1 = 2.0

        var perturbed = DesignPerturbation.Apply(geo, deldes);
        var r = SolveCase.Solve(perturbed, new CaseInputs { AlfaDeg = 0, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });

        // Reference avl3.51-32.exe (DE 1 2.0 ; X ; FT). AVL applies a *linearized* GAM_G
        // design correction; this port re-panels with the perturbed incidence and re-solves
        // (fully nonlinear), so the two agree only to O(delta^2) ~ 1e-4 relative at 2deg.
        static void Close(double golden, double actual, double absTol) =>
            Assert.True(Math.Abs(golden - actual) < absTol, $"golden={golden} actual={actual}");
        Close(0.04619, r.Totals.ClTot, 5e-4);
        Close(0.00021, r.Totals.CdTot, 5e-4);
        Close(-0.01825, r.Totals.CmTot[1], 5e-4);
    }

    [Fact]
    public void ZeroDesignChange_IsNoOp()
    {
        var geo = Load("testdes.avl");
        var same = DesignPerturbation.Apply(geo, new double[geo.DesignNames.Count]);
        Assert.Same(geo, same); // all-zero deltas return the original geometry
    }
}
