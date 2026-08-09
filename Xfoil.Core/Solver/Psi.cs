// Port of the inviscid core of xpanel.f SUBROUTINE PSILIN. PSILIN computes the
// streamfunction at a control point due to the airfoil's bound vorticity, and its
// linearization. For the inviscid solve we only need the dPsi/dGamma vector (DZDG)
// -- which builds the AIJ influence matrix -- and, for the sharp-TE tangency
// condition, dQtan/dGamma (DQDG). Those depend only on geometry, not on the
// (as-yet-unknown) circulation or on the source distribution, so this port omits
// the SIGLIN (viscous source), GEOLIN (geometric sensitivity), and LIMAGE (ground
// effect) branches. The full PSILIN will be needed later for the viscous coupling.
//
// Constants match xfoil.f INIT: QOPI = 1/(4*pi), HOPI = 1/(2*pi).

using Xfoil.Core.Geometry;

namespace Xfoil.Core.Solver;

public static class Psi
{
    private const double Pi = Math.PI;
    private const double Qopi = 0.25 / Math.PI;
    private const double Hopi = 0.50 / Math.PI;

    /// <summary>Reduced PSILIN. Fills dzdg (dPsi/dGamma) and dqdg (dQtan/dGamma), each
    /// length N, for a control point (xi,yi) with unit normal (nxi,nyi). `io` is the
    /// 1-based airfoil node number if the control point is a surface node, else 0
    /// (internal/off-body point) -- this selects the arctan reflection handling, as in
    /// the Fortran.</summary>
    public static void Psilin(PanelAirfoil g, int io, double xi, double yi, double nxi, double nyi,
                              double[] dzdg, double[] dqdg)
    {
        int n = g.N;
        double[] X = g.X, Y = g.Y, ap = g.Apanel;
        double seps = (g.S[n - 1] - g.S[0]) * 1.0e-5;
        double scs = g.Scs, sds = g.Sds;

        Array.Clear(dzdg, 0, n);
        Array.Clear(dqdg, 0, n);

        // Geometry of the last panel processed (the TE panel N->1) survives the loop
        // for the trailing-edge contribution below.
        double x1 = 0, x2 = 0, yy = 0, g1 = 0, g2 = 0, t1 = 0, t2 = 0, apan = 0;
        double x1i = 0, x2i = 0, yyi = 0;
        int joLast = -1, jpLast = -1;
        bool teNull = false;

        for (int jo = 0; jo < n; jo++)
        {
            int jp = (jo == n - 1) ? 0 : jo + 1;

            if (jo == n - 1)
            {
                double dd = (X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]);
                if (dd < seps * seps) { teNull = true; break; } // GO TO 12 (freestream only)
            }

            double dso = Math.Sqrt((X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]));
            if (dso == 0.0) continue; // skip null panel

            double dsio = 1.0 / dso;
            apan = ap[jo];

            double rx1 = xi - X[jo], ry1 = yi - Y[jo];
            double rx2 = xi - X[jp], ry2 = yi - Y[jp];
            double sx = (X[jp] - X[jo]) * dsio, sy = (Y[jp] - Y[jo]) * dsio;

            x1 = sx * rx1 + sy * ry1;
            x2 = sx * rx2 + sy * ry2;
            yy = sx * ry1 - sy * rx1;

            double rs1 = rx1 * rx1 + ry1 * ry1;
            double rs2 = rx2 * rx2 + ry2 * ry2;

            // reflection flag to keep arctan on the right branch off the surface
            double sgn = (io >= 1 && io <= n) ? 1.0 : (yy < 0.0 ? -1.0 : 1.0);

