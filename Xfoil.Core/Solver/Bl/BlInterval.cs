// Port of xblsys.f BLMID, AXSET and BLDIF: the two-point boundary-layer difference
// equations for one interval (stations 1 and 2). BLDIF assembles the 4x5 Newton
// Jacobian blocks VS1/VS2, the residual VSREZ, and the parameter-sensitivity vectors
// VSM (Mach^2), VSR (Reynolds), VSX (transition position). Rows are: 1 = amplification
// (laminar) / shear-lag (turbulent), 2 = momentum, 3 = shape-parameter. Row 4 (the Ue
// mass-defect coupling) is filled by the global assembly (SETBL), not here.
//
// The unknown ordering per station (the 5 columns) is: [ dA|dS , dT , dD , dU , dX ]
// -- amplification A for laminar, shear coefficient S for turbulent, then momentum
// thickness, displacement thickness, edge velocity, streamwise coordinate.
//
// Field-name mapping vs. Fortran: HK2_U2 -> S2.Hk_U, etc. (see BlStation).

namespace Xfoil.Core.Solver.Bl;

public sealed partial class BlInterval
{
    public BlStation S1 = new();
    public BlStation S2 = new();
    public BlEnv Env = null!;
    public BlParams P = new();

    // Interval flags (mirror the XBL.INC logicals). SIMI is the stagnation similarity
    // station; TRAN marks a transition interval (assembled by TRDIF).
    public bool Simi, Turb, Wake;

    public double Bule = 1.0; // similarity-station log-difference parameter

    // Transition state (TRCHEK2 output / TRDIF input).
    public double Xiforc = 1.0e9; // forced-transition xi location (default: none)
    public bool Tran, Trfree, Trforc;
    public double Xt; // transition-point xi location within the interval
    // XT sensitivities wrt the "1"/"2" primary variables (set by TRCHEK2, used by TRDIF).
    public double Xt_A1, Xt_X1, Xt_T1, Xt_D1, Xt_U1, Xt_X2, Xt_T2, Xt_D2, Xt_U2, Xt_Ms, Xt_Re, Xt_Xf;

    // Midpoint skin friction (VARA common: CFM and its sensitivities).
    public double Cfm, Cfm_Ms, Cfm_Re, Cfm_U1, Cfm_T1, Cfm_D1, Cfm_U2, Cfm_T2, Cfm_D2;

    // Newton system (SYS common): VS1/VS2 are 4x5, the rest length 4.
    public readonly double[,] Vs1 = new double[4, 5];
    public readonly double[,] Vs2 = new double[4, 5];
    public readonly double[] Vsrez = new double[4];
    public readonly double[] Vsm = new double[4];
    public readonly double[] Vsr = new double[4];
    public readonly double[] Vsx = new double[4];

