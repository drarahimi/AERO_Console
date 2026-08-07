// Port of aoutput.f DERMATS/DERMATM/DERMATB text output (the "ST", "SM", and
// "SB" commands): the OUTTOT block followed by the derivative tables.
//
// Port of packages/avl-core/src/output/outStability.ts, outStabilityBody.ts,
// and outStabilityGeom.ts (shared table helpers consolidated here).

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutStability
{
    private const double DIR = -1.0;

    private static string F11_6(double v) => FortranF(v, 11, 6);

    private static string ControlHeaderLine(IReadOnlyList<string> names)
    {
        string s = new string(' ', 14);
        for (int i = 0; i < names.Count; i++)
        {
            s += new string(' ', 4) + names[i].PadRight(12) + " d" + (i + 1).ToString().PadLeft(2, '0') + " ";
        }
        return s;
    }

    private static string DashRow(int n)
    {
        string cell = new string(' ', 4) + new string('-', 16);
        string s = new string(' ', 14);
        for (int i = 0; i < n; i++) s += cell;
        return s;
    }

    private static string ControlDataRow(string label, string prefix, IReadOnlyList<double> values)
    {
        string s = label;
        for (int i = 0; i < values.Count; i++)
        {
            s += " " + prefix + (i + 1).ToString().PadLeft(2, '0') + " =" + F11_6(values[i]);
        }
        return s;
    }

    private static double[] Negate(IReadOnlyList<double> vals)
    {
        var r = new double[vals.Count];
        for (int i = 0; i < vals.Count; i++) r[i] = DIR * vals[i];
        return r;
    }

    // ---- ST: DERMATS (stability-axis) --------------------------------------
    public static string FormatStabilityDerivs(Geometry geo, CaseResult result, StabDerivs derivs)
    {
        var lines = new List<string>();
        lines.Add(OutTotal.FormatOutTotal(geo, result));
        lines.Add("");
        lines.Add(" Stability-axis derivatives...");
        lines.Add("");
        lines.Add("                             alpha                beta");
        lines.Add("                  ----------------    ----------------");
        lines.Add($" z' force CL |    CLa ={F11_6(derivs.Cl.Al)}    CLb ={F11_6(derivs.Cl.Be)}");
        lines.Add($" y  force CY |    CYa ={F11_6(derivs.Cy.Al)}    CYb ={F11_6(derivs.Cy.Be)}");
        lines.Add($" x  force CD |    CDa ={F11_6(derivs.Cd.Al)}    CDb ={F11_6(derivs.Cd.Be)}");
        lines.Add($" x' mom.  Cl'|    Cla ={F11_6(DIR * derivs.Cr.Al)}    Clb ={F11_6(DIR * derivs.Cr.Be)}");
        lines.Add($" y  mom.  Cm |    Cma ={F11_6(derivs.Cm.Al)}    Cmb ={F11_6(derivs.Cm.Be)}");
        lines.Add($" z' mom.  Cn'|    Cna ={F11_6(DIR * derivs.Cn.Al)}    Cnb ={F11_6(DIR * derivs.Cn.Be)}");
        lines.Add("");
        lines.Add("                     roll rate  p'      pitch rate  q'        yaw rate  r'");
        lines.Add("                  ----------------    ----------------    ----------------");
        double BrefF(double v) => (v * 2.0) / geo.Bref;
        double CrefF(double v) => (v * 2.0) / geo.Cref;
        double BrefFR(double v) => DIR * BrefF(v);
        lines.Add($" z' force CL |    CLp ={F11_6(BrefFR(derivs.Cl.Rx))}    CLq ={F11_6(CrefF(derivs.Cl.Ry))}    CLr ={F11_6(BrefFR(derivs.Cl.Rz))}");
        lines.Add($" y  force CY |    CYp ={F11_6(BrefFR(derivs.Cy.Rx))}    CYq ={F11_6(CrefF(derivs.Cy.Ry))}    CYr ={F11_6(BrefFR(derivs.Cy.Rz))}");
        lines.Add($" x  force CD |    CDp ={F11_6(BrefFR(derivs.Cd.Rx))}    CDq ={F11_6(CrefF(derivs.Cd.Ry))}    CDr ={F11_6(BrefFR(derivs.Cd.Rz))}");
        lines.Add($" x' mom.  Cl'|    Clp ={F11_6(BrefF(derivs.Cr.Rx))}    Clq ={F11_6(DIR * CrefF(derivs.Cr.Ry))}    Clr ={F11_6(BrefF(derivs.Cr.Rz))}");
        lines.Add($" y  mom.  Cm |    Cmp ={F11_6(BrefFR(derivs.Cm.Rx))}    Cmq ={F11_6(CrefF(derivs.Cm.Ry))}    Cmr ={F11_6(BrefFR(derivs.Cm.Rz))}");
        lines.Add($" z' mom.  Cn'|    Cnp ={F11_6(BrefF(derivs.Cn.Rx))}    Cnq ={F11_6(DIR * CrefF(derivs.Cn.Ry))}    Cnr ={F11_6(BrefF(derivs.Cn.Rz))}");

        int nControl = geo.ControlNames.Count;
        if (nControl > 0)
        {
            lines.Add("");
            lines.Add(ControlHeaderLine(geo.ControlNames));
            lines.Add(DashRow(nControl));
            lines.Add(ControlDataRow(" z' force CL |", "  CLd", derivs.ClD));
            lines.Add(ControlDataRow(" y  force CY |", "  CYd", derivs.CyD));
            lines.Add(ControlDataRow(" x  force CD |", "  CDd", derivs.CdD));
            lines.Add(ControlDataRow(" x' mom.  Cl'|", "  Cld", Negate(derivs.CrD)));
            lines.Add(ControlDataRow(" y  mom.  Cm |", "  Cmd", derivs.CmD));
            lines.Add(ControlDataRow(" z' mom.  Cn'|", "  Cnd", Negate(derivs.CnD)));
            lines.Add(ControlDataRow(" Trefftz drag|", "CDffd", derivs.CdffD));
            lines.Add(ControlDataRow(" span eff.   |", "   ed", derivs.SpanefD));
            lines.Add("");
            lines.Add("");
        }

        lines.Add("");
        lines.Add($" Neutral point  Xnp ={F11_6(derivs.Xnp)}");
        lines.Add("");
        lines.Add($" Clb Cnr / Clr Cnb  ={F11_6(derivs.Bb)}    (  > 1 if spirally stable )");

        return string.Join("\n", lines);
    }

    // ---- SM: DERMATM (forces stability-axis, moments body-axis) ------------
    public static string FormatStabilityDerivsBody(Geometry geo, CaseResult result, StabDerivs derivs)
    {
        var lines = new List<string>();
        lines.Add(OutTotal.FormatOutTotal(geo, result));
        lines.Add("");
        lines.Add(" Derivatives...");
        lines.Add("                             alpha                beta");
        lines.Add("                  ----------------    ----------------");
        lines.Add($" z force     |    CLa ={F11_6(derivs.Cl.Al)}    CLb ={F11_6(derivs.Cl.Be)}");
        lines.Add($" y force     |    CYa ={F11_6(derivs.Cy.Al)}    CYb ={F11_6(derivs.Cy.Be)}");
        lines.Add($" x force     |    CDa ={F11_6(derivs.Cd.Al)}    CDb ={F11_6(derivs.Cd.Be)}");
        lines.Add($" roll  x mom.|    Cla ={F11_6(DIR * derivs.Cr.Al)}    Clb ={F11_6(DIR * derivs.Cr.Be)}");
        lines.Add($" pitch y mom.|    Cma ={F11_6(derivs.Cm.Al)}    Cmb ={F11_6(derivs.Cm.Be)}");
        lines.Add($" yaw   z mom.|    Cna ={F11_6(DIR * derivs.Cn.Al)}    Cnb ={F11_6(DIR * derivs.Cn.Be)}");
        lines.Add("");
        lines.Add("                      roll rate  p       pitch rate  q         yaw rate  r");
        lines.Add("                  ----------------    ----------------    ----------------");
        double BrefF(double v) => (v * 2.0) / geo.Bref;
        double CrefF(double v) => (v * 2.0) / geo.Cref;
        double BrefFR(double v) => DIR * BrefF(v);
        lines.Add($" z force     |    CLp ={F11_6(BrefFR(derivs.Cl.Rx))}    CLq ={F11_6(CrefF(derivs.Cl.Ry))}    CLr ={F11_6(BrefFR(derivs.Cl.Rz))}");
        lines.Add($" y force     |    CYp ={F11_6(BrefFR(derivs.Cy.Rx))}    CYq ={F11_6(CrefF(derivs.Cy.Ry))}    CYr ={F11_6(BrefFR(derivs.Cy.Rz))}");
        lines.Add($" x force     |    CDp ={F11_6(BrefFR(derivs.Cd.Rx))}    CDq ={F11_6(CrefF(derivs.Cd.Ry))}    CDr ={F11_6(BrefFR(derivs.Cd.Rz))}");
        lines.Add($" roll  x mom.|    Clp ={F11_6(BrefF(derivs.Cr.Rx))}    Clq ={F11_6(DIR * CrefF(derivs.Cr.Ry))}    Clr ={F11_6(BrefF(derivs.Cr.Rz))}");
        lines.Add($" pitch y mom.|    Cmp ={F11_6(BrefFR(derivs.Cm.Rx))}    Cmq ={F11_6(CrefF(derivs.Cm.Ry))}    Cmr ={F11_6(BrefFR(derivs.Cm.Rz))}");
        lines.Add($" yaw   z mom.|    Cnp ={F11_6(BrefF(derivs.Cn.Rx))}    Cnq ={F11_6(DIR * CrefF(derivs.Cn.Ry))}    Cnr ={F11_6(BrefF(derivs.Cn.Rz))}");

        int nControl = geo.ControlNames.Count;
        if (nControl > 0)
        {
            lines.Add("");
            lines.Add(ControlHeaderLine(geo.ControlNames));
            lines.Add(DashRow(nControl));
            lines.Add(ControlDataRow(" z force     |", "  CLd", derivs.ClD));
            lines.Add(ControlDataRow(" y force     |", "  CYd", derivs.CyD));
            lines.Add(ControlDataRow(" x force     |", "  CDd", derivs.CdD));
            lines.Add(ControlDataRow(" roll  x mom.|", "  Cld", Negate(derivs.CrD)));
            lines.Add(ControlDataRow(" pitch y mom.|", "  Cmd", derivs.CmD));
            lines.Add(ControlDataRow(" yaw   z mom.|", "  Cnd", Negate(derivs.CnD)));
            lines.Add(ControlDataRow(" Trefftz drag|", "CDffd", derivs.CdffD));
            lines.Add(ControlDataRow(" span eff.   |", "   ed", derivs.SpanefD));
            lines.Add("");
            lines.Add("");
        }

        lines.Add("");
        lines.Add($" Neutral point  Xnp ={F11_6(derivs.Xnp)}");
        lines.Add("");
        lines.Add($" Clb Cnr / Clr Cnb  ={F11_6(derivs.Bb)}    (  > 1 if spirally stable )");

        return string.Join("\n", lines);
    }

    // ---- SB: DERMATB (body/geometry-axis) ----------------------------------
    private static string UvwLine(string label, string prefix, UvwDerivs d, bool rowFlips, bool negate)
    {
        double sign = negate ? -1 : 1;
        double dirU = rowFlips ? 1 : DIR;
        double dirV = rowFlips ? DIR : 1;
        double dirW = rowFlips ? 1 : DIR;
        return $"{label}    {prefix}u ={F11_6(sign * dirU * d.U)}    {prefix}v ={F11_6(sign * dirV * d.V)}    {prefix}w ={F11_6(sign * dirW * d.W)}";
    }

    private static string PqrLine(string label, string prefix, UvwDerivs d, double bref, double cref, bool rowFlips)
    {
        double dirP = rowFlips ? 1 : DIR;
        double dirQ = rowFlips ? DIR : 1;
        double dirR = rowFlips ? 1 : DIR;
        double Bf(double v) => (v * 2.0) / bref;
        double Cf(double v) => (v * 2.0) / cref;
        return $"{label}    {prefix}p ={F11_6(dirP * Bf(d.P))}    {prefix}q ={F11_6(dirQ * Cf(d.Q))}    {prefix}r ={F11_6(dirR * Bf(d.R))}";
    }

    public static string FormatStabilityDerivsGeom(Geometry geo, CaseResult result, BodyAxisDerivs derivs)
    {
        var lines = new List<string>();
        lines.Add(OutTotal.FormatOutTotal(geo, result));
        lines.Add("");
        lines.Add(" Geometry-axis derivatives...");
        lines.Add("");
        lines.Add("                    axial   vel. u     sideslip vel. v      normal  vel. w");
        lines.Add("                  ----------------    ----------------    ----------------");
        lines.Add(UvwLine(" x force CX  |", "CX", derivs.Cx, true, true));
        lines.Add(UvwLine(" y force CY  |", "CY", derivs.Cy, false, true));
        lines.Add(UvwLine(" z force CZ  |", "CZ", derivs.Cz, true, true));
        lines.Add(UvwLine(" x mom.  Cl  |", "Cl", derivs.Cl, true, true));
        lines.Add(UvwLine(" y mom.  Cm  |", "Cm", derivs.Cm, false, true));
        lines.Add(UvwLine(" z mom.  Cn  |", "Cn", derivs.Cn, true, true));
        lines.Add("");
        lines.Add("                      roll rate  p       pitch rate  q         yaw rate  r");
        lines.Add("                  ----------------    ----------------    ----------------");
        lines.Add(PqrLine(" x force CX  |", "CX", derivs.Cx, geo.Bref, geo.Cref, true));
        lines.Add(PqrLine(" y force CY  |", "CY", derivs.Cy, geo.Bref, geo.Cref, false));
        lines.Add(PqrLine(" z force CZ  |", "CZ", derivs.Cz, geo.Bref, geo.Cref, true));
        lines.Add(PqrLine(" x mom.  Cl  |", "Cl", derivs.Cl, geo.Bref, geo.Cref, true));
        lines.Add(PqrLine(" y mom.  Cm  |", "Cm", derivs.Cm, geo.Bref, geo.Cref, false));
        lines.Add(PqrLine(" z mom.  Cn  |", "Cn", derivs.Cn, geo.Bref, geo.Cref, true));

        int nControl = geo.ControlNames.Count;
        if (nControl > 0)
        {
            lines.Add("");
            lines.Add(ControlHeaderLine(geo.ControlNames));
            lines.Add(DashRow(nControl));
            lines.Add(ControlDataRow(" x force CX  |", "  CXd", Negate(derivs.CxD)));
            lines.Add(ControlDataRow(" y force CY  |", "  CYd", derivs.CyD));
            lines.Add(ControlDataRow(" z force CZ  |", "  CZd", Negate(derivs.CzD)));
            lines.Add(ControlDataRow(" x mom.  Cl  |", "  Cld", Negate(derivs.ClD)));
            lines.Add(ControlDataRow(" y mom.  Cm  |", "  Cmd", derivs.CmD));
            lines.Add(ControlDataRow(" z mom.  Cn  |", "  Cnd", Negate(derivs.CnD)));
            lines.Add("");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }
}
