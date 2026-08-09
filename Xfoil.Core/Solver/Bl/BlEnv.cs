// Port of the flow-environment quantities XFOIL sets up before marching the boundary
// layer (xbl.f, around the MRCL/COMSET block): the gas constant, stagnation
// density/enthalpy, 1/stagnation-enthalpy, reference viscosity Reynolds number, and
// their sensitivities to Mach^2 (MS) and Reynolds (RE). These feed BLKIN/BLVAR.

namespace Xfoil.Core.Solver.Bl;

public sealed class BlEnv
{
    public double Gambl;       // ratio of specific heats
    public double Gm1bl;       // gamma - 1
    public double Qinfbl;      // freestream reference speed
    public double Rstbl, RstblMs;      // stagnation density
    public double Hstinv, HstinvMs;    // 1 / stagnation enthalpy
    public double Hvrat;       // Sutherland const / To
    public double Reybl, ReyblMs, ReyblRe; // viscosity Reynolds number
    public double Amcrit;      // critical amplification ratio Ncrit
    public double Tkbl, TkblMs; // Karman-Tsien compressibility parameter (COMSET TKLAM)

    /// <summary>Builds the environment from freestream Mach, chord Reynolds number, and
    /// reference speed (QINF, normally 1), matching xbl.f. Ncrit defaults to 9.</summary>
    public static BlEnv Create(double mach, double reynolds, double qinf = 1.0, double ncrit = 9.0)
    {
        const double gamma = 1.4, gamm1 = 0.4;
        var e = new BlEnv { Gambl = gamma, Gm1bl = gamm1, Qinfbl = qinf, Hvrat = 0.35, Amcrit = ncrit };

        double m2 = mach * mach;
        double denom = 1.0 + 0.5 * gamm1 * m2;

        e.Rstbl = Math.Pow(denom, 1.0 / gamm1);
        e.RstblMs = 0.5 * e.Rstbl / denom;

        e.Hstinv = gamm1 * (mach / qinf) * (mach / qinf) / denom;
        e.HstinvMs = gamm1 * (1.0 / qinf) * (1.0 / qinf) / denom - 0.5 * gamm1 * e.Hstinv / denom;

        double herat = 1.0 - 0.5 * qinf * qinf * e.Hstinv;
        double heratMs = -0.5 * qinf * qinf * e.HstinvMs;

        double sfac = Math.Sqrt(herat * herat * herat) * (1.0 + e.Hvrat) / (herat + e.Hvrat);
        e.Reybl = reynolds * sfac;
        e.ReyblRe = sfac;
        e.ReyblMs = e.Reybl * (1.5 / herat - 1.0 / (herat + e.Hvrat)) * heratMs;

        // Karman-Tsien parameter (COMSET), for the compressible Uei<->Uec mapping (BLPRV)
        double beta = Math.Sqrt(1.0 - m2);
        double betaMsq = -0.5 / beta;
        e.Tkbl = m2 / ((1.0 + beta) * (1.0 + beta));
        e.TkblMs = 1.0 / ((1.0 + beta) * (1.0 + beta)) - 2.0 * e.Tkbl / (1.0 + beta) * betaMsq;
        return e;
    }
}
