// Port of aoutput.f SUBROUTINE OUTSURF (the "FN" command's "Surface Forces" block).
//
// Port of packages/avl-core/src/output/outSurface.ts.

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutSurface
{
    private const double DIR = -1.0; // GETSA(LNASA_SA=.TRUE.)

    private static double[] Cross(double[] a, double[] b) => new double[]
    {
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    };

    public static string FormatOutSurface(Geometry geo, CaseResult result)
    {
        var totals = result.Totals;
        var vinf = result.Vinf;
        var stripAxes = result.StripAxes;
        const string sep = " ---------------------------------------------------------------";

        var lines = new List<string>();
        lines.Add(sep);
        lines.Add(" Surface Forces (referred to Sref,Cref,Bref about Xref,Yref,Zref)");
        lines.Add(" Standard axis orientation,  X fwd, Z down");
        lines.Add("");
        lines.Add($"     Sref ={FortranG(geo.Sref, 12, 4)}   Cref ={FortranF(geo.Cref, 10, 4)}   Bref ={FortranF(geo.Bref, 10, 4)}");
        lines.Add($"     Xref =  {FortranF(geo.Xyzref0[0], 10, 4)}   Yref ={FortranF(geo.Xyzref0[1], 10, 4)}   Zref ={FortranF(geo.Xyzref0[2], 10, 4)}");
        lines.Add("");
        lines.Add(" n      Area      CL      CD      Cm      CY      Cn      Cl     CDi     CDv");

        int nsurf = geo.Surfaces.Count;
        var ssurf = new double[nsurf];
        var caveSurf = new double[nsurf];
        var clLsrf = new double[nsurf];
        var cdLsrf = new double[nsurf];

        for (int s = 0; s < nsurf; s++)
        {
            double area = 0, wtot = 0, enY = 0, enZ = 0;
            for (int j = 0; j < geo.Strips.Count; j++)
            {
                var strip = geo.Strips[j];
                if (strip.SurfaceIndex != s) continue;
                double sr = strip.Chord * strip.Wstrip;
                area += sr;
                wtot += strip.Wstrip;
                var axes = stripAxes[j];
                enY += sr * axes.Ensy;
                enZ += sr * axes.Ensz;
            }
            ssurf[s] = area;
            caveSurf[s] = wtot == 0 ? 0 : area / wtot;

            double enaveY = area == 0 ? 0 : enY / area;
            double enaveZ = area == 0 ? 0 : enZ / area;
            double enMag = Math.Sqrt(enaveY * enaveY + enaveZ * enaveZ);
            if (enMag == 0) enaveZ = 1.0;
            else { enaveY /= enMag; enaveZ /= enMag; }
            var spn = new double[] { 0, enaveZ, -enaveY };
            var udrag = vinf;
            var ulift = Cross(udrag, spn);
            double ulMag = Math.Sqrt(ulift[0] * ulift[0] + ulift[1] * ulift[1] + ulift[2] * ulift[2]);
            ulift = ulMag == 0 ? new double[] { 0, 0, 1 } : new double[] { ulift[0] / ulMag, ulift[1] / ulMag, ulift[2] / ulMag };

            var bs = totals.BySurface[s];
            var cfLsrf = area == 0
                ? new double[] { 0, 0, 0 }
                : new double[] { (bs.CfSurf[0] * geo.Sref) / area, (bs.CfSurf[1] * geo.Sref) / area, (bs.CfSurf[2] * geo.Sref) / area };
            clLsrf[s] = ulift[0] * cfLsrf[0] + ulift[1] * cfLsrf[1] + ulift[2] * cfLsrf[2];
            cdLsrf[s] = udrag[0] * cfLsrf[0] + udrag[1] * cfLsrf[1] + udrag[2] * cfLsrf[2];
        }

        for (int s = 0; s < nsurf; s++)
        {
            var bs = totals.BySurface[s];
            string n = (s + 1).ToString().PadLeft(2);
            double cdi = bs.CdSurf - 0;
            lines.Add(
                $"{n} {FortranF(ssurf[s], 9, 3)}{FortranF(bs.ClSurf, 8, 4)}{FortranF(bs.CdSurf, 8, 4)}{FortranF(bs.CmSurf[1], 8, 4)}" +
                $"{FortranF(bs.CfSurf[1], 8, 4)}{FortranF(DIR * bs.CmSurf[2], 8, 4)}{FortranF(DIR * bs.CmSurf[0], 8, 4)}{FortranF(cdi, 8, 4)}{FortranF(0, 8, 4)}   {geo.Surfaces[s].Title}");
        }

        lines.Add("");
        lines.Add(" Surface Forces (referred to Ssurf, Cave about root LE on hinge axis)");
        lines.Add("");
        lines.Add("   n     Ssurf      Cave       cl       cd      cdv");
        for (int s = 0; s < nsurf; s++)
        {
            string n = (s + 1).ToString().PadLeft(2);
            lines.Add(
                $"  {n}{FortranF(ssurf[s], 10, 3)}{FortranF(caveSurf[s], 10, 3)} {FortranF(clLsrf[s], 8, 4)} {FortranF(cdLsrf[s], 8, 4)} {FortranF(0, 8, 4)}  {geo.Surfaces[s].Title}");
        }
        lines.Add(sep);

        return string.Join("\n", lines);
    }
}
