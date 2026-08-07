// Port of aoutput.f SUBROUTINE OUTELE (the "FE" command's per-panel/vortex
// force block).
//
// Port of packages/avl-core/src/output/outElement.ts.

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutElement
{
    private static (List<int> stripIdxs, double area, double cave) SurfaceGeometry(Geometry geo, int s)
    {
        var stripIdxs = new List<int>();
        for (int j = 0; j < geo.Strips.Count; j++) if (geo.Strips[j].SurfaceIndex == s) stripIdxs.Add(j);
        double area = 0, wtot = 0;
        foreach (var j in stripIdxs)
        {
            area += geo.Strips[j].Chord * geo.Strips[j].Wstrip;
            wtot += geo.Strips[j].Wstrip;
        }
        double cave = wtot == 0 ? 0 : area / wtot;
        return (stripIdxs, area, cave);
    }

    public static string FormatOutElement(Geometry geo, CaseResult result)
    {
        var totals = result.Totals;
        const string sep = " ---------------------------------------------------------------";
        string stars = " " + new string('*', 78);
        const double DTR = Math.PI / 180.0;

        var lines = new List<string>();
        lines.Add(sep);
        lines.Add(" Vortex Strengths (by surface, by strip)");
        lines.Add("");
        lines.Add("  Forces referred to Sref, Cref, Bref about Xref, Yref, Zref");
        lines.Add("  Standard axis orientation,  X fwd, Z down");

        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            var surf = geo.Surfaces[s];
            var (stripIdxs, area, cave) = SurfaceGeometry(geo, s);
            if (stripIdxs.Count == 0) continue;
            var bs = totals.BySurface[s];

            lines.Add("");
            lines.Add(stars);
            lines.Add($"  Surface #{FortranI(s + 1, 2)}     {surf.Title}");
            lines.Add($"     # Chordwise  ={FortranI(surf.Nvc, 3)}   # Spanwise ={FortranI(stripIdxs.Count, 3)}   First strip  ={FortranI(stripIdxs[0] + 1, 4)}");
            lines.Add($"    Surface area ={FortranF(area, 12, 6)}       Ave. chord ={FortranF(cave, 12, 6)}");
            lines.Add($"     CLsurf  ={FortranF(bs.ClSurf, 10, 5)}     Clsurf  ={FortranF(-1 * bs.CmSurf[0], 10, 5)}");
            lines.Add($"     CYsurf  ={FortranF(bs.CfSurf[1], 10, 5)}     Cmsurf  ={FortranF(bs.CmSurf[1], 10, 5)}");
            lines.Add($"     CDsurf  ={FortranF(bs.CdSurf, 10, 5)}     Cnsurf  ={FortranF(-1 * bs.CmSurf[2], 10, 5)}");
            lines.Add($"     CDisurf ={FortranF(bs.CdSurf, 10, 5)}     CDvsurf ={FortranF(0, 10, 5)}");
            lines.Add("");
            lines.Add("  Forces referred to Ssurf, Cave about hinge axis thru LE");

            double enY = 0, enZ = 0;
            foreach (var j in stripIdxs)
            {
                double sr = geo.Strips[j].Chord * geo.Strips[j].Wstrip;
                enY += sr * result.StripAxes[j].Ensy;
                enZ += sr * result.StripAxes[j].Ensz;
            }
            double enaveY = area == 0 ? 0 : enY / area;
            double enaveZ = area == 0 ? 0 : enZ / area;
            double enMag = Math.Sqrt(enaveY * enaveY + enaveZ * enaveZ);
            if (enMag == 0) enaveZ = 1.0;
            else { enaveY /= enMag; enaveZ /= enMag; }
            var spn = new double[] { 0, enaveZ, -enaveY };
            var udrag = result.Vinf;
            var ulift = new double[]
            {
                udrag[1] * spn[2] - udrag[2] * spn[1],
                udrag[2] * spn[0] - udrag[0] * spn[2],
                udrag[0] * spn[1] - udrag[1] * spn[0],
            };
            double ulMag = Math.Sqrt(ulift[0] * ulift[0] + ulift[1] * ulift[1] + ulift[2] * ulift[2]);
            ulift = ulMag == 0 ? new double[] { 0, 0, 1 } : new double[] { ulift[0] / ulMag, ulift[1] / ulMag, ulift[2] / ulMag };
            var cfLsrf = area == 0
                ? new double[] { 0, 0, 0 }
                : new double[] { (bs.CfSurf[0] * geo.Sref) / area, (bs.CfSurf[1] * geo.Sref) / area, (bs.CfSurf[2] * geo.Sref) / area };
            double clLsrf = ulift[0] * cfLsrf[0] + ulift[1] * cfLsrf[1] + ulift[2] * cfLsrf[2];
            double cdLsrf = udrag[0] * cfLsrf[0] + udrag[1] * cfLsrf[1] + udrag[2] * cfLsrf[2];
            lines.Add($"     CLsurf  ={FortranF(clLsrf, 10, 5)}     CDsurf  ={FortranF(cdLsrf, 10, 5)}");
            lines.Add(stars);

            foreach (var j in stripIdxs)
            {
                var strip = geo.Strips[j];
                var bst = totals.ByStrip[j];
                double astrp = strip.Wstrip * strip.Chord;
                var axes = result.StripAxes[j];
                double dihed = (-Math.Atan2(axes.Ensy, axes.Ensz)) / DTR;

                lines.Add("");
                lines.Add($" Strip #{FortranI(j + 1, 3)}     # Chordwise ={FortranI(strip.Nvc, 3)}   First Vortex ={FortranI(strip.FirstVortex + 1, 4)}");
                lines.Add($"    Xle ={FortranF(strip.Rle[0], 10, 5)}    Ave. Chord   ={FortranF(strip.Chord, 10, 4)}   Incidence  ={FortranF(strip.Ainc / DTR, 10, 4)} deg");
                lines.Add($"    Yle ={FortranF(strip.Rle[1], 10, 5)}    Strip Width  ={FortranF(strip.Wstrip, 10, 5)}   Strip Area ={FortranF(astrp, 12, 6)}");
                lines.Add($"    Zle ={FortranF(strip.Rle[2], 10, 5)}    Strip Dihed. ={FortranF(dihed, 10, 4)}");
                lines.Add("");
                lines.Add($"    cl  ={FortranF(bst.ClLstrp, 10, 5)}       cd  ={FortranF(bst.CdLstrp, 10, 5)}      cdv ={FortranF(0, 10, 5)}");
                lines.Add($"    cn  ={FortranF(bst.CnLstrp, 10, 5)}       ca  ={FortranF(bst.CaLstrp, 10, 5)}      cnc ={FortranF(bst.Cnc, 10, 5)}    wake dnwsh ={FortranF(result.Trefftz.Dwwake[j], 10, 5)}");
                lines.Add($"    cmLE={FortranF(bst.CmleLstrp, 10, 5)}    cm c/4 ={FortranF(bst.Cmc4Lstrp, 10, 5)}");
                lines.Add("");
                lines.Add("    I        X           Y           Z           DX        Slope        dCp");

                for (int ii = 0; ii < strip.Nvc; ii++)
                {
                    int i = strip.FirstVortex + ii;
                    var v = geo.Vortices[i];
                    double xm = 0.5 * (v.Rv1[0] + v.Rv2[0]);
                    double ym = 0.5 * (v.Rv1[1] + v.Rv2[1]);
                    double zm = 0.5 * (v.Rv1[2] + v.Rv2[2]);
                    lines.Add(
                        $"  {FortranI(i + 1, 3)}  {FortranF(xm, 10, 5)}  {FortranF(ym, 10, 5)}  {FortranF(zm, 10, 5)}  {FortranF(v.Dx, 10, 5)}  {FortranF(v.Slopec, 10, 5)}  {FortranF(totals.Dcp[i], 10, 5)}");
                }
            }
        }
        lines.Add(sep);
        return string.Join("\n", lines);
    }
}
