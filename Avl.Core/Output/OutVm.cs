// Port of getvm.f SUBROUTINE OUTVM (the "VM" command): integrates each surface's
// spanload (CNC) from tip to root to get shear Vz and bending moment Mx vs 2Y/Bref.
//
// The numeric data rows use fixed FORMAT (F10.4,G14.6,3X,G14.6) and are byte-parity
// with the reference exe; the list-directed header lines (2Ymin/2Ymax, etc.) are
// reproduced structurally (they aren't part of the machine-parsed data rows).
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using System.Globalization;
using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutVm
{
    private const double DTR = Math.PI / 180.0;

    private static string Row(double y2bref, double vz, double mx) =>
        FortranF(y2bref, 10, 4) + FortranG(vz, 14, 6) + "   " + FortranG(mx, 14, 6);

    public static string FormatOutVm(Geometry geo, CaseResult result)
    {
        var totals = result.Totals;
        double sref = geo.Sref;
        double bref = geo.Bref;
        var lines = new List<string>();

        // FORMAT 10 header (leading '/' => a blank first line).
        lines.Add("");
        lines.Add(" Shear/q and Bending Moment/q vs Y");
        lines.Add("  Configuration: " + OutTotal.Slice(geo.Title, 60));
        lines.Add("  Mach  = " + FortranF(result.Mach, 8, 3));
        lines.Add("  alpha = " + FortranF(result.Alfa / DTR, 8, 3) + "    CLtot = " + FortranF(totals.ClTot, 8, 3));
        lines.Add("  beta  = " + FortranF(result.Beta / DTR, 8, 3));
        lines.Add("");
        lines.Add("  Sref  = " + FortranF(sref, 11, 5));
        lines.Add("  Bref  = " + FortranF(bref, 11, 5));

        for (int n = 0; n < geo.Surfaces.Count; n++)
        {
            // Global strip indices belonging to surface n, in ascending (J1..JN) order.
            var js = new List<int>();
            for (int j = 0; j < geo.Strips.Count; j++) if (geo.Strips[j].SurfaceIndex == n) js.Add(j);
            if (js.Count == 0) continue;
            int m = js.Count;

            double ymin = 1e10, ymax = -1e10;
            foreach (var j in js)
            {
                var s = geo.Strips[j];
                ymin = Math.Min(ymin, Math.Min(s.Rle1[1], s.Rle2[1]));
                ymax = Math.Max(ymax, Math.Max(s.Rle1[1], s.Rle2[1]));
            }

            // Integrate spanload from last strip (tip) to first (root). 1-based arrays.
            var ystrp = new double[m + 1];
            var vArr = new double[m + 1];
            var bmArr = new double[m + 1];
            double cnclst = 0, bmlst = 0, wlst = 0, vlst = 0, dy = 0;
            for (int i = m - 1; i >= 0; i--)
            {
                int jj = i + 1;
                var s = geo.Strips[js[i]];
                double cnc = totals.ByStrip[js[i]].Cnc;
                dy = 0.5 * (s.Wstrip + wlst);
                ystrp[jj] = s.Rle[1];
                vArr[jj] = vlst + 0.5 * (cnc + cnclst) * dy;
                bmArr[jj] = bmlst + 0.5 * (vArr[jj] + vlst) * dy;
                vlst = vArr[jj];
                bmlst = bmArr[jj];
                cnclst = cnc;
                wlst = s.Wstrip;
            }

            double vroot = vlst + cnclst * 0.5 * dy;
            double bmroot = bmlst + 0.5 * (vroot + vlst) * 0.5 * dy;
            double vtip = 0.0, bmtip = 0.0;

            var surf = geo.Surfaces[n];
            double yroot, ytip;
            if (surf.ImageSign >= 0)
            {
                yroot = geo.Strips[js[0]].Rle1[1];
                ytip = geo.Strips[js[m - 1]].Rle2[1];
            }
            else
            {
                yroot = geo.Strips[js[0]].Rle2[1];
                ytip = geo.Strips[js[m - 1]].Rle1[1];
            }

            double dir = (ymin + ymax < 0.0) ? -1.0 : 1.0;

            lines.Add("   ");
            lines.Add(" Surface: " + FortranI(n + 1, 3) + "  ");
            lines.Add(OutTotal.Slice(surf.Title, 40).PadRight(40));
            lines.Add("     2Ymin/Bref =    " + (2.0 * ymin / bref).ToString("F16", CultureInfo.InvariantCulture));
            lines.Add("     2Ymax/Bref =    " + (2.0 * ymax / bref).ToString("F16", CultureInfo.InvariantCulture));
            lines.Add("   2Y/Bref      Vz/(q*Sref)      Mx/(q*Bref*Sref)");

            lines.Add(Row(2.0 * yroot / bref, vroot / sref, dir * bmroot / sref / bref));
            for (int jj = 1; jj <= m; jj++)
                lines.Add(Row(2.0 * ystrp[jj] / bref, vArr[jj] / sref, dir * bmArr[jj] / sref / bref));
            lines.Add(Row(2.0 * ytip / bref, vtip / sref, dir * bmtip / sref / bref));
        }

        return string.Join("\n", lines);
    }
}
