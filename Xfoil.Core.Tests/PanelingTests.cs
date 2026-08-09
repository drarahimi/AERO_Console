using Xfoil.Core.Geometry;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Parity tests for the PANGEN paneling port, checked against a golden airfoil
/// saved directly from the bundled xfoil.exe (NACA 0012, default 160 panels).</summary>
public class PanelingTests
{
    // Reads a plain/labeled .dat golden file into parallel x/y arrays.
    private static (double[] x, double[] y) ReadDat(string fileName)
    {
        var res = AirfoilFile.Read(TestPaths.ReadGolden(fileName));
        Assert.True(res.Ok);
        return (res.X, res.Y);
    }

    [Fact]
    public void Naca0012_NodeCount_Is160()
    {
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name);
        Assert.NotNull(pan);
        Assert.Equal(160, pan!.N);
    }

    [Fact]
    public void Naca0012_LeadingTrailingEdge_AreCorrect()
    {
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
        // TE at x=1 (endpoints), LE near the origin, unit chord, blunt TE.
        Assert.Equal(1.0, pan.X[0], 4);
        Assert.Equal(1.0, pan.X[^1], 4);
        Assert.InRange(pan.Xle, -1e-4, 1e-3);
        Assert.Equal(1.0, pan.Chord, 3);
        Assert.False(pan.Sharp);
    }

    [Fact]
    public void Naca0012_MatchesXfoilExe_NodeByNode()
    {
        var (gx, gy) = ReadDat("naca0012_pane160.dat");
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;

        Assert.Equal(gx.Length, pan.N);
        double maxErr = 0.0;
        for (int i = 0; i < pan.N; i++)
        {
            maxErr = Math.Max(maxErr, Math.Abs(pan.X[i] - gx[i]));
            maxErr = Math.Max(maxErr, Math.Abs(pan.Y[i] - gy[i]));
        }
        // xfoil.exe writes 6-decimal coordinates, so the floor on any comparison is
        // ~5e-7 rounding. The port reproduces every node to within that -- measured
        // max error ~6e-7 -- so this tight bound is a genuine regression guard.
        Assert.True(maxErr < 2e-6, $"max node coordinate error {maxErr:E3} exceeds tolerance");
    }
}
