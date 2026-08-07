// Port of aoper.f SUBROUTINE EXEC's Newton trim loop: the 5 aerodynamic free
// variables (alpha, beta, roll/pitch/yaw rate -- AVL's IVALFA..IVROTZ) plus
// one free variable per control surface.
//
// The direct constraint types use exact analytic Jacobian entries; the
// load-target constraint types (CL/CY/Cl/Cm/Cn) use a central-difference
// Jacobian, which converges to the same trimmed solution though not via the
// same per-iteration path.
//
// Port of packages/avl-core/src/solver/trim.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public enum ConstraintKind { Alpha, Beta, RotX, RotY, RotZ, Control, CL, CY, Cl, Cm, Cn }

/// <summary>One free variable (0..4 = alpha/beta/rotx/roty/rotz, 5..5+NCONTROL-1 =
/// control surfaces) and what it's constrained to hit.</summary>
public sealed class TrimConstraint
{
    public int FreeVar;
    public ConstraintKind Kind;
    /// <summary>Target value: degrees for alpha/beta/control deflection, nondimensional
    /// pb/2V-style rate for rotx/roty/rotz, coefficient value for CL/CY/Cl/Cm/Cn.</summary>
    public double Value;
}

public sealed class TrimResult
{
    public bool Converged;
    public int Iterations;
    public CaseResult Result = new();
}

public static class Trim
{
    private const double DTR = Math.PI / 180.0;
    private const double EPS = 0.00002; // aoper.f EXEC's convergence epsilon
    private const double DMAX = 1.5708; // 90 deg angle-limit guard
    private const bool LSA_RATES = true; // avl.f DEFINI default
    private const double DIR = -1.0; // GETSA(LNASA_SA=.TRUE.)
    private const double FD_EPS_ANGLE = 1e-6; // rad
    private const double FD_EPS_RATE = 1e-6;
    private const double FD_EPS_DEFLECTION = 1e-4; // degrees

    private sealed class State
    {
        public double Alfa;
        public double Beta;
        public double[] Wrot = new double[3];
        public double[] Delcon = Array.Empty<double>();

        public State Clone() => new()
        {
            Alfa = Alfa,
            Beta = Beta,
            Wrot = (double[])Wrot.Clone(),
            Delcon = (double[])Delcon.Clone(),
        };
    }

    private static CaseResult EvalAt(SolveContext ctx, State s) =>
        SolveCase.EvaluateCase(ctx, s.Alfa, s.Beta, s.Wrot, s.Delcon);

    /// <summary>Stability-axis roll/yaw moments, matching aoper.f's (CMTOT(1)*CA+CMTOT(3)*SA)*DIR pattern.</summary>
    private static (double roll, double yaw) StabRollYaw(double[] cmTot, double ca, double sa) =>
        ((cmTot[0] * ca + cmTot[2] * sa) * DIR, (cmTot[2] * ca - cmTot[0] * sa) * DIR);

