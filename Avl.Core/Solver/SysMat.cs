// Port of amode.f SUBROUTINE SYSMAT (+ ROTENS3/RATEKI3 from autil.f): assembles the
// 12x12 dimensional rigid-body dynamics matrix ASYS for eigenmode (MODE) analysis,
// linearized about a trimmed flight state. Eigenvalues come from Eigen (EISPACK RG stand-in).
//
// State order (AINDEX.INC): u, w, q, theta, v, p, r, phi, x, y, z, psi.
// Apparent-mass terms (AMASS/AINER) are taken as zero (only nonzero with bodies +
// amass.f's added-mass computation, which AVL itself defaults off for line bodies).
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using System.Numerics;

namespace Avl.Core.Solver;

/// <summary>Trimmed flight state + mass properties needed to build the dynamics matrix.
/// Forces/derivatives are body-axis; velocities/rates are the solver's VINF/WROT.</summary>
public sealed class ModeInputs
{
    public double Sref, Cref, Bref;
    public double Vee;   // flight speed (real units)
    public double Rho;   // air density
    public double Gee;   // gravity
    public double Unitl; // length unit (Lunit)
    public double Mass;  // real-units mass
    public double[,] Inertia = new double[3, 3]; // real-units inertia tensor
    public double AlfaRad;
    public double PhiDeg, TheDeg, PsiDeg;
    public double[] Vinf = { 1, 0, 0 };
    public double[] Wrot = { 0, 0, 0 };
    public double[] Cftot = new double[3];
    public double[] Cmtot = new double[3];
    public double[][] CftotU = { new double[6], new double[6], new double[6] }; // [k][iu]
    public double[][] CmtotU = { new double[6], new double[6], new double[6] };
    public double DclU, DcmU, DclA, DcmA; // viscous/Mach add-on derivatives (usually 0)
    /// <summary>Apparent (added) mass/inertia tensors from the lifting surfaces (amass.f APPGET),
    /// RAW (not yet x RHO). MAMAT += Amass*RHO, RIMAT += Ainer*RHO.</summary>
    public double[,] Amass = new double[3, 3];
    public double[,] Ainer = new double[3, 3];
}

public static class SysMat
{
    private const double DTR = Math.PI / 180.0;
    // 0-based state indices matching AINDEX.INC.
    private const int JEU = 0, JEW = 1, JEQ = 2, JETH = 3, JEV = 4, JEP = 5, JER = 6, JEPH = 7, JEX = 8, JEY = 9, JEZ = 10, JEPS = 11;
    private static readonly int[] ICRS = { 1, 2, 0 }; // Fortran {2,3,1} 1-based
    private static readonly int[] JCRS = { 2, 0, 1 }; // Fortran {3,1,2}

