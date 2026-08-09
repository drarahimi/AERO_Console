// Port of XFOIL's inviscid panel solve: GGCALC (build/factor the AIJ influence
// matrix and solve for the alpha=0 and alpha=90 unit vorticity distributions GAMU),
// plus the per-alpha combination (QISET/GAMQV), CPCALC (compressible surface Cp) and
// CLCALC (pressure integration to CL, CM, CDp). Karman-Tsien compressibility is
// carried through exactly as in the Fortran; at Mach 0 it reduces to Cp = 1 - (q)^2.
//
// The unit distributions depend only on geometry, so they're computed once in the
// constructor and reused for every alpha via SolveAlpha.

using Xfoil.Core.Geometry;

namespace Xfoil.Core.Solver;

/// <summary>Inviscid result at one operating point.</summary>
public sealed record InviscidPoint(double Alpha, double Cl, double Cm, double Cdp, double[] Cp, double[] Q);

public sealed class InviscidSolver
{
    private const double Qinf = 1.0; // XFOIL INIT: freestream reference speed
    private readonly PanelAirfoil _g;
    private readonly int _n;
    // Unit vorticity distributions (== inviscid surface speeds) for alpha = 0 and 90.
    private readonly double[] _gamu1;
    private readonly double[] _gamu2;

    // Factored influence matrix AIJ (LU) and its pivots, plus BIJ (airfoil-source dPsi/dSig)
    // -- retained for the viscous source-influence build (QDCALC).
    private double[][] _aijLu = null!;
    private int[] _aijPiv = null!;
    private double[][] _bij = null!; // [m][n]: bij[i][j] = -DZDM_j at node i

    /// <summary>Moment reference point (xfoil.f default XCMREF=0.25, YCMREF=0).</summary>
    public double Xref { get; init; } = 0.25;
    public double Yref { get; init; } = 0.0;

    /// <summary>The paneled geometry this solver runs on.</summary>
    public PanelAirfoil Geometry => _g;
    /// <summary>Unit vorticity distributions for alpha = 0 and 90 (== inviscid surface speeds).</summary>
    public double[] Gamu1 => _gamu1;
    public double[] Gamu2 => _gamu2;
    /// <summary>Airfoil-source streamfunction RHS BIJ(i,j) = -dPsi/dSig_j at node i (rows 0..n-1).</summary>
    public double[][] Bij => _bij;

    /// <summary>Solves the factored AIJ system in place (back-substitution against BIJ columns
    /// etc. in QDCALC). b has length n+1.</summary>
    public void SolveAij(double[] b) => DenseLU.Baksub(_aijLu, _n + 1, _aijPiv, b);

    public InviscidSolver(PanelAirfoil g)
    {
        _g = g;
        _n = g.N;
        (_gamu1, _gamu2) = Ggcalc();
    }

