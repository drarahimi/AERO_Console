// Port of the body source/doublet aerodynamics: aic.f SRDSET (segment strengths),
// SRDVELC (finite-core source+doublet velocity kernel), VSRD (velocity-influence
// assembly at eval points), and aero.f BDFORC (body forces).
//
// Bodies are slender source+doublet lines whose strengths are set purely from the
// freestream/rotation (SRDSET), independent of the wing circulation. They influence
// the wing through the onset velocity they induce at its control points/vortices
// (VSRD -> RHS/VELSUM in Setup/SolveCase), and carry their own forces (BDFORC).
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using Avl.Core.Model;

namespace Avl.Core.Solver;

/// <summary>One body line segment with its 6 unit-flow source/doublet strengths (SRDSET).</summary>
public sealed class BodySegment
{
    public double[] Node1 = new double[3];
    public double[] Node2 = new double[3];
    public double Rad1;
    public double Rad2;
    public double[] SrcU = new double[6];        // source strength per unit flow component
    public double[][] DblU = { new double[6], new double[6], new double[6] }; // [k][iu] doublet
}

/// <summary>Body force/moment coefficients for one body (BDFORC).</summary>
public sealed class BodyForce
{
    public double Cd;
    public double Cy;
    public double Cl;
    public double[] Cf = new double[3];
    public double[] Cm = new double[3];
}

public static class BodyAero
{
    private const double PI = 3.14159265;
    private const double PI4INV = 0.079577472;
    private const double SRCORE = 1.0; // avl.f DEFINI default: core radius = 1.0 * body radius (RAVG)