    /// <summary>Build the 12x12 system matrix (row-major) from a trimmed flight state.</summary>
    public static double[] Build(ModeInputs m)
    {
        double sina = Math.Sin(m.AlfaRad), cosa = Math.Cos(m.AlfaRad);
        var vinf = m.Vinf; var wrot = m.Wrot;

        double srefd = m.Sref * m.Unitl * m.Unitl;
        double brefd = m.Bref * m.Unitl;
        double crefd = m.Cref * m.Unitl;
        double qs = 0.5 * m.Rho * m.Vee * m.Vee * srefd;
        double rot = m.Vee / m.Unitl;

        // Mass & inertia tensors including apparent (added) mass: MAMAT = m*I + Amass*rho,
        // RIMAT = inertia + Ainer*rho (amass.f). Apparent mass is significant for lifting
        // surfaces in heave/pitch/roll and is what makes those modes' damping correct.
        var mamat = new double[3, 3];
        var rimat = new double[3, 3];
        for (int k = 0; k < 3; k++)
        {
            for (int j = 0; j < 3; j++)
            {
                mamat[k, j] = m.Amass[k, j] * m.Rho + (k == j ? m.Mass : 0.0);
                rimat[k, j] = m.Inertia[k, j] + m.Ainer[k, j] * m.Rho;
            }
        }
        var mainv = Inv3(mamat);
        var riinv = Inv3(rimat);

        // Linear/angular momentum P = -m V, H = I W (AVL axes).
        var p = new double[3]; var pU = new double[3][];
        var h = new double[3]; var hU = new double[3][];
        for (int k = 0; k < 3; k++)
        {
            p[k] = -(mamat[k, 0] * vinf[0] + mamat[k, 1] * vinf[1] + mamat[k, 2] * vinf[2]) * m.Vee;
            pU[k] = new double[] { -mamat[k, 0] * m.Vee, -mamat[k, 1] * m.Vee, -mamat[k, 2] * m.Vee, 0, 0, 0 };
            h[k] = (rimat[k, 0] * wrot[0] + rimat[k, 1] * wrot[1] + rimat[k, 2] * wrot[2]) * rot;
            hU[k] = new double[] { 0, 0, 0, rimat[k, 0] * rot, rimat[k, 1] * rot, rimat[k, 2] * rot };
        }

        // WxP, WxH and their unit-derivatives.
        var wxp = new double[3]; var wxpU = new double[3][];
        var wxh = new double[3]; var wxhU = new double[3][];
        for (int k = 0; k < 3; k++)
        {
            int i = ICRS[k], j = JCRS[k];
            wxp[k] = (wrot[i] * p[j] - wrot[j] * p[i]) * rot;
            wxh[k] = (wrot[i] * h[j] - wrot[j] * h[i]) * rot;
            wxpU[k] = new double[6];
            wxhU[k] = new double[6];
            for (int iu = 0; iu < 3; iu++)
                wxpU[k][iu] = (wrot[i] * pU[j][iu] - wrot[j] * pU[i][iu]) * rot;
            wxpU[k][i + 3] += p[j] * rot;
            wxpU[k][j + 3] -= p[i] * rot;
            for (int iu = 3; iu < 6; iu++)
                wxhU[k][iu] = (wrot[i] * hU[j][iu] - wrot[j] * hU[i][iu]) * rot;
            wxhU[k][i + 3] += h[j] * rot;
            wxhU[k][j + 3] -= h[i] * rot;
        }

        // m^-1 F, I^-1 M, m^-1(WxP), I^-1(WxH) and their derivatives.
        var mif = new double[3]; var rim = new double[3]; var prf = new double[3]; var prm = new double[3];
        var mifU = new double[3][]; var rimU = new double[3][]; var prfU = new double[3][]; var prmU = new double[3][];
        for (int k = 0; k < 3; k++)
        {
            mif[k] = (mainv[k, 0] * m.Cftot[0] + mainv[k, 1] * m.Cftot[1] + mainv[k, 2] * m.Cftot[2]) * qs;
            rim[k] = riinv[k, 0] * m.Cmtot[0] * qs * brefd + riinv[k, 1] * m.Cmtot[1] * qs * crefd + riinv[k, 2] * m.Cmtot[2] * qs * brefd;
            prf[k] = mainv[k, 0] * wxp[0] + mainv[k, 1] * wxp[1] + mainv[k, 2] * wxp[2];
            prm[k] = riinv[k, 0] * wxh[0] + riinv[k, 1] * wxh[1] + riinv[k, 2] * wxh[2];
            mifU[k] = new double[6]; rimU[k] = new double[6]; prfU[k] = new double[6]; prmU[k] = new double[6];
            for (int iu = 0; iu < 6; iu++)
            {
                mifU[k][iu] = (mainv[k, 0] * m.CftotU[0][iu] + mainv[k, 1] * m.CftotU[1][iu] + mainv[k, 2] * m.CftotU[2][iu]) * qs;
                rimU[k][iu] = riinv[k, 0] * m.CmtotU[0][iu] * qs * brefd + riinv[k, 1] * m.CmtotU[1][iu] * qs * crefd + riinv[k, 2] * m.CmtotU[2][iu] * qs * brefd;
                prfU[k][iu] = mainv[k, 0] * wxpU[0][iu] + mainv[k, 1] * wxpU[1][iu] + mainv[k, 2] * wxpU[2][iu];
                prmU[k][iu] = riinv[k, 0] * wxhU[0][iu] + riinv[k, 1] * wxhU[1][iu] + riinv[k, 2] * wxhU[2][iu];
            }
            // Additional viscous/Mach derivatives (CL_u, CM_u at iu=1; CL_a, CM_a at iu=3).
            mifU[k][0] -= mainv[k, 2] * m.DclU * qs;
            rimU[k][0] -= riinv[k, 1] * m.DcmU * qs * crefd;
            mifU[k][2] += mainv[k, 2] * m.DclA * qs;
            rimU[k][2] += riinv[k, 1] * m.DcmA * qs * crefd;
        }

        var ang = new double[] { m.PhiDeg * DTR, m.TheDeg * DTR, m.PsiDeg * DTR };
        Rotens3(ang, out var tt, out var ttAng);
        Rateki3(ang, out var rt, out var rtAng);

        var a = new double[12, 12];
        double vee = m.Vee, gee = m.Gee;

        // Force rows (x/y/z acceleration): IEQ = JEU/JEV/JEW, K = 0/1/2.
        int[] accRows = { JEU, JEV, JEW };
        for (int k = 0; k < 3; k++)
        {
            int ieq = accRows[k];
            a[ieq, JEU] = -(mifU[k][0] - prfU[k][0]) / vee;
            a[ieq, JEV] = -(mifU[k][1] - prfU[k][1]) / vee;
            a[ieq, JEW] = -(mifU[k][2] - prfU[k][2]) / vee;
            a[ieq, JEP] = (mifU[k][3] - prfU[k][3]) / rot;
            a[ieq, JEQ] = (mifU[k][4] - prfU[k][4]) / rot;
            a[ieq, JER] = (mifU[k][5] - prfU[k][5]) / rot;
            a[ieq, JEPH] = -gee * ttAng[2, k, 0];
            a[ieq, JETH] = -gee * ttAng[2, k, 1];
            a[ieq, JEPS] = -gee * ttAng[2, k, 2];
        }

        // Moment rows (x/y/z ang. accel.): IEQ = JEP/JEQ/JER.
        int[] angRows = { JEP, JEQ, JER };
        for (int k = 0; k < 3; k++)
        {
            int ieq = angRows[k];
            a[ieq, JEU] = -(rimU[k][0] - prmU[k][0]) / vee;
            a[ieq, JEV] = -(rimU[k][1] - prmU[k][1]) / vee;
            a[ieq, JEW] = -(rimU[k][2] - prmU[k][2]) / vee;
            a[ieq, JEP] = (rimU[k][3] - prmU[k][3]) / rot;
            a[ieq, JEQ] = (rimU[k][4] - prmU[k][4]) / rot;
            a[ieq, JER] = (rimU[k][5] - prmU[k][5]) / rot;
        }

        // Euler-angle kinematic rows: IEQ = JEPH/JETH/JEPS.
        int[] eulRows = { JEPH, JETH, JEPS };
        for (int k = 0; k < 3; k++)
        {
            int ieq = eulRows[k];
            a[ieq, JEP] = rt[k, 0];
            a[ieq, JEQ] = rt[k, 1];
            a[ieq, JER] = rt[k, 2];
            a[ieq, JEPH] = rot * (rtAng[k, 0, 0] * wrot[0] + rtAng[k, 1, 0] * wrot[1] + rtAng[k, 2, 0] * wrot[2]);
            a[ieq, JETH] = rot * (rtAng[k, 0, 1] * wrot[0] + rtAng[k, 1, 1] * wrot[1] + rtAng[k, 2, 1] * wrot[2]);
            a[ieq, JEPS] = rot * (rtAng[k, 0, 2] * wrot[0] + rtAng[k, 1, 2] * wrot[1] + rtAng[k, 2, 2] * wrot[2]);
        }

        // Position kinematic rows: IEQ = JEX/JEY/JEZ.
        int[] posRows = { JEX, JEY, JEZ };
        for (int k = 0; k < 3; k++)
        {
            int ieq = posRows[k];
            a[ieq, JEU] = tt[k, 0];
            a[ieq, JEV] = tt[k, 1];
            a[ieq, JEW] = tt[k, 2];
            a[ieq, JEPH] = -(ttAng[k, 0, 0] * vinf[0] + ttAng[k, 1, 0] * vinf[1] + ttAng[k, 2, 0] * vinf[2]) * vee;
            a[ieq, JETH] = -(ttAng[k, 0, 1] * vinf[0] + ttAng[k, 1, 1] * vinf[1] + ttAng[k, 2, 1] * vinf[2]) * vee;
            a[ieq, JEPS] = -(ttAng[k, 0, 2] * vinf[0] + ttAng[k, 1, 2] * vinf[1] + ttAng[k, 2, 2] * vinf[2]) * vee;
        }

        var flat = new double[12 * 12];
        for (int i = 0; i < 12; i++) for (int j = 0; j < 12; j++) flat[i * 12 + j] = a[i, j];
        return flat;
    }

