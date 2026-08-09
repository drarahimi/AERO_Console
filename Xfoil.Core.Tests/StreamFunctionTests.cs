using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the full-PSILIN streamfunction port (value + directional
/// derivative) that wake tracing depends on. The directional derivative PSI_NI along a
/// unit vector must equal a central finite difference of PSI along that vector -- a
/// strong check of the velocity field. Checked at several off-body points around a
/// NACA 0012 at alpha = 5 deg.</summary>
public class StreamFunctionTests
{
    private static InviscidSolver Naca0012()
    {
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
        return new InviscidSolver(pan);
    }

    [Theory]
    [InlineData(0.5, 0.30)]
    [InlineData(0.5, -0.30)]
    [InlineData(1.20, 0.05)]   // just behind the trailing edge (wake region)
    [InlineData(-0.20, 0.10)]  // ahead of the leading edge
    public void PsiNi_MatchesFiniteDifferenceOfPsi(double xi, double yi)
    {
        var s = Naca0012();
        const double alpha = 5.0;

        // n = (1,0): PSI_NI should be dPSI/dx
        var (_, psiX) = s.StreamFunction(alpha, xi, yi, 1.0, 0.0);
        double fdX = Fd(vx => s.StreamFunction(alpha, vx, yi, 1.0, 0.0).psi, xi);
        AssertClose(psiX, fdX, "dPsi/dx");

        // n = (0,1): PSI_NI should be dPSI/dy
        var (_, psiY) = s.StreamFunction(alpha, xi, yi, 0.0, 1.0);
        double fdY = Fd(vy => s.StreamFunction(alpha, xi, vy, 0.0, 1.0).psi, yi);
        AssertClose(psiY, fdY, "dPsi/dy");
    }

    private static double Fd(Func<double, double> f, double x)
    {
        double h = 1e-6 * (Math.Abs(x) + 1e-2);
        return (f(x + h) - f(x - h)) / (2.0 * h);
    }

    private static void AssertClose(double analytic, double fd, string what)
    {
        double tol = 1e-5 * (1.0 + Math.Abs(analytic)) + 1e-8;
        Assert.True(Math.Abs(analytic - fd) < tol, $"{what}: analytic {analytic:E6} vs fd {fd:E6}");
    }
}
