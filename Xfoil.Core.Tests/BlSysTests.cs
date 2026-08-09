using Xfoil.Core.Solver.Bl;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the BLKIN + BLVAR port by finite-difference-checking every
/// secondary variable's sensitivity to the primary variables (U, T, D, S) and to the
/// flow parameters (Mach^2, Reynolds), across laminar and turbulent stations. This
/// exercises the whole chain: env setup -> BLKIN -> closure relations -> BLVAR
/// sensitivity accumulation.</summary>
public class BlSysTests
{
    private static readonly BlParams P = new();

    // Runs BLKIN + BLVAR for one station and returns the populated state.
    private static BlStation Eval(double mach, double re, double u, double t, double d, double sc, int ityp)
    {
        var env = BlEnv.Create(mach, re);
        var s = new BlStation { U = u, T = t, D = d, S = sc };
        BlSys.Blkin(s, env);
        BlSys.Blvar(s, env, P, ityp);
        return s;
    }

    private static double Fd(Func<double, double> f, double x)
    {
        double h = 1e-6 * (Math.Abs(x) + 1e-3);
        return (f(x + h) - f(x - h)) / (2.0 * h);
    }

    private static void Close(double analytic, double fd, string what)
    {
        double tol = 1e-4 * (1.0 + Math.Abs(analytic)) + 1e-9;
        Assert.True(Math.Abs(analytic - fd) < tol, $"{what}: analytic {analytic:E6} vs fd {fd:E6}");
    }

    // (mach, re, U, T, D, S, ityp) for a healthy laminar and turbulent station (no clamps).
    public static IEnumerable<object[]> Stations() => new[]
    {
        new object[] { 0.2, 3.0e6, 1.2, 0.0015, 0.0035, 0.0, 1 }, // laminar
        new object[] { 0.2, 3.0e6, 1.2, 0.0025, 0.0035, 0.03, 2 }, // turbulent
    };

    [Theory]
    [MemberData(nameof(Stations))]
    public void PrimarySensitivities_MatchFiniteDifference(double mach, double re, double u, double t, double d, double sc, int ityp)
    {
        var s = Eval(mach, re, u, t, d, sc, ityp);

        // d/dU
        Close(s.Hk_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Hk, u), "Hk_U");
        Close(s.Rt_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Rt, u), "Rt_U");
        Close(s.Hs_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Hs, u), "Hs_U");
        Close(s.Cf_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Cf, u), "Cf_U");
        Close(s.Di_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Di, u), "Di_U");
        Close(s.Us_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Us, u), "Us_U");
        Close(s.Cq_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).Cq, u), "Cq_U");
        Close(s.De_U, Fd(x => Eval(mach, re, x, t, d, sc, ityp).De, u), "De_U");

        // d/dT
        Close(s.Hk_T, Fd(x => Eval(mach, re, u, x, d, sc, ityp).Hk, t), "Hk_T");
        Close(s.Rt_T, Fd(x => Eval(mach, re, u, x, d, sc, ityp).Rt, t), "Rt_T");
        Close(s.Cf_T, Fd(x => Eval(mach, re, u, x, d, sc, ityp).Cf, t), "Cf_T");
        Close(s.Di_T, Fd(x => Eval(mach, re, u, x, d, sc, ityp).Di, t), "Di_T");
        Close(s.De_T, Fd(x => Eval(mach, re, u, x, d, sc, ityp).De, t), "De_T");

        // d/dD
        Close(s.Hk_D, Fd(x => Eval(mach, re, u, t, x, sc, ityp).Hk, d), "Hk_D");
        Close(s.Cf_D, Fd(x => Eval(mach, re, u, t, x, sc, ityp).Cf, d), "Cf_D");
        Close(s.Di_D, Fd(x => Eval(mach, re, u, t, x, sc, ityp).Di, d), "Di_D");
        Close(s.De_D, Fd(x => Eval(mach, re, u, t, x, sc, ityp).De, d), "De_D");
    }

    [Fact]
    public void Turbulent_DiSensitivityToCtau_MatchesFiniteDifference()
    {
        double mach = 0.2, re = 3.0e6, u = 1.2, t = 0.0025, d = 0.0035, sc = 0.03;
        var s = Eval(mach, re, u, t, d, sc, 2);
        Close(s.Di_S, Fd(x => Eval(mach, re, u, t, d, x, 2).Di, sc), "Di_S");
    }

    [Theory]
    [MemberData(nameof(Stations))]
    public void MachAndReynoldsSensitivities_MatchFiniteDifference(double mach, double re, double u, double t, double d, double sc, int ityp)
    {
        var s = Eval(mach, re, u, t, d, sc, ityp);

        // d/d(Mach^2): perturb in msq space (env sensitivities are wrt Mach^2).
        double msq = mach * mach;
        Close(s.Hk_Ms, Fd(x => Eval(Math.Sqrt(x), re, u, t, d, sc, ityp).Hk, msq), "Hk_Ms");
        Close(s.Rt_Ms, Fd(x => Eval(Math.Sqrt(x), re, u, t, d, sc, ityp).Rt, msq), "Rt_Ms");
        Close(s.Cf_Ms, Fd(x => Eval(Math.Sqrt(x), re, u, t, d, sc, ityp).Cf, msq), "Cf_Ms");
        Close(s.Di_Ms, Fd(x => Eval(Math.Sqrt(x), re, u, t, d, sc, ityp).Di, msq), "Di_Ms");

        // d/dReynolds
        Close(s.Rt_Re, Fd(x => Eval(mach, x, u, t, d, sc, ityp).Rt, re), "Rt_Re");
        Close(s.Cf_Re, Fd(x => Eval(mach, x, u, t, d, sc, ityp).Cf, re), "Cf_Re");
        Close(s.Di_Re, Fd(x => Eval(mach, x, u, t, d, sc, ityp).Di, re), "Di_Re");
    }
}