    private static double[] Cross(double[] a, double[] b) => new double[]
    {
        a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0],
    };
    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

    /// <summary>Port of aic.f SRDSET: source+doublet strengths per unit flow component.</summary>
    public static List<BodySegment> SrdSet(double betm, double[] xyzref, int iysym, IReadOnlyList<Body> bodies)
    {
        var segs = new List<BodySegment>();
        foreach (var body in bodies)
        {
            int nl = body.Nodes.Count;
            if (nl < 2) continue;
            double blen = Math.Abs(body.Nodes[nl - 1][0] - body.Nodes[0][0]);
            double sdfac = (iysym == 1 && body.Nodes[0][1] <= 0.001 * blen) ? 0.5 : 1.0;

            for (int i = 0; i < nl - 1; i++)
            {
                var n1 = body.Nodes[i];
                var n2 = body.Nodes[i + 1];
                double r1 = body.Radii[i], r2 = body.Radii[i + 1];

                var drl = new double[] { (n2[0] - n1[0]) / betm, n2[1] - n1[1], n2[2] - n1[2] };
                double drlmag = Math.Sqrt(drl[0] * drl[0] + drl[1] * drl[1] + drl[2] * drl[2]);
                double drlmi = drlmag == 0 ? 0 : 1.0 / drlmag;
                var esl = new double[] { drl[0] * drlmi, drl[1] * drlmi, drl[2] * drlmi };

                double adel = PI * (r2 * r2 - r1 * r1) * sdfac;
                double aavg = PI * 0.5 * (r2 * r2 + r1 * r1) * sdfac;
                var rlref = new double[] { 0.5 * (n2[0] + n1[0]) - xyzref[0], 0.5 * (n2[1] + n1[1]) - xyzref[1], 0.5 * (n2[2] + n1[2]) - xyzref[2] };

                var seg = new BodySegment { Node1 = n1, Node2 = n2, Rad1 = r1, Rad2 = r2 };
                for (int iu = 0; iu < 6; iu++)
                {
                    var urel = new double[] { 0, 0, 0 };
                    if (iu < 3) urel[iu] = 1.0;
                    else
                    {
                        var wrot = new double[] { 0, 0, 0 };
                        wrot[iu - 3] = 1.0;
                        urel = Cross(rlref, wrot);
                    }
                    urel[0] = urel[0] / betm;
                    double us = Dot(urel, esl);
                    var un = new double[] { urel[0] - us * esl[0], urel[1] - us * esl[1], urel[2] - us * esl[2] };
                    seg.SrcU[iu] = adel * us;
                    seg.DblU[0][iu] = aavg * un[0] * drlmag * 2.0;
                    seg.DblU[1][iu] = aavg * un[1] * drlmag * 2.0;
                    seg.DblU[2][iu] = aavg * un[2] * drlmag * 2.0;
                }
                segs.Add(seg);
            }
        }
        return segs;
    }

    /// <summary>Port of aic.f SRDVELC: finite-core velocity of one source+doublet segment
    /// at (x,y,z). uvws = per-unit-source velocity; uvwd[k][j] = velocity k per unit doublet j.</summary>
    public static void SrdVelc(double x, double y, double z, double[] p1, double[] p2, double beta, double rcore,
        double[] uvws, double[][] uvwd)
    {
        var r1 = new double[] { (p1[0] - x) / beta, p1[1] - y, p1[2] - z };
        var r2 = new double[] { (p2[0] - x) / beta, p2[1] - y, p2[2] - z };
        double rcsq = rcore * rcore;
        double r1sq = r1[0] * r1[0] + r1[1] * r1[1] + r1[2] * r1[2];
        double r2sq = r2[0] * r2[0] + r2[1] * r2[1] + r2[2] * r2[2];
        double r1eps = Math.Sqrt(r1sq + rcsq);
        double r2eps = Math.Sqrt(r2sq + rcsq);
        double rdr = r1[0] * r2[0] + r1[1] * r2[1] + r1[2] * r2[2];
        var rxr = new double[] { r1[1] * r2[2] - r1[2] * r2[1], r1[2] * r2[0] - r1[0] * r2[2], r1[0] * r2[1] - r1[1] * r2[0] };
        double xdx = rxr[0] * rxr[0] + rxr[1] * rxr[1] + rxr[2] * rxr[2];
        double all = r1sq + r2sq - 2.0 * rdr;
        double den = rcsq * all + xdx;
        double ai1 = ((rdr + rcsq) / r1eps - r2eps) / den;
        double ai2 = ((rdr + rcsq) / r2eps - r1eps) / den;

        for (int k = 0; k < 3; k++)
        {
            uvws[k] = r1[k] * ai1 + r2[k] * ai2;
            double rr1 = (r1[k] + r2[k]) / r1eps - r1[k] * (rdr + rcsq) / (r1eps * r1eps * r1eps) - r2[k] / r2eps;
            double rr2 = (r1[k] + r2[k]) / r2eps - r2[k] * (rdr + rcsq) / (r2eps * r2eps * r2eps) - r1[k] / r1eps;
            double rrt = 2.0 * r1[k] * (r2sq - rdr) + 2.0 * r2[k] * (r1sq - rdr);
            double aj1 = (rr1 - ai1 * rrt) / den;
            double aj2 = (rr2 - ai2 * rrt) / den;
            for (int j = 0; j < 3; j++) uvwd[k][j] = -aj1 * r1[j] - aj2 * r2[j];
            uvwd[k][k] = uvwd[k][k] - ai1 - ai2;
        }

        uvws[0] = uvws[0] * PI4INV / beta;
        uvws[1] = uvws[1] * PI4INV;
        uvws[2] = uvws[2] * PI4INV;
        for (int l = 0; l < 3; l++)
        {
            uvwd[0][l] = uvwd[0][l] * PI4INV / beta;
            uvwd[1][l] = uvwd[1][l] * PI4INV;
            uvwd[2][l] = uvwd[2][l] * PI4INV;
        }
    }

    /// <summary>Port of aic.f VSRD: body-induced velocity per unit flow component at each
    /// eval point. Returns wcU[point][k][iu]. Includes IYSYM y-image.</summary>
    public static double[][][] Vsrd(double betm, int iysym, double ysym, int izsym, double zsym,
        IReadOnlyList<BodySegment> segs, IReadOnlyList<double[]> points)
    {
        double fysym = iysym, fzsym = izsym;
        double yoff = 2.0 * ysym, zoff = 2.0 * zsym;
        int nc = points.Count;
        var wc = new double[nc][][];
        for (int i = 0; i < nc; i++)
        {
            wc[i] = new double[3][];
            for (int k = 0; k < 3; k++) wc[i][k] = new double[6];
        }

        var uvws = new double[3];
        var uvwd = new[] { new double[3], new double[3], new double[3] };

        foreach (var seg in segs)
        {
            double ravg = Math.Sqrt(0.5 * (seg.Rad2 * seg.Rad2 + seg.Rad1 * seg.Rad1));
            double rlavg = Math.Sqrt(
                (seg.Node2[0] - seg.Node1[0]) * (seg.Node2[0] - seg.Node1[0]) +
                (seg.Node2[1] - seg.Node1[1]) * (seg.Node2[1] - seg.Node1[1]) +
                (seg.Node2[2] - seg.Node1[2]) * (seg.Node2[2] - seg.Node1[2]));
            double rcore = SRCORE > 0 ? SRCORE * ravg : SRCORE * rlavg;

            for (int i = 0; i < nc; i++)
            {
                var pt = points[i];
                void Accum(double[] p1, double[] p2, double sign)
                {
                    SrdVelc(pt[0], pt[1], pt[2], p1, p2, betm, rcore, uvws, uvwd);
                    for (int iu = 0; iu < 6; iu++)
                        for (int k = 0; k < 3; k++)
                            wc[i][k][iu] += sign * (uvws[k] * seg.SrcU[iu] + uvwd[k][0] * seg.DblU[0][iu] + uvwd[k][1] * seg.DblU[1][iu] + uvwd[k][2] * seg.DblU[2][iu]);
                }
                Accum(seg.Node1, seg.Node2, 1.0);
                if (iysym != 0)
                    Accum(new double[] { seg.Node1[0], yoff - seg.Node1[1], seg.Node1[2] },
                          new double[] { seg.Node2[0], yoff - seg.Node2[1], seg.Node2[2] }, fysym);
                if (izsym != 0)
                    Accum(new double[] { seg.Node1[0], seg.Node1[1], zoff - seg.Node1[2] },
                          new double[] { seg.Node2[0], seg.Node2[1], zoff - seg.Node2[2] }, fzsym);
            }
        }
        return wc;
    }

    /// <summary>Port of aero.f BDFORC: body force/moment coefficients for one body, from its
    /// source strengths and the local freestream+rotation velocity. Independent of the wing.</summary>
    public static BodyForce BdForc(Body body, IReadOnlyList<BodySegment> allSegs, int segOffset,
        double betm, double[] vinf, double[] wrot, double alfa, double sref, double cref, double bref, double[] xyzref)
    {
        double sina = Math.Sin(alfa), cosa = Math.Cos(alfa);
        var f = new BodyForce();
        int nseg = body.Nodes.Count - 1;
        var unit = new double[] { vinf[0], vinf[1], vinf[2], wrot[0], wrot[1], wrot[2] };

        for (int s = 0; s < nseg; s++)
        {
            var seg = allSegs[segOffset + s];
            var drl = new double[] { (seg.Node2[0] - seg.Node1[0]) / betm, seg.Node2[1] - seg.Node1[1], seg.Node2[2] - seg.Node1[2] };
            double drlmag = Math.Sqrt(drl[0] * drl[0] + drl[1] * drl[1] + drl[2] * drl[2]);
            double drlmi = drlmag == 0 ? 0 : 1.0 / drlmag;
            var esl = new double[] { drl[0] * drlmi, drl[1] * drlmi, drl[2] * drlmi };
            var rrot = new double[] { 0.5 * (seg.Node2[0] + seg.Node1[0]) - xyzref[0], 0.5 * (seg.Node2[1] + seg.Node1[1]) - xyzref[1], 0.5 * (seg.Node2[2] + seg.Node1[2]) - xyzref[2] };
            var vrot = Cross(rrot, wrot);
            var veff = new double[] { (vinf[0] + vrot[0]) / betm, vinf[1] + vrot[1], vinf[2] + vrot[2] };
            double us = veff[0] * esl[0] + veff[1] * esl[1] + veff[2] * esl[2];

            double src = 0;
            for (int iu = 0; iu < 6; iu++) src += seg.SrcU[iu] * unit[iu];

            var fb = new double[3];
            for (int k = 0; k < 3; k++) fb[k] = (veff[k] - us * esl[k]) * src;
            var mb = Cross(rrot, fb);

            f.Cd += (fb[0] * cosa + fb[2] * sina) * 2.0 / sref;
            f.Cy += fb[1] * 2.0 / sref;
            f.Cl += (-fb[0] * sina + fb[2] * cosa) * 2.0 / sref;
            for (int k = 0; k < 3; k++) f.Cf[k] += fb[k] * 2.0 / sref;
            f.Cm[0] += mb[0] * 2.0 / sref / bref;
            f.Cm[1] += mb[1] * 2.0 / sref / cref;
            f.Cm[2] += mb[2] * 2.0 / sref / bref;
        }
        return f;
    }
}
