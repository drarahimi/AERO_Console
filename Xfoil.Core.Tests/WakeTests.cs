using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the XYWAKE + SETEXP port. Structural checks (node count, TE start,
/// monotonic arc length, downstream extent) plus the key physical invariant: the wake is
/// the dividing streamline, so the streamfunction stays ~constant along it (to within
/// the explicit-tracing discretization error).</summary>
public class WakeTests
{
    private static (PanelAirfoil g, InviscidSolver s) Naca0012()
    {
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
        return (pan, new InviscidSolver(pan));
    }

    [Fact]
    public void Setexp_ProducesGeometricStretchToSmax()
    {
        var s = Wake.Setexp(0.01, 2.0, 10);
        Assert.Equal(0.0, s[0], 12);
        Assert.Equal(2.0, s[^1], 9);
        Assert.Equal(0.01, s[1] - s[0], 9); // first increment
        // increments grow monotonically
        for (int i = 2; i < s.Length; i++)
            Assert.True(s[i] - s[i - 1] >= s[i - 1] - s[i - 2] - 1e-12);
    }

    [Fact]
    public void XyWake_StructureIsCorrect()
    {
        var (g, s) = Naca0012();
        var w = Wake.XyWake(g, s, 5.0);

        Assert.Equal(g.N / 8 + 2, w.Nw);

        double xte = 0.5 * (g.X[0] + g.X[^1]);
        Assert.Equal(xte, w.X[0], 3);      // first node just behind TE
        Assert.InRange(w.Y[0], -1e-3, 1e-3);

        for (int j = 1; j < w.Nw; j++)
            Assert.True(w.S[j] > w.S[j - 1]); // arc length strictly increasing

        // wake extends ~WAKLEN*chord downstream of the TE
        Assert.InRange(w.X[^1], xte + 0.8 * g.Chord, xte + 1.2 * g.Chord);
    }

    [Fact]
    public void XyWake_FollowsStreamline()
    {
        var (g, s) = Naca0012();
        const double alpha = 5.0;
        var w = Wake.XyWake(g, s, alpha);

        double psi0 = s.StreamFunction(alpha, w.X[0], w.Y[0], 1.0, 0.0).psi;
        double maxDev = 0.0;
        for (int j = 0; j < w.Nw; j++)
            maxDev = Math.Max(maxDev, Math.Abs(s.StreamFunction(alpha, w.X[j], w.Y[j], 1.0, 0.0).psi - psi0));

        // The wake is the dividing streamline; PSI is constant along it up to the
        // explicit-tracing discretization error (measured ~1.4e-3).
        Assert.True(maxDev < 5e-3, $"streamfunction varied by {maxDev:E3} along the wake");
    }
}
