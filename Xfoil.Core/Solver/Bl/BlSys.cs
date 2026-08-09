// Port of xblsys.f BLKIN and BLVAR: given a station's primary variables (U,T,D,S),
// compute all secondary variables (M, R, V, H, Hk, Hs, Hc, Rt, Cf, Di, Us, Cq, De)
// and their sensitivities, using the BlClosure correlations. This is the per-station
// physics the BL difference equations (BLDIF) are built from. Faithful to the Fortran;
// see BlStation for the field-name mapping (HK2_U2 -> Hk_U, etc.).

namespace Xfoil.Core.Solver.Bl;

public static class BlSys
{
    /// <summary>Port of BLKIN: turbulence-independent secondary variables (edge Mach,
    /// density, viscosity, H, Hk, Rt) from the primary variables.</summary>
    public static void Blkin(BlStation s, BlEnv e)
    {
        double gm1 = e.Gm1bl, hstinv = e.Hstinv, hstinvMs = e.HstinvMs;

        // edge Mach^2
        s.M = s.U * s.U * hstinv / (gm1 * (1.0 - 0.5 * s.U * s.U * hstinv));
        double tr = 1.0 + 0.5 * gm1 * s.M;
        s.M_U = 2.0 * s.M * tr / s.U;
        s.M_Ms = s.U * s.U * tr / (gm1 * (1.0 - 0.5 * s.U * s.U * hstinv)) * hstinvMs;

        // edge static density (isentropic)
        s.R = e.Rstbl * Math.Pow(tr, -1.0 / gm1);
        s.R_U = -s.R / tr * 0.5 * s.M_U;
        s.R_Ms = -s.R / tr * 0.5 * s.M_Ms + e.RstblMs * Math.Pow(tr, -1.0 / gm1);

        // shape parameter
        s.H = s.D / s.T;
        s.H_D = 1.0 / s.T;
        s.H_T = -s.H / s.T;

        // edge static/stagnation enthalpy ratio and viscosity
        double herat = 1.0 - 0.5 * s.U * s.U * hstinv;
        double he_u = -s.U * hstinv;
        double he_ms = -0.5 * s.U * s.U * hstinvMs;

        s.V = Math.Sqrt(herat * herat * herat) * (1.0 + e.Hvrat) / (herat + e.Hvrat) / e.Reybl;
        double v_he = s.V * (1.5 / herat - 1.0 / (herat + e.Hvrat));
        s.V_U = v_he * he_u;
        s.V_Ms = -s.V / e.Reybl * e.ReyblMs + v_he * he_ms;
        s.V_Re = -s.V / e.Reybl * e.ReyblRe;

        // kinematic shape parameter
        BlClosure.Hkin(s.H, s.M, out s.Hk, out double hk_h, out double hk_m);
        s.Hk_U = hk_m * s.M_U;
        s.Hk_T = hk_h * s.H_T;
        s.Hk_D = hk_h * s.H_D;
        s.Hk_Ms = hk_m * s.M_Ms;

        // momentum-thickness Reynolds number
        s.Rt = s.R * s.U * s.T / s.V;
        s.Rt_U = s.Rt * (1.0 / s.U + s.R_U / s.R - s.V_U / s.V);
        s.Rt_T = s.Rt / s.T;
        s.Rt_Ms = s.Rt * (s.R_Ms / s.R - s.V_Ms / s.V);
        s.Rt_Re = s.Rt * (-s.V_Re / s.V);
    }

