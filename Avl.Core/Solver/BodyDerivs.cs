// Port of aoutput.f SUBROUTINE DERMATB's derivative computation. Differentiates
// body-axis forces/moments directly against the raw freestream velocity
// components u,v,w and raw body-axis rotation rates p,q,r, via central
// differences (same substitution as StabDerivs for AERO's forward-mode tracking).
//
// Port of packages/avl-core/src/solver/bodyDerivs.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class UvwDerivs
{
    public double U;
    public double V;
    public double W;
    public double P;
    public double Q;
    public double R;
}

public sealed class BodyAxisDerivs
{
    /// <summary>d(CFTOT[0])/d(u,v,w,p,q,r) -- body-axis X force, raw/undirected.</summary>
    public UvwDerivs Cx = new();
    public UvwDerivs Cy = new();
    public UvwDerivs Cz = new();
    /// <summary>d(CMTOT[0])/d(...) -- body-axis roll moment, raw/undirected.</summary>
    public UvwDerivs Cl = new();
    public UvwDerivs Cm = new();
    public UvwDerivs Cn = new();
    public double[] CxD = Array.Empty<double>();
    public double[] CyD = Array.Empty<double>();
    public double[] CzD = Array.Empty<double>();
    public double[] ClD = Array.Empty<double>();
    public double[] CmD = Array.Empty<double>();
    public double[] CnD = Array.Empty<double>();
}

public static class BodyDerivsCalc
{
    private const double FD_EPS_VEL = 1e-6;
    private const double FD_EPS_RATE = 1e-6;
    private const double FD_EPS_DEFLECTION = 1e-4; // degrees

    public static BodyAxisDerivs ComputeBodyAxisDerivs(
        Geometry geo,
        SolveContext ctx,
        double alfa,
        double[] vinf,
        double[] wrot,
        double[] delcon)
    {
        int nControl = geo.ControlNames.Count;

        UvwDerivs CentralDiff(Func<CaseResult, double> get)
        {
            double DiffVinf(int k)
            {
                var vP = (double[])vinf.Clone();
                var vM = (double[])vinf.Clone();
                vP[k] += FD_EPS_VEL;
                vM[k] -= FD_EPS_VEL;
                var rP = SolveCase.EvaluateAtVinf(ctx, vP, wrot, alfa, delcon);
                var rM = SolveCase.EvaluateAtVinf(ctx, vM, wrot, alfa, delcon);
                return (get(rP) - get(rM)) / (2 * FD_EPS_VEL);
            }
            double DiffWrot(int k)
            {
                var wP = (double[])wrot.Clone();
                var wM = (double[])wrot.Clone();
                wP[k] += FD_EPS_RATE;
                wM[k] -= FD_EPS_RATE;
                var rP = SolveCase.EvaluateAtVinf(ctx, vinf, wP, alfa, delcon);
                var rM = SolveCase.EvaluateAtVinf(ctx, vinf, wM, alfa, delcon);
                return (get(rP) - get(rM)) / (2 * FD_EPS_RATE);
            }
            return new UvwDerivs { U = DiffVinf(0), V = DiffVinf(1), W = DiffVinf(2), P = DiffWrot(0), Q = DiffWrot(1), R = DiffWrot(2) };
        }

        var cx = CentralDiff(r => r.Totals.CfTot[0]);
        var cy = CentralDiff(r => r.Totals.CfTot[1]);
        var cz = CentralDiff(r => r.Totals.CfTot[2]);
        var cl = CentralDiff(r => r.Totals.CmTot[0]);
        var cm = CentralDiff(r => r.Totals.CmTot[1]);
        var cn = CentralDiff(r => r.Totals.CmTot[2]);

        var cxD = new double[nControl];
        var cyD = new double[nControl];
        var czD = new double[nControl];
        var clD = new double[nControl];
        var cmD = new double[nControl];
        var cnD = new double[nControl];
        for (int n = 0; n < nControl; n++)
        {
            var dcP = (double[])delcon.Clone();
            var dcM = (double[])delcon.Clone();
            dcP[n] += FD_EPS_DEFLECTION;
            dcM[n] -= FD_EPS_DEFLECTION;
            var rP = SolveCase.EvaluateAtVinf(ctx, vinf, wrot, alfa, dcP);
            var rM = SolveCase.EvaluateAtVinf(ctx, vinf, wrot, alfa, dcM);
            double denom = 2 * FD_EPS_DEFLECTION;
            cxD[n] = (rP.Totals.CfTot[0] - rM.Totals.CfTot[0]) / denom;
            cyD[n] = (rP.Totals.CfTot[1] - rM.Totals.CfTot[1]) / denom;
            czD[n] = (rP.Totals.CfTot[2] - rM.Totals.CfTot[2]) / denom;
            clD[n] = (rP.Totals.CmTot[0] - rM.Totals.CmTot[0]) / denom;
            cmD[n] = (rP.Totals.CmTot[1] - rM.Totals.CmTot[1]) / denom;
            cnD[n] = (rP.Totals.CmTot[2] - rM.Totals.CmTot[2]) / denom;
        }

        return new BodyAxisDerivs
        {
            Cx = cx, Cy = cy, Cz = cz, Cl = cl, Cm = cm, Cn = cn,
            CxD = cxD, CyD = cyD, CzD = czD, ClD = clD, CmD = cmD, CnD = cnD,
        };
    }
}
