// Port of aero.f SUBROUTINE SFFORC (strip/surface/total force integration)
// and the CDoref add-on from SUBROUTINE AERO.
//
// Scope note: control/design-variable force sensitivities (*_U/_D/_G),
// the LTRFORCE trailing-leg-on-surface block (off by default in avl.f DEFINI),
// the LVISC section CD(CL) polar, and body forces (BDFORC) are not ported.
// IYSYM=1 mirror-image doubling is also not ported since the current fixtures
// use explicit YDUPLICATE surfaces instead of the iYsym flag.
//
// Port of packages/avl-core/src/solver/force.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class SurfaceForce
{
    public double CdSurf;
    public double CySurf;
    public double ClSurf;
    public double[] CfSurf = new double[3];
    public double[] CmSurf = new double[3];
}

public sealed class StripForce
{
    /// <summary>Body-axis strip forces, referred to strip c/4 and normalized by strip area/chord.</summary>
    public double[] CfLstrp = new double[3];
    /// <summary>Body-axis strip moments about strip c/4.</summary>
    public double[] CmLstrp = new double[3];
    /// <summary>Axial/normal force in the strip's own incidence-rotated plane.</summary>
    public double CaLstrp;
    public double CnLstrp;
    /// <summary>Strip lift/drag in the strip's own local lift/drag directions, for FS.</summary>
    public double ClLstrp;
    public double CdLstrp;
    /// <summary>Strip moment about c/4 along the LE-segment direction.</summary>
    public double Cmc4Lstrp;
    /// <summary>Strip moment about the LE midpoint along the LE-segment direction.</summary>
    public double CmleLstrp;
    /// <summary>ClLstrp normalized by local dynamic pressure (perp-to-span velocity).</summary>
    public double CltLstrp;
    /// <summary>Strip spanwise loading c*cn.</summary>
    public double Cnc;
}

public sealed class Totals
{
    public double CxTot;
    public double CyTot;
    public double CzTot;
    public double[] CfTot = new double[3]; // body-axes forces
    public double[] CmTot = new double[3]; // body-axes moments about xyzref
    public double ClTot;
    public double CdTot;
    public double CdvTot; // viscous+reference drag
    /// <summary>Per-surface breakdown, for the FN command.</summary>
    public SurfaceForce[] BySurface = Array.Empty<SurfaceForce>();
    /// <summary>Per-strip breakdown, for the FSB command.</summary>
    public StripForce[] ByStrip = Array.Empty<StripForce>();
    /// <summary>Per-control hinge-moment coefficient, for the HM command.</summary>
    public double[] Chinge = Array.Empty<double>();
    /// <summary>Per-vortex delta-Cp loading, for the FE command.</summary>
    public double[] Dcp = Array.Empty<double>();
    /// <summary>Per-body force breakdown (BDFORC), for the FB command. Empty when no bodies.</summary>
    public BodyForce[] ByBody = Array.Empty<BodyForce>();
}

public static class Force
{
    private static double[] Cross(double[] a, double[] b) => new double[]
    {
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    };