            int jo1 = jo + 1, jp1 = jp + 1; // 1-based node numbers (TE: jp=0 -> jp1=1)
            if (io != jo1 && rs1 > 0.0) { g1 = Math.Log(rs1); t1 = Math.Atan2(sgn * x1, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; }
            else { g1 = 0.0; t1 = 0.0; }
            if (io != jp1 && rs2 > 0.0) { g2 = Math.Log(rs2); t2 = Math.Atan2(sgn * x2, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; }
            else { g2 = 0.0; t2 = 0.0; }

            x1i = sx * nxi + sy * nyi;
            x2i = sx * nxi + sy * nyi;
            yyi = sx * nyi - sy * nxi;

            if (jo == n - 1) { joLast = jo; jpLast = jp; break; } // GO TO 11 (TE contribution)

            // ---- vortex panel contribution to dPsi/dGamma and dQtan/dGamma
            double dxinv = 1.0 / (x1 - x2);
            double psis = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);
            double psid = ((x1 + x2) * psis + 0.5 * (rs2 * g2 - rs1 * g1 + x1 * x1 - x2 * x2)) * dxinv;

            double psx1 = 0.5 * g1, psx2 = -0.5 * g2, psyy = t1 - t2;
            double pdx1 = ((x1 + x2) * psx1 + psis - x1 * g1 - psid) * dxinv;
            double pdx2 = ((x1 + x2) * psx2 + psis + x2 * g2 + psid) * dxinv;
            double pdyy = ((x1 + x2) * psyy - yy * (g1 - g2)) * dxinv;

            dzdg[jo] += Qopi * (psis - psid);
            dzdg[jp] += Qopi * (psis + psid);

            double psni = psx1 * x1i + psx2 * x2i + psyy * yyi;
            double pdni = pdx1 * x1i + pdx2 * x2i + pdyy * yyi;
            dqdg[jo] += Qopi * (psni - pdni);
            dqdg[jp] += Qopi * (psni + pdni);
        }

        if (teNull || joLast < 0) return; // no TE panel (sharp / null)

