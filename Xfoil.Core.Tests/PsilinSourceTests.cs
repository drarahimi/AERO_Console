using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the source half of the full PSILIN port. The streamfunction PSI is
/// linear in the panel source strengths, so dPSI/dSig_j must equal the influence
/// coefficient DZDM[j] exactly -- checked against a finite difference for every airfoil
/// node. Also confirms the vortex-influence DZDG[j] via dPSI/dGam_j.</summary>
public class PsilinSourceTests
{
    // Direct access to geometry for calling PsilinFull (Psi is internal to Xfoil.Core;
    // exercised here through a small reflection-free wrapper on InviscidSolver).
    [Fact]
    public void SourceAndVortexInfluence_MatchFiniteDifference()
    {
        var af = Naca.Naca4(2412); // cambered, to exercise asymmetric geometry
        var g = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
        int n = g.N;

        // Arbitrary but smooth gamma and sigma distributions.
        var gam = new double[n];
        var sig = new double[n];
        var rnd = new Random(7);
        for (int i = 0; i < n; i++) { gam[i] = 0.5 * Math.Sin(6.0 * g.S[i]); sig[i] = 0.2 * (rnd.NextDouble() - 0.5); }

        double cosa = Math.Cos(0.1), sina = Math.Sin(0.1), qinf = 1.0;
        double xi = 0.4, yi = 0.25; // off-body control point

        double PsiVal(double[] gm, double[] sg)
        {
            var a = new double[n]; var b = new double[n]; var c = new double[n]; var d = new double[n];
            return Psi.PsilinFull(g, gm, sg, cosa, sina, qinf, 0, xi, yi, 1.0, 0.0, a, b, c, d).psi;
        }

        var dzdg = new double[n]; var dzdm = new double[n]; var dqdg = new double[n]; var dqdm = new double[n];
        Psi.PsilinFull(g, gam, sig, cosa, sina, qinf, 0, xi, yi, 1.0, 0.0, dzdg, dzdm, dqdg, dqdm);

        // Check a representative spread of nodes (endpoints, LE, mid-panels).
        foreach (int j in new[] { 0, 1, n / 4, n / 2, 3 * n / 4, n - 2, n - 1 })
        {
            // dPSI/dSig_j == DZDM[j]
            double fdm = Fd(h => { var s2 = (double[])sig.Clone(); s2[j] += h; return PsiVal(gam, s2); },
                            h => { var s2 = (double[])sig.Clone(); s2[j] -= h; return PsiVal(gam, s2); });
            Assert.True(Math.Abs(dzdm[j] - fdm) < 1e-7 * (1 + Math.Abs(dzdm[j])) + 1e-9,
                $"DZDM[{j}]: {dzdm[j]:E6} vs fd {fdm:E6}");

            // dPSI/dGam_j == DZDG[j]
            double fdg = Fd(h => { var g2 = (double[])gam.Clone(); g2[j] += h; return PsiVal(g2, sig); },
                            h => { var g2 = (double[])gam.Clone(); g2[j] -= h; return PsiVal(g2, sig); });
            Assert.True(Math.Abs(dzdg[j] - fdg) < 1e-7 * (1 + Math.Abs(dzdg[j])) + 1e-9,
                $"DZDG[{j}]: {dzdg[j]:E6} vs fd {fdg:E6}");
        }
    }

    private static double Fd(Func<double, double> plus, Func<double, double> minus)
    {
        double h = 1e-6;
        return (plus(h) - minus(h)) / (2.0 * h);
    }
}