    // Port of GGCALC: assemble the (N+1)x(N+1) system for the two unit distributions.
    private (double[] gamu1, double[] gamu2) Ggcalc()
    {
        int n = _n;
        int m = n + 1;
        const double bwt = 0.1; // internal control point offset ahead of a sharp TE

        var aij = new double[m][];
        for (int i = 0; i < m; i++) aij[i] = new double[m];
        var gamu1 = new double[m];
        var gamu2 = new double[m];

        var dzdg = new double[n];
        var dqdg = new double[n];
        var dzdm = new double[n];
        var dqdm = new double[n];
        var zero = new double[n];
        _bij = new double[m][];
        for (int i = 0; i < m; i++) _bij[i] = new double[n];

        for (int i = 0; i < n; i++)
        {
            // Full PSILIN (gam=sig=0) gives the geometry-only influence vectors: DZDG builds
            // AIJ, DZDM builds BIJ (the source RHS used by QDCALC).
            Psi.PsilinFull(_g, zero, zero, 1.0, 0.0, Qinf, i + 1, _g.X[i], _g.Y[i], _g.Nx[i], _g.Ny[i], dzdg, dzdm, dqdg, dqdm);
            for (int j = 0; j < n; j++) { aij[i][j] = dzdg[j]; _bij[i][j] = -dzdm[j]; }
            aij[i][n] = -1.0; // dRes/dPsio column
            gamu1[i] = -Qinf * _g.Y[i]; // -(PSI(0)  - Psio) residual
            gamu2[i] = Qinf * _g.X[i];  // -(PSI(90) - Psio) residual
        }

        // Kutta condition: GAM(1) + GAM(N) = 0
        for (int j = 0; j < m; j++) aij[n][j] = 0.0;
        aij[n][0] = 1.0;
        aij[n][n - 1] = 1.0;
        gamu1[n] = 0.0;
        gamu2[n] = 0.0;

        if (_g.Sharp)
        {
            // Replace the last airfoil-node row with a zero-internal-velocity condition
            // at a control point on the TE bisector just ahead of the sharp TE.
            double ag1 = Math.Atan2(-_g.Yp[0], -_g.Xp[0]);
            double ag2 = Atanc(_g.Yp[n - 1], _g.Xp[n - 1], ag1);
            double abis = 0.5 * (ag1 + ag2);
            double cbis = Math.Cos(abis), sbis = Math.Sin(abis);

            double ds1 = Dist(_g.X[0], _g.Y[0], _g.X[1], _g.Y[1]);
            double ds2 = Dist(_g.X[n - 1], _g.Y[n - 1], _g.X[n - 2], _g.Y[n - 2]);
            double dsmin = Math.Min(ds1, ds2);

            double xbis = _g.Xte - bwt * dsmin * cbis;
            double ybis = _g.Yte - bwt * dsmin * sbis;

            Psi.Psilin(_g, 0, xbis, ybis, -sbis, cbis, dzdg, dqdg);
            for (int j = 0; j < n; j++) aij[n - 1][j] = dqdg[j];
            aij[n - 1][n] = 0.0;
            gamu1[n - 1] = -cbis;
            gamu2[n - 1] = -sbis;
        }

        var indx = new int[m];
        DenseLU.Ludcmp(aij, m, indx);
        DenseLU.Baksub(aij, m, indx, gamu1);
        DenseLU.Baksub(aij, m, indx, gamu2);
        _aijLu = aij;   // retain factored matrix + pivots for QDCALC
        _aijPiv = indx;
        return (gamu1, gamu2);
    }

    /// <summary>Solves the inviscid flow at the given angle of attack (degrees) and
    /// freestream Mach number, returning surface Cp/speed and integrated CL/CM/CDp.</summary>
    public InviscidPoint SolveAlpha(double alphaDeg, double mach = 0.0)
    {
        int n = _n;
        double alfa = alphaDeg * Math.PI / 180.0;
        double cosa = Math.Cos(alfa), sina = Math.Sin(alfa);

        // QISET / GAMQV: surface speed and its alpha-derivative from the unit solutions.
        var gam = new double[n];
        var gamA = new double[n];
        for (int i = 0; i < n; i++)
        {
            gam[i] = cosa * _gamu1[i] + sina * _gamu2[i];
            gamA[i] = -sina * _gamu1[i] + cosa * _gamu2[i];
        }

        var cp = Cpcalc(gam, mach);
        var (cl, cm, cdp) = Clcalc(gam, gamA, alfa, mach);
        return new InviscidPoint(alphaDeg, cl, cm, cdp, cp, gam);
    }

    /// <summary>Streamfunction PSI and its directional derivative along (nxi,nyi) at a
    /// point, for the inviscid solution at the given alpha (deg). Wraps Psi.StreamFn with
    /// the alpha-combined circulation. Used by wake-trajectory tracing (XYWAKE): calling
    /// with (1,0) and (0,1) yields the two components of grad(PSI) = (-v, u).</summary>
    public (double psi, double psiNi) StreamFunction(double alphaDeg, double xi, double yi, double nxi, double nyi)
    {
        double alfa = alphaDeg * Math.PI / 180.0;
        double cosa = Math.Cos(alfa), sina = Math.Sin(alfa);
        var gam = new double[_n];
        for (int i = 0; i < _n; i++) gam[i] = cosa * _gamu1[i] + sina * _gamu2[i];
        return Psi.StreamFn(_g, gam, cosa, sina, Qinf, 0, xi, yi, nxi, nyi);
    }

