// Port of asetup.f SUBROUTINE SETUP and SUBROUTINE GUCALC.
//
// Scope note: body source/doublet AIC (SRDSET/VSRD) and design-variable
// sensitivity right-hand-sides (GAM_U_G) are not ported -- no bodies or
// design variables in the current fixtures.
//
// Port of packages/avl-core/src/solver/setup.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class AicSetup
{
    public double Betm;
    public int Nvor;
    public List<VortexGeom> VortexGeoms = new();
    public Vec3[][] WcGam = Array.Empty<Vec3[]>(); // [controlPointIndex][vortexIndex]
    public Vec3[][] WvGam = Array.Empty<Vec3[]>(); // [vortexMidpointIndex][vortexIndex]
    public LUFactorization AicFactored = new();
    /// <summary>Unit-circulation solutions: GamU0[iu][vortexIndex], iu=0..2 -> unit VINF x,y,z; iu=3..5 -> unit WROT x,y,z.</summary>
    public double[][] GamU0 = Array.Empty<double[]>();
    /// <summary>Control-deflection circulation sensitivity: GamUD[iu][n][vortexIndex].</summary>
    public double[][][] GamUD = Array.Empty<double[][]>();
    /// <summary>Body source/doublet segments (SRDSET), empty when there are no bodies.</summary>
    public List<BodySegment> BodySegs = new();
    /// <summary>Body-induced velocity per unit flow component at each vortex midpoint:
    /// WvSrd[vortexIndex][k][iu] -- added to VELSUM's force velocity. Null when no bodies.</summary>
    public double[][][]? WvSrd;
}

public static class Setup
{
    public static AicSetup BuildAicSetup(
        Geometry geo,
        EncalcResult encalcResult,
        double mach,
        double[]? xyzref = null)
    {
        xyzref ??= geo.Xyzref0;
        double betm = Math.Sqrt(1.0 - mach * mach);
        int nvor = geo.Vortices.Count;

        var vortexGeoms = geo.Vortices.Select(v => new VortexGeom
        {
            Rv1 = v.Rv1,
            Rv2 = v.Rv2,
            Component = geo.Surfaces[geo.Strips[v.StripIndex].SurfaceIndex].Component,
            Chord = v.Chord,
        }).ToList();

        var controlPoints = new List<EvalPoint>(nvor);
        var vortexPoints = new List<EvalPoint>(nvor);
        for (int i = 0; i < nvor; i++)
        {
            controlPoints.Add(new EvalPoint { R = geo.Vortices[i].Rc, Component = vortexGeoms[i].Component });
            vortexPoints.Add(new EvalPoint { R = geo.Vortices[i].Rv, Component = vortexGeoms[i].Component });
        }

        // no y/z symmetry planes in current fixtures (allegro.avl uses explicit YDUPLICATE, iYsym=0)
        var wcGam = BiotSavart.VvorMatrix(betm, geo.IYsym, 0, geo.IZsym, geo.ZSym, 0.0, 2.0, vortexGeoms, controlPoints, false);
        var wvGam = BiotSavart.VvorMatrix(betm, geo.IYsym, 0, geo.IZsym, geo.ZSym, 0.0, 2.0, vortexGeoms, vortexPoints, true);

        // Body source/doublet AIC (SRDSET/VSRD): the body induces a known onset velocity
        // at each wing control point (enters the RHS below) and vortex midpoint (VELSUM).
        var bodySegs = BodyAero.SrdSet(betm, xyzref, geo.IYsym, geo.Bodies);
        double[][][]? wcSrd = null, wvSrd = null;
        if (bodySegs.Count > 0)
        {
            var cpPos = geo.Vortices.Select(v => v.Rc).ToList();
            var vpPos = geo.Vortices.Select(v => v.Rv).ToList();
            wcSrd = BodyAero.Vsrd(betm, geo.IYsym, 0, geo.IZsym, geo.ZSym, bodySegs, cpPos);
            wvSrd = BodyAero.Vsrd(betm, geo.IYsym, 0, geo.IZsym, geo.ZSym, bodySegs, vpPos);
        }

        // Build AICN[i][j] = WC_GAM(i,j) . ENC(i)
        var aicn = new double[nvor * nvor];
        for (int i = 0; i < nvor; i++)
        {
            var enc = encalcResult.VortexNormals[i].Enc;
            var row = wcGam[i];
            for (int j = 0; j < nvor; j++)
            {
                var c = row[j];
                aicn[i * nvor + j] = c.U * enc[0] + c.V * enc[1] + c.W * enc[2];
            }
        }

        // TE-control-point row replacement for no-wake surfaces (sum_strip(Gamma) = 0).
        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            if (!geo.Surfaces[s].NoWake) continue;
            foreach (var strip in geo.Strips.Where(st => st.SurfaceIndex == s))
            {
                int iv = strip.FirstVortex + strip.Nvc - 1;
                for (int jv = 0; jv < nvor; jv++) aicn[iv * nvor + jv] = 0;
                for (int jv = strip.FirstVortex; jv <= iv; jv++) aicn[iv * nvor + jv] = 1.0;
            }
        }