    public static TrimResult SolveTrimmedCase(
        Geometry geo,
        IReadOnlyList<TrimConstraint> constraints,
        int maxIter = 30,
        SolveContextOverrides? overrides = null)
    {
        overrides ??= new SolveContextOverrides();
        int nControl = geo.ControlNames.Count;
        int nvtot = 5 + nControl;
        if (constraints.Count != nvtot)
        {
            throw new ArgumentException($"solveTrimmedCase: expected {nvtot} constraints (5 aero + {nControl} control), got {constraints.Count}");
        }
        double bref = geo.Bref;
        double cref = geo.Cref;

        var ctx = SolveCase.BuildSolveContext(geo, overrides);

        var state = new State { Alfa = 0, Beta = 0, Wrot = new double[] { 0, 0, 0 }, Delcon = new double[nControl] };
        // Directly-specified variables can be set immediately (aoper.f EXEC:1049-1056)
        foreach (var c in constraints)
        {
            if (c.FreeVar == 0 && c.Kind == ConstraintKind.Alpha) state.Alfa = c.Value * DTR;
            if (c.FreeVar == 1 && c.Kind == ConstraintKind.Beta) state.Beta = c.Value * DTR;
            if (c.FreeVar == 2 && c.Kind == ConstraintKind.RotX) state.Wrot[0] = (c.Value * 2) / bref;
            if (c.FreeVar == 3 && c.Kind == ConstraintKind.RotY) state.Wrot[1] = (c.Value * 2) / cref;
            if (c.FreeVar == 4 && c.Kind == ConstraintKind.RotZ) state.Wrot[2] = (c.Value * 2) / bref;
            if (c.FreeVar >= 5 && c.Kind == ConstraintKind.Control) state.Delcon[c.FreeVar - 5] = c.Value;
        }

        var result = EvalAt(ctx, state);

        bool needsFd = constraints.Any(c => c.Kind == ConstraintKind.CL || c.Kind == ConstraintKind.CY || c.Kind == ConstraintKind.Cl || c.Kind == ConstraintKind.Cm || c.Kind == ConstraintKind.Cn);

        for (int iter = 1; iter <= maxIter; iter++)
        {
            double ca = LSA_RATES ? Math.Cos(state.Alfa) : 1.0;
            double sa = LSA_RATES ? Math.Sin(state.Alfa) : 0.0;
            double caA = LSA_RATES ? -sa : 0.0;
            double saA = LSA_RATES ? ca : 0.0;

            var dCLd = new double[nvtot];
            var dCYd = new double[nvtot];
            var dCld = new double[nvtot];
            var dCmd = new double[nvtot];
            var dCnd = new double[nvtot];
            if (needsFd)
            {
                for (int k = 0; k < nvtot; k++)
                {
                    double eps = k < 2 ? FD_EPS_ANGLE : k < 5 ? FD_EPS_RATE : FD_EPS_DEFLECTION;
                    var sPlus = state.Clone();
                    var sMinus = state.Clone();
                    if (k == 0)
                    {
                        sPlus.Alfa += eps;
                        sMinus.Alfa -= eps;
                    }
                    else if (k == 1)
                    {
                        sPlus.Beta += eps;
                        sMinus.Beta -= eps;
                    }
                    else if (k < 5)
                    {
                        sPlus.Wrot[k - 2] += eps;
                        sMinus.Wrot[k - 2] -= eps;
                    }
                    else
                    {
                        sPlus.Delcon[k - 5] += eps;
                        sMinus.Delcon[k - 5] -= eps;
                    }
                    var rPlus = EvalAt(ctx, sPlus);
                    var rMinus = EvalAt(ctx, sMinus);
                    double caP = LSA_RATES ? Math.Cos(sPlus.Alfa) : 1.0;
                    double saP = LSA_RATES ? Math.Sin(sPlus.Alfa) : 0.0;
                    double caM = LSA_RATES ? Math.Cos(sMinus.Alfa) : 1.0;
                    double saM = LSA_RATES ? Math.Sin(sMinus.Alfa) : 0.0;
                    dCLd[k] = (rPlus.Totals.ClTot - rMinus.Totals.ClTot) / (2 * eps);
                    dCYd[k] = (rPlus.Totals.CfTot[1] - rMinus.Totals.CfTot[1]) / (2 * eps);
                    dCld[k] = (StabRollYaw(rPlus.Totals.CmTot, caP, saP).roll - StabRollYaw(rMinus.Totals.CmTot, caM, saM).roll) / (2 * eps);
                    dCmd[k] = (rPlus.Totals.CmTot[1] - rMinus.Totals.CmTot[1]) / (2 * eps);
                    dCnd[k] = (StabRollYaw(rPlus.Totals.CmTot, caP, saP).yaw - StabRollYaw(rMinus.Totals.CmTot, caM, saM).yaw) / (2 * eps);
                }
            }

            var vsys = new double[nvtot * nvtot];
            var vres = new double[nvtot];

            for (int iv = 0; iv < nvtot; iv++)
            {
                var c = constraints[iv];
                void Row(int col, double val) => vsys[iv * nvtot + col] = val;

                switch (c.Kind)
                {
                    case ConstraintKind.Alpha:
                        vres[iv] = state.Alfa - c.Value * DTR;
                        Row(0, 1.0);
                        break;
                    case ConstraintKind.Beta:
                        vres[iv] = state.Beta - c.Value * DTR;
                        Row(1, 1.0);
                        break;
                    case ConstraintKind.RotX:
                        vres[iv] = (state.Wrot[0] * ca + state.Wrot[2] * sa) * DIR - (c.Value * 2.0) / bref;
                        Row(2, ca * DIR);
                        Row(4, sa * DIR);
                        Row(0, (state.Wrot[0] * caA + state.Wrot[2] * saA) * DIR);
                        break;
                    case ConstraintKind.RotY:
                        vres[iv] = state.Wrot[1] - (c.Value * 2.0) / cref;
                        Row(3, 1.0);
                        break;
                    case ConstraintKind.RotZ:
                        vres[iv] = (state.Wrot[2] * ca - state.Wrot[0] * sa) * DIR - (c.Value * 2.0) / bref;
                        Row(2, -sa * DIR);
                        Row(4, ca * DIR);
                        Row(0, (state.Wrot[2] * caA - state.Wrot[0] * saA) * DIR);
                        break;
                    case ConstraintKind.Control:
                    {
                        int n = c.FreeVar - 5;
                        if (n < 0 || n >= nControl) throw new ArgumentException($"'control' constraint requires freeVar >= 5, got {c.FreeVar}");
                        vres[iv] = state.Delcon[n] - c.Value;
                        Row(5 + n, 1.0);
                        break;
                    }
                    case ConstraintKind.CL:
                        vres[iv] = result.Totals.ClTot - c.Value;
                        for (int k = 0; k < nvtot; k++) Row(k, dCLd[k]);
                        break;
                    case ConstraintKind.CY:
                        vres[iv] = result.Totals.CfTot[1] - c.Value;
                        for (int k = 0; k < nvtot; k++) Row(k, dCYd[k]);
                        break;
                    case ConstraintKind.Cl:
                        vres[iv] = StabRollYaw(result.Totals.CmTot, ca, sa).roll - c.Value;
                        for (int k = 0; k < nvtot; k++) Row(k, dCld[k]);
                        break;
                    case ConstraintKind.Cm:
                        vres[iv] = result.Totals.CmTot[1] - c.Value;
                        for (int k = 0; k < nvtot; k++) Row(k, dCmd[k]);
                        break;
                    case ConstraintKind.Cn:
                        vres[iv] = StabRollYaw(result.Totals.CmTot, ca, sa).yaw - c.Value;
                        for (int k = 0; k < nvtot; k++) Row(k, dCnd[k]);
                        break;
                }
            }

            var fac = DenseLU.LuFactor(vsys, nvtot);
            var delta = DenseLU.LuSolve(fac, vres);
            double dAl = -delta[0];
            double dBe = -delta[1];
            double dWx = -delta[2];
            double dWy = -delta[3];
            double dWz = -delta[4];
            var dDc = new double[nControl];
            for (int n = 0; n < nControl; n++) dDc[n] = -delta[5 + n];

            double dMaxA = DMAX;
            double dMaxR = (5.0 * DMAX) / bref;
            if (Math.Abs(state.Alfa + dAl) > dMaxA) return new TrimResult { Converged = false, Iterations = iter, Result = result };
            if (Math.Abs(state.Beta + dBe) > dMaxA) return new TrimResult { Converged = false, Iterations = iter, Result = result };
            if (Math.Abs(state.Wrot[0] + dWx) > dMaxR) return new TrimResult { Converged = false, Iterations = iter, Result = result };
            if (Math.Abs(state.Wrot[1] + dWy) > dMaxR) return new TrimResult { Converged = false, Iterations = iter, Result = result };
            if (Math.Abs(state.Wrot[2] + dWz) > dMaxR) return new TrimResult { Converged = false, Iterations = iter, Result = result };

            var newDelcon = new double[nControl];
            for (int n = 0; n < nControl; n++) newDelcon[n] = state.Delcon[n] + dDc[n];
            state = new State
            {
                Alfa = state.Alfa + dAl,
                Beta = state.Beta + dBe,
                Wrot = new double[] { state.Wrot[0] + dWx, state.Wrot[1] + dWy, state.Wrot[2] + dWz },
                Delcon = newDelcon,
            };
            result = EvalAt(ctx, state);

            double delMax = Math.Max(Math.Abs(dAl), Math.Max(Math.Abs(dBe), Math.Max(Math.Abs(dWx * bref) / 2.0, Math.Max(Math.Abs(dWy * cref) / 2.0, Math.Abs(dWz * bref) / 2.0))));
            foreach (var d in dDc) delMax = Math.Max(delMax, Math.Abs(d));
            if (delMax < EPS) return new TrimResult { Converged = true, Iterations = iter, Result = result };
        }

        return new TrimResult { Converged = false, Iterations = maxIter, Result = result };
    }
}
