// Port of atpforc.f SUBROUTINE TPFORC and SUBROUTINE PGMAT (Trefftz-plane
// far-field drag/span-efficiency).
//
// Scope note: PGMAT is always called with alfa=beta=0, so the Prandtl-Glauert
// transform reduces to diag(1/sqrt(1-M^2), 1, 1); that simplification is baked
// in here. IYSYM/IZSYM image-plane contributions are not ported.
//
// Port of packages/avl-core/src/solver/trefftz.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class TrefftzResult
{
    public double Clff;
    public double Cyff;
    public double Cdff;
    public double Spanef;
    /// <summary>Per-strip Trefftz-plane wake downwash (atpforc.f's DWWAKE), for the FS command.</summary>
    public double[] Dwwake = Array.Empty<double>();
}

public static class Trefftz
{
    private const double VRCOREC = 0.0; // vortex core radius fraction of strip chord
    private const double VRCOREW = 2.0; // vortex core radius fraction of strip width

    public static TrefftzResult ComputeTrefftz(Geometry geo, double[] gam, double mach)
    {
        double hpi = 1.0 / (2.0 * Math.PI);
        // double binv = 1.0 / Math.Sqrt(1.0 - mach * mach); // only affects x-coordinate, unused below

        int nstrip = geo.Strips.Count;
        var gams = new double[nstrip];
        for (int j = 0; j < nstrip; j++)
        {
            var strip = geo.Strips[j];
            double sum = 0;
            for (int ii = 0; ii < strip.Nvc; ii++) sum += gam[strip.FirstVortex + ii];
            gams[j] = sum;
        }

        var rt1y = new double[nstrip];
        var rt1z = new double[nstrip];
        var rt2y = new double[nstrip];
        var rt2z = new double[nstrip];
        var rtcy = new double[nstrip];
        var rtcz = new double[nstrip];
        for (int j = 0; j < nstrip; j++)
        {
            var strip = geo.Strips[j];
            int ic = strip.FirstVortex + strip.Nvc - 1;
            var v = geo.Vortices[ic];
            rt1y[j] = v.Rv1[1];
            rt1z[j] = v.Rv1[2];
            rt2y[j] = v.Rv2[1];
            rt2z[j] = v.Rv2[2];
            rtcy[j] = v.Rc[1];
            rtcz[j] = v.Rc[2];
        }

        double clff = 0, cyff = 0, cdff = 0;
        var dwwake = new double[nstrip];

        for (int jc = 0; jc < nstrip; jc++)
        {
            double dyt = rt2y[jc] - rt1y[jc];
            double dzt = rt2z[jc] - rt1z[jc];
            double ycntr = rtcy[jc];
            double zcntr = rtcz[jc];
            double dst = Math.Sqrt(dyt * dyt + dzt * dzt);
            double ny = -dzt / dst;
            double nz = dyt / dst;

            int compJc = geo.Surfaces[geo.Strips[jc].SurfaceIndex].Component;

            double vy = 0, vz = 0;
            for (int jv = 0; jv < nstrip; jv++)
            {
                int compJv = geo.Surfaces[geo.Strips[jv].SurfaceIndex].Component;
                double dsyz = Math.Sqrt((rt2y[jv] - rt1y[jv]) * (rt2y[jv] - rt1y[jv]) + (rt2z[jv] - rt1z[jv]) * (rt2z[jv] - rt1z[jv]));
                double rcore = compJc == compJv ? 0 : Math.Max(VRCOREC * geo.Strips[jv].Chord, VRCOREW * dsyz);
                double rcore4 = rcore * rcore * rcore * rcore;

                double dy1 = ycntr - rt1y[jv];
                double dy2 = ycntr - rt2y[jv];
                double dz1 = zcntr - rt1z[jv];
                double dz2 = zcntr - rt2z[jv];
                double rsq1 = Math.Sqrt((dy1 * dy1 + dz1 * dz1) * (dy1 * dy1 + dz1 * dz1) + rcore4);
                double rsq2 = Math.Sqrt((dy2 * dy2 + dz2 * dz2) * (dy2 * dy2 + dz2 * dz2) + rcore4);
                vy += hpi * gams[jv] * (dz1 / rsq1 - dz2 / rsq2);
                vz += hpi * gams[jv] * (-dy1 / rsq1 + dy2 / rsq2);
            }

            dwwake[jc] = -(ny * vy + nz * vz);

            int s = geo.Strips[jc].SurfaceIndex;
            if (!geo.Surfaces[s].NoLoad)
            {
                clff += (2.0 * gams[jc] * dyt) / geo.Sref;
                cyff -= (2.0 * gams[jc] * dzt) / geo.Sref;
                cdff += (gams[jc] * (dzt * vy - dyt * vz)) / geo.Sref;
            }
        }

        double ar = geo.Bref * geo.Bref / geo.Sref;
        double spanef = 0;
        if (cdff != 0) spanef = (clff * clff + cyff * cyff) / (Math.PI * ar * cdff);

        return new TrefftzResult { Clff = clff, Cyff = cyff, Cdff = cdff, Spanef = spanef, Dwwake = dwwake };
    }
}
