// Port of xfoil.f SUBROUTINE PANGEN (plus the TECALC/NCALC/APCALC finishing steps
// it calls): turns a buffer airfoil (XB,YB) into the paneled "current airfoil"
// (X,Y,S,N) that the panel solver runs on. The node distribution is chosen by a
// curvature-attraction Newton iteration -- points bunch where the surface curves
// (LE, TE) and where the user requests refinement -- exactly as XFOIL does.
//
// Faithful to the Fortran including the temporary IPFAC-times-finer node set, the
// curvature smoothing, and the TE panel-length fudging. The sharp-LE (doubled
// buffer node) branch is ported too, though NACA and ordinary .dat airfoils don't
// hit it. Arrays are 0-based; Fortran 1-based indices are noted where it aids review.

namespace Xfoil.Core.Geometry;

public static class Paneling
{
    private const int Ipfac = 3;      // temporary node oversampling factor
    private const double Rdste = 0.667; // TE/adjacent panel length ratio target

    /// <summary>Port of PANGEN. Returns the paneled current airfoil, or null if the
    /// buffer is degenerate (NB &lt; 2).</summary>
    public static PanelAirfoil? Pangen(double[] xb, double[] yb, string name, PanelingParams? prm = null)
    {
        prm ??= new PanelingParams();
        int nb = xb.Length;
        if (nb < 2) return null;

        int npan = prm.Npan;
        double cvpar = prm.CvPar, cterat = prm.CteRat, ctrrat = prm.CtrRat;

        // ---- spline the buffer airfoil over arc length
        var sb = Spline.Scalc(xb, yb, nb);
        var xbp = Spline.Segspl(xb, sb, nb);
        var ybp = Spline.Segspl(yb, sb, nb);

        double sbref = 0.5 * (sb[nb - 1] - sb[0]); // ~chord

        // ---- raw curvature array (W5), normalized by SBREF
        var cv = new double[nb];
        for (int i = 0; i < nb; i++)
            cv[i] = Math.Abs(Spline.Curv(sb[i], xb, xbp, yb, ybp, sb, nb)) * sbref;

        // ---- LE location and its curvature
        double sble = Xgeom.Lefind(xb, xbp, yb, ybp, sb, nb);
        double cvle = Math.Abs(Spline.Curv(sble, xb, xbp, yb, ybp, sb, nb)) * sbref;

        // ---- check for a doubled buffer point (sharp corner) at the LE.
        // ible = -1 means none; otherwise the 0-based index of the first of the pair.
        int ible = -1;
        for (int i = 0; i < nb - 1; i++)
            if (sble == sb[i] && sble == sb[i + 1]) { ible = i; break; }

        double xble = Spline.Seval(sble, xb, xbp, sb, nb);
        double yble = Spline.Seval(sble, yb, ybp, sb, nb);
        double xbte = 0.5 * (xb[0] + xb[nb - 1]);
        double ybte = 0.5 * (yb[0] + yb[nb - 1]);
        double chbsq = (xbte - xble) * (xbte - xble) + (ybte - yble) * (ybte - yble);

        // ---- average curvature over 2*NK+1 points within Rcurv of the LE
        const int nk = 3;
        double cvsum = 0.0;
        for (int k = -nk; k <= nk; k++)
        {
            double frac = (double)k / nk;
            double sbk = sble + frac * sbref / Math.Max(cvle, 20.0);
            cvsum += Math.Abs(Spline.Curv(sbk, xb, xbp, yb, ybp, sb, nb)) * sbref;
        }
        double cvavg = cvsum / (2 * nk + 1);
        if (ible != -1) cvavg = 10.0; // dummy curvature for sharp LE

        double cc = 6.0 * cvpar; // curvature attraction coefficient

        // ---- artificial curvature at the TE to bunch panels there
        double cvte = cvavg * cterat;
        cv[0] = cvte;
        cv[nb - 1] = cvte;

        // ==== smooth the curvature array (tri-diagonal solve) ====
        double smool = Math.Max(1.0 / Math.Max(cvavg, 20.0), 0.25 / (npan / 2));
        double smoosq = (smool * sbref) * (smool * sbref);

        var lo = new double[nb]; // W1 (lower diag)
        var md = new double[nb]; // W2 (main diag)
        var up = new double[nb]; // W3 (upper diag)

        md[0] = 1.0; up[0] = 0.0;
        for (int i = 1; i < nb - 1; i++)
        {
            double dsm = sb[i] - sb[i - 1];
            double dsp = sb[i + 1] - sb[i];
            double dso = 0.5 * (sb[i + 1] - sb[i - 1]);
            if (dsm == 0.0 || dsp == 0.0)
            {
                lo[i] = 0.0; md[i] = 1.0; up[i] = 0.0; // leave corner curvature unchanged
            }
            else
            {
                lo[i] = smoosq * (-1.0 / dsm) / dso;
                md[i] = smoosq * (1.0 / dsp + 1.0 / dsm) / dso + 1.0;
                up[i] = smoosq * (-1.0 / dsp) / dso;
            }
        }
        lo[nb - 1] = 0.0; md[nb - 1] = 1.0;

        // ---- pin curvature at the LE by modifying the equations adjacent to it
        for (int i = 1; i < nb - 1; i++)
        {
            if (sb[i] == sble || i == ible || i == ible + 1)
            {
                lo[i] = 0.0; md[i] = 1.0; up[i] = 0.0; cv[i] = cvle;
            }
            else if (sb[i - 1] < sble && sb[i] > sble)
            {
                // node just before the LE
                double dsm = sb[i - 1] - sb[i - 2];
                double dsp = sble - sb[i - 1];
                double dso = 0.5 * (sble - sb[i - 2]);
                lo[i - 1] = smoosq * (-1.0 / dsm) / dso;
                md[i - 1] = smoosq * (1.0 / dsp + 1.0 / dsm) / dso + 1.0;
                up[i - 1] = 0.0;
                cv[i - 1] += smoosq * cvle / (dsp * dso);

                // node just after the LE
                dsm = sb[i] - sble;
                dsp = sb[i + 1] - sb[i];
                dso = 0.5 * (sb[i + 1] - sble);
                lo[i] = 0.0;
                md[i] = smoosq * (1.0 / dsp + 1.0 / dsm) / dso + 1.0;
                up[i] = smoosq * (-1.0 / dsp) / dso;
                cv[i] += smoosq * cvle / (dsm * dso);
                break;
            }
        }

        // ---- artificial curvature at user refinement bunching points (off by default)
        for (int i = 1; i < nb - 1; i++)
        {
            double xoc = ((xb[i] - xble) * (xbte - xble) + (yb[i] - yble) * (ybte - yble)) / chbsq;
            if (sb[i] < sble)
            {
                if (xoc > prm.XsRef1 && xoc < prm.XsRef2)
                { lo[i] = 0.0; md[i] = 1.0; up[i] = 0.0; cv[i] = cvle * ctrrat; }
            }
            else
            {
                if (xoc > prm.XpRef1 && xoc < prm.XpRef2)
                { lo[i] = 0.0; md[i] = 1.0; up[i] = 0.0; cv[i] = cvle * ctrrat; }
            }
        }

        // ---- solve for the smoothed curvature array
        if (ible == -1)
            Spline.TriSol(md, lo, up, cv, 0, nb);
        else
        {
            Spline.TriSol(md, lo, up, cv, 0, ible + 1);
            Spline.TriSol(md, lo, up, cv, ible + 1, nb - (ible + 1));
        }

        // ---- normalize and spline the curvature array
        double cvmax = 0.0;
        for (int i = 0; i < nb; i++) cvmax = Math.Max(cvmax, Math.Abs(cv[i]));
        for (int i = 0; i < nb; i++) cv[i] /= cvmax;
        var cvs = Spline.Segspl(cv, sb, nb);

        // ==== node-position Newton iteration on an IPFAC-times-finer set ====
        int n = npan;
        int nn = Ipfac * (n - 1) + 1;
        double rtf = (Rdste - 1.0) * Ipfac + 1.0;

        var snew = new double[nn];
        int nn1 = 0; // sharp-LE only: index of the fixed LE node
        if (ible == -1)
        {
            double dsavg = (sb[nb - 1] - sb[0]) / ((nn - 3) + 2.0 * rtf);
            snew[0] = sb[0];
            for (int i = 1; i < nn - 1; i++) snew[i] = sb[0] + dsavg * ((i - 1) + rtf);
            snew[nn - 1] = sb[nb - 1];
        }
        else
        {
            int nfrac1 = (n * (ible + 1)) / nb;
            nn1 = Ipfac * (nfrac1 - 1) + 1;
            double dsavg1 = (sble - sb[0]) / ((nn1 - 2) + rtf);
            snew[0] = sb[0];
            for (int i = 1; i < nn1; i++) snew[i] = sb[0] + dsavg1 * ((i - 1) + rtf);
            int nn2 = nn - nn1 + 1;
            double dsavg2 = (sb[nb - 1] - sble) / ((nn2 - 2) + rtf);
            for (int i = 1; i < nn2 - 1; i++) snew[i + nn1 - 1] = sble + dsavg2 * ((i - 1) + rtf);
            snew[nn - 1] = sb[nb - 1];
        }

        var a = new double[nn]; // W1 lower
        var b = new double[nn]; // W2 main
        var c = new double[nn]; // W3 upper
        var r = new double[nn]; // W4 rhs / deltas

        for (int iter = 0; iter < 20; iter++)
        {
            double cv1 = Spline.Seval(snew[0], cv, cvs, sb, nb);
            double cv2 = Spline.Seval(snew[1], cv, cvs, sb, nb);
            double cvs1 = Spline.Deval(snew[0], cv, cvs, sb, nb);
            double cvs2 = Spline.Deval(snew[1], cv, cvs, sb, nb);

            double cavm = Math.Sqrt(cv1 * cv1 + cv2 * cv2);
            double cavmS1, cavmS2;
            if (cavm == 0.0) { cavmS1 = 0.0; cavmS2 = 0.0; }
            else { cavmS1 = cvs1 * cv1 / cavm; cavmS2 = cvs2 * cv2 / cavm; }

            for (int i = 1; i < nn - 1; i++)
            {
                double dsm = snew[i] - snew[i - 1];
                double dsp = snew[i] - snew[i + 1];
                double cv3 = Spline.Seval(snew[i + 1], cv, cvs, sb, nb);
                double cvs3 = Spline.Deval(snew[i + 1], cv, cvs, sb, nb);

                double cavp = Math.Sqrt(cv3 * cv3 + cv2 * cv2);
                double cavpS2, cavpS3;
                if (cavp == 0.0) { cavpS2 = 0.0; cavpS3 = 0.0; }
                else { cavpS2 = cvs2 * cv2 / cavp; cavpS3 = cvs3 * cv3 / cavp; }

                double fm = cc * cavm + 1.0;
                double fp = cc * cavp + 1.0;
                double rez = dsp * fp + dsm * fm;

                a[i] = -fm + cc * dsm * cavmS1;
                b[i] = fp + fm + cc * (dsp * cavpS2 + dsm * cavmS2);
                c[i] = -fp + cc * dsp * cavpS3;
                r[i] = -rez;

                cv1 = cv2; cv2 = cv3;
                cvs1 = cvs2; cvs2 = cvs3;
                cavm = cavp; cavmS1 = cavpS2; cavmS2 = cavpS3;
            }

            // fix endpoints (at TE)
            b[0] = 1.0; c[0] = 0.0; r[0] = 0.0;
            a[nn - 1] = 0.0; b[nn - 1] = 1.0; r[nn - 1] = 0.0;

            if (rtf != 1.0)
            {
                // fudge equations adjacent to the TE to hit the TE panel length ratio
                int i = 1;
                r[i] = -((snew[i] - snew[i - 1]) + rtf * (snew[i] - snew[i + 1]));
                a[i] = -1.0; b[i] = 1.0 + rtf; c[i] = -rtf;

                i = nn - 2;
                r[i] = -((snew[i] - snew[i + 1]) + rtf * (snew[i] - snew[i - 1]));
                c[i] = -1.0; b[i] = 1.0 + rtf; a[i] = -rtf;
            }

            if (ible != -1)
            {
                int i = nn1 - 1; // 0-based fixed LE node (Fortran NN1)
                a[i] = 0.0; b[i] = 1.0; c[i] = 0.0; r[i] = sble - snew[i];
            }

            Spline.TriSol(b, a, c, r, 0, nn);

            // under-relaxation to keep nodes from changing order
            double rlx = 1.0, dmax = 0.0;
            for (int i = 0; i < nn - 1; i++)
            {
                double ds = snew[i + 1] - snew[i];
                double dds = r[i + 1] - r[i];
                double dsrat = 1.0 + rlx * dds / ds;
                if (dsrat > 4.0) rlx = (4.0 - 1.0) * ds / dds;
                if (dsrat < 0.2) rlx = (0.2 - 1.0) * ds / dds;
                dmax = Math.Max(Math.Abs(r[i]), dmax);
            }
            for (int i = 1; i < nn - 1; i++) snew[i] += rlx * r[i];

            if (Math.Abs(dmax) < 1.0e-3) break;
        }

        // ---- extract the N panel nodes from every IPFAC-th temporary node
        var xl = new List<double>(n);
        var yl = new List<double>(n);
        var sl = new List<double>(n);
        for (int i = 0; i < n; i++)
        {
            int ind = Ipfac * i;
            sl.Add(snew[ind]);
            xl.Add(Spline.Seval(snew[ind], xb, xbp, sb, nb));
            yl.Add(Spline.Seval(snew[ind], yb, ybp, sb, nb));
        }

        // ---- insert nodes at any buffer corners (doubled buffer points)
        for (int ib = 0; ib < nb - 1; ib++)
        {
            if (sb[ib] != sb[ib + 1]) continue;
            double sbcorn = sb[ib];
            // find the first current node past the corner
            for (int i = 0; i < sl.Count; i++)
            {
                if (sl[i] <= sbcorn) continue;
                xl.Insert(i, xb[ib]);
                yl.Insert(i, yb[ib]);
                sl.Insert(i, sbcorn);
                // shift neighbours to keep panel sizes comparable
                if (i - 2 >= 0)
                {
                    sl[i - 1] = 0.5 * (sl[i] + sl[i - 2]);
                    xl[i - 1] = Spline.Seval(sl[i - 1], xb, xbp, sb, nb);
                    yl[i - 1] = Spline.Seval(sl[i - 1], yb, ybp, sb, nb);
                }
                if (i + 2 <= sl.Count - 1)
                {
                    sl[i + 1] = 0.5 * (sl[i] + sl[i + 2]);
                    xl[i + 1] = Spline.Seval(sl[i + 1], xb, xbp, sb, nb);
                    yl[i + 1] = Spline.Seval(sl[i + 1], yb, ybp, sb, nb);
                }
                break;
            }
        }

        var x = xl.ToArray();
        var y = yl.ToArray();
        n = x.Length;

        // ---- finalize: re-spline, locate LE/TE, TE geometry, normals, panel angles
        var s = Spline.Scalc(x, y, n);
        var xp = Spline.Segspl(x, s, n);
        var yp = Spline.Segspl(y, s, n);
        double sle = Xgeom.Lefind(x, xp, y, yp, s, n);
        double xle = Spline.Seval(sle, x, xp, s, n);
        double yle = Spline.Seval(sle, y, yp, s, n);
        double xte = 0.5 * (x[0] + x[n - 1]);
        double yte = 0.5 * (y[0] + y[n - 1]);
        double chord = Math.Sqrt((xte - xle) * (xte - xle) + (yte - yle) * (yte - yle));

        var (sharp, ante, aste, dste, scs, sds) = Tecalc(x, y, xp, yp, n, chord);
        var (nx, ny) = Ncalc(x, y, s, n);
        var apanel = Apcalc(x, y, n, nx, ny, sharp);

        return new PanelAirfoil
        {
            Name = name, N = n, X = x, Y = y, S = s, Xp = xp, Yp = yp,
            Sle = sle, Xle = xle, Yle = yle, Xte = xte, Yte = yte, Chord = chord,
            Nx = nx, Ny = ny, Apanel = apanel,
            Sharp = sharp, Ante = ante, Aste = aste, Dste = dste, Scs = scs, Sds = sds,
        };
    }