    /// <summary>Port of AXSET: rms-averaged amplification rate over the interval, plus a
    /// small term keeping dN/dx > 0 near N = Ncrit. Uses the 2nd-order (idamp=0) DAMPL.</summary>
    public static void Axset(double hk1, double t1, double rt1, double a1,
                             double hk2, double t2, double rt2, double a2, double acrit,
                             out double ax, out double ax_hk1, out double ax_t1, out double ax_rt1, out double ax_a1,
                             out double ax_hk2, out double ax_t2, out double ax_rt2, out double ax_a2)
    {
        BlClosure.Dampl(hk1, t1, rt1, out double ax1, out double ax1_hk1, out double ax1_t1, out double ax1_rt1);
        BlClosure.Dampl(hk2, t2, rt2, out double ax2, out double ax2_hk2, out double ax2_t2, out double ax2_rt2);

        // rms-average
        double axsq = 0.5 * (ax1 * ax1 + ax2 * ax2);
        double axa, axa_ax1, axa_ax2;
        if (axsq <= 0.0) { axa = 0.0; axa_ax1 = 0.0; axa_ax2 = 0.0; }
        else { axa = Math.Sqrt(axsq); axa_ax1 = 0.5 * ax1 / axa; axa_ax2 = 0.5 * ax2 / axa; }

        // small additional term near N = Ncrit
        double arg = Math.Min(20.0 * (acrit - 0.5 * (a1 + a2)), 20.0);
        double exn, exn_a1, exn_a2;
        if (arg <= 0.0) { exn = 1.0; exn_a1 = 0.0; exn_a2 = 0.0; }
        else { exn = Math.Exp(-arg); exn_a1 = 20.0 * 0.5 * exn; exn_a2 = 20.0 * 0.5 * exn; }

        double dax = exn * 0.002 / (t1 + t2);
        double dax_a1 = exn_a1 * 0.002 / (t1 + t2);
        double dax_a2 = exn_a2 * 0.002 / (t1 + t2);
        double dax_t1 = -dax / (t1 + t2);
        double dax_t2 = -dax / (t1 + t2);

        ax = axa + dax;
        ax_hk1 = axa_ax1 * ax1_hk1;
        ax_t1 = axa_ax1 * ax1_t1 + dax_t1;
        ax_rt1 = axa_ax1 * ax1_rt1;
        ax_a1 = dax_a1;
        ax_hk2 = axa_ax2 * ax2_hk2;
        ax_t2 = axa_ax2 * ax2_t2 + dax_t2;
        ax_rt2 = axa_ax2 * ax2_rt2;
        ax_a2 = dax_a2;
    }

    /// <summary>Port of BLMID: midpoint skin-friction coefficient CFM and its
    /// sensitivities. ityp: 1 laminar, 2 turbulent, 3 wake.</summary>
    public void Blmid(int ityp)
    {
        if (Simi)
        {
            // similarity station: "1" secondary vars equal "2"
            S1.Hk = S2.Hk; S1.Hk_T = S2.Hk_T; S1.Hk_D = S2.Hk_D; S1.Hk_U = S2.Hk_U; S1.Hk_Ms = S2.Hk_Ms;
            S1.Rt = S2.Rt; S1.Rt_T = S2.Rt_T; S1.Rt_U = S2.Rt_U; S1.Rt_Ms = S2.Rt_Ms; S1.Rt_Re = S2.Rt_Re;
            S1.M = S2.M; S1.M_U = S2.M_U; S1.M_Ms = S2.M_Ms;
        }

        double hka = 0.5 * (S1.Hk + S2.Hk);
        double rta = 0.5 * (S1.Rt + S2.Rt);
        double ma = 0.5 * (S1.M + S2.M);

        double cfm_hka, cfm_rta, cfm_ma;
        if (ityp == 3) { Cfm = 0; cfm_hka = 0; cfm_rta = 0; cfm_ma = 0; }
        else if (ityp == 1) BlClosure.Cfl(hka, rta, ma, out Cfm, out cfm_hka, out cfm_rta, out cfm_ma);
        else
        {
            BlClosure.Cft(hka, rta, ma, out Cfm, out cfm_hka, out cfm_rta, out cfm_ma);
            BlClosure.Cfl(hka, rta, ma, out double cfml, out double cfml_hka, out double cfml_rta, out double cfml_ma);
            if (cfml > Cfm) { Cfm = cfml; cfm_hka = cfml_hka; cfm_rta = cfml_rta; cfm_ma = cfml_ma; }
        }

        Cfm_U1 = 0.5 * (cfm_hka * S1.Hk_U + cfm_ma * S1.M_U + cfm_rta * S1.Rt_U);
        Cfm_T1 = 0.5 * (cfm_hka * S1.Hk_T + cfm_rta * S1.Rt_T);
        Cfm_D1 = 0.5 * (cfm_hka * S1.Hk_D);
        Cfm_U2 = 0.5 * (cfm_hka * S2.Hk_U + cfm_ma * S2.M_U + cfm_rta * S2.Rt_U);
        Cfm_T2 = 0.5 * (cfm_hka * S2.Hk_T + cfm_rta * S2.Rt_T);
        Cfm_D2 = 0.5 * (cfm_hka * S2.Hk_D);
        Cfm_Ms = 0.5 * (cfm_hka * S1.Hk_Ms + cfm_ma * S1.M_Ms + cfm_rta * S1.Rt_Ms
                      + cfm_hka * S2.Hk_Ms + cfm_ma * S2.M_Ms + cfm_rta * S2.Rt_Ms);
        Cfm_Re = 0.5 * (cfm_rta * S1.Rt_Re + cfm_rta * S2.Rt_Re);
    }

