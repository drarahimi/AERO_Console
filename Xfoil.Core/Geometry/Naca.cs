// Port of XFOIL's naca.f: NACA4 and NACA5 buffer-airfoil generators. Produces the
// unpaneled "buffer" coordinates (XB, YB) that xfoil.f's NACA subroutine then
// splines and hands to PANGEN. Point counts and the TE-bunching parameter match
// XFOIL exactly (NSIDE = IQX/3 = 166 per side by default; see XFOIL.INC IQX=500).
//
// Coordinate ordering matches the Fortran: TE -> upper surface -> LE -> lower
// surface -> TE (a standard XFOIL/Selig-style closed loop).

namespace Xfoil.Core.Geometry;

/// <summary>Buffer airfoil produced by a NACA generator: a closed coordinate loop
/// plus the resolved airfoil name. NB is XB.Length.</summary>
public sealed record NacaAirfoil(double[] Xb, double[] Yb, string Name);

public static class Naca
{
    /// <summary>Points per side XFOIL uses (NACA subroutine: NSIDE = IQX/3, IQX=500).</summary>
    public const int DefaultNSide = 500 / 3; // 166

    // TE point bunching parameter (naca.f DATA AN / 1.5 /).
    private const double An = 1.5;

    /// <summary>Generates a buffer airfoil for a 4- or 5-digit NACA designation, or
    /// returns null if the designation isn't recognized (matching xfoil.f: ITYPE=0
    /// for anything above 25099, and NACA5's "illegal designation" cases).</summary>
    public static NacaAirfoil? Generate(int ides, int nside = DefaultNSide)
    {
        if (ides <= 9999) return Naca4(ides, nside);
        if (ides <= 25099) return Naca5(ides, nside);
        return null;
    }

    /// <summary>Port of naca.f SUBROUTINE NACA4.</summary>
    public static NacaAirfoil Naca4(int ides, int nside = DefaultNSide)
    {
        int n4 = ides / 1000;
        int n3 = (ides - n4 * 1000) / 100;
        int n2 = (ides - n4 * 1000 - n3 * 100) / 10;
        int n1 = ides - n4 * 1000 - n3 * 100 - n2 * 10;

        double m = n4 / 100.0;
        double p = n3 / 10.0;
        double t = (n2 * 10 + n1) / 100.0;

        // Camber line: parabolic ahead of P, straight-tapered behind. Matches naca.f
        // exactly -- the xi<P branch (the only one dividing by P**2) is unreachable when
        // P=0 since xi>=0, so a symmetric section (P=0, M=0) yields YC=0 with no divide
        // by zero, and the M>0/P=0 case reduces to the else branch just as in Fortran.
        var (xx, yt, yc) = BuildSide(nside, xi =>
            xi < p
                ? m / (p * p) * (2.0 * p * xi - xi * xi)
                : m / ((1.0 - p) * (1.0 - p)) * ((1.0 - 2.0 * p) + 2.0 * p * xi - xi * xi), t);

        var (xb, yb) = Loop(xx, yt, yc, nside);
        string name = "NACA " + Digit(n4) + Digit(n3) + Digit(n2) + Digit(n1);
        return new NacaAirfoil(xb, yb, name);
    }

    /// <summary>Port of naca.f SUBROUTINE NACA5. Only the standard 210..250 mean-line
    /// families are defined (as in the Fortran); anything else returns null.</summary>
    public static NacaAirfoil? Naca5(int ides, int nside = DefaultNSide)
    {
        int n5 = ides / 10000;
        int n4 = (ides - n5 * 10000) / 1000;
        int n3 = (ides - n5 * 10000 - n4 * 1000) / 100;
        int n2 = (ides - n5 * 10000 - n4 * 1000 - n3 * 100) / 10;
        int n1 = ides - n5 * 10000 - n4 * 1000 - n3 * 100 - n2 * 10;

        int n543 = 100 * n5 + 10 * n4 + n3;
        double m, c;
        switch (n543)
        {
            case 210: m = 0.0580; c = 361.4; break;
            case 220: m = 0.1260; c = 51.64; break;
            case 230: m = 0.2025; c = 15.957; break;
            case 240: m = 0.2900; c = 6.643; break;
            case 250: m = 0.3910; c = 3.230; break;
            default: return null; // "Illegal 5-digit designation"
        }

        double t = (n2 * 10 + n1) / 100.0;
        var (xx, yt, yc) = BuildSide(nside, xi =>
            xi < m
                ? (c / 6.0) * (xi * xi * xi - 3.0 * m * xi * xi + m * m * (3.0 - m) * xi)
                : (c / 6.0) * m * m * m * (1.0 - xi), t);

        var (xb, yb) = Loop(xx, yt, yc, nside);
        string name = "NACA " + Digit(n5) + Digit(n4) + Digit(n3) + Digit(n2) + Digit(n1);
        return new NacaAirfoil(xb, yb, name);
    }

    // Builds the per-side x distribution (with TE bunching), the half-thickness YT,
    // and the camber YC evaluated by the family-specific delegate.
    private static (double[] xx, double[] yt, double[] yc) BuildSide(int nside, Func<double, double> camber, double t)
    {
        double anp = An + 1.0;
        var xx = new double[nside];
        var yt = new double[nside];
        var yc = new double[nside];
        for (int i = 0; i < nside; i++)
        {
            double frac = (double)i / (nside - 1);
            xx[i] = i == nside - 1
                ? 1.0
                : 1.0 - anp * frac * Math.Pow(1.0 - frac, An) - Math.Pow(1.0 - frac, anp);
            double x = xx[i];
            yt[i] = (0.29690 * Math.Sqrt(x)
                   - 0.12600 * x
                   - 0.35160 * x * x
                   + 0.28430 * x * x * x
                   - 0.10150 * x * x * x * x) * t / 0.20;
            yc[i] = camber(x);
        }
        return (xx, yt, yc);
    }

    // Assembles the closed loop: upper surface from TE to LE, then lower from LE to TE
    // (naca.f loops 20 and 30). NB = 2*NSIDE - 1.
    private static (double[] xb, double[] yb) Loop(double[] xx, double[] yt, double[] yc, int nside)
    {
        var xb = new double[2 * nside - 1];
        var yb = new double[2 * nside - 1];
        int ib = 0;
        for (int i = nside - 1; i >= 0; i--)
        {
            xb[ib] = xx[i];
            yb[ib] = yc[i] + yt[i];
            ib++;
        }
        for (int i = 1; i < nside; i++)
        {
            xb[ib] = xx[i];
            yb[ib] = yc[i] - yt[i];
            ib++;
        }
        return (xb, yb);
    }

    private static char Digit(int d) => (char)('0' + d);
}
