// Port of aoutput.f SUBROUTINE OUTTOT (the "FT" command's "Vortex Lattice
// Output -- Total Forces" block) -- FORMAT statements 200-231.
//
// Port of packages/avl-core/src/output/outTotal.ts.

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutTotal
{
    private const double DTR = Math.PI / 180.0;
    private const double DIR = -1.0; // GETSA(LNASA_SA=.TRUE.) -> "Standard axis orientation, X fwd, Z down"

    internal static string Slice(string s, int len) => s.Length > len ? s.Substring(0, len) : s;

    public static string FormatOutTotal(Geometry geo, CaseResult result, double[]? xyzref = null, string title = " -unnamed-")
    {
        xyzref ??= geo.Xyzref0;
        double alfa = result.Alfa, beta = result.Beta, mach = result.Mach;
        var wrot = result.Wrot;
        var totals = result.Totals;
        var trefftz = result.Trefftz;
        double ca = Math.Cos(alfa);
        double sa = Math.Sin(alfa);

        double rxB = wrot[0] * (geo.Bref / 2.0);
        double ryB = wrot[1] * (geo.Cref / 2.0);
        double rzB = wrot[2] * (geo.Bref / 2.0);
        double rxS = (wrot[0] * ca + wrot[2] * sa) * (geo.Bref / 2.0);
        double rzS = (wrot[2] * ca - wrot[0] * sa) * (geo.Bref / 2.0);

        double crsax = totals.CmTot[0] * ca + totals.CmTot[2] * sa;
        double cnsax = totals.CmTot[2] * ca - totals.CmTot[0] * sa;

        double cditot = totals.CdTot - totals.CdvTot;

        var lines = new List<string>();
        const string sep = " ---------------------------------------------------------------";
        lines.Add(sep);
        lines.Add(" Vortex Lattice Output -- Total Forces");
        lines.Add("");
        lines.Add($" Configuration: {Slice(geo.Title, 60).PadRight(60)}");
        lines.Add($"     # Surfaces ={FortranI(geo.Surfaces.Count, 4)}");
        lines.Add($"     # Strips   ={FortranI(geo.Strips.Count, 4)}");
        lines.Add($"     # Vortices ={FortranI(geo.Vortices.Count, 4)}");
        lines.Add("");
        lines.Add($"  Sref ={FortranG(geo.Sref, 12, 5)}   Cref ={FortranG(geo.Cref, 12, 5)}   Bref ={FortranG(geo.Bref, 12, 5)}");
        lines.Add($"  Xref ={FortranG(xyzref[0], 12, 5)}   Yref ={FortranG(xyzref[1], 12, 5)}   Zref ={FortranG(xyzref[2], 12, 5)}");
        lines.Add("");
        // SATYPE is CHARACTER*50 in aoutput.f (WRITE 205: /1X,A) -- pad to its full field.
        lines.Add(" " + "Standard axis orientation,  X fwd, Z down".PadRight(50));
        lines.Add("");
        lines.Add($" Run case: {Slice(title, 40).PadRight(40)}");
        lines.Add("");
        lines.Add($"  Alpha ={FortranF(alfa / DTR, 10, 5)}     pb/2V ={FortranF(DIR * rxB, 10, 5)}     p'b/2V ={FortranF(DIR * rxS, 10, 5)}");
        lines.Add($"  Beta  ={FortranF(beta / DTR, 10, 5)}     qc/2V ={FortranF(ryB, 10, 5)}");
        lines.Add($"  Mach  ={FortranF(mach, 10, 3)}     rb/2V ={FortranF(DIR * rzB, 10, 5)}     r'b/2V ={FortranF(DIR * rzS, 10, 5)}");
        lines.Add("");
        lines.Add($"  CXtot ={FortranF(DIR * totals.CfTot[0], 10, 5)}     Cltot ={FortranF(DIR * totals.CmTot[0], 10, 5)}     Cl'tot ={FortranF(DIR * crsax, 10, 5)}");
        lines.Add($"  CYtot ={FortranF(totals.CfTot[1], 10, 5)}     Cmtot ={FortranF(totals.CmTot[1], 10, 5)}");
        lines.Add($"  CZtot ={FortranF(DIR * totals.CfTot[2], 10, 5)}     Cntot ={FortranF(DIR * totals.CmTot[2], 10, 5)}     Cn'tot ={FortranF(DIR * cnsax, 10, 5)}");
        lines.Add("");
        lines.Add($"  CLtot ={FortranF(totals.ClTot, 10, 5)}");
        lines.Add($"  CDtot ={FortranF(totals.CdTot, 10, 5)}");
        lines.Add($"  CDvis ={FortranF(totals.CdvTot, 10, 5)}     CDind ={FortranF(cditot, 10, 7)}");
        lines.Add($"  CLff  ={FortranF(trefftz.Clff, 10, 5)}     CDff  ={FortranF(trefftz.Cdff, 10, 7)}    | Trefftz");
        lines.Add($"  CYff  ={FortranF(trefftz.Cyff, 10, 5)}         e ={FortranF(trefftz.Spanef, 10, 4)}    | Plane  ");
        lines.Add("");
        for (int n = 0; n < geo.ControlNames.Count; n++)
        {
            double d = n < result.Delcon.Length ? result.Delcon[n] : 0;
            lines.Add($"   {geo.ControlNames[n].PadRight(16)}={FortranF(d, 10, 5)}");
        }
        lines.Add("");
        foreach (var name in geo.DesignNames)
        {
            lines.Add($"   {name.PadRight(16)}={FortranF(0, 10, 5)}");
        }
        lines.Add(sep);
        return string.Join("\n", lines);
    }
}