    private void ClearSystem()
    {
        for (int k = 0; k < 4; k++)
        {
            Vsrez[k] = 0; Vsm[k] = 0; Vsr[k] = 0; Vsx[k] = 0;
            for (int l = 0; l < 5; l++) { Vs1[k, l] = 0; Vs2[k, l] = 0; }
        }
    }

    /// <summary>Port of BLDIF: assemble the interval's Newton system. ityp: 0 similarity,
    /// 1 laminar, 2 turbulent, 3 wake. BLKIN+BLVAR (both stations) and BLMID must have
    /// run first.</summary>
    public void Bldif(int ityp)
    {
        BlStation a = S1, b = S2;
        double duxcon = P.Duxcon, dlcon = P.Dlcon, gacon = P.Gacon, gbcon = P.Gbcon, gccon = P.Gccon, sccon = P.Sccon;

        double xlog, ulog, tlog, hlog, ddlog;
        if (ityp == 0)
        {
            xlog = 1.0; ulog = Bule; tlog = 0.5 * (1.0 - Bule); hlog = 0.0; ddlog = 0.0;
        }
        else
        {
            xlog = Math.Log(b.X / a.X); ulog = Math.Log(b.U / a.U);
            tlog = Math.Log(b.T / a.T); hlog = Math.Log(b.Hs / a.Hs); ddlog = 1.0;
        }

        ClearSystem();

        // local-upwinding parameter UPW
        double hupwt = 1.0;
        double hdcon = 5.0 * hupwt / (b.Hk * b.Hk);
        double hd_hk1 = 0.0;
        double hd_hk2 = -hdcon * 2.0 / b.Hk;
        if (ityp == 3)
        {
            hdcon = hupwt / (b.Hk * b.Hk);
            hd_hk1 = 0.0;
            hd_hk2 = -hdcon * 2.0 / b.Hk;
        }

        double arg = Math.Abs((b.Hk - 1.0) / (a.Hk - 1.0));
        double hl = Math.Log(arg);
        double hl_hk1 = -1.0 / (a.Hk - 1.0);
        double hl_hk2 = 1.0 / (b.Hk - 1.0);

        double hlsq = Math.Min(hl * hl, 15.0);
        double ehh = Math.Exp(-hlsq * hdcon);
        double upw = 1.0 - 0.5 * ehh;
        double upw_hl = ehh * hl * hdcon;
        double upw_hd = 0.5 * ehh * hlsq;

        double upw_hk1 = upw_hl * hl_hk1 + upw_hd * hd_hk1;
        double upw_hk2 = upw_hl * hl_hk2 + upw_hd * hd_hk2;

        double upw_u1 = upw_hk1 * a.Hk_U, upw_t1 = upw_hk1 * a.Hk_T, upw_d1 = upw_hk1 * a.Hk_D;
        double upw_u2 = upw_hk2 * b.Hk_U, upw_t2 = upw_hk2 * b.Hk_T, upw_d2 = upw_hk2 * b.Hk_D;
        double upw_ms = upw_hk1 * a.Hk_Ms + upw_hk2 * b.Hk_Ms;

        // ---- row 1: amplification / shear-lag
        if (ityp == 0)
        {
            Vs2[0, 0] = 1.0;
            Vsr[0] = 0.0;
            Vsrez[0] = -b.Ampl;
        }
        else if (ityp == 1)
        {
            Axset(a.Hk, a.T, a.Rt, a.Ampl, b.Hk, b.T, b.Rt, b.Ampl, Env.Amcrit,
                out double ax, out double ax_hk1, out double ax_t1, out double ax_rt1, out double ax_a1,
                out double ax_hk2, out double ax_t2, out double ax_rt2, out double ax_a2);

            double rezc = b.Ampl - a.Ampl - ax * (b.X - a.X);
            double z_ax = -(b.X - a.X);

            Vs1[0, 0] = z_ax * ax_a1 - 1.0;
            Vs1[0, 1] = z_ax * (ax_hk1 * a.Hk_T + ax_t1 + ax_rt1 * a.Rt_T);
            Vs1[0, 2] = z_ax * (ax_hk1 * a.Hk_D);
            Vs1[0, 3] = z_ax * (ax_hk1 * a.Hk_U + ax_rt1 * a.Rt_U);
            Vs1[0, 4] = ax;
            Vs2[0, 0] = z_ax * ax_a2 + 1.0;
            Vs2[0, 1] = z_ax * (ax_hk2 * b.Hk_T + ax_t2 + ax_rt2 * b.Rt_T);
            Vs2[0, 2] = z_ax * (ax_hk2 * b.Hk_D);
            Vs2[0, 3] = z_ax * (ax_hk2 * b.Hk_U + ax_rt2 * b.Rt_U);
            Vs2[0, 4] = -ax;
            Vsm[0] = z_ax * (ax_hk1 * a.Hk_Ms + ax_rt1 * a.Rt_Ms + ax_hk2 * b.Hk_Ms + ax_rt2 * b.Rt_Ms);
            Vsr[0] = z_ax * (ax_rt1 * a.Rt_Re + ax_rt2 * b.Rt_Re);
            Vsx[0] = 0.0;
            Vsrez[0] = -rezc;
        }
        else
        {
            // turbulent shear-lag equation
            double sa = (1.0 - upw) * a.S + upw * b.S;
            double cqa = (1.0 - upw) * a.Cq + upw * b.Cq;
            double cfa = (1.0 - upw) * a.Cf + upw * b.Cf;
            double hka = (1.0 - upw) * a.Hk + upw * b.Hk;
            double usa = 0.5 * (a.Us + b.Us);
            double rta = 0.5 * (a.Rt + b.Rt);
            double dea = 0.5 * (a.De + b.De);
            double da = 0.5 * (a.D + b.D);
            double ald = ityp == 3 ? dlcon : 1.0;

            double gcc, hkc, hkc_hka, hkc_rta;
            if (ityp == 2)
            {
                gcc = gccon;
                hkc = hka - 1.0 - gcc / rta;
                hkc_hka = 1.0;
                hkc_rta = gcc / (rta * rta);
                if (hkc < 0.01) { hkc = 0.01; hkc_hka = 0.0; hkc_rta = 0.0; }
            }
            else { gcc = 0.0; hkc = hka - 1.0; hkc_hka = 1.0; hkc_rta = 0.0; }

            double hr = hkc / (gacon * ald * hka);
            double hr_hka = hkc_hka / (gacon * ald * hka) - hr / hka;
            double hr_rta = hkc_rta / (gacon * ald * hka);

            double uq = (0.5 * cfa - hr * hr) / (gbcon * da);
            double uq_hka = -2.0 * hr * hr_hka / (gbcon * da);
            double uq_rta = -2.0 * hr * hr_rta / (gbcon * da);
            double uq_cfa = 0.5 / (gbcon * da);
            double uq_da = -uq / da;
            double uq_upw = uq_cfa * (b.Cf - a.Cf) + uq_hka * (b.Hk - a.Hk);

            double uq_t1 = (1.0 - upw) * (uq_cfa * a.Cf_T + uq_hka * a.Hk_T) + uq_upw * upw_t1;
            double uq_d1 = (1.0 - upw) * (uq_cfa * a.Cf_D + uq_hka * a.Hk_D) + uq_upw * upw_d1;
            double uq_u1 = (1.0 - upw) * (uq_cfa * a.Cf_U + uq_hka * a.Hk_U) + uq_upw * upw_u1;
            double uq_t2 = upw * (uq_cfa * b.Cf_T + uq_hka * b.Hk_T) + uq_upw * upw_t2;
            double uq_d2 = upw * (uq_cfa * b.Cf_D + uq_hka * b.Hk_D) + uq_upw * upw_d2;
            double uq_u2 = upw * (uq_cfa * b.Cf_U + uq_hka * b.Hk_U) + uq_upw * upw_u2;
            double uq_ms = (1.0 - upw) * (uq_cfa * a.Cf_Ms + uq_hka * a.Hk_Ms) + uq_upw * upw_ms
                         + upw * (uq_cfa * b.Cf_Ms + uq_hka * b.Hk_Ms);
            double uq_re = (1.0 - upw) * uq_cfa * a.Cf_Re + upw * uq_cfa * b.Cf_Re;

            uq_t1 += 0.5 * uq_rta * a.Rt_T;
            uq_d1 += 0.5 * uq_da;
            uq_u1 += 0.5 * uq_rta * a.Rt_U;
            uq_t2 += 0.5 * uq_rta * b.Rt_T;
            uq_d2 += 0.5 * uq_da;
            uq_u2 += 0.5 * uq_rta * b.Rt_U;
            uq_ms += 0.5 * uq_rta * a.Rt_Ms + 0.5 * uq_rta * b.Rt_Ms;
            uq_re += 0.5 * uq_rta * a.Rt_Re + 0.5 * uq_rta * b.Rt_Re;

            double scc = sccon * 1.333 / (1.0 + usa);
            double scc_usa = -scc / (1.0 + usa);

            double slog = Math.Log(b.S / a.S);
            double dxi = b.X - a.X;

            double rezc = scc * (cqa - sa * ald) * dxi - dea * 2.0 * slog + dea * 2.0 * (uq * dxi - ulog) * duxcon;

            double z_cfa = dea * 2.0 * uq_cfa * dxi * duxcon;
            double z_hka = dea * 2.0 * uq_hka * dxi * duxcon;
            double z_da = dea * 2.0 * uq_da * dxi * duxcon;
            double z_sl = -dea * 2.0;
            double z_ul = -dea * 2.0 * duxcon;
            double z_dxi = scc * (cqa - sa * ald) + dea * 2.0 * uq * duxcon;
            double z_usa = scc_usa * (cqa - sa * ald) * dxi;
            double z_cqa = scc * dxi;
            double z_sa = -scc * dxi * ald;
            double z_dea = 2.0 * ((uq * dxi - ulog) * duxcon - slog);

            double z_upw = z_cqa * (b.Cq - a.Cq) + z_sa * (b.S - a.S) + z_cfa * (b.Cf - a.Cf) + z_hka * (b.Hk - a.Hk);
            double z_de1 = 0.5 * z_dea, z_de2 = 0.5 * z_dea;
            double z_us1 = 0.5 * z_usa, z_us2 = 0.5 * z_usa;
            double z_d1 = 0.5 * z_da, z_d2 = 0.5 * z_da;
            double z_u1 = -z_ul / a.U, z_u2 = z_ul / b.U;
            double z_x1 = -z_dxi, z_x2 = z_dxi;
            double z_s1 = (1.0 - upw) * z_sa - z_sl / a.S;
            double z_s2 = upw * z_sa + z_sl / b.S;
            double z_cq1 = (1.0 - upw) * z_cqa, z_cq2 = upw * z_cqa;
            double z_cf1 = (1.0 - upw) * z_cfa, z_cf2 = upw * z_cfa;
            double z_hk1 = (1.0 - upw) * z_hka, z_hk2 = upw * z_hka;

            Vs1[0, 0] = z_s1;
            Vs1[0, 1] = z_upw * upw_t1 + z_de1 * a.De_T + z_us1 * a.Us_T;
            Vs1[0, 2] = z_d1 + z_upw * upw_d1 + z_de1 * a.De_D + z_us1 * a.Us_D;
            Vs1[0, 3] = z_u1 + z_upw * upw_u1 + z_de1 * a.De_U + z_us1 * a.Us_U;
            Vs1[0, 4] = z_x1;
            Vs2[0, 0] = z_s2;
            Vs2[0, 1] = z_upw * upw_t2 + z_de2 * b.De_T + z_us2 * b.Us_T;
            Vs2[0, 2] = z_d2 + z_upw * upw_d2 + z_de2 * b.De_D + z_us2 * b.Us_D;
            Vs2[0, 3] = z_u2 + z_upw * upw_u2 + z_de2 * b.De_U + z_us2 * b.Us_U;
            Vs2[0, 4] = z_x2;
            Vsm[0] = z_upw * upw_ms + z_de1 * a.De_Ms + z_us1 * a.Us_Ms + z_de2 * b.De_Ms + z_us2 * b.Us_Ms;

            Vs1[0, 1] += z_cq1 * a.Cq_T + z_cf1 * a.Cf_T + z_hk1 * a.Hk_T;
            Vs1[0, 2] += z_cq1 * a.Cq_D + z_cf1 * a.Cf_D + z_hk1 * a.Hk_D;
            Vs1[0, 3] += z_cq1 * a.Cq_U + z_cf1 * a.Cf_U + z_hk1 * a.Hk_U;
            Vs2[0, 1] += z_cq2 * b.Cq_T + z_cf2 * b.Cf_T + z_hk2 * b.Hk_T;
            Vs2[0, 2] += z_cq2 * b.Cq_D + z_cf2 * b.Cf_D + z_hk2 * b.Hk_D;
            Vs2[0, 3] += z_cq2 * b.Cq_U + z_cf2 * b.Cf_U + z_hk2 * b.Hk_U;
            Vsm[0] += z_cq1 * a.Cq_Ms + z_cf1 * a.Cf_Ms + z_hk1 * a.Hk_Ms + z_cq2 * b.Cq_Ms + z_cf2 * b.Cf_Ms + z_hk2 * b.Hk_Ms;
            Vsr[0] = z_cq1 * a.Cq_Re + z_cf1 * a.Cf_Re + z_cq2 * b.Cq_Re + z_cf2 * b.Cf_Re;
            Vsx[0] = 0.0;
            Vsrez[0] = -rezc;
        }

        // ---- row 2: momentum equation
        {
            double ha = 0.5 * (a.H + b.H);
            double ma = 0.5 * (a.M + b.M);
            double xa = 0.5 * (a.X + b.X);
            double ta = 0.5 * (a.T + b.T);
            double hwa = 0.5 * (a.Dw / a.T + b.Dw / b.T);

            double cfx = 0.50 * Cfm * xa / ta + 0.25 * (a.Cf * a.X / a.T + b.Cf * b.X / b.T);
            double cfx_xa = 0.50 * Cfm / ta;
            double cfx_ta = -0.50 * Cfm * xa / (ta * ta);
            double cfx_x1 = 0.25 * a.Cf / a.T + cfx_xa * 0.5;
            double cfx_x2 = 0.25 * b.Cf / b.T + cfx_xa * 0.5;
            double cfx_t1 = -0.25 * a.Cf * a.X / (a.T * a.T) + cfx_ta * 0.5;
            double cfx_t2 = -0.25 * b.Cf * b.X / (b.T * b.T) + cfx_ta * 0.5;
            double cfx_cf1 = 0.25 * a.X / a.T;
            double cfx_cf2 = 0.25 * b.X / b.T;
            double cfx_cfm = 0.50 * xa / ta;

            double btmp = ha + 2.0 - ma + hwa;
            double rezt = tlog + btmp * ulog - xlog * 0.5 * cfx;
            double z_cfx = -xlog * 0.5;
            double z_ha = ulog;
            double z_hwa = ulog;
            double z_ma = -ulog;
            double z_xl = -ddlog * 0.5 * cfx;
            double z_ul = ddlog * btmp;
            double z_tl = ddlog;

            double z_cfm = z_cfx * cfx_cfm;
            double z_cf1 = z_cfx * cfx_cf1;
            double z_cf2 = z_cfx * cfx_cf2;

            double z_t1 = -z_tl / a.T + z_cfx * cfx_t1 + z_hwa * 0.5 * (-a.Dw / (a.T * a.T));
            double z_t2 = z_tl / b.T + z_cfx * cfx_t2 + z_hwa * 0.5 * (-b.Dw / (b.T * b.T));
            double z_x1 = -z_xl / a.X + z_cfx * cfx_x1;
            double z_x2 = z_xl / b.X + z_cfx * cfx_x2;
            double z_u1 = -z_ul / a.U;
            double z_u2 = z_ul / b.U;

            Vs1[1, 1] = 0.5 * z_ha * a.H_T + z_cfm * Cfm_T1 + z_cf1 * a.Cf_T + z_t1;
            Vs1[1, 2] = 0.5 * z_ha * a.H_D + z_cfm * Cfm_D1 + z_cf1 * a.Cf_D;
            Vs1[1, 3] = 0.5 * z_ma * a.M_U + z_cfm * Cfm_U1 + z_cf1 * a.Cf_U + z_u1;
            Vs1[1, 4] = z_x1;
            Vs2[1, 1] = 0.5 * z_ha * b.H_T + z_cfm * Cfm_T2 + z_cf2 * b.Cf_T + z_t2;
            Vs2[1, 2] = 0.5 * z_ha * b.H_D + z_cfm * Cfm_D2 + z_cf2 * b.Cf_D;
            Vs2[1, 3] = 0.5 * z_ma * b.M_U + z_cfm * Cfm_U2 + z_cf2 * b.Cf_U + z_u2;
            Vs2[1, 4] = z_x2;
            Vsm[1] = 0.5 * z_ma * a.M_Ms + z_cfm * Cfm_Ms + z_cf1 * a.Cf_Ms + 0.5 * z_ma * b.M_Ms + z_cf2 * b.Cf_Ms;
            Vsr[1] = z_cfm * Cfm_Re + z_cf1 * a.Cf_Re + z_cf2 * b.Cf_Re;
            Vsx[1] = 0.0;
            Vsrez[1] = -rezt;
        }

        // ---- row 3: shape-parameter equation
        {
            double xot1 = a.X / a.T;
            double xot2 = b.X / b.T;
            double ha = 0.5 * (a.H + b.H);
            double hsa = 0.5 * (a.Hs + b.Hs);
            double hca = 0.5 * (a.Hc + b.Hc);
            double hwa = 0.5 * (a.Dw / a.T + b.Dw / b.T);

            double dix = (1.0 - upw) * a.Di * xot1 + upw * b.Di * xot2;
            double cfx = (1.0 - upw) * a.Cf * xot1 + upw * b.Cf * xot2;
            double dix_upw = b.Di * xot2 - a.Di * xot1;
            double cfx_upw = b.Cf * xot2 - a.Cf * xot1;

            double btmp = 2.0 * hca / hsa + 1.0 - ha - hwa;
            double rezh = hlog + btmp * ulog + xlog * (0.5 * cfx - dix);
            double z_cfx = xlog * 0.5;
            double z_dix = -xlog;
            double z_hca = 2.0 * ulog / hsa;
            double z_ha = -ulog;
            double z_hwa = -ulog;
            double z_xl = ddlog * (0.5 * cfx - dix);
            double z_ul = ddlog * btmp;
            double z_hl = ddlog;

            double z_upw = z_cfx * cfx_upw + z_dix * dix_upw;
            double z_hs1 = -hca * ulog / (hsa * hsa) - z_hl / a.Hs;
            double z_hs2 = -hca * ulog / (hsa * hsa) + z_hl / b.Hs;

            double z_cf1 = (1.0 - upw) * z_cfx * xot1;
            double z_cf2 = upw * z_cfx * xot2;
            double z_di1 = (1.0 - upw) * z_dix * xot1;
            double z_di2 = upw * z_dix * xot2;

            double z_t1 = (1.0 - upw) * (z_cfx * a.Cf + z_dix * a.Di) * (-xot1 / a.T);
            double z_t2 = upw * (z_cfx * b.Cf + z_dix * b.Di) * (-xot2 / b.T);
            double z_x1 = (1.0 - upw) * (z_cfx * a.Cf + z_dix * a.Di) / a.T - z_xl / a.X;
            double z_x2 = upw * (z_cfx * b.Cf + z_dix * b.Di) / b.T + z_xl / b.X;
            double z_u1 = -z_ul / a.U;
            double z_u2 = z_ul / b.U;

            z_t1 += z_hwa * 0.5 * (-a.Dw / (a.T * a.T));
            z_t2 += z_hwa * 0.5 * (-b.Dw / (b.T * b.T));

            Vs1[2, 0] = z_di1 * a.Di_S;
            Vs1[2, 1] = z_hs1 * a.Hs_T + z_cf1 * a.Cf_T + z_di1 * a.Di_T + z_t1;
            Vs1[2, 2] = z_hs1 * a.Hs_D + z_cf1 * a.Cf_D + z_di1 * a.Di_D;
            Vs1[2, 3] = z_hs1 * a.Hs_U + z_cf1 * a.Cf_U + z_di1 * a.Di_U + z_u1;
            Vs1[2, 4] = z_x1;
            Vs2[2, 0] = z_di2 * b.Di_S;
            Vs2[2, 1] = z_hs2 * b.Hs_T + z_cf2 * b.Cf_T + z_di2 * b.Di_T + z_t2;
            Vs2[2, 2] = z_hs2 * b.Hs_D + z_cf2 * b.Cf_D + z_di2 * b.Di_D;
            Vs2[2, 3] = z_hs2 * b.Hs_U + z_cf2 * b.Cf_U + z_di2 * b.Di_U + z_u2;
            Vs2[2, 4] = z_x2;
            Vsm[2] = z_hs1 * a.Hs_Ms + z_cf1 * a.Cf_Ms + z_di1 * a.Di_Ms + z_hs2 * b.Hs_Ms + z_cf2 * b.Cf_Ms + z_di2 * b.Di_Ms;
            Vsr[2] = z_hs1 * a.Hs_Re + z_cf1 * a.Cf_Re + z_di1 * a.Di_Re + z_hs2 * b.Hs_Re + z_cf2 * b.Cf_Re + z_di2 * b.Di_Re;

            Vs1[2, 1] += 0.5 * (z_hca * a.Hc_T + z_ha * a.H_T) + z_upw * upw_t1;
            Vs1[2, 2] += 0.5 * (z_hca * a.Hc_D + z_ha * a.H_D) + z_upw * upw_d1;
            Vs1[2, 3] += 0.5 * (z_hca * a.Hc_U) + z_upw * upw_u1;
            Vs2[2, 1] += 0.5 * (z_hca * b.Hc_T + z_ha * b.H_T) + z_upw * upw_t2;
            Vs2[2, 2] += 0.5 * (z_hca * b.Hc_D + z_ha * b.H_D) + z_upw * upw_d2;
            Vs2[2, 3] += 0.5 * (z_hca * b.Hc_U) + z_upw * upw_u2;
            Vsm[2] += 0.5 * (z_hca * a.Hc_Ms) + z_upw * upw_ms + 0.5 * (z_hca * b.Hc_Ms);
            Vsx[2] = 0.0;
            Vsrez[2] = -rezh;
        }
    }
}