    /// <summary>Port of BLVAR: all remaining secondary variables. ityp = 1 laminar,
    /// 2 turbulent, 3 turbulent wake. BLKIN must have been called first.</summary>
    public static void Blvar(BlStation s, BlEnv e, BlParams p, int ityp)
    {
        double gbcon = p.Gbcon, gccon = p.Gccon, ctcon = p.Ctcon;

        s.Hk = ityp == 3 ? Math.Max(s.Hk, 1.00005) : Math.Max(s.Hk, 1.05000);

        // density thickness shape parameter H**
        BlClosure.Hct(s.Hk, s.M, out s.Hc, out double hc_hk, out double hc_m);
        s.Hc_U = hc_hk * s.Hk_U + hc_m * s.M_U;
        s.Hc_T = hc_hk * s.Hk_T;
        s.Hc_D = hc_hk * s.Hk_D;
        s.Hc_Ms = hc_hk * s.Hk_Ms + hc_m * s.M_Ms;

        // KE thickness shape parameter Hs
        double hs_hk, hs_rt, hs_m;
        if (ityp == 1) BlClosure.Hsl(s.Hk, s.Rt, s.M, out s.Hs, out hs_hk, out hs_rt, out hs_m);
        else BlClosure.Hst(s.Hk, s.Rt, s.M, out s.Hs, out hs_hk, out hs_rt, out hs_m);
        s.Hs_U = hs_hk * s.Hk_U + hs_rt * s.Rt_U + hs_m * s.M_U;
        s.Hs_T = hs_hk * s.Hk_T + hs_rt * s.Rt_T;
        s.Hs_D = hs_hk * s.Hk_D;
        s.Hs_Ms = hs_hk * s.Hk_Ms + hs_rt * s.Rt_Ms + hs_m * s.M_Ms;
        s.Hs_Re = hs_rt * s.Rt_Re;

        // normalized slip velocity Us
        s.Us = 0.5 * s.Hs * (1.0 - (s.Hk - 1.0) / (gbcon * s.H));
        double us_hs = 0.5 * (1.0 - (s.Hk - 1.0) / (gbcon * s.H));
        double us_hk = 0.5 * s.Hs * (-1.0 / (gbcon * s.H));
        double us_h = 0.5 * s.Hs * (s.Hk - 1.0) / (gbcon * s.H * s.H);

        s.Us_U = us_hs * s.Hs_U + us_hk * s.Hk_U;
        s.Us_T = us_hs * s.Hs_T + us_hk * s.Hk_T + us_h * s.H_T;
        s.Us_D = us_hs * s.Hs_D + us_hk * s.Hk_D + us_h * s.H_D;
        s.Us_Ms = us_hs * s.Hs_Ms + us_hk * s.Hk_Ms;
        s.Us_Re = us_hs * s.Hs_Re;

        if (ityp <= 2 && s.Us > 0.95)
        {
            s.Us = 0.98; s.Us_U = 0; s.Us_T = 0; s.Us_D = 0; s.Us_Ms = 0; s.Us_Re = 0;
        }
        if (ityp == 3 && s.Us > 0.99995)
        {
            s.Us = 0.99995; s.Us_U = 0; s.Us_T = 0; s.Us_D = 0; s.Us_Ms = 0; s.Us_Re = 0;
        }

        // equilibrium shear coefficient (Ctau)eq^1/2
        double hkc = s.Hk - 1.0;
        double hkc_hk = 1.0, hkc_rt = 0.0;
        if (ityp == 2)
        {
            double gcc = gccon;
            hkc = s.Hk - 1.0 - gcc / s.Rt;
            hkc_hk = 1.0;
            hkc_rt = gcc / (s.Rt * s.Rt);
            if (hkc < 0.01) { hkc = 0.01; hkc_hk = 0.0; hkc_rt = 0.0; }
        }

        double hkb = s.Hk - 1.0;
        double usb = 1.0 - s.Us;
        s.Cq = Math.Sqrt(ctcon * s.Hs * hkb * hkc * hkc / (usb * s.H * s.Hk * s.Hk));
        double cq_hs = ctcon * hkb * hkc * hkc / (usb * s.H * s.Hk * s.Hk) * 0.5 / s.Cq;
        double cq_us = ctcon * s.Hs * hkb * hkc * hkc / (usb * s.H * s.Hk * s.Hk) / usb * 0.5 / s.Cq;
        double cq_hk = ctcon * s.Hs * hkc * hkc / (usb * s.H * s.Hk * s.Hk) * 0.5 / s.Cq
                     - ctcon * s.Hs * hkb * hkc * hkc / (usb * s.H * s.Hk * s.Hk * s.Hk) * 2.0 * 0.5 / s.Cq
                     + ctcon * s.Hs * hkb * hkc / (usb * s.H * s.Hk * s.Hk) * 2.0 * 0.5 / s.Cq * hkc_hk;
        double cq_rt = ctcon * s.Hs * hkb * hkc / (usb * s.H * s.Hk * s.Hk) * 2.0 * 0.5 / s.Cq * hkc_rt;
        double cq_h = -ctcon * s.Hs * hkb * hkc * hkc / (usb * s.H * s.Hk * s.Hk) / s.H * 0.5 / s.Cq;

        s.Cq_U = cq_hs * s.Hs_U + cq_us * s.Us_U + cq_hk * s.Hk_U;
        s.Cq_T = cq_hs * s.Hs_T + cq_us * s.Us_T + cq_hk * s.Hk_T;
        s.Cq_D = cq_hs * s.Hs_D + cq_us * s.Us_D + cq_hk * s.Hk_D;
        s.Cq_Ms = cq_hs * s.Hs_Ms + cq_us * s.Us_Ms + cq_hk * s.Hk_Ms;
        s.Cq_Re = cq_hs * s.Hs_Re + cq_us * s.Us_Re;

        s.Cq_U = s.Cq_U + cq_rt * s.Rt_U;
        s.Cq_T = s.Cq_T + cq_h * s.H_T + cq_rt * s.Rt_T;
        s.Cq_D = s.Cq_D + cq_h * s.H_D;
        s.Cq_Ms = s.Cq_Ms + cq_rt * s.Rt_Ms;
        s.Cq_Re = s.Cq_Re + cq_rt * s.Rt_Re;

        // skin friction coefficient
        double cf_hk, cf_rt, cf_m;
        if (ityp == 3)
        {
            s.Cf = 0; cf_hk = 0; cf_rt = 0; cf_m = 0;
        }
        else if (ityp == 1)
        {
            BlClosure.Cfl(s.Hk, s.Rt, s.M, out s.Cf, out cf_hk, out cf_rt, out cf_m);
        }
        else
        {
            BlClosure.Cft(s.Hk, s.Rt, s.M, out s.Cf, out cf_hk, out cf_rt, out cf_m);
            BlClosure.Cfl(s.Hk, s.Rt, s.M, out double cf2l, out double cf2l_hk, out double cf2l_rt, out double cf2l_m);
            if (cf2l > s.Cf) { s.Cf = cf2l; cf_hk = cf2l_hk; cf_rt = cf2l_rt; cf_m = cf2l_m; }
        }
        s.Cf_U = cf_hk * s.Hk_U + cf_rt * s.Rt_U + cf_m * s.M_U;
        s.Cf_T = cf_hk * s.Hk_T + cf_rt * s.Rt_T;
        s.Cf_D = cf_hk * s.Hk_D;
        s.Cf_Ms = cf_hk * s.Hk_Ms + cf_rt * s.Rt_Ms + cf_m * s.M_Ms;
        s.Cf_Re = cf_rt * s.Rt_Re;

        // dissipation function 2 CD/H*
        if (ityp == 1)
        {
            BlClosure.Dil(s.Hk, s.Rt, out s.Di, out double di_hk, out double di_rt);
            s.Di_U = di_hk * s.Hk_U + di_rt * s.Rt_U;
            s.Di_T = di_hk * s.Hk_T + di_rt * s.Rt_T;
            s.Di_D = di_hk * s.Hk_D;
            s.Di_S = 0;
            s.Di_Ms = di_hk * s.Hk_Ms + di_rt * s.Rt_Ms;
            s.Di_Re = di_rt * s.Rt_Re;
        }
        else if (ityp == 2)
        {
            // turbulent wall contribution
            BlClosure.Cft(s.Hk, s.Rt, s.M, out double cf2t, out double cf2t_hk, out double cf2t_rt, out double cf2t_m);
            double cf2t_u = cf2t_hk * s.Hk_U + cf2t_rt * s.Rt_U + cf2t_m * s.M_U;
            double cf2t_t = cf2t_hk * s.Hk_T + cf2t_rt * s.Rt_T;
            double cf2t_d = cf2t_hk * s.Hk_D;
            double cf2t_ms = cf2t_hk * s.Hk_Ms + cf2t_rt * s.Rt_Ms + cf2t_m * s.M_Ms;
            double cf2t_re = cf2t_rt * s.Rt_Re;

            s.Di = (0.5 * cf2t * s.Us) * 2.0 / s.Hs;
            double di_hs = -(0.5 * cf2t * s.Us) * 2.0 / (s.Hs * s.Hs);
            double di_us = (0.5 * cf2t) * 2.0 / s.Hs;
            double di_cf2t = (0.5 * s.Us) * 2.0 / s.Hs;

            s.Di_S = 0;
            s.Di_U = di_hs * s.Hs_U + di_us * s.Us_U + di_cf2t * cf2t_u;
            s.Di_T = di_hs * s.Hs_T + di_us * s.Us_T + di_cf2t * cf2t_t;
            s.Di_D = di_hs * s.Hs_D + di_us * s.Us_D + di_cf2t * cf2t_d;
            s.Di_Ms = di_hs * s.Hs_Ms + di_us * s.Us_Ms + di_cf2t * cf2t_ms;
            s.Di_Re = di_hs * s.Hs_Re + di_us * s.Us_Re + di_cf2t * cf2t_re;

            // low-Hk wall-dissipation correction
            double grt = Math.Log(s.Rt);
            double hmin = 1.0 + 2.1 / grt;
            double hm_rt = -(2.1 / (grt * grt)) / s.Rt;
            double fl = (s.Hk - 1.0) / (hmin - 1.0);
            double fl_hk = 1.0 / (hmin - 1.0);
            double fl_rt = (-fl / (hmin - 1.0)) * hm_rt;
            double tfl = Math.Tanh(fl);
            double dfac = 0.5 + 0.5 * tfl;
            double df_fl = 0.5 * (1.0 - tfl * tfl);
            double df_hk = df_fl * fl_hk;
            double df_rt = df_fl * fl_rt;

            s.Di_S = s.Di_S * dfac;
            s.Di_U = s.Di_U * dfac + s.Di * (df_hk * s.Hk_U + df_rt * s.Rt_U);
            s.Di_T = s.Di_T * dfac + s.Di * (df_hk * s.Hk_T + df_rt * s.Rt_T);
            s.Di_D = s.Di_D * dfac + s.Di * (df_hk * s.Hk_D);
            s.Di_Ms = s.Di_Ms * dfac + s.Di * (df_hk * s.Hk_Ms + df_rt * s.Rt_Ms);
            s.Di_Re = s.Di_Re * dfac + s.Di * (df_rt * s.Rt_Re);
            s.Di = s.Di * dfac;
        }
        else
        {
            s.Di = 0; s.Di_S = 0; s.Di_U = 0; s.Di_T = 0; s.Di_D = 0; s.Di_Ms = 0; s.Di_Re = 0;
        }

        // turbulent outer-layer contribution
        if (ityp != 1)
        {
            double dd = s.S * s.S * (0.995 - s.Us) * 2.0 / s.Hs;
            double dd_hs = -s.S * s.S * (0.995 - s.Us) * 2.0 / (s.Hs * s.Hs);
            double dd_us = -s.S * s.S * 2.0 / s.Hs;
            double dd_s = s.S * 2.0 * (0.995 - s.Us) * 2.0 / s.Hs;

            s.Di = s.Di + dd;
            s.Di_S = dd_s;
            s.Di_U = s.Di_U + dd_hs * s.Hs_U + dd_us * s.Us_U;
            s.Di_T = s.Di_T + dd_hs * s.Hs_T + dd_us * s.Us_T;
            s.Di_D = s.Di_D + dd_hs * s.Hs_D + dd_us * s.Us_D;
            s.Di_Ms = s.Di_Ms + dd_hs * s.Hs_Ms + dd_us * s.Us_Ms;
            s.Di_Re = s.Di_Re + dd_hs * s.Hs_Re + dd_us * s.Us_Re;

            // laminar stress contribution to outer-layer CD
            dd = 0.15 * (0.995 - s.Us) * (0.995 - s.Us) / s.Rt * 2.0 / s.Hs;
            dd_us = -0.15 * (0.995 - s.Us) * 2.0 / s.Rt * 2.0 / s.Hs;
            dd_hs = -dd / s.Hs;
            double dd_rt = -dd / s.Rt;

            s.Di = s.Di + dd;
            s.Di_U = s.Di_U + dd_hs * s.Hs_U + dd_us * s.Us_U + dd_rt * s.Rt_U;
            s.Di_T = s.Di_T + dd_hs * s.Hs_T + dd_us * s.Us_T + dd_rt * s.Rt_T;
            s.Di_D = s.Di_D + dd_hs * s.Hs_D + dd_us * s.Us_D;
            s.Di_Ms = s.Di_Ms + dd_hs * s.Hs_Ms + dd_us * s.Us_Ms + dd_rt * s.Rt_Ms;
            s.Di_Re = s.Di_Re + dd_hs * s.Hs_Re + dd_us * s.Us_Re + dd_rt * s.Rt_Re;
        }

        // fall back to laminar CD if it exceeds turbulent (very low Rtheta)
        if (ityp == 2)
        {
            BlClosure.Dil(s.Hk, s.Rt, out double di2l, out double di2l_hk, out double di2l_rt);
            if (di2l > s.Di)
            {
                s.Di = di2l; s.Di_S = 0;
                s.Di_U = di2l_hk * s.Hk_U + di2l_rt * s.Rt_U;
                s.Di_T = di2l_hk * s.Hk_T + di2l_rt * s.Rt_T;
                s.Di_D = di2l_hk * s.Hk_D;
                s.Di_Ms = di2l_hk * s.Hk_Ms + di2l_rt * s.Rt_Ms;
                s.Di_Re = di2l_rt * s.Rt_Re;
            }
        }
        if (ityp == 3)
        {
            BlClosure.Dilw(s.Hk, s.Rt, out double di2l, out double di2l_hk, out double di2l_rt);
            if (di2l > s.Di)
            {
                s.Di = di2l; s.Di_S = 0;
                s.Di_U = di2l_hk * s.Hk_U + di2l_rt * s.Rt_U;
                s.Di_T = di2l_hk * s.Hk_T + di2l_rt * s.Rt_T;
                s.Di_D = di2l_hk * s.Hk_D;
                s.Di_Ms = di2l_hk * s.Hk_Ms + di2l_rt * s.Rt_Ms;
                s.Di_Re = di2l_rt * s.Rt_Re;
            }
            // double dissipation for the wake (two halves)
            s.Di *= 2.0; s.Di_S *= 2.0; s.Di_U *= 2.0; s.Di_T *= 2.0;
            s.Di_D *= 2.0; s.Di_Ms *= 2.0; s.Di_Re *= 2.0;
        }

        // BL thickness Delta (Green's correlation), capped at HDMAX*T
        s.De = (3.15 + 1.72 / (s.Hk - 1.0)) * s.T + s.D;
        double de_hk = (-1.72 / ((s.Hk - 1.0) * (s.Hk - 1.0))) * s.T;
        s.De_U = de_hk * s.Hk_U;
        s.De_T = de_hk * s.Hk_T + (3.15 + 1.72 / (s.Hk - 1.0));
        s.De_D = de_hk * s.Hk_D + 1.0;
        s.De_Ms = de_hk * s.Hk_Ms;

        const double hdmax = 12.0;
        if (s.De > hdmax * s.T)
        {
            s.De = hdmax * s.T;
            s.De_U = 0; s.De_T = hdmax; s.De_D = 0; s.De_Ms = 0;
        }
    }
}