        var aicFactored = DenseLU.LuFactor(aicn, nvor);

        // Per-vortex flags: LVNC (has a V.n equation) and LVALBE (surface has
        // freestream-angle influence enabled -- NOALBE keyword disables it).
        var hasVn = new bool[nvor];
        for (int i = 0; i < nvor; i++) hasVn[i] = true;
        var hasAlbe = new bool[nvor];
        for (int v = 0; v < nvor; v++)
        {
            hasAlbe[v] = !geo.Surfaces[geo.Strips[geo.Vortices[v].StripIndex].SurfaceIndex].NoAlbe;
        }
        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            if (!geo.Surfaces[s].NoWake) continue;
            foreach (var strip in geo.Strips.Where(st => st.SurfaceIndex == s))
            {
                hasVn[strip.FirstVortex + strip.Nvc - 1] = false;
            }
        }

        // GUCALC: 6 unit right-hand-sides plus each control variable's ENC_D-driven
        // sensitivity RHS, solved via the same AIC factorization.
        int nControl = geo.ControlNames.Count;
        var gamU0 = new List<double[]>();
        var gamUD = new List<double[][]>();
        for (int iu = 0; iu < 6; iu++)
        {
            var rhs = new double[nvor];
            var rhsD = new double[nControl][];
            for (int n = 0; n < nControl; n++) rhsD[n] = new double[nvor];
            for (int i = 0; i < nvor; i++)
            {
                if (!hasVn[i]) continue; // rhs stays 0, matching GUCALC's ELSE branch
                var enc = encalcResult.VortexNormals[i].Enc;
                double[] vunit;
                if (iu < 3)
                {
                    vunit = new double[] { 0, 0, 0 };
                    if (hasAlbe[i]) vunit[iu] = 1.0;
                }
                else
                {
                    var rrot = new double[]
                    {
                        geo.Vortices[i].Rc[0] - xyzref[0],
                        geo.Vortices[i].Rc[1] - xyzref[1],
                        geo.Vortices[i].Rc[2] - xyzref[2],
                    };
                    var wunit = new double[] { 0, 0, 0 };
                    if (hasAlbe[i]) wunit[iu - 3] = 1.0;
                    vunit = new double[]
                    {
                        rrot[1] * wunit[2] - rrot[2] * wunit[1],
                        rrot[2] * wunit[0] - rrot[0] * wunit[2],
                        rrot[0] * wunit[1] - rrot[1] * wunit[0],
                    };
                }
                // Always add the body's indirect freestream influence at this control point.
                if (wcSrd != null)
                {
                    vunit[0] += wcSrd[i][0][iu];
                    vunit[1] += wcSrd[i][1][iu];
                    vunit[2] += wcSrd[i][2][iu];
                }
                rhs[i] = -(enc[0] * vunit[0] + enc[1] * vunit[1] + enc[2] * vunit[2]);
                var encD = encalcResult.VortexNormals[i].EncD;
                for (int n = 0; n < nControl; n++)
                {
                    var ed = encD[n];
                    rhsD[n][i] = -(ed[0] * vunit[0] + ed[1] * vunit[1] + ed[2] * vunit[2]);
                }
            }
            gamU0.Add(DenseLU.LuSolve(aicFactored, rhs));
            var solvedD = new double[nControl][];
            for (int n = 0; n < nControl; n++) solvedD[n] = DenseLU.LuSolve(aicFactored, rhsD[n]);
            gamUD.Add(solvedD);
        }

        return new AicSetup
        {
            Betm = betm,
            Nvor = nvor,
            VortexGeoms = vortexGeoms,
            WcGam = wcGam,
            WvGam = wvGam,
            AicFactored = aicFactored,
            GamU0 = gamU0.ToArray(),
            GamUD = gamUD.ToArray(),
            BodySegs = bodySegs,
            WvSrd = wvSrd,
        };
    }
}
