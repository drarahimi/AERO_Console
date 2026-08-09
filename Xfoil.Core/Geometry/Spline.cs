// Port of XFOIL's spline.f: the cubic-spline toolkit used throughout XFOIL for
// geometry (X(S), Y(S)), Cp interpolation, and spline inversion. This is the
// canonical original -- Avl.Core/Geometry/Spline.cs is a trimmed derivative of
// the same code -- so this port keeps the full surface XFOIL relies on:
// SPLINE/SPLIND/SPLINA (derivative solves), SEVAL/DEVAL/D2VAL (evaluation),
// CURV/CURVS (curvature), SINVRT (Newton inversion), SCALC (arc length),
// SEGSPL/SEGSPLD (segment-aware splines that allow slope discontinuities at
// duplicated-S joints), and TRISOL (tridiagonal solve).
//
// Fortran arrays are 1-based; this port is 0-based. XFOIL's DP builds use
// double precision, matched here. Where the Fortran passes an array section
// (e.g. X(ISEG0)) the port passes an explicit (offset,count) instead.

namespace Xfoil.Core.Geometry;

public static class Spline
{
    /// <summary>Port of spline.f SUBROUTINE TRISOL. Solves a kk-long tridiagonal
    /// system in place over the sub-range [off, off+kk); a,c are destroyed and d
    /// is replaced by the solution. Internal so paneling (PANGEN) can reuse it.</summary>
    internal static void TriSol(double[] a, double[] b, double[] c, double[] d, int off, int kk)
    {
        for (int k = 1; k < kk; k++)
        {
            int km = k - 1;
            c[off + km] /= a[off + km];
            d[off + km] /= a[off + km];
            a[off + k] -= b[off + k] * c[off + km];
            d[off + k] -= b[off + k] * d[off + km];
        }
        d[off + kk - 1] /= a[off + kk - 1];
        for (int k = kk - 2; k >= 0; k--)
            d[off + k] -= c[off + k] * d[off + k + 1];
    }

    /// <summary>Port of spline.f SUBROUTINE SPLINE: dX/dS array for X(S) with the
    /// usual zero second-derivative end conditions.</summary>
    public static double[] SplineDeriv(double[] x, double[] s, int n)
        => Splind(x, s, n, 999.0, 999.0);

    /// <summary>Port of spline.f SUBROUTINE SPLIND. XS1/XS2 = 999 means "zero 2nd
    /// derivative", -999 means "zero 3rd derivative", any other value is a
    /// specified first-derivative end condition. Returns the dX/dS array.</summary>
    public static double[] Splind(double[] x, double[] s, int n, double xs1, double xs2)
    {
        var xs = new double[n];
        SplindInto(x, xs, s, 0, n, xs1, xs2);
        return xs;
    }

    // Core SPLIND working on the sub-range [off, off+n) of the given arrays.
    private static void SplindInto(double[] x, double[] xs, double[] s, int off, int n, double xs1, double xs2)
    {
        var a = new double[n];
        var b = new double[n];
        var c = new double[n];
        var d = new double[n]; // local RHS = dX/dS being solved for

        for (int i = 1; i < n - 1; i++)
        {
            double dsm = s[off + i] - s[off + i - 1];
            double dsp = s[off + i + 1] - s[off + i];
            b[i] = dsp;
            a[i] = 2.0 * (dsm + dsp);
            c[i] = dsm;
            d[i] = 3.0 * ((x[off + i + 1] - x[off + i]) * dsm / dsp
                        + (x[off + i] - x[off + i - 1]) * dsp / dsm);
        }

        if (xs1 == 999.0) { a[0] = 2.0; c[0] = 1.0; d[0] = 3.0 * (x[off + 1] - x[off]) / (s[off + 1] - s[off]); }
        else if (xs1 == -999.0) { a[0] = 1.0; c[0] = 1.0; d[0] = 2.0 * (x[off + 1] - x[off]) / (s[off + 1] - s[off]); }
        else { a[0] = 1.0; c[0] = 0.0; d[0] = xs1; }

        if (xs2 == 999.0) { b[n - 1] = 1.0; a[n - 1] = 2.0; d[n - 1] = 3.0 * (x[off + n - 1] - x[off + n - 2]) / (s[off + n - 1] - s[off + n - 2]); }
        else if (xs2 == -999.0) { b[n - 1] = 1.0; a[n - 1] = 1.0; d[n - 1] = 2.0 * (x[off + n - 1] - x[off + n - 2]) / (s[off + n - 1] - s[off + n - 2]); }
        else { a[n - 1] = 1.0; b[n - 1] = 0.0; d[n - 1] = xs2; }

        if (n == 2 && xs1 == -999.0 && xs2 == -999.0)
        {
            b[n - 1] = 1.0; a[n - 1] = 2.0;
            d[n - 1] = 3.0 * (x[off + n - 1] - x[off + n - 2]) / (s[off + n - 1] - s[off + n - 2]);
        }

        TriSol(a, b, c, d, 0, n);
        for (int i = 0; i < n; i++) xs[off + i] = d[i];
    }

    /// <summary>Port of spline.f SUBROUTINE SPLINA: non-oscillatory spline built by
    /// averaging adjacent segment slopes; end conditions from the end segment slope.
    /// Handles duplicated-S joints (DS == 0) as slope breaks.</summary>
    public static double[] Splina(double[] x, double[] s, int n)
    {
        var xs = new double[n];
        bool lend = true;
        double xs1 = 0.0, xs2 = 0.0;
        for (int i = 0; i < n - 1; i++)
        {
            double ds = s[i + 1] - s[i];
            if (ds == 0.0)
            {
                xs[i] = xs1;
                lend = true;
            }
            else
            {
                double dx = x[i + 1] - x[i];
                xs2 = dx / ds;
                if (lend) { xs[i] = xs2; lend = false; }
                else xs[i] = 0.5 * (xs1 + xs2);
            }
            xs1 = xs2;
        }
        xs[n - 1] = xs1;
        return xs;
    }

