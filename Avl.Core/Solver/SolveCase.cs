// Orchestrates one operating-point solve: VINFAB -> GAMSUM -> VELSUM (VV
// only, no bodies) -> AERO (SFFORC totals + TPFORC).
//
// Port of packages/avl-core/src/solver/solveCase.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class CaseInputs
{
    public double AlfaDeg;
    public double BetaDeg;
    /// <summary>Roll/pitch/yaw rate, nondimensionalized (pb/2V, qc/2V, rb/2V); 0 for the static base case.</summary>
    public double[] Wrot = new double[3];
    /// <summary>Per-control-variable deflection in degrees; defaults to all-zero.</summary>
    public double[]? Delcon;
}

public sealed class CaseResult
{
    public double Alfa;
    public double Beta;
    public double Mach;
    public double[] Vinf = new double[3];
    public double[] Wrot = new double[3];
    public double[] Delcon = Array.Empty<double>();
    public Totals Totals = new();
    public TrefftzResult Trefftz = new();
    /// <summary>Per-strip ESS/ENSY/ENSZ axes (amake.f ENCALC), carried through for output formatters (FN/FS).</summary>
    public StripAxes[] StripAxes = Array.Empty<StripAxes>();
}

/// <summary>The Mach/geometry-dependent pieces (asetup.f SETUP+GUCALC), expensive
/// and reusable across many operating-point evaluations at fixed Mach.</summary>
public sealed class SolveContext
{
    public Geometry Geo = new();
    public EncalcResult EncResult = new();
    public AicSetup Setup = new();
    public double Mach;
    public double[] Xyzref = new double[3];
    public double Cdref;
}

/// <summary>Optional per-run-case overrides for values that otherwise default to the
/// geometry's own Mach/XYZREF/CDoref.</summary>
public sealed class SolveContextOverrides
{
    public double? Mach;
    public double[]? Xyzref;
    public double? Cdref;
}

public static class SolveCase
{
    private const double DTR = Math.PI / 180.0;

    public static SolveContext BuildSolveContext(Geometry geo, SolveContextOverrides? overrides = null)
    {
        overrides ??= new SolveContextOverrides();
        double mach = overrides.Mach ?? geo.Mach0;
        double[] xyzref = overrides.Xyzref ?? geo.Xyzref0;
        double cdref = overrides.Cdref ?? geo.Cdoref0;
        var encResult = Encalc.Compute(geo);
        var setup = Setup.BuildAicSetup(geo, encResult, mach, xyzref);
        return new SolveContext { Geo = geo, EncResult = encResult, Setup = setup, Mach = mach, Xyzref = xyzref, Cdref = cdref };
    }

    /// <summary>The cheap per-operating-point part (aoper.f EXEC's VINFAB->GAMSUM->
    /// VELSUM->AERO sequence, minus the Newton loop itself).</summary>
    public static CaseResult EvaluateCase(
        SolveContext ctx,
        double alfa,
        double beta,
        double[] wrot,
        double[]? delcon = null)
    {
        // VINFAB (aero.f)
        double sina = Math.Sin(alfa), cosa = Math.Cos(alfa);
        double sinb = Math.Sin(beta), cosb = Math.Cos(beta);
        var vinf = new double[] { cosa * cosb, -sinb, sina * cosb };

        var r = EvaluateAtVinf(ctx, vinf, wrot, alfa, delcon);
        r.Alfa = alfa;
        r.Beta = beta;
        return r;
    }

