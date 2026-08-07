// Port of sgutil.f SUBROUTINE AKIMA — the locally-fitted-cubic interpolator
// used to look up camberline slope/thickness at arbitrary x/c stations
// (distinct from spline.f's global SPLIND/SEVAL spline used inside GETCAM).
//
// Port of packages/avl-core/src/geometry/akima.ts.

namespace Avl.Core.Model;

public readonly record struct AkimaResult(double Yy, double Slp);

public static class Akima
{
    public static AkimaResult Compute(IReadOnlyList<double> x, IReadOnlyList<double> y, int n, double xx)
    {
        if (x[0] == x[n - 1])
        {
            return new AkimaResult(y[0], 0);
        }

        double xordr = x[0] > x[n - 1] ? -1.0 : 1.0;
        int ibot = 1;
        int itop = n;
        double xxo = xx * xordr;

        int i;
        for (; ; )
        {
            int nstep = (itop - ibot) / 2;
            i = ibot + nstep;
            double xoStep = x[i - 1] * xordr;
            if (xxo >= xoStep) ibot = i;
            if (xxo < xoStep) itop = i;
            if (nstep == 0) break;
        }

        // d[0..4] correspond to Fortran D(1..5); k = i + (j-2), j=1..5 (1-based i)
        var d = new double[5];
        for (int j = 1; j <= 5; j++)
        {
            int k = i + (j - 2);
            if (k - 1 >= 1 && k <= n)
            {
                d[j - 1] = (y[k - 1] - y[k - 2]) / (x[k - 1] - x[k - 2]);
            }
        }

        if (n == 2) d[1] = d[2];
        if (i + 2 > n) d[3] = 2.0 * d[2] - d[1];
        if (i + 3 > n) d[4] = 2.0 * d[3] - d[2];
        if (i - 1 < 1) d[1] = 2.0 * d[2] - d[3];
        if (i - 2 < 1) d[0] = 2.0 * d[1] - d[2];

        var t = new double[2];
        for (int j = 0; j < 2; j++)
        {
            double a = Math.Abs(d[j + 3] - d[j + 2]);
            double b = Math.Abs(d[j + 1] - d[j]);
            if (a + b == 0)
            {
                a = 1;
                b = 1;
            }
            t[j] = (a * d[j + 1] + b * d[j + 2]) / (a + b);
        }

        if (xx == x[i])
        {
            return new AkimaResult(y[i], t[1]);
        }

        double xint = x[i] - x[i - 1];
        double xdif = xx - x[i - 1];
        double p0 = y[i - 1];
        double p1 = t[0];
        double p2 = (3.0 * d[2] - 2.0 * t[0] - t[1]) / xint;
        double p3 = (t[0] + t[1] - 2.0 * d[2]) / (xint * xint);

        double yy = p0 + xdif * (p1 + xdif * (p2 + xdif * p3));
        double slp = p1 + xdif * (2.0 * p2 + xdif * 3.0 * p3);
        return new AkimaResult(yy, slp);
    }
}