    /// <summary>Port of spline.f SUBROUTINE SEGSPL: like SPLINE but allows derivative
    /// discontinuities at segment joints (identical successive S values), splining
    /// each segment with -999/-999 end conditions.</summary>
    public static double[] Segspl(double[] x, double[] s, int n)
        => SegsplCore(x, s, n, -999.0, -999.0);

    /// <summary>Port of spline.f SUBROUTINE SEGSPLD: SEGSPL with caller-specified
    /// end conditions applied to each segment.</summary>
    public static double[] Segspld(double[] x, double[] s, int n, double xs1, double xs2)
        => SegsplCore(x, s, n, xs1, xs2);

    private static double[] SegsplCore(double[] x, double[] s, int n, double xs1, double xs2)
    {
        if (n == 1) return new double[1];
        if (s[0] == s[1]) throw new InvalidOperationException("SEGSPL: First input point duplicated");
        if (s[n - 1] == s[n - 2]) throw new InvalidOperationException("SEGSPL: Last input point duplicated");

        var xs = new double[n];
        int iseg0 = 0;
        for (int iseg = 1; iseg <= n - 3; iseg++)
        {
            if (s[iseg] == s[iseg + 1])
            {
                int nseg = iseg - iseg0 + 1;
                SplindInto(x, xs, s, iseg0, nseg, xs1, xs2);
                iseg0 = iseg + 1;
            }
        }
        SplindInto(x, xs, s, iseg0, n - iseg0, xs1, xs2);
        return xs;
    }

    // Binary search matching spline.f's SEVAL/DEVAL loop: returns the upper index i
    // of the bracketing interval [i-1, i].
    private static int FindInterval(double ss, double[] s, int n)
    {
        int ilow = 0;
        int i = n - 1;
        while (i - ilow > 1)
        {
            int imid = (i + ilow) / 2;
            if (ss < s[imid]) i = imid;
            else ilow = imid;
        }
        return i;
    }

    /// <summary>Port of spline.f FUNCTION SEVAL: X(ss).</summary>
    public static double Seval(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return t * x[i] + (1.0 - t) * x[i - 1] + (t - t * t) * ((1.0 - t) * cx1 - t * cx2);
    }

    /// <summary>Port of spline.f FUNCTION DEVAL: dX/dS(ss).</summary>
    public static double Deval(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return (x[i] - x[i - 1] + (1.0 - 4.0 * t + 3.0 * t * t) * cx1 + t * (3.0 * t - 2.0) * cx2) / ds;
    }

    /// <summary>Port of spline.f FUNCTION D2VAL: d2X/dS2(ss).</summary>
    public static double D2val(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return ((6.0 * t - 4.0) * cx1 + (6.0 * t - 2.0) * cx2) / (ds * ds);
    }

    /// <summary>Port of spline.f FUNCTION CURV: signed curvature of the splined 2-D
    /// curve (x(s), y(s)) at s = ss.</summary>
    public static double Curv(double ss, double[] x, double[] xs, double[] y, double[] ys, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;

        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        double xd = x[i] - x[i - 1] + (1.0 - 4.0 * t + 3.0 * t * t) * cx1 + t * (3.0 * t - 2.0) * cx2;
        double xdd = (6.0 * t - 4.0) * cx1 + (6.0 * t - 2.0) * cx2;

        double cy1 = ds * ys[i - 1] - y[i] + y[i - 1];
        double cy2 = ds * ys[i] - y[i] + y[i - 1];
        double yd = y[i] - y[i - 1] + (1.0 - 4.0 * t + 3.0 * t * t) * cy1 + t * (3.0 * t - 2.0) * cy2;
        double ydd = (6.0 * t - 4.0) * cy1 + (6.0 * t - 2.0) * cy2;

        double sd = Math.Sqrt(xd * xd + yd * yd);
        sd = Math.Max(sd, 0.001 * ds);
        return (xd * ydd - yd * xdd) / (sd * sd * sd);
    }

    /// <summary>Port of spline.f SUBROUTINE SINVRT: Newton-inverts S(X) near guess si.
    /// Returns the refined s such that Seval(s) == xi, or the input guess if it fails
    /// to converge in 10 iterations (matching the Fortran).</summary>
    public static double Sinvrt(double si, double xi, double[] x, double[] xs, double[] s, int n)
    {
        double sisav = si;
        for (int iter = 0; iter < 10; iter++)
        {
            double res = Seval(si, x, xs, s, n) - xi;
            double resp = Deval(si, x, xs, s, n);
            double ds = -res / resp;
            si += ds;
            if (Math.Abs(ds / (s[n - 1] - s[0])) < 1.0e-5) return si;
        }
        return sisav;
    }

    /// <summary>Port of spline.f SUBROUTINE SCALC: arc-length array for points (x,y).</summary>
    public static double[] Scalc(double[] x, double[] y, int n)
    {
        var s = new double[n];
        for (int i = 1; i < n; i++)
            s[i] = s[i - 1] + Math.Sqrt((x[i] - x[i - 1]) * (x[i] - x[i - 1]) + (y[i] - y[i - 1]) * (y[i] - y[i - 1]));
        return s;
    }
}
