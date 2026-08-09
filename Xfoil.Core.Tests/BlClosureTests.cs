using Xfoil.Core.Solver;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Tests for the xblsys.f closure-relation port. Beyond a few known values,
/// every analytic sensitivity is cross-checked against a central finite difference of
/// the value itself -- a strong guard against transcription errors in the (lengthy)
/// derivative formulas.</summary>
public class BlClosureTests
{
    // Central difference of f at x with a relative step.
    private static double Fd(Func<double, double> f, double x)
    {
        double h = 1e-6 * (Math.Abs(x) + 1e-3);
        return (f(x + h) - f(x - h)) / (2.0 * h);
    }

    private static void AssertClose(double analytic, double fd, string what)
    {
        double tol = 1e-4 * (1.0 + Math.Abs(analytic));
        Assert.True(Math.Abs(analytic - fd) < tol,
            $"{what}: analytic {analytic:E6} vs finite-diff {fd:E6}");
    }

    [Fact]
    public void Hkin_KnownValues_AndSensitivities()
    {
        BlClosure.Hkin(2.5, 0.0, out double hk, out double hk_h, out double hk_msq);
        Assert.Equal(2.5, hk, 12);          // Hk == H at Mach 0
        Assert.Equal(1.0, hk_h, 12);

        BlClosure.Hkin(2.5, 0.1, out double v, out double dh, out double dm);
        AssertClose(dh, Fd(h => { BlClosure.Hkin(h, 0.1, out double o, out _, out _); return o; }, 2.5), "hk_h");
        AssertClose(dm, Fd(m => { BlClosure.Hkin(2.5, m, out double o, out _, out _); return o; }, 0.1), "hk_msq");
    }

    [Theory]
    [InlineData(2.5, 500.0)]  // Hk < 4 branch
    [InlineData(5.0, 800.0)]  // Hk > 4 branch
    public void Dil_Sensitivities(double hk, double rt)
    {
        BlClosure.Dil(hk, rt, out _, out double dhk, out double drt);
        AssertClose(dhk, Fd(h => { BlClosure.Dil(h, rt, out double o, out _, out _); return o; }, hk), "di_hk");
        AssertClose(drt, Fd(r => { BlClosure.Dil(hk, r, out double o, out _, out _); return o; }, rt), "di_rt");
    }

    [Fact]
    public void Dilw_RtSensitivity_AndValue()
    {
        double hk = 3.0, rt = 400.0;
        BlClosure.Dilw(hk, rt, out double di, out double dhk, out double drt);
        // di_rt is exact -> FD-checkable.
        AssertClose(drt, Fd(r => { BlClosure.Dilw(hk, r, out double o, out _, out _); return o; }, rt), "diw_rt");
        // di_hk reproduces XFOIL's (deliberately inexact) Jacobian formula, so verify it
        // against that formula rather than a finite difference. See BlClosure.Dilw note.
        BlClosure.Hsl(hk, rt, 0.0, out double hs, out double hs_hk, out _, out _);
        double rcd = 1.10 * (1.0 - 1.0 / hk) * (1.0 - 1.0 / hk) / hk;
        double rcd_hk = -1.10 * (1.0 - 1.0 / hk) * 2.0 / (hk * hk * hk) - rcd / hk;
        double expectedDiHk = 2.0 * rcd_hk / (hs * rt) - (di / hs) * hs_hk;
        Assert.Equal(expectedDiHk, dhk, 12);
    }

    [Theory]
    [InlineData(2.5, 500.0, 0.1)]  // Hk < 4.35 branch
    [InlineData(6.0, 500.0, 0.1)]  // else branch
    public void Hsl_Sensitivities(double hk, double rt, double msq)
    {
        BlClosure.Hsl(hk, rt, msq, out _, out double dhk, out _, out _);
        AssertClose(dhk, Fd(h => { BlClosure.Hsl(h, rt, msq, out double o, out _, out _, out _); return o; }, hk), "hsl_hk");
    }

    [Theory]
    [InlineData(3.0, 500.0, 0.1)]  // Hk < 5.5 branch
    [InlineData(6.5, 500.0, 0.1)]  // else branch
    public void Cfl_Sensitivities(double hk, double rt, double msq)
    {
        BlClosure.Cfl(hk, rt, msq, out _, out double dhk, out double drt, out _);
        AssertClose(dhk, Fd(h => { BlClosure.Cfl(h, rt, msq, out double o, out _, out _, out _); return o; }, hk), "cfl_hk");
        AssertClose(drt, Fd(r => { BlClosure.Cfl(hk, r, msq, out double o, out _, out _, out _); return o; }, rt), "cfl_rt");
    }

