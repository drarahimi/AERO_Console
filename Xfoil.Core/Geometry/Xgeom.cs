// Port of the airfoil-geometry helpers from XFOIL's xgeom.f that the paneling and
// solver need. LEFIND (leading-edge locator) is the piece PANGEN depends on;
// further xgeom.f routines (GEOPAR thickness/camber, etc.) can be added here as the
// port grows.

namespace Xfoil.Core.Geometry;

public static class Xgeom
{
    /// <summary>Port of xgeom.f SUBROUTINE LEFIND: locates the leading-edge spline
    /// parameter SLE, defined by the surface tangent being normal to the chord line
    /// from the point to the TE. Requires the splined arrays x,xp,y,yp over arc
    /// length s (length n).</summary>
    public static double Lefind(double[] x, double[] xp, double[] y, double[] yp, double[] s, int n)
    {
        double dseps = (s[n - 1] - s[0]) * 1.0e-5;

        double xte = 0.5 * (x[0] + x[n - 1]);
        double yte = 0.5 * (y[0] + y[n - 1]);

        // First guess: first node (from the nose region) whose forward segment points
        // back toward the TE (dot product with the chord vector goes negative).
        // Fortran loops I=3..N-2 (1-based); 0-based that is i=2..n-3.
        int i = 2;
        for (; i <= n - 3; i++)
        {
            double dxte = x[i] - xte;
            double dyte = y[i] - yte;
            double dx = x[i + 1] - x[i];
            double dy = y[i + 1] - y[i];
            if (dxte * dx + dyte * dy < 0.0) break;
        }
        if (i > n - 3) i = n - 3;

        double sle = s[i];

        // Sharp-LE case: a doubled node (identical successive s) -- return as-is.
        if (s[i] == s[i - 1]) return sle;

        for (int iter = 0; iter < 50; iter++)
        {
            double xle = Spline.Seval(sle, x, xp, s, n);
            double yle = Spline.Seval(sle, y, yp, s, n);
            double dxds = Spline.Deval(sle, x, xp, s, n);
            double dyds = Spline.Deval(sle, y, yp, s, n);
            double dxdd = Spline.D2val(sle, x, xp, s, n);
            double dydd = Spline.D2val(sle, y, yp, s, n);

            double xchord = xle - xte;
            double ychord = yle - yte;

            double res = xchord * dxds + ychord * dyds;
            double ress = dxds * dxds + dyds * dyds + xchord * dxdd + ychord * dydd;

            double dsle = -res / ress;
            double lim = 0.02 * Math.Abs(xchord + ychord);
            dsle = Math.Max(dsle, -lim);
            dsle = Math.Min(dsle, lim);
            sle += dsle;
            if (Math.Abs(dsle) < dseps) return sle;
        }
        return s[i]; // "LE point not found. Continuing..."
    }

    /// <summary>Port of xgeom.f TCCALC (via SOPPS): max thickness and camber of an
    /// airfoil given as a raw coordinate loop (x,y). Splines the loop internally, matching
    /// what xfoil.f's NACA/LOAD does before calling GEOPAR. Returns
    /// (thick, xthick, cambr, xcambr).</summary>
    public static (double thick, double xthick, double cambr, double xcambr) ThicknessCamber(double[] x, double[] y)
    {
        int n = x.Length;
        var s = Spline.Scalc(x, y, n);
        var xp = Spline.Segspl(x, s, n);
        var yp = Spline.Segspl(y, s, n);
        double sle = Lefind(x, xp, y, yp, s, n);

        double xle = Spline.Seval(sle, x, xp, s, n);
        double yle = Spline.Seval(sle, y, yp, s, n);
        double xte = 0.5 * (x[0] + x[n - 1]);
        double yte = 0.5 * (y[0] + y[n - 1]);
        double chord = Math.Sqrt((xte - xle) * (xte - xle) + (yte - yle) * (yte - yle));
        double dxc = (xte - xle) / chord, dyc = (yte - yle) / chord;

        double thick = 0, xthick = 0, cambr = 0, xcambr = 0;
        for (int i = 0; i < n; i++)
        {
            double ybar = (y[i] - yle) * dxc - (x[i] - xle) * dyc;
            double sopp = Sopps(s[i], x, xp, y, yp, s, n, sle);
            double xopp = Spline.Seval(sopp, x, xp, s, n);
            double yopp = Spline.Seval(sopp, y, yp, s, n);
            double ybarop = (yopp - yle) * dxc - (xopp - xle) * dyc;
            double yc = 0.5 * (ybar + ybarop);
            double yt = Math.Abs(ybar - ybarop);
            if (Math.Abs(yc) > Math.Abs(cambr)) { cambr = yc; xcambr = xopp; }
            if (Math.Abs(yt) > Math.Abs(thick)) { thick = yt; xthick = xopp; }
        }
        return (thick, xthick, cambr, xcambr);
    }

    // Port of xgeom.f SOPPS: arc length of the point opposite si across the chord line.
    private static double Sopps(double si, double[] x, double[] xp, double[] y, double[] yp, double[] s, int n, double sle)
    {
        double slen = s[n - 1] - s[0];
        double xle = Spline.Seval(sle, x, xp, s, n);
        double yle = Spline.Seval(sle, y, yp, s, n);
        double xte = 0.5 * (x[0] + x[n - 1]);
        double yte = 0.5 * (y[0] + y[n - 1]);
        double chord = Math.Sqrt((xte - xle) * (xte - xle) + (yte - yle) * (yte - yle));
        double dxc = (xte - xle) / chord, dyc = (yte - yle) / chord;

        int inn = si < sle ? 0 : n - 1;
        int inopp = si < sle ? n - 1 : 0;
        double sfrac = (si - sle) / (s[inn] - sle);
        double sopp = sle + sfrac * (s[inopp] - sle);
        if (Math.Abs(sfrac) <= 1.0e-5) return sle;

        double xi = Spline.Seval(si, x, xp, s, n);
        double yi = Spline.Seval(si, y, yp, s, n);
        double xbar = (xi - xle) * dxc + (yi - yle) * dyc;

        for (int iter = 0; iter < 12; iter++)
        {
            double xopp = Spline.Seval(sopp, x, xp, s, n);
            double yopp = Spline.Seval(sopp, y, yp, s, n);
            double xoppd = Spline.Deval(sopp, x, xp, s, n);
            double yoppd = Spline.Deval(sopp, y, yp, s, n);
            double res = (xopp - xle) * dxc + (yopp - yle) * dyc - xbar;
            double resd = xoppd * dxc + yoppd * dyc;
            if (Math.Abs(res) / slen < 1.0e-5) break;
            if (resd == 0.0) break;
            double dsopp = -res / resd;
            sopp += dsopp;
            if (Math.Abs(dsopp) / slen < 1.0e-5) break;
        }
        return sopp;
    }
}
