// Port of the boundary-layer closure relations from xblsys.f: the algebraic
// correlations (with analytic sensitivities) that give the kinematic shape
// parameter, skin friction, energy shape parameter, dissipation, density shape
// parameter, and the envelope e^N amplification rate. These are pure functions --
// no shared BL state -- and form the foundation the marching/Newton BL solver
// (SETBL/BLVAR/BLDIF, still to be ported) is built on.
//
// Each routine returns its value plus partial derivatives with respect to its
// inputs, matching the Fortran out-argument convention. XFOIL's DP builds use
// double precision, matched here. Fortran subroutine names are kept in the summaries.

namespace Xfoil.Core.Solver;

public static class BlClosure
{
    /// <summary>Port of HKIN: kinematic shape parameter Hk from H and edge Mach^2
    /// (Whitfield). Returns hk and its sensitivities to h and msq.</summary>
    public static void Hkin(double h, double msq, out double hk, out double hk_h, out double hk_msq)
    {
        hk = (h - 0.29 * msq) / (1.0 + 0.113 * msq);
        hk_h = 1.0 / (1.0 + 0.113 * msq);
        hk_msq = (-0.29 - 0.113 * hk) / (1.0 + 0.113 * msq);
    }

    /// <summary>Port of DIL: laminar dissipation function (2 CD/H*) from Falkner-Skan,
    /// as a function of Hk and momentum-thickness Reynolds number Rt.</summary>
    public static void Dil(double hk, double rt, out double di, out double di_hk, out double di_rt)
    {
        if (hk < 4.0)
        {
            di = (0.00205 * Math.Pow(4.0 - hk, 5.5) + 0.207) / rt;
            di_hk = (-0.00205 * 5.5 * Math.Pow(4.0 - hk, 4.5)) / rt;
        }
        else
        {
            double hkb = hk - 4.0;
            double den = 1.0 + 0.02 * hkb * hkb;
            di = (-0.0016 * hkb * hkb / den + 0.207) / rt;
            di_hk = (-0.0016 * 2.0 * hkb * (1.0 / den - 0.02 * hkb * hkb / (den * den))) / rt;
        }
        di_rt = -di / rt;
    }

    /// <summary>Port of DILW: laminar wake dissipation function (2 CD/H*).
    /// NOTE: XFOIL's RCD_HK term below carries a sign that does not equal the exact
    /// d(RCD)/dHk (the leading term should be +, not -). This is reproduced verbatim
    /// for XFOIL parity -- it only affects the Newton Jacobian (convergence speed) for
    /// the minor laminar-wake dissipation term, not the converged solution.</summary>
    public static void Dilw(double hk, double rt, out double di, out double di_hk, out double di_rt)
    {
        Hsl(hk, rt, 0.0, out double hs, out double hs_hk, out double hs_rt, out _);
        double rcd = 1.10 * (1.0 - 1.0 / hk) * (1.0 - 1.0 / hk) / hk;
        double rcd_hk = -1.10 * (1.0 - 1.0 / hk) * 2.0 / (hk * hk * hk) - rcd / hk;

        di = 2.0 * rcd / (hs * rt);
        di_hk = 2.0 * rcd_hk / (hs * rt) - (di / hs) * hs_hk;
        di_rt = -di / rt - (di / hs) * hs_rt;
    }

    /// <summary>Port of HSL: laminar kinetic-energy shape parameter Hs correlation.</summary>
    public static void Hsl(double hk, double rt, double msq, out double hs, out double hs_hk, out double hs_rt, out double hs_msq)
    {
        if (hk < 4.35)
        {
            double tmp = hk - 4.35;
            hs = 0.0111 * tmp * tmp / (hk + 1.0)
               - 0.0278 * tmp * tmp * tmp / (hk + 1.0) + 1.528
               - 0.0002 * (tmp * hk) * (tmp * hk);
            hs_hk = 0.0111 * (2.0 * tmp - tmp * tmp / (hk + 1.0)) / (hk + 1.0)
                  - 0.0278 * (3.0 * tmp * tmp - tmp * tmp * tmp / (hk + 1.0)) / (hk + 1.0)
                  - 0.0002 * 2.0 * tmp * hk * (tmp + hk);
        }
        else
        {
            hs = 0.015 * (hk - 4.35) * (hk - 4.35) / hk + 1.528;
            hs_hk = 0.015 * 2.0 * (hk - 4.35) / hk - 0.015 * (hk - 4.35) * (hk - 4.35) / (hk * hk);
        }
        hs_rt = 0.0;
        hs_msq = 0.0;
    }