        // ---- trailing-edge panel contribution (label 11), using the TE panel geometry
        int jjo = joLast, jjp = jpLast;
        double psig = 0.5 * yy * (g1 - g2) + x2 * (t2 - apan) - x1 * (t1 - apan);
        double pgam = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);

        double psigx1 = -(t1 - apan), psigx2 = t2 - apan, psigyy = 0.5 * (g1 - g2);
        double pgamx1 = 0.5 * g1, pgamx2 = -0.5 * g2, pgamyy = t1 - t2;
        double psigni = psigx1 * x1i + psigx2 * x2i + psigyy * yyi;
        double pgamni = pgamx1 * x1i + pgamx2 * x2i + pgamyy * yyi;

        dzdg[jjo] += -Hopi * psig * scs * 0.5;
        dzdg[jjp] += Hopi * psig * scs * 0.5;
        dzdg[jjo] += Hopi * pgam * sds * 0.5;
        dzdg[jjp] += -Hopi * pgam * sds * 0.5;

        dqdg[jjo] += -Hopi * (psigni * 0.5 * scs - pgamni * 0.5 * sds);
        dqdg[jjp] += Hopi * (psigni * 0.5 * scs - pgamni * 0.5 * sds);
    }

    /// <summary>Full PSILIN streamfunction value: the streamfunction PSI at a control
    /// point (xi,yi) and its directional derivative PSI_NI along unit normal (nxi,nyi),
    /// due to the airfoil bound vorticity `gam` plus freestream. This is the inviscid
    /// (no source) form used by XYWAKE/QWCALC. `io` is the 1-based node index for a
    /// surface point, else 0. Returns (psi, psiNi).</summary>
    public static (double psi, double psiNi) StreamFn(PanelAirfoil g, double[] gam,
        double cosa, double sina, double qinf, int io, double xi, double yi, double nxi, double nyi)
    {
        int n = g.N;
        double[] X = g.X, Y = g.Y, ap = g.Apanel;
        double seps = (g.S[n - 1] - g.S[0]) * 1.0e-5;
        double scs = g.Scs, sds = g.Sds;
        double psi = 0.0, psiNi = 0.0;

        double x1 = 0, x2 = 0, yy = 0, g1 = 0, g2 = 0, t1 = 0, t2 = 0, apan = 0, x1i = 0, x2i = 0, yyi = 0;
        int joLast = -1, jpLast = -1;
        bool teNull = false;

        for (int jo = 0; jo < n; jo++)
        {
            int jp = (jo == n - 1) ? 0 : jo + 1;
            if (jo == n - 1)
            {
                double dd = (X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]);
                if (dd < seps * seps) { teNull = true; break; }
            }
            double dso = Math.Sqrt((X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]));
            if (dso == 0.0) continue;

            double dsio = 1.0 / dso;
            apan = ap[jo];
            double rx1 = xi - X[jo], ry1 = yi - Y[jo], rx2 = xi - X[jp], ry2 = yi - Y[jp];
            double sx = (X[jp] - X[jo]) * dsio, sy = (Y[jp] - Y[jo]) * dsio;
            x1 = sx * rx1 + sy * ry1; x2 = sx * rx2 + sy * ry2; yy = sx * ry1 - sy * rx1;
            double rs1 = rx1 * rx1 + ry1 * ry1, rs2 = rx2 * rx2 + ry2 * ry2;
            double sgn = (io >= 1 && io <= n) ? 1.0 : (yy < 0.0 ? -1.0 : 1.0);
            int jo1 = jo + 1, jp1 = jp + 1;
            if (io != jo1 && rs1 > 0.0) { g1 = Math.Log(rs1); t1 = Math.Atan2(sgn * x1, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; } else { g1 = 0.0; t1 = 0.0; }
            if (io != jp1 && rs2 > 0.0) { g2 = Math.Log(rs2); t2 = Math.Atan2(sgn * x2, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; } else { g2 = 0.0; t2 = 0.0; }
            x1i = sx * nxi + sy * nyi; x2i = sx * nxi + sy * nyi; yyi = sx * nyi - sy * nxi;

            if (jo == n - 1) { joLast = jo; jpLast = jp; break; }

            double dxinv = 1.0 / (x1 - x2);
            double psis = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);
            double psid = ((x1 + x2) * psis + 0.5 * (rs2 * g2 - rs1 * g1 + x1 * x1 - x2 * x2)) * dxinv;
            double psx1 = 0.5 * g1, psx2 = -0.5 * g2, psyy = t1 - t2;
            double pdx1 = ((x1 + x2) * psx1 + psis - x1 * g1 - psid) * dxinv;
            double pdx2 = ((x1 + x2) * psx2 + psis + x2 * g2 + psid) * dxinv;
            double pdyy = ((x1 + x2) * psyy - yy * (g1 - g2)) * dxinv;

            double gsum = gam[jp] + gam[jo], gdif = gam[jp] - gam[jo];
            psi += Qopi * (psis * gsum + psid * gdif);
            double psni = psx1 * x1i + psx2 * x2i + psyy * yyi;
            double pdni = pdx1 * x1i + pdx2 * x2i + pdyy * yyi;
            psiNi += Qopi * (gsum * psni + gdif * pdni);
        }

        if (!teNull && joLast >= 0)
        {
            int jjo = joLast, jjp = jpLast;
            double psig = 0.5 * yy * (g1 - g2) + x2 * (t2 - apan) - x1 * (t1 - apan);
            double pgam = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);
            double psigx1 = -(t1 - apan), psigx2 = t2 - apan, psigyy = 0.5 * (g1 - g2);
            double pgamx1 = 0.5 * g1, pgamx2 = -0.5 * g2, pgamyy = t1 - t2;
            double psigni = psigx1 * x1i + psigx2 * x2i + psigyy * yyi;
            double pgamni = pgamx1 * x1i + pgamx2 * x2i + pgamyy * yyi;
            double sigte = 0.5 * scs * (gam[jjp] - gam[jjo]);
            double gamte = -0.5 * sds * (gam[jjp] - gam[jjo]);
            psi += Hopi * (psig * sigte + pgam * gamte);
            psiNi += Hopi * (psigni * sigte + pgamni * gamte);
        }

        // freestream
        psi += qinf * (cosa * yi - sina * xi);
        psiNi += qinf * (cosa * nyi - sina * nxi);
        return (psi, psiNi);
    }

    /// <summary>Full PSILIN including the viscous source distribution (SIGLIN=true). Fills
    /// dzdg/dzdm (dPsi/dGamma, dPsi/dSig) and dqdg/dqdm (their normal-velocity analogues),
    /// each length n, and returns the streamfunction PSI (which depends on gam and sig)
    /// and its directional derivative. This is the airfoil-panel influence used by QDCALC
    /// to build the mass-defect->edge-velocity matrix DIJ. Geometric-sensitivity (GEOLIN)
    /// and ground-image (LIMAGE) terms are omitted. `io` is the 1-based node index for a
    /// surface control point, else 0.</summary>
    public static (double psi, double psiNi) PsilinFull(PanelAirfoil g, double[] gam, double[] sig,
        double cosa, double sina, double qinf, int io, double xi, double yi, double nxi, double nyi,
        double[] dzdg, double[] dzdm, double[] dqdg, double[] dqdm)
    {
        int n = g.N;
        double[] X = g.X, Y = g.Y, ap = g.Apanel;
        double seps = (g.S[n - 1] - g.S[0]) * 1.0e-5;
        double scs = g.Scs, sds = g.Sds;
        Array.Clear(dzdg, 0, n); Array.Clear(dzdm, 0, n);
        Array.Clear(dqdg, 0, n); Array.Clear(dqdm, 0, n);
        double psi = 0.0, psiNi = 0.0;

        double x1 = 0, x2 = 0, yy = 0, g1 = 0, g2 = 0, t1 = 0, t2 = 0, apan = 0, x1i = 0, x2i = 0, yyi = 0;
        int joLast = -1, jpLast = -1;
        bool teNull = false;

        for (int jo = 0; jo < n; jo++)
        {
            int jp = (jo == n - 1) ? 0 : jo + 1;
            int jm = (jo == 0) ? 0 : jo - 1;
            int jq = (jo == n - 2) ? jp : jp + 1;

            if (jo == n - 1)
            {
                double dd = (X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]);
                if (dd < seps * seps) { teNull = true; break; }
            }
            double dso = Math.Sqrt((X[jo] - X[jp]) * (X[jo] - X[jp]) + (Y[jo] - Y[jp]) * (Y[jo] - Y[jp]));
            if (dso == 0.0) continue;

            double dsio = 1.0 / dso;
            apan = ap[jo];
            double rx1 = xi - X[jo], ry1 = yi - Y[jo], rx2 = xi - X[jp], ry2 = yi - Y[jp];
            double sx = (X[jp] - X[jo]) * dsio, sy = (Y[jp] - Y[jo]) * dsio;
            x1 = sx * rx1 + sy * ry1; x2 = sx * rx2 + sy * ry2; yy = sx * ry1 - sy * rx1;
            double rs1 = rx1 * rx1 + ry1 * ry1, rs2 = rx2 * rx2 + ry2 * ry2;
            double sgn = (io >= 1 && io <= n) ? 1.0 : (yy < 0.0 ? -1.0 : 1.0);
            int jo1 = jo + 1, jp1 = jp + 1;
            if (io != jo1 && rs1 > 0.0) { g1 = Math.Log(rs1); t1 = Math.Atan2(sgn * x1, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; } else { g1 = 0.0; t1 = 0.0; }
            if (io != jp1 && rs2 > 0.0) { g2 = Math.Log(rs2); t2 = Math.Atan2(sgn * x2, sgn * yy) + (0.5 - 0.5 * sgn) * Pi; } else { g2 = 0.0; t2 = 0.0; }
            x1i = sx * nxi + sy * nyi; x2i = sx * nxi + sy * nyi; yyi = sx * nyi - sy * nxi;

            if (jo == n - 1) { joLast = jo; jpLast = jp; break; }

            // ---- source contribution (SIGLIN), split at the panel midpoint x0 ----
            {
                double x0 = 0.5 * (x1 + x2);
                double rs0 = x0 * x0 + yy * yy;
                double g0 = Math.Log(rs0);
                double t0 = Math.Atan2(sgn * x0, sgn * yy) + (0.5 - 0.5 * sgn) * Pi;

                // 1-0 half panel
                double dxinv = 1.0 / (x1 - x0);
                double psum = x0 * (t0 - apan) - x1 * (t1 - apan) + 0.5 * yy * (g1 - g0);
                double pdif = ((x1 + x0) * psum + rs1 * (t1 - apan) - rs0 * (t0 - apan) + (x0 - x1) * yy) * dxinv;
                double psx1 = -(t1 - apan), psx0 = t0 - apan, psyy = 0.5 * (g1 - g0);
                double pdx1 = ((x1 + x0) * psx1 + psum + 2.0 * x1 * (t1 - apan) - pdif) * dxinv;
                double pdx0 = ((x1 + x0) * psx0 + psum - 2.0 * x0 * (t0 - apan) + pdif) * dxinv;
                double pdyy = ((x1 + x0) * psyy + 2.0 * (x0 - x1 + yy * (t1 - t0))) * dxinv;

                double dsm = Math.Sqrt((X[jp] - X[jm]) * (X[jp] - X[jm]) + (Y[jp] - Y[jm]) * (Y[jp] - Y[jm]));
                double dsim = 1.0 / dsm;
                double ssum = (sig[jp] - sig[jo]) * dsio + (sig[jp] - sig[jm]) * dsim;
                double sdif = (sig[jp] - sig[jo]) * dsio - (sig[jp] - sig[jm]) * dsim;
                psi += Qopi * (psum * ssum + pdif * sdif);
                dzdm[jm] += Qopi * (-psum * dsim + pdif * dsim);
                dzdm[jo] += Qopi * (-psum * dsio - pdif * dsio);
                dzdm[jp] += Qopi * (psum * (dsio + dsim) + pdif * (dsio - dsim));
                double psni = psx1 * x1i + psx0 * (x1i + x2i) * 0.5 + psyy * yyi;
                double pdni = pdx1 * x1i + pdx0 * (x1i + x2i) * 0.5 + pdyy * yyi;
                psiNi += Qopi * (psni * ssum + pdni * sdif);
                dqdm[jm] += Qopi * (-psni * dsim + pdni * dsim);
                dqdm[jo] += Qopi * (-psni * dsio - pdni * dsio);
                dqdm[jp] += Qopi * (psni * (dsio + dsim) + pdni * (dsio - dsim));

                // 0-2 half panel
                dxinv = 1.0 / (x0 - x2);
                psum = x2 * (t2 - apan) - x0 * (t0 - apan) + 0.5 * yy * (g0 - g2);
                pdif = ((x0 + x2) * psum + rs0 * (t0 - apan) - rs2 * (t2 - apan) + (x2 - x0) * yy) * dxinv;
                psx0 = -(t0 - apan); double psx2 = t2 - apan; psyy = 0.5 * (g0 - g2);
                pdx0 = ((x0 + x2) * psx0 + psum + 2.0 * x0 * (t0 - apan) - pdif) * dxinv;
                double pdx2 = ((x0 + x2) * psx2 + psum - 2.0 * x2 * (t2 - apan) + pdif) * dxinv;
                pdyy = ((x0 + x2) * psyy + 2.0 * (x2 - x0 + yy * (t0 - t2))) * dxinv;

                double dsp = Math.Sqrt((X[jq] - X[jo]) * (X[jq] - X[jo]) + (Y[jq] - Y[jo]) * (Y[jq] - Y[jo]));
                double dsip = 1.0 / dsp;
                ssum = (sig[jq] - sig[jo]) * dsip + (sig[jp] - sig[jo]) * dsio;
                sdif = (sig[jq] - sig[jo]) * dsip - (sig[jp] - sig[jo]) * dsio;
                psi += Qopi * (psum * ssum + pdif * sdif);
                dzdm[jo] += Qopi * (-psum * (dsip + dsio) - pdif * (dsip - dsio));
                dzdm[jp] += Qopi * (psum * dsio - pdif * dsio);
                dzdm[jq] += Qopi * (psum * dsip + pdif * dsip);
                psni = psx0 * (x1i + x2i) * 0.5 + psx2 * x2i + psyy * yyi;
                pdni = pdx0 * (x1i + x2i) * 0.5 + pdx2 * x2i + pdyy * yyi;
                psiNi += Qopi * (psni * ssum + pdni * sdif);
                dqdm[jo] += Qopi * (-psni * (dsip + dsio) - pdni * (dsip - dsio));
                dqdm[jp] += Qopi * (psni * dsio - pdni * dsio);
                dqdm[jq] += Qopi * (psni * dsip + pdni * dsip);
            }

            // ---- vortex contribution ----
            {
                double dxinv = 1.0 / (x1 - x2);
                double psis = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);
                double psid = ((x1 + x2) * psis + 0.5 * (rs2 * g2 - rs1 * g1 + x1 * x1 - x2 * x2)) * dxinv;
                double psx1 = 0.5 * g1, psx2 = -0.5 * g2, psyy = t1 - t2;
                double pdx1 = ((x1 + x2) * psx1 + psis - x1 * g1 - psid) * dxinv;
                double pdx2 = ((x1 + x2) * psx2 + psis + x2 * g2 + psid) * dxinv;
                double pdyy = ((x1 + x2) * psyy - yy * (g1 - g2)) * dxinv;
                double gsum = gam[jp] + gam[jo], gdif = gam[jp] - gam[jo];
                psi += Qopi * (psis * gsum + psid * gdif);
                dzdg[jo] += Qopi * (psis - psid);
                dzdg[jp] += Qopi * (psis + psid);
                double psni = psx1 * x1i + psx2 * x2i + psyy * yyi;
                double pdni = pdx1 * x1i + pdx2 * x2i + pdyy * yyi;
                psiNi += Qopi * (gsum * psni + gdif * pdni);
                dqdg[jo] += Qopi * (psni - pdni);
                dqdg[jp] += Qopi * (psni + pdni);
            }
        }

        if (!teNull && joLast >= 0)
        {
            int jjo = joLast, jjp = jpLast;
            double psig = 0.5 * yy * (g1 - g2) + x2 * (t2 - apan) - x1 * (t1 - apan);
            double pgam = 0.5 * x1 * g1 - 0.5 * x2 * g2 + x2 - x1 + yy * (t1 - t2);
            double psigx1 = -(t1 - apan), psigx2 = t2 - apan, psigyy = 0.5 * (g1 - g2);
            double pgamx1 = 0.5 * g1, pgamx2 = -0.5 * g2, pgamyy = t1 - t2;
            double psigni = psigx1 * x1i + psigx2 * x2i + psigyy * yyi;
            double pgamni = pgamx1 * x1i + pgamx2 * x2i + pgamyy * yyi;
            double sigte = 0.5 * scs * (gam[jjp] - gam[jjo]);
            double gamte = -0.5 * sds * (gam[jjp] - gam[jjo]);
            psi += Hopi * (psig * sigte + pgam * gamte);
            dzdg[jjo] += -Hopi * psig * scs * 0.5 + Hopi * pgam * sds * 0.5;
            dzdg[jjp] += Hopi * psig * scs * 0.5 - Hopi * pgam * sds * 0.5;
            psiNi += Hopi * (psigni * sigte + pgamni * gamte);
            dqdg[jjo] += -Hopi * (psigni * 0.5 * scs - pgamni * 0.5 * sds);
            dqdg[jjp] += Hopi * (psigni * 0.5 * scs - pgamni * 0.5 * sds);
        }

        psi += qinf * (cosa * yi - sina * xi);
        psiNi += qinf * (cosa * nyi - sina * nxi);
        return (psi, psiNi);
    }
}
