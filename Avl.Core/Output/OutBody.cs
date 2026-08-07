// Port of aoutput.f SUBROUTINE OUTBODY (the "FB" command): per-body force summary.
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutBody
{
    private const double DTR = Math.PI / 180.0;
    private const double DIR = -1.0;
    private const string DASHES = " ---------------------------------------------------------------";

    public static string FormatOutBody(Geometry geo, CaseResult result, double[]? xyzref = null, string title = " -unnamed-")
    {
        xyzref ??= geo.Xyzref0;
        var wrot = result.Wrot;
        double ca = Math.Cos(result.Alfa), sa = Math.Sin(result.Alfa);
        double rxB = wrot[0] * geo.Bref / 2.0, ryB = wrot[1] * geo.Cref / 2.0, rzB = wrot[2] * geo.Bref / 2.0;
        double rxS = (wrot[0] * ca + wrot[2] * sa) * geo.Bref / 2.0;
        double ryS = wrot[1] * geo.Cref / 2.0;
        double rzS = (wrot[2] * ca - wrot[0] * sa) * geo.Bref / 2.0;

        var l = new List<string>();
        l.Add(DASHES);
        l.Add(" Body Forces (referred to Sref,Cref,Bref about Xref,Yref,Zref)");
        l.Add(" Standard axis orientation,  X fwd, Z down         ");
        l.Add("");
        l.Add("  Sref =" + FortranG(geo.Sref, 12, 4) + "   Cref =" + FortranF(geo.Cref, 10, 4) + "   Bref =" + FortranF(geo.Bref, 10, 4));
        l.Add("  Xref =  " + FortranF(xyzref[0], 10, 4) + "   Yref =" + FortranF(xyzref[1], 10, 4) + "   Zref =" + FortranF(xyzref[2], 10, 4));
        l.Add("");
        l.Add(" Run case: " + title);
        l.Add("  Alpha =" + FortranF(result.Alfa / DTR, 10, 5) + "     pb/2V =" + FortranF(DIR * rxB, 10, 5) + "     p'b/2V =" + FortranF(DIR * rxS, 10, 5));
        l.Add("  Beta  =" + FortranF(result.Beta / DTR, 10, 5) + "     qc/2V =" + FortranF(ryB, 10, 5) + "     q'c/2V =" + FortranF(ryS, 10, 5));
        l.Add("  Mach  =" + FortranF(result.Mach, 10, 3) + "     rb/2V =" + FortranF(DIR * rzB, 10, 5) + "     r'b/2V =" + FortranF(DIR * rzS, 10, 5));
        l.Add("");
        l.Add("");
        l.Add(" Ibdy       Length        Asurf          Vol          CL          CD          Cm          CY          Cn          Cl");

        var byBody = result.Totals.ByBody;
        for (int ib = 0; ib < geo.Bodies.Count; ib++)
        {
            var b = geo.Bodies[ib];
            var f = ib < byBody.Length ? byBody[ib] : new BodyForce();
            l.Add(" " + FortranI(ib + 1, 4)
                + " " + FortranF(b.Length, 12, 6) + " " + FortranF(b.Surface, 12, 6) + " " + FortranF(b.Volume, 12, 6)
                + FortranF(f.Cl, 12, 6) + FortranF(f.Cd, 12, 6) + FortranF(f.Cm[1], 12, 6)
                + FortranF(f.Cf[1], 12, 6) + FortranF(DIR * f.Cm[2], 12, 6) + FortranF(DIR * f.Cm[0], 12, 6)
                + "   " + b.Title.Trim());
        }
        l.Add(DASHES);

        return string.Join("\n", l);
    }
}