    [Fact]
    public void Dit_Sensitivities()
    {
        double hs = 1.6, us = 0.3, cf = 0.003, st = 0.02;
        BlClosure.Dit(hs, us, cf, st, out _, out double dhs, out double dus, out double dcf, out double dst);
        AssertClose(dhs, Fd(v => { BlClosure.Dit(v, us, cf, st, out double o, out _, out _, out _, out _); return o; }, hs), "di_hs");
        AssertClose(dus, Fd(v => { BlClosure.Dit(hs, v, cf, st, out double o, out _, out _, out _, out _); return o; }, us), "di_us");
        AssertClose(dcf, Fd(v => { BlClosure.Dit(hs, us, v, st, out double o, out _, out _, out _, out _); return o; }, cf), "di_cf");
        AssertClose(dst, Fd(v => { BlClosure.Dit(hs, us, cf, v, out double o, out _, out _, out _, out _); return o; }, st), "di_st");
    }

    [Theory]
    [InlineData(2.0, 500.0, 0.1)]   // attached branch (Hk < Ho), Rt > 400
    [InlineData(5.0, 1000.0, 0.1)]  // separated branch (Hk > Ho)
    [InlineData(2.0, 150.0, 0.1)]   // Rt < 200 clamp branch
    public void Hst_Sensitivities(double hk, double rt, double msq)
    {
        BlClosure.Hst(hk, rt, msq, out _, out double dhk, out double drt, out double dmsq);
        AssertClose(dhk, Fd(h => { BlClosure.Hst(h, rt, msq, out double o, out _, out _, out _); return o; }, hk), "hst_hk");
        AssertClose(drt, Fd(r => { BlClosure.Hst(hk, r, msq, out double o, out _, out _, out _); return o; }, rt), "hst_rt");
        AssertClose(dmsq, Fd(m => { BlClosure.Hst(hk, rt, m, out double o, out _, out _, out _); return o; }, msq), "hst_msq");
    }

    [Fact]
    public void Cft_Sensitivities()
    {
        double hk = 2.0, rt = 1000.0, msq = 0.1;
        BlClosure.Cft(hk, rt, msq, out _, out double dhk, out double drt, out double dmsq);
        AssertClose(dhk, Fd(h => { BlClosure.Cft(h, rt, msq, out double o, out _, out _, out _); return o; }, hk), "cft_hk");
        AssertClose(drt, Fd(r => { BlClosure.Cft(hk, r, msq, out double o, out _, out _, out _); return o; }, rt), "cft_rt");
        AssertClose(dmsq, Fd(m => { BlClosure.Cft(hk, rt, m, out double o, out _, out _, out _); return o; }, msq), "cft_msq");
    }

    [Fact]
    public void Hct_KnownAndSensitivities()
    {
        BlClosure.Hct(2.0, 0.0, out double hc, out _, out _);
        Assert.Equal(0.0, hc, 12); // Hc == 0 at Mach 0

        double hk = 2.0, msq = 0.1;
        BlClosure.Hct(hk, msq, out _, out double dhk, out double dmsq);
        AssertClose(dhk, Fd(h => { BlClosure.Hct(h, msq, out double o, out _, out _); return o; }, hk), "hc_hk");
        AssertClose(dmsq, Fd(m => { BlClosure.Hct(hk, m, out double o, out _, out _); return o; }, msq), "hc_msq");
    }

    [Fact]
    public void Dampl_BelowCriticalReynolds_IsZero()
    {
        // Very low Rt is well below the critical Rtheta -> no amplification.
        BlClosure.Dampl(2.5, 0.01, 50.0, out double ax, out double axhk, out double axth, out double axrt);
        Assert.Equal(0.0, ax, 12);
        Assert.Equal(0.0, axhk, 12);
        Assert.Equal(0.0, axth, 12);
        Assert.Equal(0.0, axrt, 12);
    }

    [Theory]
    [InlineData(2.5, 0.01, 2000.0)] // fully-on region (rfac = 1)
    [InlineData(2.5, 0.01, 700.0)]  // cubic ramp region (0 < rfac < 1)
    public void Dampl_Sensitivities(double hk, double th, double rt)
    {
        BlClosure.Dampl(hk, th, rt, out double ax, out double axhk, out double axth, out double axrt);
        Assert.True(ax > 0.0);
        AssertClose(axhk, Fd(h => { BlClosure.Dampl(h, th, rt, out double o, out _, out _, out _); return o; }, hk), "ax_hk");
        AssertClose(axth, Fd(t => { BlClosure.Dampl(hk, t, rt, out double o, out _, out _, out _); return o; }, th), "ax_th");
        AssertClose(axrt, Fd(r => { BlClosure.Dampl(hk, th, r, out double o, out _, out _, out _); return o; }, rt), "ax_rt");
    }
}