    public static Totals ComputeForces(
        Geometry geo,
        EncalcResult enc,
        double[] gam,
        double[][] vv,
        double[] vinf,
        double[] wrot,
        double alfa,
        double cdref,
        double[]? xyzref = null)
    {
        xyzref ??= geo.Xyzref0;
        double sina = Math.Sin(alfa);
        double cosa = Math.Cos(alfa);
        double sref = geo.Sref;

        int nsurf = geo.Surfaces.Count;
        var cdSurf = new double[nsurf];
        var cySurf = new double[nsurf];
        var clSurf = new double[nsurf];
        var cfSurf = new double[nsurf][];
        var cmSurf = new double[nsurf][];
        for (int s = 0; s < nsurf; s++) { cfSurf[s] = new double[3]; cmSurf[s] = new double[3]; }
        var byStrip = new StripForce[geo.Strips.Count];
        int nControl = geo.ControlNames.Count;
        var chinge = new double[nControl];
        var dcp = new double[geo.Vortices.Count];

        for (int j = 0; j < geo.Strips.Count; j++)
        {
            var strip = geo.Strips[j];
            double cr = strip.Chord;
            double sr = strip.Chord * strip.Wstrip;
            var rc4 = new double[] { strip.Rle[0] + 0.25 * cr, strip.Rle[1], strip.Rle[2] };

            double cfx = 0, cfy = 0, cfz = 0, cmx = 0, cmy = 0, cmz = 0;

            for (int ii = 0; ii < strip.Nvc; ii++)
            {
                int i = strip.FirstVortex + ii;
                var v = geo.Vortices[i];

                var r = new double[] { v.Rv[0] - rc4[0], v.Rv[1] - rc4[1], v.Rv[2] - rc4[2] };
                var rrot = new double[] { v.Rv[0] - xyzref[0], v.Rv[1] - xyzref[1], v.Rv[2] - xyzref[2] };
                var vrot = Cross(rrot, wrot);

                var veff = new double[] { vinf[0] + vrot[0] + vv[i][0], vinf[1] + vrot[1] + vv[i][1], vinf[2] + vrot[2] + vv[i][2] };

                var g = new double[] { v.Rv2[0] - v.Rv1[0], v.Rv2[1] - v.Rv1[1], v.Rv2[2] - v.Rv1[2] };
                var f = Cross(veff, g);
                var fgam = new double[] { 2.0 * gam[i] * f[0], 2.0 * gam[i] * f[1], 2.0 * gam[i] * f[2] };

                // Delta-Cp loading (aero.f:492-499).
                var env = enc.VortexNormals[i].Env;
                double fnv = env[0] * fgam[0] + env[1] * fgam[1] + env[2] * fgam[2];
                dcp[i] = fnv / (v.Dx * strip.Wstrip);

                double dcfx = fgam[0] / sr;
                double dcfy = fgam[1] / sr;
                double dcfz = fgam[2] / sr;

                cfx += dcfx;
                cfy += dcfy;
                cfz += dcfz;
                cmx += (dcfz * r[1] - dcfy * r[2]) / cr;
                cmy += (dcfx * r[2] - dcfz * r[0]) / cr;
                cmz += (dcfy * r[0] - dcfx * r[1]) / cr;

                // Hinge moments (aero.f SFFORC:580-603).
                for (int l = 0; l < nControl; l++)
                {
                    double dfac = v.Dcontrol[l] / (sref * geo.Cref);
                    if (dfac == 0) continue;
                    var hinge = strip.Vhinge[l];
                    var phinge = strip.Phinge[l];
                    var rh = new double[] { v.Rv[0] - phinge[0], v.Rv[1] - phinge[1], v.Rv[2] - phinge[2] };
                    var mh = Cross(rh, fgam);
                    chinge[l] += (mh[0] * hinge[0] + mh[1] * hinge[1] + mh[2] * hinge[2]) * dfac;
                }
            }

            // Axial/normal force rotated into the strip's own incidence plane (aero.f:1070-1077).
            var axes = enc.StripAxes[j];
            double caxl0 = cfx;
            double cnrm0 = axes.Ensy * cfy + axes.Ensz * cfz;
            double sinAinc = Math.Sin(strip.Ainc);
            double cosAinc = Math.Cos(strip.Ainc);

            // Strip local lift/drag directions (aero.f:257-273).
            var spn = new double[] { 0, axes.Ensz, -axes.Ensy };
            var ulift = Cross(vinf, spn);
            double ulMag = Math.Sqrt(ulift[0] * ulift[0] + ulift[1] * ulift[1] + ulift[2] * ulift[2]);
            ulift = ulMag == 0 ? new double[] { 0, 0, 1 } : new double[] { ulift[0] / ulMag, ulift[1] / ulMag, ulift[2] / ulMag };
            double clLstrp = ulift[0] * cfx + ulift[1] * cfy + ulift[2] * cfz;
            double cdLstrp = vinf[0] * cfx + vinf[1] * cfy + vinf[2] * cfz;
            double cmc4Lstrp = axes.Ensz * cmy - axes.Ensy * cmz;

            // Local dynamic pressure at the strip's SAXFR reference point (aero.f:1079-1112).
            var rrotS = new double[] { axes.Sref[0] - xyzref[0], axes.Sref[1] - xyzref[1], axes.Sref[2] - xyzref[2] };
            var vrotS = Cross(rrotS, wrot);
            var veffS = new double[] { vinf[0] + vrotS[0], vinf[1] + vrotS[1], vinf[2] + vrotS[2] };
            double vspan = veffS[0] * axes.Ess[0] + veffS[1] * axes.Ess[1] + veffS[2] * axes.Ess[2];
            var vperp = new double[] { veffS[0] - axes.Ess[0] * vspan, veffS[1] - axes.Ess[1] * vspan, veffS[2] - axes.Ess[2] * vspan };
            double vpsq = vperp[0] * vperp[0] + vperp[1] * vperp[1] + vperp[2] * vperp[2];
            double vpsqi = vpsq == 0 ? 1.0 : 1.0 / vpsq;
            double cltLstrp = clLstrp * vpsqi;

            // Strip moment about the LE midpoint, along the LE-segment direction (aero.f:1114-1133).
            var rle3 = new double[] { rc4[0] - strip.Rle[0], rc4[1] - strip.Rle[1], rc4[2] - strip.Rle[2] };
            double delx = strip.Rle2[0] - strip.Rle1[0];
            double dely = strip.Rle2[1] - strip.Rle1[1];
            double delz = strip.Rle2[2] - strip.Rle1[2];
            if (geo.Surfaces[strip.SurfaceIndex].ImageSign < 0)
            {
                delx = -delx;
                dely = -dely;
                delz = -delz;
            }
            double dmag = Math.Sqrt(delx * delx + dely * dely + delz * delz);
            double cmleLstrp = 0;
            if (dmag != 0)
            {
                cmleLstrp =
                    (delx / dmag) * (cmx + (cfz * rle3[1] - cfy * rle3[2]) / cr) +
                    (dely / dmag) * (cmy + (cfx * rle3[2] - cfz * rle3[0]) / cr) +
                    (delz / dmag) * (cmz + (cfy * rle3[0] - cfx * rle3[1]) / cr);
            }

            byStrip[j] = new StripForce
            {
                CfLstrp = new double[] { cfx, cfy, cfz },
                CmLstrp = new double[] { cmx, cmy, cmz },
                CaLstrp = caxl0 * cosAinc - cnrm0 * sinAinc,
                CnLstrp = cnrm0 * cosAinc + caxl0 * sinAinc,
                ClLstrp = clLstrp,
                CdLstrp = cdLstrp,
                Cmc4Lstrp = cmc4Lstrp,
                CmleLstrp = cmleLstrp,
                CltLstrp = cltLstrp,
                Cnc = cr * (axes.Ensy * cfy + axes.Ensz * cfz),
            };

            double cdStrip = cfx * cosa + cfz * sina;
            double cyStrip = cfy;
            double clStrip = -cfx * sina + cfz * cosa;

            // moments about the case reference point XYZREF
            var r2 = new double[] { rc4[0] - xyzref[0], rc4[1] - xyzref[1], rc4[2] - xyzref[2] };
            var cmStrip = new double[]
            {
                cmx + (cfz * r2[1] - cfy * r2[2]) / cr,
                cmy + (cfx * r2[2] - cfz * r2[0]) / cr,
                cmz + (cfy * r2[0] - cfx * r2[1]) / cr,
            };

            int sIdx = strip.SurfaceIndex;
            cdSurf[sIdx] += (cdStrip * sr) / sref;
            cySurf[sIdx] += (cyStrip * sr) / sref;
            clSurf[sIdx] += (clStrip * sr) / sref;
            cfSurf[sIdx][0] += (cfx * sr) / sref;
            cfSurf[sIdx][1] += (cfy * sr) / sref;
            cfSurf[sIdx][2] += (cfz * sr) / sref;
            cmSurf[sIdx][0] += cmStrip[0] * (sr / sref) * (cr / geo.Bref);
            cmSurf[sIdx][1] += cmStrip[1] * (sr / sref) * (cr / geo.Cref);
            cmSurf[sIdx][2] += cmStrip[2] * (sr / sref) * (cr / geo.Bref);
        }

        double cdTot = 0, cyTot = 0, clTot = 0, cdvTot = 0;
        var cfTot = new double[] { 0, 0, 0 };
        var cmTot = new double[] { 0, 0, 0 };

        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            if (geo.Surfaces[s].NoLoad) continue;
            cdTot += cdSurf[s];
            cyTot += cySurf[s];
            clTot += clSurf[s];
            cfTot[0] += cfSurf[s][0];
            cfTot[1] += cfSurf[s][1];
            cfTot[2] += cfSurf[s][2];
            cmTot[0] += cmSurf[s][0];
            cmTot[1] += cmSurf[s][1];
            cmTot[2] += cmSurf[s][2];
        }