    /// <summary>Lower-level evaluation taking the freestream direction VINF directly
    /// (rather than deriving it from alfa/beta via VINFAB) -- needed for DERMATB's
    /// body-axis u/v/w velocity-component derivatives (StabDerivs).</summary>
    public static CaseResult EvaluateAtVinf(
        SolveContext ctx,
        double[] vinf,
        double[] wrot,
        double alfaForRotation,
        double[]? delcon = null)
    {
        var geo = ctx.Geo;
        var setup = ctx.Setup;
        int nControl = geo.ControlNames.Count;
        var dc = new double[nControl];
        for (int n = 0; n < nControl; n++) dc[n] = delcon != null && n < delcon.Length ? delcon[n] : 0;

        var gam = ComputeGamma(ctx, vinf, wrot, dc);
        int nvor = geo.Vortices.Count;

        // VELSUM: wing horseshoe-vortex velocity only. The body source/doublet velocity
        // (WVSRD) is deliberately NOT added here -- aero.f SFFORC evaluates the strip forces
        // with the wing-only VV (the body couples in solely through the RHS / GAM, plus its
        // own BDFORC forces below).
        var vv = new double[nvor][];
        for (int i = 0; i < nvor; i++)
        {
            double vx = 0, vy = 0, vz = 0;
            var row = setup.WvGam[i];
            for (int j = 0; j < nvor; j++)
            {
                var c = row[j];
                double g = gam[j];
                vx += c.U * g;
                vy += c.V * g;
                vz += c.W * g;
            }
            vv[i] = new double[] { vx, vy, vz };
        }

        var totals = Force.ComputeForces(geo, ctx.EncResult, gam, vv, vinf, wrot, alfaForRotation, ctx.Cdref, ctx.Xyzref);
        var trefftz = Trefftz.ComputeTrefftz(geo, gam, ctx.Mach);

        // Add body forces (BDFORC): independent of the wing solution, added to the totals.
        if (geo.Bodies.Count > 0)
        {
            var byBody = new BodyForce[geo.Bodies.Count];
            int segOffset = 0;
            for (int b = 0; b < geo.Bodies.Count; b++)
            {
                var bf = BodyAero.BdForc(geo.Bodies[b], setup.BodySegs, segOffset, setup.Betm, vinf, wrot, alfaForRotation, geo.Sref, geo.Cref, geo.Bref, ctx.Xyzref);
                byBody[b] = bf;
                segOffset += geo.Bodies[b].Nodes.Count - 1;
                totals.CdTot += bf.Cd;
                totals.CyTot += bf.Cy;
                totals.ClTot += bf.Cl;
                for (int k = 0; k < 3; k++) { totals.CfTot[k] += bf.Cf[k]; totals.CmTot[k] += bf.Cm[k]; }
            }
            totals.ByBody = byBody;
            double cosa = Math.Cos(alfaForRotation), sina = Math.Sin(alfaForRotation);
            totals.CxTot = totals.CdTot * cosa - totals.ClTot * sina;
            totals.CzTot = totals.CdTot * sina + totals.ClTot * cosa;
        }

        return new CaseResult
        {
            Mach = ctx.Mach,
            Vinf = vinf,
            Wrot = wrot,
            Delcon = dc,
            Totals = totals,
            Trefftz = trefftz,
            StripAxes = ctx.EncResult.StripAxes,
        };
    }

    /// <summary>GAMSUM: the per-vortex circulation for a given freestream/rates/controls.
    /// GAM(i) = sum_iu (GAM_U_0(i,iu) + sum_n GAM_U_D(i,iu,n)*DELCON(n)) * (VINF|WROT)[iu].
    /// Exposed for off-body surveys and any other post-solve field evaluation.</summary>
    public static double[] ComputeGamma(SolveContext ctx, double[] vinf, double[] wrot, double[] delcon)
    {
        var geo = ctx.Geo;
        var setup = ctx.Setup;
        int nControl = geo.ControlNames.Count;
        var dc = new double[nControl];
        for (int n = 0; n < nControl; n++) dc[n] = delcon != null && n < delcon.Length ? delcon[n] : 0;

        int nvor = geo.Vortices.Count;
        var gam = new double[nvor];
        var unit = new double[] { vinf[0], vinf[1], vinf[2], wrot[0], wrot[1], wrot[2] };
        for (int i = 0; i < nvor; i++)
        {
            double s = 0;
            for (int iu = 0; iu < 6; iu++)
            {
                double gamU = setup.GamU0[iu][i];
                for (int n = 0; n < nControl; n++) gamU += setup.GamUD[iu][n][i] * dc[n];
                s += gamU * unit[iu];
            }
            gam[i] = s;
        }
        return gam;
    }

    public static CaseResult Solve(Geometry geo, CaseInputs inputs)
    {
        var ctx = BuildSolveContext(geo);
        return EvaluateCase(ctx, inputs.AlfaDeg * DTR, inputs.BetaDeg * DTR, inputs.Wrot, inputs.Delcon);
    }
}