    /// <summary>Port of CFL: laminar skin-friction function Cf from Falkner-Skan.</summary>
    public static void Cfl(double hk, double rt, double msq, out double cf, out double cf_hk, out double cf_rt, out double cf_msq)
    {
        if (hk < 5.5)
        {
            double tmp = Math.Pow(5.5 - hk, 3) / (hk + 1.0);
            cf = (0.0727 * tmp - 0.07) / rt;
            cf_hk = (-0.0727 * tmp * 3.0 / (5.5 - hk) - 0.0727 * tmp / (hk + 1.0)) / rt;
        }
        else
        {
            double tmp = 1.0 - 1.0 / (hk - 4.5);
            cf = (0.015 * tmp * tmp - 0.07) / rt;
            cf_hk = (0.015 * tmp * 2.0 / ((hk - 4.5) * (hk - 4.5))) / rt;
        }
        cf_rt = -cf / rt;
        cf_msq = 0.0;
    }

    /// <summary>Port of DIT: turbulent dissipation function (2 CD/H*) from Hs, slip
    /// velocity Us, Cf, and shear coefficient St.</summary>
    public static void Dit(double hs, double us, double cf, double st,
        out double di, out double di_hs, out double di_us, out double di_cf, out double di_st)
    {
        di = (0.5 * cf * us + st * st * (1.0 - us)) * 2.0 / hs;
        di_hs = -(0.5 * cf * us + st * st * (1.0 - us)) * 2.0 / (hs * hs);
        di_us = (0.5 * cf - st * st) * 2.0 / hs;
        di_cf = (0.5 * us) * 2.0 / hs;
        di_st = (2.0 * st * (1.0 - us)) * 2.0 / hs;
    }

    /// <summary>Port of HST: turbulent kinetic-energy shape parameter Hs correlation,
    /// with the limited Rtheta dependence below Rt=200 and Whitfield's compressibility
    /// correction.</summary>
    public static void Hst(double hk, double rt, double msq, out double hs, out double hs_hk, out double hs_rt, out double hs_msq)
    {
        const double hsmin = 1.500, dhsinf = 0.015;

        double ho, ho_rt;
        if (rt > 400.0) { ho = 3.0 + 400.0 / rt; ho_rt = -400.0 / (rt * rt); }
        else { ho = 4.0; ho_rt = 0.0; }

        double rtz, rtz_rt;
        if (rt > 200.0) { rtz = rt; rtz_rt = 1.0; }
        else { rtz = 200.0; rtz_rt = 0.0; }

        if (hk < ho)
        {
            // attached branch (new arctan(y+)+Schlichting correlation)
            double hr = (ho - hk) / (ho - 1.0);
            double hr_hk = -1.0 / (ho - 1.0);
            double hr_rt = (1.0 - hr) / (ho - 1.0) * ho_rt;
            hs = (2.0 - hsmin - 4.0 / rtz) * hr * hr * 1.5 / (hk + 0.5) + hsmin + 4.0 / rtz;
            hs_hk = -(2.0 - hsmin - 4.0 / rtz) * hr * hr * 1.5 / ((hk + 0.5) * (hk + 0.5))
                  + (2.0 - hsmin - 4.0 / rtz) * hr * 2.0 * 1.5 / (hk + 0.5) * hr_hk;
            hs_rt = (2.0 - hsmin - 4.0 / rtz) * hr * 2.0 * 1.5 / (hk + 0.5) * hr_rt
                  + (hr * hr * 1.5 / (hk + 0.5) - 1.0) * 4.0 / (rtz * rtz) * rtz_rt;
        }
        else
        {
            // separated branch
            double grt = Math.Log(rtz);
            double hdif = hk - ho;
            double rtmp = hk - ho + 4.0 / grt;
            double htmp = 0.007 * grt / (rtmp * rtmp) + dhsinf / hk;
            double htmp_hk = -0.014 * grt / (rtmp * rtmp * rtmp) - dhsinf / (hk * hk);
            double htmp_rt = -0.014 * grt / (rtmp * rtmp * rtmp) * (-ho_rt - 4.0 / (grt * grt) / rtz * rtz_rt)
                           + 0.007 / (rtmp * rtmp) / rtz * rtz_rt;
            hs = hdif * hdif * htmp + hsmin + 4.0 / rtz;
            hs_hk = hdif * 2.0 * htmp + hdif * hdif * htmp_hk;
            hs_rt = hdif * hdif * htmp_rt - 4.0 / (rtz * rtz) * rtz_rt + hdif * 2.0 * htmp * (-ho_rt);
        }

        // Whitfield's minor additional compressibility correction
        double fm = 1.0 + 0.014 * msq;
        hs = (hs + 0.028 * msq) / fm;
        hs_hk /= fm;
        hs_rt /= fm;
        hs_msq = 0.028 / fm - 0.014 * hs / fm;
    }

