// Port of aoutput.f SUBROUTINE DERMATS's derivative computation (not the text
// output formatting). Computes alpha/beta/rate/control derivatives directly via
// central differences on EvaluateCase's totals -- mathematically equivalent to
// AERO's forward-mode sensitivity tracking, not bit-identical.
//
// Port of packages/avl-core/src/solver/stabDerivs.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class AxisDerivs
{
    public double Al; // d/dalpha
    public double Be; // d/dbeta
    public double Rx; // d/d(pb/2V)
    public double Ry; // d/d(qc/2V)
    public double Rz; // d/d(rb/2V)
}

public sealed class StabDerivs
{
    public AxisDerivs Cl = new();
    public AxisDerivs Cy = new();
    public AxisDerivs Cd = new();
    /// <summary>Stability-axis roll moment Cl' (CRSAX, undirected).</summary>
    public AxisDerivs Cr = new();
    public AxisDerivs Cm = new();
    /// <summary>Stability-axis yaw moment Cn' (CNSAX, undirected).</summary>
    public AxisDerivs Cn = new();
    public double[] ClD = Array.Empty<double>();
    public double[] CyD = Array.Empty<double>();
    public double[] CdD = Array.Empty<double>();
    public double[] CrD = Array.Empty<double>();
    public double[] CmD = Array.Empty<double>();
    public double[] CnD = Array.Empty<double>();
    public double[] CdffD = Array.Empty<double>();
    public double[] SpanefD = Array.Empty<double>();
    public double Xnp;
    public double Bb;
}

public enum MomentAxis { Stability, Body }

public static class StabDerivsCalc
{
    private const double FD_EPS_ANGLE = 1e-6; // rad
    private const double FD_EPS_RATE = 1e-6;
    private const double FD_EPS_DEFLECTION = 1e-4; // degrees

    private static (double roll, double yaw) StabRollYawRaw(double[] cmTot, double ca, double sa) =>
        (cmTot[0] * ca + cmTot[2] * sa, cmTot[2] * ca - cmTot[0] * sa);

    public static StabDerivs ComputeStabDerivs(
        Geometry geo,
        SolveContext ctx,
        double alfa,
        double beta,
        double[] wrot,
        double[] delcon,
        MomentAxis momentAxis = MomentAxis.Stability)
    {
        int nControl = geo.ControlNames.Count;
        double cref = geo.Cref;

        double ca = Math.Cos(alfa);
        double sa = Math.Sin(alfa);

        (double roll, double yaw) RollYaw(double[] cmTot, double c, double s) =>
            momentAxis == MomentAxis.Stability ? StabRollYawRaw(cmTot, c, s) : (cmTot[0], cmTot[2]);

        AxisDerivs CentralDiff(Func<CaseResult, double, double, double> get)
        {
            double Diff(int k)
            {
                double eps = k == 0 || k == 1 ? FD_EPS_ANGLE : FD_EPS_RATE;
                double aP = alfa, bP = beta;
                double[] wP = (double[])wrot.Clone();
                double aM = alfa, bM = beta;
                double[] wM = (double[])wrot.Clone();
                if (k == 0) { aP += eps; aM -= eps; }
                else if (k == 1) { bP += eps; bM -= eps; }
                else { wP[k - 2] += eps; wM[k - 2] -= eps; }
                var rP = SolveCase.EvaluateCase(ctx, aP, bP, wP, delcon);
                var rM = SolveCase.EvaluateCase(ctx, aM, bM, wM, delcon);
                return (get(rP, Math.Cos(aP), Math.Sin(aP)) - get(rM, Math.Cos(aM), Math.Sin(aM))) / (2 * eps);
            }
            return new AxisDerivs { Al = Diff(0), Be = Diff(1), Rx = Diff(2), Ry = Diff(3), Rz = Diff(4) };
        }

        double EvalGet(CaseResult r, double c, double s, string which)
        {
            switch (which)
            {
                case "cl":
                    return r.Totals.ClTot;
                case "cy":
                {
                    double vmag = Math.Sqrt(r.Vinf[0] * r.Vinf[0] + r.Vinf[1] * r.Vinf[1] + r.Vinf[2] * r.Vinf[2]);
                    return r.Totals.CfTot[1] - geo.Cdoref0 * r.Vinf[1] * vmag;
                }
                case "cd":
                    return r.Totals.CdTot;
                case "cr":
                    return RollYaw(r.Totals.CmTot, c, s).roll;
                case "cm":
                    return r.Totals.CmTot[1];
                case "cn":
                    return RollYaw(r.Totals.CmTot, c, s).yaw;
                default:
                    throw new ArgumentOutOfRangeException(nameof(which));
            }
        }

        var cl = CentralDiff((r, c, s) => EvalGet(r, c, s, "cl"));
        var cy = CentralDiff((r, c, s) => EvalGet(r, c, s, "cy"));
        var cd = CentralDiff((r, c, s) => EvalGet(r, c, s, "cd"));
        var cr = CentralDiff((r, c, s) => EvalGet(r, c, s, "cr"));
        var cm = CentralDiff((r, c, s) => EvalGet(r, c, s, "cm"));
        var cn = CentralDiff((r, c, s) => EvalGet(r, c, s, "cn"));

        var clD = new double[nControl];
        var cyD = new double[nControl];
        var cdD = new double[nControl];
        var crD = new double[nControl];
        var cmD = new double[nControl];
        var cnD = new double[nControl];
        var cdffD = new double[nControl];
        var spanefD = new double[nControl];
        for (int n = 0; n < nControl; n++)
        {
            var dcP = (double[])delcon.Clone();
            var dcM = (double[])delcon.Clone();
            dcP[n] += FD_EPS_DEFLECTION;
            dcM[n] -= FD_EPS_DEFLECTION;
            var rP = SolveCase.EvaluateCase(ctx, alfa, beta, wrot, dcP);
            var rM = SolveCase.EvaluateCase(ctx, alfa, beta, wrot, dcM);
            double denom = 2 * FD_EPS_DEFLECTION;
            clD[n] = (rP.Totals.ClTot - rM.Totals.ClTot) / denom;
            cyD[n] = (rP.Totals.CfTot[1] - rM.Totals.CfTot[1]) / denom;
            cdD[n] = (rP.Totals.CdTot - rM.Totals.CdTot) / denom;
            crD[n] = (RollYaw(rP.Totals.CmTot, ca, sa).roll - RollYaw(rM.Totals.CmTot, ca, sa).roll) / denom;
            cmD[n] = (rP.Totals.CmTot[1] - rM.Totals.CmTot[1]) / denom;
            cnD[n] = (RollYaw(rP.Totals.CmTot, ca, sa).yaw - RollYaw(rM.Totals.CmTot, ca, sa).yaw) / denom;
            cdffD[n] = (rP.Trefftz.Cdff - rM.Trefftz.Cdff) / denom;
            spanefD[n] = (rP.Trefftz.Spanef - rM.Trefftz.Spanef) / denom;
        }

        double xnp = cl.Al != 0 ? geo.Xyzref0[0] - (cref * cm.Al) / cl.Al : -1e30;
        double bb = Math.Abs(cr.Rz * cn.Be) > 0.0001 ? (cr.Be * cn.Rz) / (cr.Rz * cn.Be) : -1e30;

        return new StabDerivs
        {
            Cl = cl, Cy = cy, Cd = cd, Cr = cr, Cm = cm, Cn = cn,
            ClD = clD, CyD = cyD, CdD = cdD, CrD = crD, CmD = cmD, CnD = cnD,
            CdffD = cdffD, SpanefD = spanefD, Xnp = xnp, Bb = bb,
        };
    }
}
