using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Parity tests for the inviscid panel solve, checked against the bundled
/// xfoil.exe: the Cp distribution (CPWR dump) and integrated forces (PACC polar) for
/// NACA 0012 at alpha = 5 deg, Mach 0.</summary>
public class InviscidSolverTests
{
    private static InviscidSolver BuildNaca0012()
    {
        var af = Naca.Naca4(12);
        var pan = Paneling.Pangen(af.Xb, af.Yb, af.Name)!;
        return new InviscidSolver(pan);
    }

    // Reads XFOIL's CPWR dump: header lines then "x  y  Cp" rows.
    private static (double[] x, double[] cp) ReadCpDump(string fileName)
    {
        var xs = new List<double>();
        var cps = new List<double>();
        foreach (var raw in TestPaths.ReadGolden(fileName).Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var t = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 3) continue;
            if (!double.TryParse(t[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double x)) continue;
            xs.Add(x);
            cps.Add(double.Parse(t[2], System.Globalization.CultureInfo.InvariantCulture));
        }
        return (xs.ToArray(), cps.ToArray());
    }

    [Fact]
    public void Naca0012_Alpha5_ClCmCdp_MatchXfoilExe()
    {
        var pt = BuildNaca0012().SolveAlpha(5.0);
        // xfoil.exe PACC polar (inviscid): CL 0.6034, CM -0.0070, CDp -0.00109.
        Assert.Equal(0.6034, pt.Cl, 3);
        Assert.Equal(-0.0070, pt.Cm, 3);
        Assert.Equal(-0.00109, pt.Cdp, 4);
    }

    [Fact]
    public void Naca0012_Alpha5_Cp_MatchesXfoilExe_NodeByNode()
    {
        var (gx, gcp) = ReadCpDump("naca0012_inviscid_a5_cp.dat");
        var pt = BuildNaca0012().SolveAlpha(5.0);

        Assert.Equal(gx.Length, pt.Cp.Length);
        double maxErr = 0.0;
        for (int i = 0; i < pt.Cp.Length; i++)
            maxErr = Math.Max(maxErr, Math.Abs(pt.Cp[i] - gcp[i]));
        // xfoil.exe writes Cp to 5 decimals; the solve reproduces every node within a
        // few 1e-4 (single- vs double-precision panel arithmetic).
        Assert.True(maxErr < 2e-3, $"max Cp error {maxErr:E3} exceeds tolerance");
    }

    [Fact]
    public void Naca0012_ZeroAlpha_IsSymmetric_ZeroLift()
    {
        var pt = BuildNaca0012().SolveAlpha(0.0);
        Assert.Equal(0.0, pt.Cl, 6);
        Assert.Equal(0.0, pt.Cm, 6);
    }
}
