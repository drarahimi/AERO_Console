using System.IO;
using Avl.Core.Model;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>BODY blocks (source/doublet fuselage) + FB body forces vs the reference exe,
/// using bd.avl (Bubble Dancer, one fuselage pod).</summary>
public class BodyTests
{
    private static Geometry LoadBd() =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("bd.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    [Fact]
    public void BodyGeometry_MakeBody_MatchesReference()
    {
        var geo = LoadBd();
        Assert.Single(geo.Bodies); // one "Fuse pod" (no YDUPLICATE)
        var b = geo.Bodies[0];

        // Reference avl3.51-32.exe FB output for bd.avl: Length, Asurf, Vol.
        Assert.Equal(24.168728, b.Length, 3);
        Assert.Equal(135.596150, b.Surface, 2);
        Assert.Equal(63.920303, b.Volume, 2);
    }

    [Fact]
    public void BodyForces_BdForc_MatchReference()
    {
        var geo = LoadBd();
        double alfa = 3.0 * Math.PI / 180.0;
        double betm = Math.Sqrt(1.0 - geo.Mach0 * geo.Mach0);
        var vinf = new double[] { Math.Cos(alfa), 0, Math.Sin(alfa) };
        var wrot = new double[] { 0, 0, 0 };

        var segs = BodyAero.SrdSet(betm, geo.Xyzref0, geo.IYsym, geo.Bodies);
        var f = BodyAero.BdForc(geo.Bodies[0], segs, 0, betm, vinf, wrot, alfa,
            geo.Sref, geo.Cref, geo.Bref, geo.Xyzref0);

        // Reference FB (bd.avl, alpha=3): CL, CD, Cm.
        Assert.Equal(0.000049, f.Cl, 5);
        Assert.Equal(0.000003, f.Cd, 5);
        Assert.Equal(0.000630, f.Cm[1], 5);
    }

    [Fact]
    public void CoupledTotals_WithBody_MatchReference()
    {
        var geo = LoadBd();
        var r = SolveCase.Solve(geo, new CaseInputs { AlfaDeg = 3, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });

        // Reference avl3.51-32.exe FT for bd.avl at alpha=3 (wing coupled to the fuselage body).
        Assert.Equal(0.72651, r.Totals.ClTot, 4);
        Assert.Equal(0.02920, r.Totals.CdTot, 4);
        Assert.Equal(-0.01828, r.Totals.CmTot[1], 4);
        Assert.Single(r.Totals.ByBody);
    }
}
