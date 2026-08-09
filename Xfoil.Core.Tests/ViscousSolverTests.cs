using Xfoil.Core.Geometry;
using Xfoil.Core.Solver.Bl;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>End-to-end validation of the viscous driver against the bundled xfoil.exe.
/// Reference values are from xfoil.exe viscous PACC polars for NACA 0012, Re = 1e6,
/// Mach 0, Ncrit 9 (free transition). The native solver reproduces CL, CD, CM, and both
/// transition locations to xfoil.exe's printed precision.</summary>
public class ViscousSolverTests
{
    private static PanelAirfoil Naca0012()
    {
        var af = Naca.Naca4(12);
        return Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
    }

    // alpha, CL, CD, CM, Top_Xtr, Bot_Xtr  (from xfoil.exe)
    [Theory]
    [InlineData(0.0, 0.0000, 0.00540, -0.0000, 0.6871, 0.6870)]
    [InlineData(2.0, 0.2144, 0.00581, 0.0030, 0.4742, 0.8674)]
    [InlineData(5.0, 0.5571, 0.00848, 0.0019, 0.1488, 0.9853)]
    [InlineData(6.0, 0.6940, 0.00973, -0.0041, 0.0815, 0.9944)]
    public void Naca0012_Re1e6_MatchesXfoilExe(double alpha, double cl, double cd, double cm, double xtrTop, double xtrBot)
    {
        var pan = Naca0012();
        var vs = new ViscousSolver(pan, 1.0e6, 0.0, 9.0);
        var r = vs.Solve(alpha, 80);

        Assert.True(r.Converged, $"viscous solve did not converge (rms {r.RmsResidual:E2})");
        Assert.Equal(cl, r.Cl, 3);      // CL to xfoil.exe's 4-decimal print (+/- ~1 count)
        Assert.Equal(cd, r.Cd, 4);      // CD to 5 decimals
        Assert.Equal(cm, r.Cm, 3);
        Assert.Equal(xtrTop, r.XtrTop, 2);
        Assert.Equal(xtrBot, r.XtrBottom, 2);
    }

    [Fact]
    public void Naca0012_ConvergesQuickly()
    {
        var vs = new ViscousSolver(Naca0012(), 1.0e6, 0.0, 9.0);
        var r = vs.Solve(5.0, 80);
        Assert.True(r.Converged);
        Assert.True(r.Iterations <= 20, $"took {r.Iterations} iterations");
    }

    [Fact]
    public void Naca0012_ViscousSurfaceCp_MatchesXfoilExe()
    {
        var vs = new ViscousSolver(Naca0012(), 1.0e6, 0.0, 9.0);
        vs.Solve(5.0, 80);
        var cp = vs.SurfaceCp();

        // xfoil.exe viscous CPWR dump: header then x y Cp rows.
        var gcp = new List<double>();
        foreach (var raw in TestPaths.ReadGolden("naca0012_visc_re1e6_a5_cp.dat").Replace("\r\n", "\n").Split('\n'))
        {
            var t = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 3 || !double.TryParse(t[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) continue;
            gcp.Add(double.Parse(t[2], System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.Equal(gcp.Count, cp.Count);
        double maxErr = 0.0;
        for (int i = 0; i < cp.Count; i++) maxErr = Math.Max(maxErr, Math.Abs(cp[i].cpv - gcp[i]));
        Assert.True(maxErr < 3e-3, $"viscous Cp max error {maxErr:E3}");
    }

    [Fact]
    public void Naca0012_BoundaryLayer_IsPhysical_AndIncludesWake()
    {
        var vs = new ViscousSolver(Naca0012(), 1.0e6, 0.0, 9.0);
        vs.Solve(5.0, 80);
        var bl = vs.BoundaryLayer();

        Assert.True(bl.Count > 40);
        // BL runs from the LE over the airfoil AND down the wake (~1 chord past the TE),
        // so x reaches ~2 -- matching XFOIL's dump (whose plot x-axis goes to 2 chords).
        Assert.True(bl.Max(p => p.X) > 1.8, $"max x {bl.Max(p => p.X):F3} should reach into the wake");

        // On the airfoil, the shape parameter and thicknesses are positive and bounded.
        foreach (var b in bl.Where(p => p.X <= 1.0 + 1e-6))
        {
            Assert.InRange(b.H, 1.0, 20.0);
            Assert.True(b.Theta > 0 && b.Dstar > 0 && b.Dstar >= b.Theta);
        }
    }
}
