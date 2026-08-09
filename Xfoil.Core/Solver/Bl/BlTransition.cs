// Port of xblsys.f TRCHEK2 and TRDIF: the transition machinery.
//
// TRCHEK2 solves the implicit amplification equation across the interval by an inner
// Newton iteration. If the amplification N2 exceeds Ncrit (or a forced-transition xi
// lies in the interval), it locates the transition point XT and its sensitivities to
// the "1"/"2" primary variables; otherwise it just sets the amplification AMPL2.
//
// TRDIF assembles the transition-interval Newton system by summing the laminar part
// (X1..XT) and turbulent part (XT..X2), converting each part's sensitivities (which
// are expressed in terms of the transition-point "T" variables) back into "1"/"2"
// variables via the XT/TT/DT/UT/ST chain rules.

namespace Xfoil.Core.Solver.Bl;

public sealed partial class BlInterval
{
    /// <summary>Port of TRCHEK2. Requires BLKIN+BLVAR(1) already run on S1 and S2, with
    /// S2.Ampl holding the incoming amplification. Sets Tran/Trfree/Trforc, and on
    /// transition sets Xt and the Xt_* sensitivities (also updates S2.Ampl otherwise).</summary>
    public void Trchek2()
    {
        BlStation a = S1, b = S2;
        double acrit = Env.Amcrit;
        const double daeps = 5.0e-5;

        var save2 = b.Clone();

        // initial guess for AMPL2 from the average rate over 1..2
        Axset(a.Hk, a.T, a.Rt, a.Ampl, b.Hk, b.T, b.Rt, b.Ampl, acrit,
            out double ax0, out _, out _, out _, out _, out _, out _, out _, out _);
        b.Ampl = a.Ampl + ax0 * (b.X - a.X);

        double x1 = a.X, x2 = b.X, t1 = a.T, t2 = b.T, d1 = a.D, d2 = b.D, u1 = a.U, u2 = b.U;

        // Weighting factors and interpolated T-quantities (declared here so they survive
        // the loop for the sensitivity block below).
        double wf1 = 0, wf2 = 0, wf1_a1 = 0, wf1_a2 = 0, wf1_x1 = 0, wf1_x2 = 0, wf1_xf = 0;
        double wf2_a1 = 0, wf2_a2 = 0, wf2_x1 = 0, wf2_x2 = 0, wf2_xf = 0;
        double xt = 0, tt = 0, dt = 0, ut = 0, amplt = 0, amplt_a2 = 0;
        double xt_a2 = 0, tt_a2 = 0, dt_a2 = 0, ut_a2 = 0;
        // "T" secondary variables and their sensitivities from BLKIN.
        double hkt = 0, hkt_tt = 0, hkt_dt = 0, hkt_ut = 0, hkt_ms = 0;
        double rtt = 0, rtt_tt = 0, rtt_ut = 0, rtt_ms = 0, rtt_re = 0;
        // AX and its sensitivities from the 1..XT AXSET.
        double ax = 0, ax_hk1 = 0, ax_t1 = 0, ax_rt1 = 0, ax_a1 = 0, ax_hkt = 0, ax_tt = 0, ax_rtt = 0, ax_at = 0;

        for (int itam = 0; itam < 30; itam++)
        {
            // weighting factors for the "T" location
            double sfa, sfa_a1, sfa_a2;
            if (b.Ampl <= acrit) { amplt = b.Ampl; amplt_a2 = 1.0; sfa = 1.0; sfa_a1 = 0.0; sfa_a2 = 0.0; }
            else
            {
                amplt = acrit; amplt_a2 = 0.0;
                sfa = (amplt - a.Ampl) / (b.Ampl - a.Ampl);
                sfa_a1 = (sfa - 1.0) / (b.Ampl - a.Ampl);
                sfa_a2 = (-sfa) / (b.Ampl - a.Ampl);
            }

            double sfx, sfx_x1, sfx_x2, sfx_xf;
            if (Xiforc < x2) { sfx = (Xiforc - x1) / (x2 - x1); sfx_x1 = (sfx - 1.0) / (x2 - x1); sfx_x2 = (-sfx) / (x2 - x1); sfx_xf = 1.0 / (x2 - x1); }
            else { sfx = 1.0; sfx_x1 = 0.0; sfx_x2 = 0.0; sfx_xf = 0.0; }

            if (sfa < sfx) { wf2 = sfa; wf2_a1 = sfa_a1; wf2_a2 = sfa_a2; wf2_x1 = 0.0; wf2_x2 = 0.0; wf2_xf = 0.0; }
            else { wf2 = sfx; wf2_a1 = 0.0; wf2_a2 = 0.0; wf2_x1 = sfx_x1; wf2_x2 = sfx_x2; wf2_xf = sfx_xf; }

            wf1 = 1.0 - wf2; wf1_a1 = -wf2_a1; wf1_a2 = -wf2_a2; wf1_x1 = -wf2_x1; wf1_x2 = -wf2_x2; wf1_xf = -wf2_xf;

            xt = x1 * wf1 + x2 * wf2; tt = t1 * wf1 + t2 * wf2; dt = d1 * wf1 + d2 * wf2; ut = u1 * wf1 + u2 * wf2;
            xt_a2 = x1 * wf1_a2 + x2 * wf2_a2; tt_a2 = t1 * wf1_a2 + t2 * wf2_a2; dt_a2 = d1 * wf1_a2 + d2 * wf2_a2; ut_a2 = u1 * wf1_a2 + u2 * wf2_a2;

            // temporarily set "2" primaries to "T" for BLKIN
            double amp = b.Ampl; // preserve the current AMPL2 iterate
            b.X = xt; b.T = tt; b.D = dt; b.U = ut;
            BlSys.Blkin(b, Env);
            hkt = b.Hk; hkt_tt = b.Hk_T; hkt_dt = b.Hk_D; hkt_ut = b.Hk_U; hkt_ms = b.Hk_Ms;
            rtt = b.Rt; rtt_tt = b.Rt_T; rtt_ut = b.Rt_U; rtt_ms = b.Rt_Ms; rtt_re = b.Rt_Re;

            b.CopyFrom(save2); b.Ampl = amp; // restore "2", keep AMPL2 iterate
            x2 = b.X; // (unchanged, but keep in sync)

            Axset(a.Hk, a.T, a.Rt, a.Ampl, hkt, tt, rtt, amplt, acrit,
                out ax, out ax_hk1, out ax_t1, out ax_rt1, out ax_a1, out ax_hkt, out ax_tt, out ax_rtt, out ax_at);

            if (ax <= 0.0) break; // no amplification here

            double ax_a2 = (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_a2
                         + (ax_hkt * hkt_dt) * dt_a2
                         + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_a2
                         + ax_at * amplt_a2;

            double res = b.Ampl - a.Ampl - ax * (x2 - x1);
            double res_a2 = 1.0 - ax_a2 * (x2 - x1);
            double da2 = -res / res_a2;

            double rlx = 1.0;
            double dxt = xt_a2 * da2;
            if (rlx * Math.Abs(dxt / (x2 - x1)) > 0.05) rlx = 0.05 * Math.Abs((x2 - x1) / dxt);
            if (rlx * Math.Abs(da2) > 1.0) rlx = 1.0 * Math.Abs(1.0 / da2);

            if (Math.Abs(da2) < daeps) break;

            if ((b.Ampl > acrit && b.Ampl + rlx * da2 < acrit) ||
                (b.Ampl < acrit && b.Ampl + rlx * da2 > acrit))
                b.Ampl = acrit;
            else
                b.Ampl = b.Ampl + rlx * da2;
        }

        // free / forced transition tests
        Trfree = b.Ampl >= acrit;
        Trforc = Xiforc > x1 && Xiforc <= x2;
        Tran = Trforc || Trfree;
        Xt = xt;
        if (!Tran) return;

        if (Trfree && Trforc) { Trforc = Xiforc < xt; Trfree = Xiforc >= xt; }

        if (Trforc)
        {
            Xt = Xiforc;
            Xt_A1 = 0; Xt_X1 = 0; Xt_T1 = 0; Xt_D1 = 0; Xt_U1 = 0;
            Xt_X2 = 0; Xt_T2 = 0; Xt_D2 = 0; Xt_U2 = 0; Xt_Ms = 0; Xt_Re = 0; Xt_Xf = 1.0;
            return;
        }

        // free transition: sensitivities of XT
        double xt_x1 = wf1, tt_t1 = wf1, dt_d1 = wf1, ut_u1 = wf1;
        double xt_x2 = wf2, tt_t2 = wf2, dt_d2 = wf2, ut_u2 = wf2;
        double xt_a1 = x1 * wf1_a1 + x2 * wf2_a1, tt_a1 = t1 * wf1_a1 + t2 * wf2_a1, dt_a1 = d1 * wf1_a1 + d2 * wf2_a1, ut_a1 = u1 * wf1_a1 + u2 * wf2_a1;

        xt_x1 = x1 * wf1_x1 + x2 * wf2_x1 + xt_x1;
        double tt_x1 = t1 * wf1_x1 + t2 * wf2_x1, dt_x1 = d1 * wf1_x1 + d2 * wf2_x1, ut_x1 = u1 * wf1_x1 + u2 * wf2_x1;
        xt_x2 = x1 * wf1_x2 + x2 * wf2_x2 + xt_x2;
        double tt_x2 = t1 * wf1_x2 + t2 * wf2_x2, dt_x2 = d1 * wf1_x2 + d2 * wf2_x2, ut_x2 = u1 * wf1_x2 + u2 * wf2_x2;
        double xt_xf = x1 * wf1_xf + x2 * wf2_xf, tt_xf = t1 * wf1_xf + t2 * wf2_xf, dt_xf = d1 * wf1_xf + d2 * wf2_xf, ut_xf = u1 * wf1_xf + u2 * wf2_xf;

        // AX sensitivities wrt (T1 D1 U1 A1 T2 D2 U2 A2 MS RE)
        double ax_t1f = ax_hk1 * a.Hk_T + ax_t1 + ax_rt1 * a.Rt_T + (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_t1;
        double ax_d1f = ax_hk1 * a.Hk_D + (ax_hkt * hkt_dt) * dt_d1;
        double ax_u1f = ax_hk1 * a.Hk_U + ax_rt1 * a.Rt_U + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_u1;
        double ax_a1f = ax_a1
            + (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_a1
            + (ax_hkt * hkt_dt) * dt_a1
            + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_a1;
        double ax_x1f = (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_x1
            + (ax_hkt * hkt_dt) * dt_x1
            + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_x1;

        double ax_t2f = (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_t2;
        double ax_d2f = (ax_hkt * hkt_dt) * dt_d2;
        double ax_u2f = (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_u2;
        double ax_a2f = ax_at * amplt_a2
            + (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_a2
            + (ax_hkt * hkt_dt) * dt_a2
            + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_a2;
        double ax_x2f = (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_x2
            + (ax_hkt * hkt_dt) * dt_x2
            + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_x2;
        double ax_xff = (ax_hkt * hkt_tt + ax_tt + ax_rtt * rtt_tt) * tt_xf
            + (ax_hkt * hkt_dt) * dt_xf
            + (ax_hkt * hkt_ut + ax_rtt * rtt_ut) * ut_xf;
        double ax_msf = ax_hkt * hkt_ms + ax_rtt * rtt_ms + ax_hk1 * a.Hk_Ms + ax_rt1 * a.Rt_Ms;
        double ax_ref = ax_rtt * rtt_re + ax_rt1 * a.Rt_Re;

        // residual sensitivities  RES = AMPL2 - AMPL1 - AX*(X2-X1)
        double z_ax = -(x2 - x1);
        double z_a1 = z_ax * ax_a1f - 1.0;
        double z_t1 = z_ax * ax_t1f, z_d1 = z_ax * ax_d1f, z_u1 = z_ax * ax_u1f, z_x1 = z_ax * ax_x1f + ax;
        double z_a2 = z_ax * ax_a2f + 1.0;
        double z_t2 = z_ax * ax_t2f, z_d2 = z_ax * ax_d2f, z_u2 = z_ax * ax_u2f, z_x2 = z_ax * ax_x2f - ax;
        double z_xf = z_ax * ax_xff, z_ms = z_ax * ax_msf, z_re = z_ax * ax_ref;

        // constrain RES stationary wrt A2
        Xt_A1 = xt_a1 - (xt_a2 / z_a2) * z_a1;
        Xt_T1 = -(xt_a2 / z_a2) * z_t1;
        Xt_D1 = -(xt_a2 / z_a2) * z_d1;
        Xt_U1 = -(xt_a2 / z_a2) * z_u1;
        Xt_X1 = xt_x1 - (xt_a2 / z_a2) * z_x1;
        Xt_T2 = -(xt_a2 / z_a2) * z_t2;
        Xt_D2 = -(xt_a2 / z_a2) * z_d2;
        Xt_U2 = -(xt_a2 / z_a2) * z_u2;
        Xt_X2 = xt_x2 - (xt_a2 / z_a2) * z_x2;
        Xt_Ms = -(xt_a2 / z_a2) * z_ms;
        Xt_Re = -(xt_a2 / z_a2) * z_re;
        Xt_Xf = 0.0;
    }

    /// <summary>Port of TRDIF. Assembles the transition interval's Newton system into
    /// VS1/VS2/VSREZ/VSM/VSR/VSX. Requires Trchek2 to have set Xt and the Xt_*
    /// sensitivities, and S1/S2 to hold the (laminar) "1" and (turbulent) "2" states.</summary>
    public void Trdif()
    {
        BlStation a = S1, b = S2;
        var save1 = a.Clone();
        var save2 = b.Clone();

        double x1 = a.X, x2 = b.X, t1 = a.T, t2 = b.T, d1 = a.D, d2 = b.D, u1 = a.U, u2 = b.U;

        // interpolation weight to the transition point and its sensitivities (from XT)
        double wf2 = (Xt - x1) / (x2 - x1);
        double wf2_xt = 1.0 / (x2 - x1);
        double wf2_a1 = wf2_xt * Xt_A1;
        double wf2_x1 = wf2_xt * Xt_X1 + (wf2 - 1.0) / (x2 - x1);
        double wf2_x2 = wf2_xt * Xt_X2 - wf2 / (x2 - x1);
        double wf2_t1 = wf2_xt * Xt_T1, wf2_t2 = wf2_xt * Xt_T2;
        double wf2_d1 = wf2_xt * Xt_D1, wf2_d2 = wf2_xt * Xt_D2;
        double wf2_u1 = wf2_xt * Xt_U1, wf2_u2 = wf2_xt * Xt_U2;
        double wf2_ms = wf2_xt * Xt_Ms, wf2_re = wf2_xt * Xt_Re, wf2_xf = wf2_xt * Xt_Xf;

        double wf1 = 1.0 - wf2;
        double wf1_a1 = -wf2_a1, wf1_x1 = -wf2_x1, wf1_x2 = -wf2_x2, wf1_t1 = -wf2_t1, wf1_t2 = -wf2_t2;
        double wf1_d1 = -wf2_d1, wf1_d2 = -wf2_d2, wf1_u1 = -wf2_u1, wf1_u2 = -wf2_u2, wf1_ms = -wf2_ms, wf1_re = -wf2_re, wf1_xf = -wf2_xf;

        // interpolated primaries at XT and their full sensitivities
        double tt = t1 * wf1 + t2 * wf2;
        double tt_a1 = t1 * wf1_a1 + t2 * wf2_a1, tt_x1 = t1 * wf1_x1 + t2 * wf2_x1, tt_x2 = t1 * wf1_x2 + t2 * wf2_x2;
        double tt_t1 = t1 * wf1_t1 + t2 * wf2_t1 + wf1, tt_t2 = t1 * wf1_t2 + t2 * wf2_t2 + wf2;
        double tt_d1 = t1 * wf1_d1 + t2 * wf2_d1, tt_d2 = t1 * wf1_d2 + t2 * wf2_d2;
        double tt_u1 = t1 * wf1_u1 + t2 * wf2_u1, tt_u2 = t1 * wf1_u2 + t2 * wf2_u2;
        double tt_ms = t1 * wf1_ms + t2 * wf2_ms, tt_re = t1 * wf1_re + t2 * wf2_re, tt_xf = t1 * wf1_xf + t2 * wf2_xf;

        double dt = d1 * wf1 + d2 * wf2;
        double dt_a1 = d1 * wf1_a1 + d2 * wf2_a1, dt_x1 = d1 * wf1_x1 + d2 * wf2_x1, dt_x2 = d1 * wf1_x2 + d2 * wf2_x2;
        double dt_t1 = d1 * wf1_t1 + d2 * wf2_t1, dt_t2 = d1 * wf1_t2 + d2 * wf2_t2;
        double dt_d1 = d1 * wf1_d1 + d2 * wf2_d1 + wf1, dt_d2 = d1 * wf1_d2 + d2 * wf2_d2 + wf2;
        double dt_u1 = d1 * wf1_u1 + d2 * wf2_u1, dt_u2 = d1 * wf1_u2 + d2 * wf2_u2;
        double dt_ms = d1 * wf1_ms + d2 * wf2_ms, dt_re = d1 * wf1_re + d2 * wf2_re, dt_xf = d1 * wf1_xf + d2 * wf2_xf;

        double ut = u1 * wf1 + u2 * wf2;
        double ut_a1 = u1 * wf1_a1 + u2 * wf2_a1, ut_x1 = u1 * wf1_x1 + u2 * wf2_x1, ut_x2 = u1 * wf1_x2 + u2 * wf2_x2;
        double ut_t1 = u1 * wf1_t1 + u2 * wf2_t1, ut_t2 = u1 * wf1_t2 + u2 * wf2_t2;
        double ut_d1 = u1 * wf1_d1 + u2 * wf2_d1, ut_d2 = u1 * wf1_d2 + u2 * wf2_d2;
        double ut_u1 = u1 * wf1_u1 + u2 * wf2_u1 + wf1, ut_u2 = u1 * wf1_u2 + u2 * wf2_u2 + wf2;
        double ut_ms = u1 * wf1_ms + u2 * wf2_ms, ut_re = u1 * wf1_re + u2 * wf2_re, ut_xf = u1 * wf1_xf + u2 * wf2_xf;

        double xt_a1 = Xt_A1, xt_x1 = Xt_X1, xt_x2 = Xt_X2, xt_t1 = Xt_T1, xt_t2 = Xt_T2;
        double xt_d1 = Xt_D1, xt_d2 = Xt_D2, xt_u1 = Xt_U1, xt_u2 = Xt_U2, xt_ms = Xt_Ms, xt_re = Xt_Re, xt_xf = Xt_Xf;

        // ===== laminar part X1..XT =====
        b.X = Xt; b.T = tt; b.D = dt; b.U = ut; b.Ampl = Env.Amcrit; b.S = 0.0;
        BlSys.Blkin(b, Env);
        BlSys.Blvar(b, Env, P, 1);
        Blmid(1);
        Bldif(1);

        var bl1 = new double[4, 5];
        var bl2 = new double[4, 5];
        var blrez = new double[4];
        var blm = new double[4];
        var blr = new double[4];
        var blx = new double[4];
        // column map: Fortran (K,2..5) -> 0-based [k,1..4]; primary A/S is col 0.
        for (int k = 1; k <= 2; k++) // rows 2,3 (momentum, shape)
        {
            blrez[k] = Vsrez[k];
            blm[k] = Vsm[k] + Vs2[k, 1] * tt_ms + Vs2[k, 2] * dt_ms + Vs2[k, 3] * ut_ms + Vs2[k, 4] * xt_ms;
            blr[k] = Vsr[k] + Vs2[k, 1] * tt_re + Vs2[k, 2] * dt_re + Vs2[k, 3] * ut_re + Vs2[k, 4] * xt_re;
            blx[k] = Vsx[k] + Vs2[k, 1] * tt_xf + Vs2[k, 2] * dt_xf + Vs2[k, 3] * ut_xf + Vs2[k, 4] * xt_xf;

            bl1[k, 0] = Vs1[k, 0] + Vs2[k, 1] * tt_a1 + Vs2[k, 2] * dt_a1 + Vs2[k, 3] * ut_a1 + Vs2[k, 4] * xt_a1;
            bl1[k, 1] = Vs1[k, 1] + Vs2[k, 1] * tt_t1 + Vs2[k, 2] * dt_t1 + Vs2[k, 3] * ut_t1 + Vs2[k, 4] * xt_t1;
            bl1[k, 2] = Vs1[k, 2] + Vs2[k, 1] * tt_d1 + Vs2[k, 2] * dt_d1 + Vs2[k, 3] * ut_d1 + Vs2[k, 4] * xt_d1;
            bl1[k, 3] = Vs1[k, 3] + Vs2[k, 1] * tt_u1 + Vs2[k, 2] * dt_u1 + Vs2[k, 3] * ut_u1 + Vs2[k, 4] * xt_u1;
            bl1[k, 4] = Vs1[k, 4] + Vs2[k, 1] * tt_x1 + Vs2[k, 2] * dt_x1 + Vs2[k, 3] * ut_x1 + Vs2[k, 4] * xt_x1;

            bl2[k, 0] = 0.0;
            bl2[k, 1] = Vs2[k, 1] * tt_t2 + Vs2[k, 2] * dt_t2 + Vs2[k, 3] * ut_t2 + Vs2[k, 4] * xt_t2;
            bl2[k, 2] = Vs2[k, 1] * tt_d2 + Vs2[k, 2] * dt_d2 + Vs2[k, 3] * ut_d2 + Vs2[k, 4] * xt_d2;
            bl2[k, 3] = Vs2[k, 1] * tt_u2 + Vs2[k, 2] * dt_u2 + Vs2[k, 3] * ut_u2 + Vs2[k, 4] * xt_u2;
            bl2[k, 4] = Vs2[k, 1] * tt_x2 + Vs2[k, 2] * dt_x2 + Vs2[k, 3] * ut_x2 + Vs2[k, 4] * xt_x2;
        }

        // ===== turbulent part XT..X2 =====
        BlSys.Blvar(b, Env, P, 2); // recompute "T" secondary as turbulent -> Cq (CQT)
        double ctr = P.Ctrcon * Math.Exp(-P.Ctrcex / (b.Hk - 1.0));
        double ctr_hk2 = ctr * P.Ctrcex / ((b.Hk - 1.0) * (b.Hk - 1.0));

        double st = ctr * b.Cq;
        double st_tt = ctr * b.Cq_T + b.Cq * ctr_hk2 * b.Hk_T;
        double st_dt = ctr * b.Cq_D + b.Cq * ctr_hk2 * b.Hk_D;
        double st_ut = ctr * b.Cq_U + b.Cq * ctr_hk2 * b.Hk_U;
        double st_ms0 = ctr * b.Cq_Ms + b.Cq * ctr_hk2 * b.Hk_Ms;
        double st_re0 = ctr * b.Cq_Re;

        double st_a1 = st_tt * tt_a1 + st_dt * dt_a1 + st_ut * ut_a1;
        double st_x1 = st_tt * tt_x1 + st_dt * dt_x1 + st_ut * ut_x1;
        double st_x2 = st_tt * tt_x2 + st_dt * dt_x2 + st_ut * ut_x2;
        double st_t1 = st_tt * tt_t1 + st_dt * dt_t1 + st_ut * ut_t1;
        double st_t2 = st_tt * tt_t2 + st_dt * dt_t2 + st_ut * ut_t2;
        double st_d1 = st_tt * tt_d1 + st_dt * dt_d1 + st_ut * ut_d1;
        double st_d2 = st_tt * tt_d2 + st_dt * dt_d2 + st_ut * ut_d2;
        double st_u1 = st_tt * tt_u1 + st_dt * dt_u1 + st_ut * ut_u1;
        double st_u2 = st_tt * tt_u2 + st_dt * dt_u2 + st_ut * ut_u2;
        double st_ms = st_tt * tt_ms + st_dt * dt_ms + st_ut * ut_ms + st_ms0;
        double st_re = st_tt * tt_re + st_dt * dt_re + st_ut * ut_re + st_re0;
        double st_xf = st_tt * tt_xf + st_dt * dt_xf + st_ut * ut_xf;

        b.Ampl = 0.0; b.S = st;
        BlSys.Blvar(b, Env, P, 2); // recompute with proper Ctau

        a.CopyFrom(b);        // "1" <- "T"
        b.CopyFrom(save2);    // "2" <- saved turbulent
        Blmid(2);
        Bldif(2);

        var bt1 = new double[4, 5];
        var bt2 = new double[4, 5];
        var btrez = new double[4];
        var btm = new double[4];
        var btr = new double[4];
        var btx = new double[4];
        for (int k = 0; k <= 2; k++) // rows 1,2,3
        {
            btrez[k] = Vsrez[k];
            btm[k] = Vsm[k] + Vs1[k, 0] * st_ms + Vs1[k, 1] * tt_ms + Vs1[k, 2] * dt_ms + Vs1[k, 3] * ut_ms + Vs1[k, 4] * xt_ms;
            btr[k] = Vsr[k] + Vs1[k, 0] * st_re + Vs1[k, 1] * tt_re + Vs1[k, 2] * dt_re + Vs1[k, 3] * ut_re + Vs1[k, 4] * xt_re;
            btx[k] = Vsx[k] + Vs1[k, 0] * st_xf + Vs1[k, 1] * tt_xf + Vs1[k, 2] * dt_xf + Vs1[k, 3] * ut_xf + Vs1[k, 4] * xt_xf;

            bt1[k, 0] = Vs1[k, 0] * st_a1 + Vs1[k, 1] * tt_a1 + Vs1[k, 2] * dt_a1 + Vs1[k, 3] * ut_a1 + Vs1[k, 4] * xt_a1;
            bt1[k, 1] = Vs1[k, 0] * st_t1 + Vs1[k, 1] * tt_t1 + Vs1[k, 2] * dt_t1 + Vs1[k, 3] * ut_t1 + Vs1[k, 4] * xt_t1;
            bt1[k, 2] = Vs1[k, 0] * st_d1 + Vs1[k, 1] * tt_d1 + Vs1[k, 2] * dt_d1 + Vs1[k, 3] * ut_d1 + Vs1[k, 4] * xt_d1;
            bt1[k, 3] = Vs1[k, 0] * st_u1 + Vs1[k, 1] * tt_u1 + Vs1[k, 2] * dt_u1 + Vs1[k, 3] * ut_u1 + Vs1[k, 4] * xt_u1;
            bt1[k, 4] = Vs1[k, 0] * st_x1 + Vs1[k, 1] * tt_x1 + Vs1[k, 2] * dt_x1 + Vs1[k, 3] * ut_x1 + Vs1[k, 4] * xt_x1;

            bt2[k, 0] = Vs2[k, 0];
            bt2[k, 1] = Vs2[k, 1] + Vs1[k, 0] * st_t2 + Vs1[k, 1] * tt_t2 + Vs1[k, 2] * dt_t2 + Vs1[k, 3] * ut_t2 + Vs1[k, 4] * xt_t2;
            bt2[k, 2] = Vs2[k, 2] + Vs1[k, 0] * st_d2 + Vs1[k, 1] * tt_d2 + Vs1[k, 2] * dt_d2 + Vs1[k, 3] * ut_d2 + Vs1[k, 4] * xt_d2;
            bt2[k, 3] = Vs2[k, 3] + Vs1[k, 0] * st_u2 + Vs1[k, 1] * tt_u2 + Vs1[k, 2] * dt_u2 + Vs1[k, 3] * ut_u2 + Vs1[k, 4] * xt_u2;
            bt2[k, 4] = Vs2[k, 4] + Vs1[k, 0] * st_x2 + Vs1[k, 1] * tt_x2 + Vs1[k, 2] * dt_x2 + Vs1[k, 3] * ut_x2 + Vs1[k, 4] * xt_x2;
        }

        // ===== sum laminar + turbulent =====
        Vsrez[0] = btrez[0]; Vsrez[1] = blrez[1] + btrez[1]; Vsrez[2] = blrez[2] + btrez[2];
        Vsm[0] = btm[0]; Vsm[1] = blm[1] + btm[1]; Vsm[2] = blm[2] + btm[2];
        Vsr[0] = btr[0]; Vsr[1] = blr[1] + btr[1]; Vsr[2] = blr[2] + btr[2];
        Vsx[0] = btx[0]; Vsx[1] = blx[1] + btx[1]; Vsx[2] = blx[2] + btx[2];
        for (int l = 0; l < 5; l++)
        {
            Vs1[0, l] = bt1[0, l]; Vs2[0, l] = bt2[0, l];
            Vs1[1, l] = bl1[1, l] + bt1[1, l]; Vs2[1, l] = bl2[1, l] + bt2[1, l];
            Vs1[2, l] = bl1[2, l] + bt1[2, l]; Vs2[2, l] = bl2[2, l] + bt2[2, l];
        }

        a.CopyFrom(save1); // restore "1" (S2 already = save2)
    }
}
