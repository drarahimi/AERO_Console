// Port of aoutput.f SUBROUTINE OUTHINGE (the "HM" command's per-control
// hinge-moment block). VEE=0 default makes the dimensional Moment column
// always 0 (see the TS module doc for the full rationale).
//
// Port of packages/avl-core/src/output/outHinge.ts.

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutHinge
{
    public static string FormatOutHinge(Geometry geo, Totals totals, double[] delcon)
    {
        const string sep = " ---------------------------------------------------------------";
        var lines = new List<string>();
        lines.Add(sep);
        lines.Add(" Control Hinge Moments");
        lines.Add($" (referred to    Sref ={FortranG(geo.Sref, 12, 4)}   Cref ={FortranF(geo.Cref, 10, 4)})");
        lines.Add("");
        lines.Add(" Control          Chinge      Deflection   Moment(N-m)");
        lines.Add(" ---------------- ----------- ------------ -------------");

        const double que = 0; // 0.5*RHO*VEE^2 with VEE=0
        for (int n = 0; n < geo.ControlNames.Count; n++)
        {
            double hmom = totals.Chinge[n] * que * geo.Sref * geo.Cref;
            double d = n < delcon.Length ? delcon[n] : 0;
            lines.Add(
                $" {geo.ControlNames[n].PadRight(16)}{FortranG(totals.Chinge[n], 12, 4)}   {FortranG(d, 12, 4)}   {FortranG(hmom, 12, 4)}");
        }

        lines.Add(sep);
        return string.Join("\n", lines);
    }
}