    /// <summary>Port of xfoil.f SUBROUTINE TECALC (geometry-only part): TE gap areas
    /// and the sharp/blunt determination. The GAM-dependent SIGTE/GAMTE strengths are
    /// computed later, after the flow solve.</summary>
    private static (bool sharp, double ante, double aste, double dste, double scs, double sds)
        Tecalc(double[] x, double[] y, double[] xp, double[] yp, int n, double chord)
    {
        double dxte = x[0] - x[n - 1];
        double dyte = y[0] - y[n - 1];
        double dxs = 0.5 * (-xp[0] + xp[n - 1]);
        double dys = 0.5 * (-yp[0] + yp[n - 1]);

        double ante = dxs * dyte - dys * dxte;
        double aste = dxs * dxte + dys * dyte;
        double dste = Math.Sqrt(dxte * dxte + dyte * dyte);

        bool sharp = dste < 0.0001 * chord;
        double scs, sds;
        if (sharp) { scs = 1.0; sds = 0.0; }
        else { scs = ante / dste; sds = aste / dste; }
        return (sharp, ante, aste, dste, scs, sds);
    }

    /// <summary>Port of xpanel.f SUBROUTINE NCALC: node normal unit vectors, averaged
    /// at corner points (duplicated-s nodes).</summary>
    private static (double[] nx, double[] ny) Ncalc(double[] x, double[] y, double[] s, int n)
    {
        var nx = Spline.Segspl(x, s, n); // reuse: dX/dS
        var ny = Spline.Segspl(y, s, n); // dY/dS
        for (int i = 0; i < n; i++)
        {
            double sx = ny[i];
            double sy = -nx[i];
            double smod = Math.Sqrt(sx * sx + sy * sy);
            nx[i] = sx / smod;
            ny[i] = sy / smod;
        }
        for (int i = 0; i < n - 1; i++)
        {
            if (s[i] == s[i + 1])
            {
                double sx = 0.5 * (nx[i] + nx[i + 1]);
                double sy = 0.5 * (ny[i] + ny[i + 1]);
                double smod = Math.Sqrt(sx * sx + sy * sy);
                nx[i] = sx / smod; ny[i] = sy / smod;
                nx[i + 1] = sx / smod; ny[i + 1] = sy / smod;
            }
        }
        return (nx, ny);
    }

    /// <summary>Port of xpanel.f SUBROUTINE APCALC: panel angles. Index n-1 is the TE
    /// panel (closing panel from node n back to node 1).</summary>
    private static double[] Apcalc(double[] x, double[] y, int n, double[] nx, double[] ny, bool sharp)
    {
        var apanel = new double[n];
        for (int i = 0; i < n - 1; i++)
        {
            double sx = x[i + 1] - x[i];
            double sy = y[i + 1] - y[i];
            apanel[i] = (sx == 0.0 && sy == 0.0)
                ? Math.Atan2(-ny[i], -nx[i])
                : Math.Atan2(sx, -sy);
        }
        if (sharp)
            apanel[n - 1] = Math.PI;
        else
        {
            double sx = x[0] - x[n - 1];
            double sy = y[0] - y[n - 1];
            apanel[n - 1] = Math.Atan2(-sx, sy) + Math.PI;
        }
        return apanel;
    }
}
