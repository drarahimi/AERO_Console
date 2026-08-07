// Port of aoutput.f SUBROUTINE OUTSTRP (the "FS" command's stability-axis
// per-strip force block) and SUBROUTINE OUTSTRPB (the "FSB" body-axes variant).
//
// Port of packages/avl-core/src/output/outStrip.ts.

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutStrip
{
    private const double DIR = -1.0; // GETSA(LNASA_SA=.TRUE.)
    private const double DTR = Math.PI / 180.0;

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

    public static string FormatOutStrip(Geometry geo, CaseResult result)
    {
        var totals = result.Totals;
        const string sep = " ---------------------------------------------------------------";
        var lines = new List<string>();
        lines.Add(sep);
        lines.Add(" Surface and Strip Forces by surface");
        lines.Add("");
        lines.Add($"  Sref ={FortranG(geo.Sref, 12, 5)}   Cref ={FortranG(geo.Cref, 12, 5)}   Bref ={FortranG(geo.Bref, 12, 5)}");
        lines.Add($"  Xref ={FortranG(geo.Xyzref0[0], 12, 5)}   Yref ={FortranG(geo.Xyzref0[1], 12, 5)}   Zref ={FortranG(geo.Xyzref0[2], 12, 5)}");

        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            var surf = geo.Surfaces[s];
            var (stripIdxs, area, cave) = SurfaceGeometry(geo, s);
            if (stripIdxs.Count == 0) continue;

            lines.Add("");
            lines.Add($"  Surface #{FortranI(s + 1, 2)}     {surf.Title}");
            lines.Add($"     # Chordwise ={FortranI(surf.Nvc, 3)}   # Spanwise ={FortranI(stripIdxs.Count, 3)}     First strip ={FortranI(stripIdxs[0] + 1, 3)}");
            lines.Add($"     Surface area Ssurf ={FortranF(area, 12, 6)}     Ave. chord Cave ={FortranF(cave, 12, 6)}");
            lines.Add("");
            lines.Add(" Forces referred to Sref, Cref, Bref about Xref, Yref, Zref");
            lines.Add(" Standard axis orientation,  X fwd, Z down");
            var bs = totals.BySurface[s];
            double cdisurf = bs.CdSurf;
            lines.Add($"     CLsurf  ={FortranF(bs.ClSurf, 10, 5)}     Clsurf  ={FortranF(DIR * bs.CmSurf[0], 10, 5)}");
            lines.Add($"     CYsurf  ={FortranF(bs.CfSurf[1], 10, 5)}     Cmsurf  ={FortranF(bs.CmSurf[1], 10, 5)}");
            lines.Add($"     CDsurf  ={FortranF(bs.CdSurf, 10, 5)}     Cnsurf  ={FortranF(DIR * bs.CmSurf[2], 10, 5)}");
            lines.Add($"     CDisurf ={FortranF(cdisurf, 10, 5)}     CDvsurf ={FortranF(0, 10, 5)}");
            lines.Add("");
            lines.Add(" Forces referred to Ssurf, Cave ");

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

            lines.Add("");
            lines.Add(" Strip Forces referred to Strip Area, Chord");
            lines.Add("    j     Xle      Yle      Zle      Chord    Area     c_cl     ai     cl_norm    cl       cd       cdv    cm_c/4     cm_LE   C.P.x/c");

            foreach (var j in stripIdxs)
            {
                var strip = geo.Strips[j];
                var bst = totals.ByStrip[j];
                double astrp = strip.Wstrip * strip.Chord;
                double xcp = 999.0;
                if (bst.ClLstrp != 0) xcp = 0.25 - bst.Cmc4Lstrp / bst.ClLstrp;
                if (!(xcp < 2.0 && xcp > -1.0)) xcp = 999.0;

                lines.Add(
                    $"  {FortranI(j + 1, 4)}" +
                    $" {FortranF(strip.Rle[0], 8, 4)} {FortranF(strip.Rle[1], 8, 4)} {FortranF(strip.Rle[2], 8, 4)}" +
                    $" {FortranF(strip.Chord, 8, 4)} {FortranF(astrp, 8, 4)} {FortranF(bst.Cnc, 8, 4)} {FortranF(result.Trefftz.Dwwake[j], 8, 4)}" +
                    $" {FortranF(bst.CltLstrp, 8, 4)} {FortranF(bst.ClLstrp, 8, 4)} {FortranF(bst.CdLstrp, 8, 4)} {FortranF(0, 8, 4)}" +
                    $" {FortranF(bst.Cmc4Lstrp, 8, 4)} {FortranF(bst.CmleLstrp, 8, 4)} {FortranF(xcp, 8, 3)}");
            }
        }
        lines.Add(sep);
        return string.Join("\n", lines);
    }

    public static string FormatOutStripBody(Geometry geo, CaseResult result)
    {
        var totals = result.Totals;
        var stripAxes = result.StripAxes;
        const string sep = " ---------------------------------------------------------------";
        var lines = new List<string>();
        lines.Add(sep);
        lines.Add(" Surface and Strip Forces by surface");
        lines.Add("");
        lines.Add($"  Sref ={FortranG(geo.Sref, 12, 5)}   Cref ={FortranG(geo.Cref, 12, 5)}   Bref ={FortranG(geo.Bref, 12, 5)}");
        lines.Add($"  Xref ={FortranG(geo.Xyzref0[0], 12, 5)}   Yref ={FortranG(geo.Xyzref0[1], 12, 5)}   Zref ={FortranG(geo.Xyzref0[2], 12, 5)}");

        for (int s = 0; s < geo.Surfaces.Count; s++)
        {
            var surf = geo.Surfaces[s];
            var stripIdxs = new List<int>();
            for (int j = 0; j < geo.Strips.Count; j++) if (geo.Strips[j].SurfaceIndex == s) stripIdxs.Add(j);
            if (stripIdxs.Count == 0) continue;

            double area = 0, wtot = 0;
            foreach (var j in stripIdxs)
            {
                area += geo.Strips[j].Chord * geo.Strips[j].Wstrip;
                wtot += geo.Strips[j].Wstrip;
            }
            double cave = wtot == 0 ? 0 : area / wtot;

            lines.Add("");
            lines.Add($"  Surface #{FortranI(s + 1, 2)}     {surf.Title}");
            lines.Add($"     # Chordwise ={FortranI(surf.Nvc, 3)}   # Spanwise ={FortranI(stripIdxs.Count, 3)}     First strip ={FortranI(stripIdxs[0] + 1, 3)}");
            lines.Add($"     Surface area Ssurf ={FortranF(area, 12, 6)}     Ave. chord Cave ={FortranF(cave, 12, 6)}");
            lines.Add("");
            lines.Add(" Body Axes Forces referred to Sref, Cref, Bref about Xref, Yref, Zref");
            lines.Add(" Standard axis orientation,  X fwd, Z down");
            var bs = totals.BySurface[s];
            lines.Add($"     CFXsurf  ={FortranG(DIR * bs.CfSurf[0], 12, 5)}     CMXsurf  ={FortranG(DIR * bs.CmSurf[0], 12, 5)}");
            lines.Add($"     CFYsurf  ={FortranG(bs.CfSurf[1], 12, 5)}     CMYsurf  ={FortranG(bs.CmSurf[1], 12, 5)}");
            lines.Add($"     CFZsurf  ={FortranG(DIR * bs.CfSurf[2], 12, 5)}     CMZsurf  ={FortranG(DIR * bs.CmSurf[2], 12, 5)}");
            lines.Add("");
            lines.Add(" Body Axes Strip Forces referred to Strip Area, Chord, about strip c/4");
            lines.Add(" ");
            lines.Add("    j      Xc/4       Yc/4       Zc/4       Chord      Area      Dihedral    Incid       CFXstrp      CFYstrp      CFZstrp      CMXstrp      CMYstrp      CMZstrp      CNRMstrp     CAXLstrp");

            foreach (var j in stripIdxs)
            {
                var strip = geo.Strips[j];
                var bst = totals.ByStrip[j];
                var axes = stripAxes[j];
                double xc4 = strip.Rle[0] + 0.25 * strip.Chord;
                double astrp = strip.Wstrip * strip.Chord;
                double dihed = -Math.Atan2(axes.Ensy, axes.Ensz) / DTR;
                double incid = strip.Ainc / DTR;
                lines.Add(
                    $"  {FortranI(j + 1, 4)}" +
                    $" {FortranF(xc4, 10, 5)} {FortranF(strip.Rle[1], 10, 5)} {FortranF(strip.Rle[2], 10, 5)}" +
                    $" {FortranF(strip.Chord, 10, 5)} {FortranF(astrp, 10, 5)} {FortranF(dihed, 10, 5)} {FortranF(incid, 10, 5)}" +
                    $"    {FortranG(DIR * bst.CfLstrp[0], 12, 5)} {FortranG(bst.CfLstrp[1], 12, 5)} {FortranG(DIR * bst.CfLstrp[2], 12, 5)}" +
                    $" {FortranG(DIR * bst.CmLstrp[0], 12, 5)} {FortranG(bst.CmLstrp[1], 12, 5)} {FortranG(DIR * bst.CmLstrp[2], 12, 5)}" +
                    $" {FortranG(bst.CnLstrp, 12, 5)} {FortranG(bst.CaLstrp, 12, 5)}");
            }
        }
        lines.Add(sep);
        return string.Join("\n", lines);
    }
}
