// Port of spline.f: SPLIND/TRISOL (spline-derivative solve), SEVAL/DEVAL/D2VAL
// (spline evaluation), SINVRT (spline inversion by Newton iteration), SCALC
// (arc-length parameterization), SEGSPL (segment-aware spline, reduces to a
// single SPLIND call when there are no duplicated-S segment joints, which is
// the case for the plain-loop airfoil .dat files handled here).
//
// Port of packages/avl-core/src/geometry/spline.ts (Float64Array -> double[]).

namespace Avl.Core.Model;

public static class Spline
{
    /// <summary>Port of spline.f SUBROUTINE TRISOL. Solves a KK-long tridiagonal system
    /// in place; a,c are destroyed, d is replaced by the solution.</summary>
    private static void TriSol(double[] a, double[] b, double[] c, double[] d, int kk)
    {
        for (int k = 1; k < kk; k++)
        {
            int km = k - 1;
            c[km] = c[km] / a[km];
            d[km] = d[km] / a[km];
            a[k] = a[k] - b[k] * c[km];
            d[k] = d[k] - b[k] * d[km];
        }
        d[kk - 1] = d[kk - 1] / a[kk - 1];
        for (int k = kk - 2; k >= 0; k--)
        {
            d[k] = d[k] - c[k] * d[k + 1];
        }
    }

    /// <summary>Port of spline.f SUBROUTINE SPLIND: spline derivative array XS for X(S),
    /// with XS1/XS2 = 999 meaning "zero 2nd derivative" and -999 meaning "zero
    /// 3rd derivative" end conditions (matching SEGSPL's usage).</summary>
    public static double[] Splind(double[] x, double[] s, int n, double xs1, double xs2)
    {
        var a = new double[n];
        var b = new double[n];
        var c = new double[n];
        var xs = new double[n];

        for (int i = 1; i < n - 1; i++)
        {
            double dsm = s[i] - s[i - 1];
            double dsp = s[i + 1] - s[i];
            b[i] = dsp;
            a[i] = 2.0 * (dsm + dsp);
            c[i] = dsm;
            xs[i] = 3.0 * (((x[i + 1] - x[i]) * dsm) / dsp + ((x[i] - x[i - 1]) * dsp) / dsm);
        }

        if (xs1 == 999.0)
        {
            a[0] = 2.0;
            c[0] = 1.0;
            xs[0] = (3.0 * (x[1] - x[0])) / (s[1] - s[0]);
        }
        else if (xs1 == -999.0)
        {
            a[0] = 1.0;
            c[0] = 1.0;
            xs[0] = (2.0 * (x[1] - x[0])) / (s[1] - s[0]);
        }
        else
        {
            a[0] = 1.0;
            c[0] = 0.0;
            xs[0] = xs1;
        }

        if (xs2 == 999.0)
        {
            b[n - 1] = 1.0;
            a[n - 1] = 2.0;
            xs[n - 1] = (3.0 * (x[n - 1] - x[n - 2])) / (s[n - 1] - s[n - 2]);
        }
        else if (xs2 == -999.0)
        {
            b[n - 1] = 1.0;
            a[n - 1] = 1.0;
            xs[n - 1] = (2.0 * (x[n - 1] - x[n - 2])) / (s[n - 1] - s[n - 2]);
        }
        else
        {
            a[n - 1] = 1.0;
            b[n - 1] = 0.0;
            xs[n - 1] = xs2;
        }

        if (n == 2 && xs1 == -999.0 && xs2 == -999.0)
        {
            b[n - 1] = 1.0;
            a[n - 1] = 2.0;
            xs[n - 1] = (3.0 * (x[n - 1] - x[n - 2])) / (s[n - 1] - s[n - 2]);
        }

        TriSol(a, b, c, xs, n);
        return xs;
    }

    /// <summary>Port of spline.f SUBROUTINE SEGSPL, specialized to arrays with no
    /// duplicated-S segment joints (true for the plain-loop .dat airfoil files
    /// this port reads).</summary>
    public static double[] Segspl(double[] x, double[] s, int n)
    {
        return Splind(x, s, n, -999.0, -999.0);
    }

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

    /// <summary>Port of spline.f FUNCTION SEVAL.</summary>
    public static double Seval(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return t * x[i] + (1.0 - t) * x[i - 1] + (t - t * t) * ((1.0 - t) * cx1 - t * cx2);
    }

    /// <summary>Port of spline.f FUNCTION DEVAL.</summary>
    public static double Deval(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return (x[i] - x[i - 1] + (1.0 - 4.0 * t + 3.0 * t * t) * cx1 + t * (3.0 * t - 2.0) * cx2) / ds;
    }

    /// <summary>Port of spline.f FUNCTION D2VAL.</summary>
    public static double D2val(double ss, double[] x, double[] xs, double[] s, int n)
    {
        int i = FindInterval(ss, s, n);
        double ds = s[i] - s[i - 1];
        double t = (ss - s[i - 1]) / ds;
        double cx1 = ds * xs[i - 1] - x[i] + x[i - 1];
        double cx2 = ds * xs[i] - x[i] + x[i - 1];
        return ((6.0 * t - 4.0) * cx1 + (6.0 * t - 2.0) * cx2) / (ds * ds);
    }

    /// <summary>Port of spline.f SUBROUTINE SINVRT: Newton-inverts S(X) near initial guess si.</summary>
    public static double Sinvrt(double si, double xi, double[] x, double[] xs, double[] s, int n)
    {
        double s0 = si;
        for (int iter = 0; iter < 10; iter++)
        {
            double res = Seval(s0, x, xs, s, n) - xi;
            double resp = Deval(s0, x, xs, s, n);
            double ds = -res / resp;
            s0 += ds;
            if (Math.Abs(ds / (s[n - 1] - s[0])) < 1.0e-5) return s0;
        }
        return s0;
    }

    /// <summary>Port of spline.f SUBROUTINE SCALC.</summary>
    public static double[] Scalc(double[] x, double[] y, int n)
    {
        var s = new double[n];
        for (int i = 1; i < n; i++)
        {
            s[i] = s[i - 1] + Math.Sqrt((x[i] - x[i - 1]) * (x[i] - x[i - 1]) + (y[i] - y[i - 1]) * (y[i] - y[i - 1]));
        }
        return s;
    }
}