    // Port of CPCALC: compressible Cp from surface speed via the Karman-Tsien rule.
    private double[] Cpcalc(double[] q, double minf)
    {
        double beta = Math.Sqrt(1.0 - minf * minf);
        double bfac = 0.5 * minf * minf / (1.0 + beta);
        var cp = new double[q.Length];
        for (int i = 0; i < q.Length; i++)
        {
            double cpinc = 1.0 - (q[i] / Qinf) * (q[i] / Qinf);
            double den = beta + bfac * cpinc;
            cp[i] = cpinc / den;
        }
        return cp;
    }

    // Port of CLCALC: integrate compressible surface pressures to CL, CM, CDp.
    private (double cl, double cm, double cdp) Clcalc(double[] gam, double[] gamA, double alfa, double minf)
    {
        int n = _n;
        double sa = Math.Sin(alfa), ca = Math.Cos(alfa);
        double beta = Math.Sqrt(1.0 - minf * minf);
        double betaMsq = -0.5 / beta;
        double bfac = 0.5 * minf * minf / (1.0 + beta);

        double cl = 0.0, cm = 0.0, cdp = 0.0;

        double cginc = 1.0 - (gam[0] / Qinf) * (gam[0] / Qinf);
        double cpg1 = cginc / (beta + bfac * cginc);
        double cpiGam = -2.0 * gam[0] / (Qinf * Qinf);
        double cpcCpi = (1.0 - bfac * cpg1) / (beta + bfac * cginc);
        double cpg1Alf = cpcCpi * cpiGam * gamA[0];

        for (int i = 0; i < n; i++)
        {
            int ip = (i == n - 1) ? 0 : i + 1;

            cginc = 1.0 - (gam[ip] / Qinf) * (gam[ip] / Qinf);
            double cpg2 = cginc / (beta + bfac * cginc);
            cpiGam = -2.0 * gam[ip] / (Qinf * Qinf);
            cpcCpi = (1.0 - bfac * cpg2) / (beta + bfac * cginc);
            double cpg2Alf = cpcCpi * cpiGam * gamA[ip];

            double dx = (_g.X[ip] - _g.X[i]) * ca + (_g.Y[ip] - _g.Y[i]) * sa;
            double dy = (_g.Y[ip] - _g.Y[i]) * ca - (_g.X[ip] - _g.X[i]) * sa;
            double dg = cpg2 - cpg1;

            double ax = (0.5 * (_g.X[ip] + _g.X[i]) - Xref) * ca + (0.5 * (_g.Y[ip] + _g.Y[i]) - Yref) * sa;
            double ay = (0.5 * (_g.Y[ip] + _g.Y[i]) - Yref) * ca - (0.5 * (_g.X[ip] + _g.X[i]) - Xref) * sa;
            double ag = 0.5 * (cpg2 + cpg1);

            cl += dx * ag;
            cdp -= dy * ag;
            cm += -dx * (ag * ax + dg * dx / 12.0) - dy * (ag * ay + dg * dy / 12.0);

            cpg1 = cpg2;
            cpg1Alf = cpg2Alf;
        }
        // betaMsq/cpg1Alf feed the CL sensitivities (CL_ALF/CL_MSQ) that prescribed-CL
        // and MINF-from-CL modes need; not required for the fixed-alpha result here.
        _ = betaMsq;
        return (cl, cm, cdp);
    }

    // Port of xutils.f ATANC: ATAN2 that increments continuously from a prior angle,
    // avoiding the branch cut. Used only for the sharp-TE bisector.
    private static double Atanc(double y, double x, double thold)
    {
        const double tpi = 2.0 * Math.PI;
        double thnew = Math.Atan2(y, x);
        double dthet = thnew - thold;
        double dtcorr = dthet - tpi * (int)((dthet + Math.CopySign(Math.PI, dthet)) / tpi);
        return thold + dtcorr;
    }

    private static double Dist(double x1, double y1, double x2, double y2)
        => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
}
