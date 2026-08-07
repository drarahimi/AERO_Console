// Off-body flow survey core (obsetup.f OBVELSUM): the induced/total velocity at
// an arbitrary point, via the same Biot-Savart horseshoe-vortex kernel used for
// the AIC. This is the numerically meaningful part of the OB command; the
// interactive point/line/grid survey sub-menu (OBOPER) is a UI wrapper over this.
//
// Body source/doublet contribution (WOBSRD) is zero without BODY blocks (task #13).
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

namespace Avl.Core.Solver;

public static class OffBody
{
    /// <summary>Horseshoe-vortex induced velocity at <paramref name="point"/> (VOB).</summary>
    public static double[] InducedVelocityAt(SolveContext ctx, CaseResult result, double[] point)
    {
        var geo = ctx.Geo;
        var gam = SolveCase.ComputeGamma(ctx, result.Vinf, result.Wrot, result.Delcon);
        var pts = new List<EvalPoint> { new EvalPoint { R = point, Component = -1 } };
        var infl = BiotSavart.VvorMatrix(
            ctx.Setup.Betm, geo.IYsym, 0, geo.IZsym, geo.ZSym, 0.0, 2.0,
            ctx.Setup.VortexGeoms, pts, false);
        var row = infl[0];
        double vx = 0, vy = 0, vz = 0;
        for (int j = 0; j < row.Length; j++)
        {
            vx += row[j].U * gam[j];
            vy += row[j].V * gam[j];
            vz += row[j].W * gam[j];
        }
        return new double[] { vx, vy, vz };
    }

    /// <summary>Total velocity at <paramref name="point"/> (WOB = VINF + induced).</summary>
    public static double[] VelocityAt(SolveContext ctx, CaseResult result, double[] point)
    {
        var v = InducedVelocityAt(ctx, result, point);
        return new double[] { result.Vinf[0] + v[0], result.Vinf[1] + v[1], result.Vinf[2] + v[2] };
    }
}
