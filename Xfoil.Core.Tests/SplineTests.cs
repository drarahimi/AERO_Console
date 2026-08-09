using Xfoil.Core.Geometry;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Parity tests for the spline.f port.</summary>
public class SplineTests
{
    [Fact]
    public void Scalc_ArcLength_IsMonotonicAndCorrect()
    {
        double[] x = { 0, 3, 3 };
        double[] y = { 0, 4, 9 };
        var s = Spline.Scalc(x, y, 3);
        Assert.Equal(0.0, s[0], 12);
        Assert.Equal(5.0, s[1], 12);   // 3-4-5 triangle
        Assert.Equal(10.0, s[2], 12);  // + straight run of 5
    }

    [Fact]
    public void Seval_AtNodes_ReturnsNodeValues()
    {
        // y = x^2 sampled; a cubic spline must pass through every sample point exactly.
        double[] x = { 0, 1, 2, 3, 4, 5 };
        double[] y = x.Select(v => v * v).ToArray();
        var s = Spline.Scalc(x, y, x.Length);
        var ys = Spline.Segspl(y, s, y.Length);
        for (int i = 0; i < x.Length; i++)
            Assert.Equal(y[i], Spline.Seval(s[i], y, ys, s, y.Length), 9);
    }

    [Fact]
    public void SevalDeval_OnLine_ReproduceValueAndSlope()
    {
        // A straight line y = 2x + 1 must be reproduced exactly. Note s is the arc
        // length along (x,y), so ds/dx = sqrt(1 + 2^2) = sqrt(5) and dy/ds = 2/sqrt(5).
        double[] x = { 0, 1, 2, 3, 4 };
        double[] y = x.Select(v => 2.0 * v + 1.0).ToArray();
        var s = Spline.Scalc(x, y, x.Length);
        var ys = Spline.Segspl(y, s, y.Length);
        double sMid = 0.5 * (s[1] + s[2]); // arc-length midpoint of segment [1,2] -> x = 1.5
        Assert.Equal(4.0, Spline.Seval(sMid, y, ys, s, y.Length), 9);          // y(1.5) = 4
        Assert.Equal(2.0 / Math.Sqrt(5.0), Spline.Deval(sMid, y, ys, s, y.Length), 9);
    }

    [Fact]
    public void Sinvrt_RecoversArcLengthForKnownX()
    {
        double[] x = { 0, 1, 2, 3, 4, 5 };
        double[] y = x.Select(v => v * v).ToArray();
        var s = Spline.Scalc(x, y, x.Length);
        var xs = Spline.Segspl(x, s, x.Length);
        // Invert to find s where x(s) == 2.5, starting from a nearby guess.
        double si = Spline.Sinvrt(2.4, 2.5, x, xs, s, x.Length);
        Assert.Equal(2.5, Spline.Seval(si, x, xs, s, x.Length), 6);
    }
}
