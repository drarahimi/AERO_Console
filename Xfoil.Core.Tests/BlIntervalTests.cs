using Xfoil.Core.Solver.Bl;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates BLMID + AXSET + BLDIF by finite-difference-checking the assembled
/// Newton Jacobian blocks (VS1/VS2) against the interval residuals (REZ = -VSREZ) for
/// every primary unknown at both stations. Covers the laminar amplification interval
/// and the turbulent shear-lag interval -- the heart of the coupled BL solver.</summary>
public class BlIntervalTests
{
    private const double Mach = 0.2, Re = 3.0e6;

    // Primary unknowns for one interval: [A/S, T, D, U, X] at station 1 and 2.
    private sealed record Vars(
        double A1, double T1, double D1, double U1, double X1, double S1,
        double A2, double T2, double D2, double U2, double X2, double S2);

    private static readonly Vars LaminarBase = new(
        A1: 3.0, T1: 0.0015, D1: 0.0035, U1: 1.20, X1: 0.30, S1: 0.0,
        A2: 3.4, T2: 0.0016, D2: 0.0037, U2: 1.22, X2: 0.32, S2: 0.0);

    private static readonly Vars TurbBase = new(
        A1: 9.0, T1: 0.0030, D1: 0.0044, U1: 1.15, X1: 0.60, S1: 0.030,
        A2: 9.0, T2: 0.0032, D2: 0.0047, U2: 1.14, X2: 0.63, S2: 0.031);

    // Builds an interval at the given vars and assembles its Newton system for the type.
    private static BlInterval Build(Vars v, int ityp)
    {
        var itv = new BlInterval { Env = BlEnv.Create(Mach, Re), Turb = ityp == 2 };
        Setup(itv.S1, itv.Env, v.A1, v.T1, v.D1, v.U1, v.X1, v.S1, ityp);
        Setup(itv.S2, itv.Env, v.A2, v.T2, v.D2, v.U2, v.X2, v.S2, ityp);
        itv.Blmid(ityp);
        itv.Bldif(ityp);
        return itv;
    }

    private static void Setup(BlStation s, BlEnv env, double a, double t, double d, double u, double x, double sc, int ityp)
    {
        s.Ampl = a; s.T = t; s.D = d; s.U = u; s.X = x; s.S = sc; s.Dw = 0.0;
        BlSys.Blkin(s, env);
        BlSys.Blvar(s, env, new BlParams(), ityp);
    }

    // Residual vector REZ = -VSREZ at the given vars.
    private static double[] Rez(Vars v, int ityp)
    {
        var itv = Build(v, ityp);
        return new[] { -itv.Vsrez[0], -itv.Vsrez[1], -itv.Vsrez[2] };
    }

    // Central difference of residual k wrt a chosen scalar, via a Vars mutator.
    private static double FdRez(Vars v, int ityp, int k, double x0, Func<Vars, double, Vars> withX)
    {
        double h = 1e-6 * (Math.Abs(x0) + 1e-3);
        double rp = Rez(withX(v, x0 + h), ityp)[k];
        double rm = Rez(withX(v, x0 - h), ityp)[k];
        return (rp - rm) / (2.0 * h);
    }

    private static void Close(double analytic, double fd, string what)
    {
        double tol = 5e-4 * (1.0 + Math.Abs(analytic)) + 1e-7;
        Assert.True(Math.Abs(analytic - fd) < tol, $"{what}: analytic {analytic:E6} vs fd {fd:E6}");
    }

    public static IEnumerable<object[]> Cases() => new[]
    {
        new object[] { 1 }, // laminar interval
        new object[] { 2 }, // turbulent interval
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Bldif_Jacobian_MatchesFiniteDifferenceOfResiduals(int ityp)
    {
        var v = ityp == 1 ? LaminarBase : TurbBase;
        var itv = Build(v, ityp);

        // Column accessors: 0 -> A(lam)/S(turb), 1 -> T, 2 -> D, 3 -> U, 4 -> X.
        // Station 1 mutators:
        var s1 = new (int col, Func<Vars, double> get, Func<Vars, double, Vars> set)[]
        {
            (0, x => ityp == 1 ? x.A1 : x.S1, (x, val) => ityp == 1 ? x with { A1 = val } : x with { S1 = val }),
            (1, x => x.T1, (x, val) => x with { T1 = val }),
            (2, x => x.D1, (x, val) => x with { D1 = val }),
            (3, x => x.U1, (x, val) => x with { U1 = val }),
            (4, x => x.X1, (x, val) => x with { X1 = val }),
        };
        var s2 = new (int col, Func<Vars, double> get, Func<Vars, double, Vars> set)[]
        {
            (0, x => ityp == 1 ? x.A2 : x.S2, (x, val) => ityp == 1 ? x with { A2 = val } : x with { S2 = val }),
            (1, x => x.T2, (x, val) => x with { T2 = val }),
            (2, x => x.D2, (x, val) => x with { D2 = val }),
            (3, x => x.U2, (x, val) => x with { U2 = val }),
            (4, x => x.X2, (x, val) => x with { X2 = val }),
        };

        for (int k = 0; k < 3; k++)
        {
            foreach (var (col, get, set) in s1)
                Close(itv.Vs1[k, col], FdRez(v, ityp, k, get(v), set), $"VS1[{k},{col}]");
            foreach (var (col, get, set) in s2)
                Close(itv.Vs2[k, col], FdRez(v, ityp, k, get(v), set), $"VS2[{k},{col}]");
        }
    }
}