        // XZ symmetry-plane case (IYSYM=1, aero.f:93-104): the model is the y>=0 half,
        // solved with y=0 image vortices already in the AIC. The reported totals cover
        // the whole aircraft: double the symmetric components, zero the antisymmetric
        // ones (side force CY, roll Cl, yaw Cn all cancel across the symmetry plane).
        if (geo.IYsym == 1)
        {
            cdTot *= 2.0; cyTot = 0.0; clTot *= 2.0; cdvTot *= 2.0;
            cfTot[0] *= 2.0; cfTot[1] = 0.0; cfTot[2] *= 2.0;
            cmTot[0] = 0.0; cmTot[1] *= 2.0; cmTot[2] = 0.0;
        }

        // AERO's CDoref add-on (body-axis reference drag, aero.f:154-172)
        double vsq = vinf[0] * vinf[0] + vinf[1] * vinf[1] + vinf[2] * vinf[2];
        double vmag = Math.Sqrt(vsq);
        cdvTot += cdref * vsq;
        cdTot += cdref * vsq;
        cyTot += cdref * vinf[1] * vmag;
        cfTot[0] += cdref * vinf[0] * vmag;
        cfTot[1] += cdref * vinf[1] * vmag;
        cfTot[2] += cdref * vinf[2] * vmag;

        double cxTot = cdTot * cosa - clTot * sina;
        double czTot = cdTot * sina + clTot * cosa;

        var bySurface = new SurfaceForce[geo.Surfaces.Count];
        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            bySurface[s] = new SurfaceForce
            {
                CdSurf = cdSurf[s],
                CySurf = cySurf[s],
                ClSurf = clSurf[s],
                CfSurf = cfSurf[s],
                CmSurf = cmSurf[s],
            };
        }

        return new Totals
        {
            CxTot = cxTot,
            CyTot = cyTot,
            CzTot = czTot,
            CfTot = cfTot,
            CmTot = cmTot,
            ClTot = clTot,
            CdTot = cdTot,
            CdvTot = cdvTot,
            BySurface = bySurface,
            ByStrip = byStrip,
            Chinge = chinge,
            Dcp = dcp,
        };
    }
}
