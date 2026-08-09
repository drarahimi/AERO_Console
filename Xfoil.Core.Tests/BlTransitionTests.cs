using Xfoil.Core.Solver.Bl;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the TRCHEK2 (transition-point location) and TRDIF (blended
/// transition-interval system) ports. TRCHEK2's XT sensitivities and TRDIF's assembled
/// residuals are finite-difference-checked against the primary unknowns. A transition
/// scenario is engineered so the laminar amplification crosses Ncrit inside the
/// interval.</summary>
public class BlTransitionTests
{
    private const double Mach = 0.15, Re = 2.0e6;

    private sealed record Vars(
        double A1, double T1, double D1, double U1, double X1,
        double C2, double T2, double D2, double U2, double X2);

    // Amplification starts near Ncrit(=9) at station 1 and, with a decelerating (high-Hk)
    // laminar layer, crosses Ncrit inside the interval -> transition at Xt ~ 0.355.
    private static readonly Vars Base = new(
        A1: 8.6, T1: 0.0012, D1: 0.00360, U1: 1.20, X1: 0.30,
        C2: 0.030, T2: 0.0014, D2: 0.00420, U2: 1.20, X2: 0.45);

    // Builds an interval, runs Trchek2, and (if transitioning) Trdif.
    private static BlInterval Build(Vars v)
    {
        var itv = new BlInterval { Env = BlEnv.Create(Mach, Re), Xiforc = 1.0e9 };
        var env = itv.Env; var p = new BlParams();
        itv.S1.Ampl = v.A1; itv.S1.T = v.T1; itv.S1.D = v.D1; itv.S1.U = v.U1; itv.S1.X = v.X1; itv.S1.S = 0; itv.S1.Dw = 0;
        BlSys.Blkin(itv.S1, env); BlSys.Blvar(itv.S1, env, p, 1);
        itv.S2.Ampl = 0; itv.S2.T = v.T2; itv.S2.D = v.D2; itv.S2.U = v.U2; itv.S2.X = v.X2; itv.S2.S = v.C2; itv.S2.Dw = 0;
        BlSys.Blkin(itv.S2, env); BlSys.Blvar(itv.S2, env, p, 2);
        itv.Trchek2();
        if (itv.Tran) itv.Trdif();
        return itv;
    }

    private static double Fd(Func<double, double> f, double x0, double relStep = 1e-6)
    {
        double h = relStep * (Math.Abs(x0) + 1e-3);
        return (f(x0 + h) - f(x0 - h)) / (2.0 * h);
    }

    [Fact]
    public void Trchek2_DetectsTransition_InsideInterval()
    {
        var itv = Build(Base);
        Assert.True(itv.Tran, "expected transition in interval");
        Assert.True(itv.Trfree);
        Assert.InRange(itv.Xt, Base.X1, Base.X2);
    }

    [Fact]
    public void Trchek2_XtSensitivities_MatchFiniteDifference()
    {
        var itv = Build(Base);
        Assert.True(itv.Tran);

        // XT depends on the laminar primaries at both stations. Check the strongest ones.
        double Xt(Vars v) => Build(v).Xt;
        AssertClose(itv.Xt_T1, Fd(x => Xt(Base with { T1 = x }), Base.T1), "Xt_T1", 5e-3);
        AssertClose(itv.Xt_U1, Fd(x => Xt(Base with { U1 = x }), Base.U1), "Xt_U1", 5e-3);
        AssertClose(itv.Xt_X1, Fd(x => Xt(Base with { X1 = x }), Base.X1), "Xt_X1", 5e-3);
        AssertClose(itv.Xt_T2, Fd(x => Xt(Base with { T2 = x }), Base.T2), "Xt_T2", 5e-3);
        AssertClose(itv.Xt_U2, Fd(x => Xt(Base with { U2 = x }), Base.U2), "Xt_U2", 5e-3);
        AssertClose(itv.Xt_X2, Fd(x => Xt(Base with { X2 = x }), Base.X2), "Xt_X2", 5e-3);
        AssertClose(itv.Xt_A1, Fd(x => Xt(Base with { A1 = x }), Base.A1), "Xt_A1", 5e-3);
    }

    [Fact]
    public void Trdif_Jacobian_MatchesFiniteDifferenceOfResiduals()
    {
        var itv = Build(Base);
        Assert.True(itv.Tran);

        double[] Rez(Vars v) { var it = Build(v); return new[] { -it.Vsrez[0], -it.Vsrez[1], -it.Vsrez[2] }; }

        // Station-1 unknowns [A1,T1,D1,U1,X1] and station-2 unknowns [Ctau2,T2,D2,U2,X2].
        var s1 = new (int col, Func<Vars, double> get, Func<Vars, double, Vars> set)[]
        {
            (0, v => v.A1, (v, x) => v with { A1 = x }),
            (1, v => v.T1, (v, x) => v with { T1 = x }),
            (2, v => v.D1, (v, x) => v with { D1 = x }),
            (3, v => v.U1, (v, x) => v with { U1 = x }),
            (4, v => v.X1, (v, x) => v with { X1 = x }),
        };
        var s2 = new (int col, Func<Vars, double> get, Func<Vars, double, Vars> set)[]
        {
            (0, v => v.C2, (v, x) => v with { C2 = x }),
            (1, v => v.T2, (v, x) => v with { T2 = x }),
            (2, v => v.D2, (v, x) => v with { D2 = x }),
            (3, v => v.U2, (v, x) => v with { U2 = x }),
            (4, v => v.X2, (v, x) => v with { X2 = x }),
        };

        for (int k = 0; k < 3; k++)
        {
            foreach (var (col, get, set) in s1)
                AssertClose(itv.Vs1[k, col], Fd(x => Rez(set(Base, x))[k], get(Base)), $"VS1[{k},{col}]", 1e-2);
            foreach (var (col, get, set) in s2)
                AssertClose(itv.Vs2[k, col], Fd(x => Rez(set(Base, x))[k], get(Base)), $"VS2[{k},{col}]", 1e-2);
        }
    }

    private static void AssertClose(double analytic, double fd, string what, double rel)
    {
        double tol = rel * (1.0 + Math.Abs(analytic)) + 1e-6;
        Assert.True(Math.Abs(analytic - fd) < tol, $"{what}: analytic {analytic:E6} vs fd {fd:E6}");
    }
}