    /// <summary>Eigenvalues of the dynamics matrix (the aircraft's dynamic modes).</summary>
    public static Complex[] Eigenvalues(ModeInputs m) => Eigen.Eigenvalues(Build(m), 12);

    /// <summary>Port of amass.f APPGET: apparent (added) mass/inertia tensors from the
    /// lifting-surface strips. Returns RAW tensors (caller multiplies by RHO). unitl = Lunit.</summary>
    public static (double[,] amass, double[,] ainer) AppMass(Avl.Core.Model.Geometry geo, EncalcResult enc, double unitl)
    {
        double[] Cross(double[] a, double[] b) => new double[]
        { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        var amass = new double[3, 3];
        var ainer = new double[3, 3];
        double u3 = unitl * unitl * unitl;
        double u5 = u3 * unitl * unitl;

        for (int j = 0; j < geo.Strips.Count; j++)
        {
            var s = geo.Strips[j];
            double cr = s.Chord;
            double sr = s.Chord * s.Wstrip;
            var un = new double[] { 0.0, enc.StripAxes[j].Ensy, enc.StripAxes[j].Ensz };

            var us = new double[]
            {
                s.Rle2[0] - s.Rle1[0] + 0.5 * (s.Chord2 - s.Chord1),
                s.Rle2[1] - s.Rle1[1],
                s.Rle2[2] - s.Rle1[2],
            };
            double umag = Math.Sqrt(us[0] * us[0] + us[1] * us[1] + us[2] * us[2]);
            if (umag > 0) { us[0] /= umag; us[1] /= umag; us[2] /= umag; }

            var rm = new double[] { s.Rle[0] + 0.5 * cr, s.Rle[1], s.Rle[2] };
            var rxun = Cross(rm, un);
            double cperp = cr * (us[1] * un[2] - us[2] * un[1]);

            double appm = sr * 0.25 * Math.PI * cperp;
            double appi = sr * 0.25 * Math.PI * cperp * cperp * cperp / 64.0;

            for (int k = 0; k < 3; k++)
                for (int l = 0; l < 3; l++)
                {
                    amass[k, l] += appm * un[k] * un[l] * u3;
                    ainer[k, l] += appm * rxun[k] * rxun[l] * u5 + appi * us[k] * us[l] * u5;
                }
        }
        return (amass, ainer);
    }

    // ---- helpers -----------------------------------------------------------

    private static double[,] Inv3(double[,] a)
    {
        double det =
            a[0, 0] * (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1])
          - a[0, 1] * (a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0])
          + a[0, 2] * (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]);
        // A singular tensor (e.g. an all-zero inertia) would make 1/det non-finite and poison the
        // system matrix with Inf/NaN. Fail loudly here so the caller reports it instead of feeding
        // a non-finite matrix into the eigenvalue solver.
        if (!double.IsFinite(det) || det == 0.0)
            throw new InvalidOperationException("Inv3: singular matrix");
        double di = 1.0 / det;
        var b = new double[3, 3];
        b[0, 0] = (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) * di;
        b[0, 1] = (a[0, 2] * a[2, 1] - a[0, 1] * a[2, 2]) * di;
        b[0, 2] = (a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1]) * di;
        b[1, 0] = (a[1, 2] * a[2, 0] - a[1, 0] * a[2, 2]) * di;
        b[1, 1] = (a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0]) * di;
        b[1, 2] = (a[0, 2] * a[1, 0] - a[0, 0] * a[1, 2]) * di;
        b[2, 0] = (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]) * di;
        b[2, 1] = (a[0, 1] * a[2, 0] - a[0, 0] * a[2, 1]) * di;
        b[2, 2] = (a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0]) * di;
        return b;
    }

    /// <summary>Port of autil.f ROTENS3: rotation tensor T and dT/d(angle).</summary>
    private static void Rotens3(double[] ang, out double[,] t, out double[,,] tA)
    {
        double c1 = Math.Cos(ang[0]), c2 = Math.Cos(ang[1]), c3 = Math.Cos(ang[2]);
        double s1 = Math.Sin(ang[0]), s2 = Math.Sin(ang[1]), s3 = Math.Sin(ang[2]);
        t = new double[3, 3];
        t[0, 0] = c2 * c3; t[1, 0] = -c2 * s3; t[2, 0] = -s2;
        t[0, 1] = -s1 * s2 * c3 + c1 * s3; t[1, 1] = s1 * s2 * s3 + c1 * c3; t[2, 1] = -s1 * c2;
        t[0, 2] = c1 * s2 * c3 + s1 * s3; t[1, 2] = -c1 * s2 * s3 + s1 * c3; t[2, 2] = c1 * c2;

        tA = new double[3, 3, 3];
        // d/d(phi) = index 0
        tA[0, 1, 0] = -c1 * s2 * c3 - s1 * s3; tA[1, 1, 0] = c1 * s2 * s3 - s1 * c3; tA[2, 1, 0] = -c1 * c2;
        tA[0, 2, 0] = -s1 * s2 * c3 + c1 * s3; tA[1, 2, 0] = s1 * s2 * s3 + c1 * c3; tA[2, 2, 0] = -s1 * c2;
        // d/d(theta) = index 1
        tA[0, 0, 1] = -s2 * c3; tA[1, 0, 1] = s2 * s3; tA[2, 0, 1] = -c2;
        tA[0, 1, 1] = -s1 * c2 * c3; tA[1, 1, 1] = s1 * c2 * s3; tA[2, 1, 1] = s1 * s2;
        tA[0, 2, 1] = c1 * c2 * c3; tA[1, 2, 1] = -c1 * c2 * s3; tA[2, 2, 1] = -c1 * s2;
        // d/d(psi) = index 2
        tA[0, 0, 2] = -c2 * s3; tA[1, 0, 2] = -c2 * c3;
        tA[0, 1, 2] = s1 * s2 * s3 + c1 * c3; tA[1, 1, 2] = s1 * s2 * c3 - c1 * s3;
        tA[0, 2, 2] = -c1 * s2 * s3 + s1 * c3; tA[1, 2, 2] = -c1 * s2 * c3 - s1 * s3;
    }

    /// <summary>Port of autil.f RATEKI3: rate-kinematics tensor R and dR/d(angle).</summary>
    private static void Rateki3(double[] ang, out double[,] r, out double[,,] rA)
    {
        double c1 = Math.Cos(ang[0]), c2 = Math.Cos(ang[1]), s1 = Math.Sin(ang[0]), t2 = Math.Tan(ang[1]);
        r = new double[3, 3];
        r[0, 0] = -1.0; r[1, 0] = 0; r[2, 0] = 0;
        r[0, 1] = s1 * t2; r[1, 1] = c1; r[2, 1] = s1 / c2;
        r[0, 2] = -c1 * t2; r[1, 2] = s1; r[2, 2] = -c1 / c2;

        rA = new double[3, 3, 3];
        // d/d(phi) = index 0
        rA[0, 1, 0] = c1 * t2; rA[1, 1, 0] = -s1; rA[2, 1, 0] = c1 / c2;
        rA[0, 2, 0] = s1 * t2; rA[1, 2, 0] = c1; rA[2, 2, 0] = s1 / c2;
        // d/d(theta) = index 1
        rA[0, 1, 1] = s1 / (c2 * c2); rA[2, 1, 1] = s1 * t2 / c2;
        rA[0, 2, 1] = -c1 / (c2 * c2); rA[2, 2, 1] = -c1 * t2 / c2;
    }
}
