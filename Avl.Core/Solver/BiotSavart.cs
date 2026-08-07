// Port of aic.f SUBROUTINE VORVELC and SUBROUTINE VVOR -- the Biot-Savart
// core-radius vortex-velocity kernel and the normalwash/velocity influence
// matrix assembly. Ported algebraically line-for-line per the numeric
// fidelity rules in the port plan: this is the single most load-bearing
// routine in the whole solver.
//
// Scope note: body source/doublet elements (VSRD/SRDSET/SRDVELC) are not
// ported -- none of the Milestone 1/2 fixtures (allegro.avl) define BODY blocks.
//
// Port of packages/avl-core/src/solver/biotSavart.ts.

namespace Avl.Core.Solver;

public struct Vec3
{
    public double U;
    public double V;
    public double W;

    public Vec3(double u, double v, double w) { U = u; V = v; W = w; }
}

public sealed class VortexGeom
{
    public double[] Rv1 = new double[3];
    public double[] Rv2 = new double[3];
    public int Component;
    public double Chord;
}

public sealed class EvalPoint
{
    public double[] R = new double[3];
    public int Component;
}

public static class BiotSavart
{
    private const double PI4INV = 0.079577472; // Fortran literal from aic.f DATA PI4INV, kept verbatim for parity

    /// <summary>Port of aic.f SUBROUTINE VORVELC: induced velocity of one finite-core
    /// horseshoe-vortex leg pair (Leishman R^4 core model) at point (x,y,z).</summary>
    public static Vec3 Vorvelc(
        double x, double y, double z,
        bool lbound,
        double x1, double y1, double z1,
        double x2, double y2, double z2,
        double beta, double rcore)
    {
        var a = new double[] { (x1 - x) / beta, y1 - y, z1 - z };
        var b = new double[] { (x2 - x) / beta, y2 - y, z2 - z };

        double asq = a[0] * a[0] + a[1] * a[1] + a[2] * a[2];
        double bsq = b[0] * b[0] + b[1] * b[1] + b[2] * b[2];
        double amag = Math.Sqrt(asq);
        double bmag = Math.Sqrt(bsq);

        double rcore2 = rcore * rcore;
        double rcore4 = rcore2 * rcore2;

        double u = 0;
        double v = 0;
        double w = 0;

        if (lbound && amag * bmag != 0.0)
        {
            var axb = new double[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0],
            };
            double axbsq = axb[0] * axb[0] + axb[1] * axb[1] + axb[2] * axb[2];
            if (axbsq != 0.0)
            {
                double adb = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
                double alsq = asq + bsq - 2.0 * adb;
                double t =
                    ((bsq - adb) / Math.Sqrt(Math.Sqrt(bsq * bsq + rcore4)) +
                     (asq - adb) / Math.Sqrt(Math.Sqrt(asq * asq + rcore4))) /
                    Math.Sqrt(axbsq * axbsq + alsq * alsq * rcore4);
                u = axb[0] * t;
                v = axb[1] * t;
                w = axb[2] * t;
            }
        }

        if (amag != 0.0)
        {
            double axisq = a[2] * a[2] + a[1] * a[1];
            double adx = a[0];
            double rsq = axisq;
            double t = -(1.0 - adx / amag) / Math.Sqrt(rsq * rsq + rcore4);
            v = v + a[2] * t;
            w = w - a[1] * t;
        }

        if (bmag != 0.0)
        {
            double bxisq = b[2] * b[2] + b[1] * b[1];
            double bdx = b[0];
            double rsq = bxisq;
            double t = (1.0 - bdx / bmag) / Math.Sqrt(rsq * rsq + rcore4);
            v = v + b[2] * t;
            w = w - b[1] * t;
        }

        return new Vec3((u * PI4INV) / beta, v * PI4INV, w * PI4INV);
    }

    /// <summary>
    /// Port of aic.f SUBROUTINE VVOR: velocity-influence matrix WC_GAM[i][j] =
    /// induced velocity at eval point i per unit circulation of vortex j,
    /// including symmetry-plane image vortices.
    /// </summary>
    public static Vec3[][] VvorMatrix(
        double betm,
        int iysym, double ysym,
        int izsym, double zsym,
        double vrcorec, double vrcorew,
        IReadOnlyList<VortexGeom> vortices,
        IReadOnlyList<EvalPoint> points,
        bool lvtest)
    {
        double fysym = iysym;
        double fzsym = izsym;
        int nv = vortices.Count;
        int nc = points.Count;
        bool sameCount = nc == nv;

        var wcGam = new Vec3[nc][];

        for (int i = 0; i < nc; i++)
        {
            double x = points[i].R[0], y = points[i].R[1], z = points[i].R[2];
            var row = new Vec3[nv];

            for (int j = 0; j < nv; j++)
            {
                var vx = vortices[j];
                double dsyz = Math.Sqrt((vx.Rv2[1] - vx.Rv1[1]) * (vx.Rv2[1] - vx.Rv1[1]) + (vx.Rv2[2] - vx.Rv1[2]) * (vx.Rv2[2] - vx.Rv1[2]));
                double rcore = 0.0001 * dsyz;
                if (sameCount && points[i].Component != vx.Component)
                {
                    rcore = Math.Max(vrcorec * vx.Chord, vrcorew * dsyz);
                }

                double yoff = 2.0 * ysym;
                double zoff = 2.0 * zsym;

                bool lboundReal = !(lvtest && i == j);
                var real = Vorvelc(x, y, z, lboundReal, vx.Rv1[0], vx.Rv1[1], vx.Rv1[2], vx.Rv2[0], vx.Rv2[1], vx.Rv2[2], betm, rcore);
                double u = real.U;
                double v = real.V;
                double w = real.W;

                double ui = 0, vi = 0, wi = 0;

                if (iysym != 0)
                {
                    bool lbound = true;
                    if (iysym == 1)
                    {
                        double xave = 0.5 * (vx.Rv1[0] + vx.Rv2[0]);
                        double yave = yoff - 0.5 * (vx.Rv1[1] + vx.Rv2[1]);
                        double zave = 0.5 * (vx.Rv1[2] + vx.Rv2[2]);
                        if (x == xave && y == yave && z == zave) lbound = false;
                    }
                    var im = Vorvelc(x, y, z, lbound, vx.Rv2[0], yoff - vx.Rv2[1], vx.Rv2[2], vx.Rv1[0], yoff - vx.Rv1[1], vx.Rv1[2], betm, rcore);
                    ui = im.U * fysym;
                    vi = im.V * fysym;
                    wi = im.W * fysym;
                }

                if (izsym != 0)
                {
                    var im = Vorvelc(x, y, z, true, vx.Rv2[0], vx.Rv2[1], zoff - vx.Rv2[2], vx.Rv1[0], vx.Rv1[1], zoff - vx.Rv1[2], betm, rcore);
                    u += im.U * fzsym;
                    v += im.V * fzsym;
                    w += im.W * fzsym;

                    if (iysym != 0)
                    {
                        var im2 = Vorvelc(
                            x, y, z, true,
                            vx.Rv1[0], yoff - vx.Rv1[1], zoff - vx.Rv1[2],
                            vx.Rv2[0], yoff - vx.Rv2[1], zoff - vx.Rv2[2],
                            betm, rcore);
                        ui += im2.U * fysym * fzsym;
                        vi += im2.V * fysym * fzsym;
                        wi += im2.W * fysym * fzsym;
                    }
                }

                row[j] = new Vec3(u + ui, v + vi, w + wi);
            }
            wcGam[i] = row;
        }

        return wcGam;
    }
}