    /// <summary>Port of CFT: turbulent skin-friction function Cf (Coles).</summary>
    public static void Cft(double hk, double rt, double msq, out double cf, out double cf_hk, out double cf_rt, out double cf_msq)
    {
        const double gam = 1.4;
        double gm1 = gam - 1.0;
        double fc = Math.Sqrt(1.0 + 0.5 * gm1 * msq);
        double grt = Math.Log(rt / fc);
        grt = Math.Max(grt, 3.0);

        double gex = -1.74 - 0.31 * hk;
        double arg = Math.Max(-20.0, -1.33 * hk);
        double thk = Math.Tanh(4.0 - hk / 0.875);

        double cfo = 0.3 * Math.Exp(arg) * Math.Pow(grt / 2.3026, gex);
        cf = (cfo + 1.1e-4 * (thk - 1.0)) / fc;
        cf_hk = (-1.33 * cfo - 0.31 * Math.Log(grt / 2.3026) * cfo
                 - 1.1e-4 * (1.0 - thk * thk) / 0.875) / fc;
        cf_rt = gex * cfo / (fc * grt) / rt;
        cf_msq = gex * cfo / (fc * grt) * (-0.25 * gm1 / (fc * fc)) - 0.25 * gm1 * cf / (fc * fc);
    }

    /// <summary>Port of HCT: density shape parameter Hc from Hk and edge Mach^2
    /// (Whitfield).</summary>
    public static void Hct(double hk, double msq, out double hc, out double hc_hk, out double hc_msq)
    {
        hc = msq * (0.064 / (hk - 0.8) + 0.251);
        hc_hk = msq * (-0.064 / ((hk - 0.8) * (hk - 0.8)));
        hc_msq = 0.064 / (hk - 0.8) + 0.251;
    }

    /// <summary>Port of DAMPL: envelope e^N spatial amplification rate AX = dN/dx, from
    /// the kinematic shape parameter Hk, momentum thickness Th, and Rt. Returns zero
    /// below the critical Rtheta (with a smooth cubic ramp turn-on), so N(x) can be
    /// integrated from the leading edge; transition is where N reaches Ncrit.</summary>
    public static void Dampl(double hk, double th, double rt, out double ax, out double ax_hk, out double ax_th, out double ax_rt)
    {
        const double dgr = 0.08;

        double hmi = 1.0 / (hk - 1.0);
        double hmi_hk = -hmi * hmi;

        // log10(critical Rth) - H correlation (Falkner-Skan)
        double aa = 2.492 * Math.Pow(hmi, 0.43);
        double aa_hk = (aa / hmi) * 0.43 * hmi_hk;
        double bb = Math.Tanh(14.0 * hmi - 9.24);
        double bb_hk = (1.0 - bb * bb) * 14.0 * hmi_hk;
        double grcrit = aa + 0.7 * (bb + 1.0);
        double grc_hk = aa_hk + 0.7 * bb_hk;

        double gr = Math.Log10(rt);
        double gr_rt = 1.0 / (2.3025851 * rt);

        if (gr < grcrit - dgr)
        {
            ax = 0.0; ax_hk = 0.0; ax_th = 0.0; ax_rt = 0.0;
            return;
        }

        // smooth cubic ramp over -DGR < log10(Rtheta/Rcrit) < DGR
        double rnorm = (gr - (grcrit - dgr)) / (2.0 * dgr);
        double rn_hk = -grc_hk / (2.0 * dgr);
        double rn_rt = gr_rt / (2.0 * dgr);

        double rfac, rfac_hk, rfac_rt;
        if (rnorm >= 1.0)
        {
            rfac = 1.0; rfac_hk = 0.0; rfac_rt = 0.0;
        }
        else
        {
            rfac = 3.0 * rnorm * rnorm - 2.0 * rnorm * rnorm * rnorm;
            double rfac_rn = 6.0 * rnorm - 6.0 * rnorm * rnorm;
            rfac_hk = rfac_rn * rn_hk;
            rfac_rt = rfac_rn * rn_rt;
        }

        // amplification envelope slope correlation (Falkner-Skan)
        double arg = 3.87 * hmi - 2.52;
        double arg_hk = 3.87 * hmi_hk;
        double ex = Math.Exp(-arg * arg);
        double ex_hk = ex * (-2.0 * arg * arg_hk);

        double dadr = 0.028 * (hk - 1.0) - 0.0345 * ex;
        double dadr_hk = 0.028 - 0.0345 * ex_hk;

        // new m(H) correlation
        double af = -0.05 + 2.7 * hmi - 5.5 * hmi * hmi + 3.0 * hmi * hmi * hmi;
        double af_hmi = 2.7 - 11.0 * hmi + 9.0 * hmi * hmi;
        double af_hk = af_hmi * hmi_hk;

        ax = (af * dadr / th) * rfac;
        ax_hk = (af_hk * dadr / th + af * dadr_hk / th) * rfac + (af * dadr / th) * rfac_hk;
        ax_th = -ax / th;
        ax_rt = (af * dadr / th) * rfac_rt;
    }
}
