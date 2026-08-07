// Port of sgutil.f SUBROUTINE SPACER and SUBROUTINE CSPACER.
// Ported algebraically line-for-line to preserve exact floating point behavior.
//
// Port of packages/avl-core/src/geometry/spacing.ts.

namespace Avl.Core.Model;

/// <summary>Chordwise spacing output from CSPACER.</summary>
public sealed class ChordSpacing
{
    /// <summary>Panel edge x/c fractions, length nvc+1 (XPT(1)=0, XPT(nvc+1)=1).</summary>
    public double[] Xpt = Array.Empty<double>();
    /// <summary>Vortex (bound-leg) x/c fraction per panel, length nvc.</summary>
    public double[] Xvr = Array.Empty<double>();
    /// <summary>Trailing (sense-point) x/c fraction per panel, length nvc.</summary>
    public double[] Xsr = Array.Empty<double>();
    /// <summary>Control-point x/c fraction per panel, length nvc.</summary>
    public double[] Xcp = Array.Empty<double>();
}

public static class Spacing
{
    private const double PI = 3.1415926535;

    /// <summary>
    /// Normalized (0..1) spacing array of N points, blending equal/cosine/sine
    /// distributions according to pspace in [-3, 3].
    /// </summary>
    public static double[] Spacer(int n, double pspace)
    {
        var x = new double[n];
        double pabs = Math.Abs(pspace);
        int nabs = (int)pabs + 1; // IFIX truncates toward zero

        double pequ, pcos, psin;
        if (nabs == 1)
        {
            pequ = 1 - pabs;
            pcos = pabs;
            psin = 0;
        }
        else if (nabs == 2)
        {
            pequ = 0;
            pcos = 2 - pabs;
            psin = pabs - 1;
        }
        else
        {
            // nabs === 3 or 4
            pequ = pabs - 2;
            pcos = 0;
            psin = 3 - pabs;
        }

        for (int k = 1; k <= n; k++)
        {
            double frac = (double)(k - 1) / (n - 1);
            double theta = frac * PI;
            if (pspace >= 0)
            {
                x[k - 1] =
                    pequ * frac +
                    (pcos * (1 - Math.Cos(theta))) / 2 +
                    psin * (1 - Math.Cos(theta / 2));
            }
            else
            {
                x[k - 1] =
                    pequ * frac +
                    (pcos * (1 - Math.Cos(theta))) / 2 +
                    psin * Math.Sin(theta / 2);
            }
        }
        return x;
    }

    /// <summary>Port of sgutil.f SUBROUTINE CSPACER.</summary>
    public static ChordSpacing Cspacer(int nvc, double cspace, double claf)
    {
        double acsp = Math.Abs(cspace);
        int ncsp = (int)acsp;

        double f0, f1, f2;
        if (ncsp == 0)
        {
            f0 = 1 - acsp;
            f1 = acsp;
            f2 = 0;
        }
        else if (ncsp == 1)
        {
            f0 = 0;
            f1 = 2 - acsp;
            f2 = acsp - 1;
        }
        else
        {
            f0 = acsp - 2;
            f1 = 0;
            f2 = 3 - acsp;
        }

        double dth1 = Math.PI / (4 * nvc + 2);
        double dth2 = (0.5 * Math.PI) / (4 * nvc + 1);
        double dxc0 = 1.0 / (4 * nvc);

        var xpt = new double[nvc + 1];
        var xvr = new double[nvc];
        var xsr = new double[nvc];
        var xcp = new double[nvc];

        for (int ivc = 1; ivc <= nvc; ivc++)
        {
            // uniform
            double xc0 = (4 * ivc - 4) * dxc0;
            double xpt0 = xc0;
            double xvr0 = xc0 + dxc0;
            double xsr0 = xc0 + 2.0 * dxc0;
            double xcp0 = xc0 + dxc0 + 2.0 * dxc0 * claf;

            // cosine
            double th1 = (4 * ivc - 3) * dth1;
            double xpt1 = 0.5 * (1.0 - Math.Cos(th1));
            double xvr1 = 0.5 * (1.0 - Math.Cos(th1 + dth1));
            double xsr1 = 0.5 * (1.0 - Math.Cos(th1 + 2.0 * dth1));
            double xcp1 = 0.5 * (1.0 - Math.Cos(th1 + dth1 + 2.0 * dth1 * claf));

            double xpt2, xvr2, xsr2, xcp2;
            if (cspace > 0.0)
            {
                // sine
                double th2 = (4 * ivc - 3) * dth2;
                xpt2 = 1.0 - Math.Cos(th2);
                xvr2 = 1.0 - Math.Cos(th2 + dth2);
                xsr2 = 1.0 - Math.Cos(th2 + 2.0 * dth2);
                xcp2 = 1.0 - Math.Cos(th2 + dth2 + 2.0 * dth2 * claf);
            }
            else
            {
                // -sine
                double th2 = (4 * ivc - 4) * dth2;
                xpt2 = Math.Sin(th2);
                xvr2 = Math.Sin(th2 + dth2);
                xsr2 = Math.Sin(th2 + 2.0 * dth2);
                xcp2 = Math.Sin(th2 + dth2 + 2.0 * dth2 * claf);
            }

            xpt[ivc - 1] = f0 * xpt0 + f1 * xpt1 + f2 * xpt2;
            xvr[ivc - 1] = f0 * xvr0 + f1 * xvr1 + f2 * xvr2;
            xsr[ivc - 1] = f0 * xsr0 + f1 * xsr1 + f2 * xsr2;
            xcp[ivc - 1] = f0 * xcp0 + f1 * xcp1 + f2 * xcp2;
        }
        xpt[0] = 0.0;
        xpt[nvc] = 1.0;

        return new ChordSpacing { Xpt = xpt, Xvr = xvr, Xsr = xsr, Xcp = xcp };
    }
}
