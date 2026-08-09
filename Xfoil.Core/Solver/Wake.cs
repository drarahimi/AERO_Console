// Port of xpanel.f XYWAKE (wake-trajectory tracing) and xutils.f SETEXP (geometric
// arc-length stretching). The wake is grown downstream from the trailing edge, each
// new node placed along the local inviscid streamline direction (from the
// streamfunction gradient), with geometrically increasing spacing out to WAKLEN
// chords. This produces the wake node coordinates, arc length, node normals, and panel
// angles the viscous BL marches over past the TE.

using Xfoil.Core.Geometry;

namespace Xfoil.Core.Solver;

/// <summary>Wake geometry appended to the airfoil (XFOIL nodes N+1..N+NW). Arrays are
/// length Nw; index 0 is the first wake node, just behind the TE.</summary>
public sealed record WakeGeometry(int Nw, double[] X, double[] Y, double[] S, double[] Nx, double[] Ny, double[] Apanel);

public static class Wake
{
    /// <summary>Default wake length in chords (xfoil.f WAKLEN = 1.0).</summary>
    public const double WakLen = 1.0;

    /// <summary>Port of xutils.f SETEXP: geometrically stretched array of nn points from
    /// 0 to smax with first increment ds1.</summary>
    public static double[] Setexp(double ds1, double smax, int nn)
    {
        double sigma = smax / ds1;
        int nex = nn - 1;
        double rnex = nex;
        double rni = 1.0 / rnex;

        double aaa = rnex * (rnex - 1.0) * (rnex - 2.0) / 6.0;
        double bbb = rnex * (rnex - 1.0) / 2.0;
        double ccc = rnex - sigma;
        double disc = Math.Max(0.0, bbb * bbb - 4.0 * aaa * ccc);

        double ratio;
        if (nex <= 1) throw new InvalidOperationException("SETEXP: N too small");
        else if (nex == 2) ratio = -ccc / bbb + 1.0;
        else ratio = (-bbb + Math.Sqrt(disc)) / (2.0 * aaa) + 1.0;

        if (ratio != 1.0)
        {
            for (int iter = 0; iter < 100; iter++)
            {
                double sigman = (Math.Pow(ratio, nex) - 1.0) / (ratio - 1.0);
                double res = Math.Pow(sigman, rni) - Math.Pow(sigma, rni);
                double dresdr = rni * Math.Pow(sigman, rni)
                    * (rnex * Math.Pow(ratio, nex - 1) - sigman) / (Math.Pow(ratio, nex) - 1.0);
                double dratio = -res / dresdr;
                ratio += dratio;
                if (Math.Abs(dratio) < 1.0e-5) break;
            }
        }

        var s = new double[nn];
        s[0] = 0.0;
        double ds = ds1;
        for (int i = 1; i < nn; i++) { s[i] = s[i - 1] + ds; ds *= ratio; }
        return s;
    }

    /// <summary>Port of XYWAKE. Traces the wake for the inviscid solution at the given
    /// alpha (deg). `solver` provides the streamfunction gradient used to follow the
    /// local streamline.</summary>
    public static WakeGeometry XyWake(PanelAirfoil g, InviscidSolver solver, double alphaDeg)
    {
        int n = g.N;
        int nw = n / 8 + 2;

        double ds1 = 0.5 * (g.S[1] - g.S[0] + g.S[n - 1] - g.S[n - 2]);
        var snew = Setexp(ds1, WakLen * g.Chord, nw);

        double xte = 0.5 * (g.X[0] + g.X[n - 1]);
        double yte = 0.5 * (g.Y[0] + g.Y[n - 1]);

        var x = new double[nw];
        var y = new double[nw];
        var s = new double[nw];
        var nx = new double[nw];
        var ny = new double[nw];
        var apanel = new double[nw];

        // first wake node, a tiny distance behind the TE along the TE bisector normal
        double sx = 0.5 * (g.Yp[n - 1] - g.Yp[0]);
        double sy = 0.5 * (g.Xp[0] - g.Xp[n - 1]);
        double smod = Math.Sqrt(sx * sx + sy * sy);
        nx[0] = sx / smod;
        ny[0] = sy / smod;
        x[0] = xte - 0.0001 * ny[0];
        y[0] = yte + 0.0001 * nx[0];
        s[0] = g.S[n - 1];

        // streamfunction gradient at the first node -> normal of the next node
        SetNextNormal(solver, alphaDeg, x[0], y[0], nx, ny, apanel, 0);

        for (int j = 1; j < nw; j++)
        {
            double ds = snew[j] - snew[j - 1];
            x[j] = x[j - 1] - ds * ny[j];
            y[j] = y[j - 1] + ds * nx[j];
            s[j] = s[j - 1] + ds;
            if (j < nw - 1)
                SetNextNormal(solver, alphaDeg, x[j], y[j], nx, ny, apanel, j);
        }

        return new WakeGeometry(nw, x, y, s, nx, ny, apanel);
    }

    // Computes grad(PSI) at (xi,yi) and sets the normal of node j+1 plus the panel angle
    // of node j (matching XYWAKE's per-point normal update).
    private static void SetNextNormal(InviscidSolver solver, double alphaDeg, double xi, double yi,
        double[] nx, double[] ny, double[] apanel, int j)
    {
        double psiX = solver.StreamFunction(alphaDeg, xi, yi, 1.0, 0.0).psiNi;
        double psiY = solver.StreamFunction(alphaDeg, xi, yi, 0.0, 1.0).psiNi;
        double gmod = Math.Sqrt(psiX * psiX + psiY * psiY);
        nx[j + 1] = -psiX / gmod;
        ny[j + 1] = -psiY / gmod;
        apanel[j] = Math.Atan2(psiY, psiX);
    }
}
