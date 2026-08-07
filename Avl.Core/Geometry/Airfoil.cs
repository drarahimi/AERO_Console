// Port of airutil.f SUBROUTINE GETCAM (and its helpers LEFIND/NORMIT):
// converts raw airfoil boundary coordinates (a single Selig-style loop from
// trailing edge, over one surface, through the leading edge, back along the
// other surface to the trailing edge) into a camberline (x, mean-line y,
// thickness) sampled at NC cosine-spaced x/c stations.
//
// Port of packages/avl-core/src/geometry/airfoil.ts.

namespace Avl.Core.Model;

public sealed class CamberResult
{
    public double[] Xc = Array.Empty<double>();
    public double[] Yc = Array.Empty<double>();
    public double[] Tc = Array.Empty<double>();
    public int N;
}

public static class AirfoilCamber
{
    /// <summary>Port of airutil.f SUBROUTINE LEFIND: Newton-solves for the arc-length
    /// parameter at the leading edge (the leftmost/minimum-x point).</summary>
    private static double Lefind(double[] x, double[] xp, double[] s, int n)
    {
        // Fortran: DO I=2,N; IF(X(I).GT.X(I-1)) GOTO 6; ENDDO; 6 SLE=S(I-1)
        int iF;
        for (iF = 2; iF <= n; iF++)
        {
            if (x[iF - 1] > x[iF - 2]) break;
        }
        double sle = s[iF - 2];

        double sref = s[n - 1] - s[0];
        for (int iter = 0; iter < 20; iter++)
        {
            double res = Spline.Deval(sle, x, xp, s, n);
            double resp = Spline.D2val(sle, x, xp, s, n);
            double dsle = -res / resp;
            sle += dsle;
            if (Math.Abs(dsle) / sref < 1.0e-5) return sle;
        }
        return sle;
    }

    /// <summary>Port of airutil.f SUBROUTINE NORMIT: normalizes x,y,s to unit chord with
    /// leading edge at x=0, in place. Returns the rescaled SLE.</summary>
    private static double Normit(double sleIn, double[] x, double[] xp, double[] y, double[] s, int n)
    {
        double xle = Spline.Seval(sleIn, x, xp, s, n);
        double xte = 0.5 * (x[0] + x[n - 1]);
        double dnorm = 1.0 / (xte - xle);
        for (int i = 0; i < n; i++)
        {
            x[i] = (x[i] - xle) * dnorm;
            y[i] = y[i] * dnorm;
            s[i] = s[i] * dnorm;
        }
        return sleIn * dnorm;
    }

    /// <summary>Port of airutil.f SUBROUTINE GETCAM. xb/yb is the raw boundary-coordinate
    /// loop (TE -> one surface -> LE -> other surface -> TE); nc is the desired
    /// camberline point count (defaults to 30 if &lt;=0, matching the Fortran).</summary>
    /// <param name="lnorm">true (airfoils) normalizes to unit chord with LE at x=0; false
    /// (bodies, GETCAM's LNORM=.FALSE.) keeps the raw coordinates so XBOD/YBOD/TBOD are
    /// the real body x-stations, centerline, and diameter.</param>
    public static CamberResult Getcam(IReadOnlyList<double> xbIn, IReadOnlyList<double> ybIn, int nb, int nc, bool lnorm = true)
    {
        var x = new double[nb];
        var y = new double[nb];
        for (int i = 0; i < nb; i++) { x[i] = xbIn[i]; y[i] = ybIn[i]; }
        var s = Spline.Scalc(x, y, nb);
        var xp = Spline.Segspl(x, s, nb);
        var yp = Spline.Segspl(y, s, nb);

        double sle = Lefind(x, xp, s, nb);
        if (lnorm) sle = Normit(sle, x, xp, y, s, nb);

        double xle = Spline.Seval(sle, x, xp, s, nb);
        double yle = Spline.Seval(sle, y, yp, s, nb);
        double xte = 0.5 * (x[0] + x[nb - 1]);

        int n = nc <= 0 ? 30 : nc;
        var xc = new double[n];
        var yc = new double[n];
        var tc = new double[n];
        xc[0] = xle;
        yc[0] = yle;
        tc[0] = 0.0;

        double su = sle - 0.01;
        double sl = sle + 0.01;
        double fnc1 = n - 1;
        for (int i = 2; i <= n; i++)
        {
            double xout = xle + (xte - xle) * 0.5 * (1.0 - Math.Cos((Math.PI * (i - 1)) / fnc1));
            su = Spline.Sinvrt(su, xout, x, xp, s, nb);
            double yu = Spline.Seval(su, y, yp, s, nb);
            sl = Spline.Sinvrt(sl, xout, x, xp, s, nb);
            double yl = Spline.Seval(sl, y, yp, s, nb);
            xc[i - 1] = xout;
            yc[i - 1] = 0.5 * (yu + yl);
            tc[i - 1] = yu - yl;
        }

        return new CamberResult { Xc = xc, Yc = yc, Tc = tc, N = n };
    }
}
