// Port of avl.f SUBROUTINE RUNSAV: writes run cases to a .run file (the OPER
// menu's "S" command). Matches the reference exe's layout so files round-trip
// through RunCaseFile.ParseRunCaseFile and are loadable by avl.exe itself.
//
// The 4 trailing "visc CL_a/CL_u/CM_a/CM_u" params avl.exe emits are omitted:
// they are always 0 for a freshly loaded case, this port doesn't model them,
// and writing them would confuse the loose-substring parameter matcher on read.
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.RunCaseNs;

public static class RunCaseWriter
{
    private const string DASHES = " ---------------------------------------------";

    private static readonly string[] FreeVarNames = { "alpha", "beta", "pb/2V", "qc/2V", "rb/2V" };

    private static readonly Dictionary<ConstraintKind, string> ConnamByKind = new()
    {
        [ConstraintKind.Alpha] = "alpha",
        [ConstraintKind.Beta] = "beta",
        [ConstraintKind.RotX] = "pb/2V",
        [ConstraintKind.RotY] = "qc/2V",
        [ConstraintKind.RotZ] = "rb/2V",
        [ConstraintKind.CL] = "CL",
        [ConstraintKind.CY] = "CY",
        [ConstraintKind.Cl] = "Cl roll mom",
        [ConstraintKind.Cm] = "Cm pitchmom",
        [ConstraintKind.Cn] = "Cn yaw  mom",
    };

    // Parameter block: (name, unit, value selector). Order matches RUNSAV/RUNGET.
    private static (string name, string unit, Func<Geometry, RunCase, double> value)[] ParamSpecs() => new (string, string, Func<Geometry, RunCase, double>)[]
    {
        ("alpha",     "deg",           (g, r) => r.Params.Alpha ?? 0),
        ("beta",      "deg",           (g, r) => r.Params.Beta ?? 0),
        ("pb/2V",     "",              (g, r) => r.Params.RotX ?? 0),
        ("qc/2V",     "",              (g, r) => r.Params.RotY ?? 0),
        ("rb/2V",     "",              (g, r) => r.Params.RotZ ?? 0),
        ("CL",        "",              (g, r) => r.Params.Cl ?? 0),
        ("CDo",       "",              (g, r) => r.Params.Cdo ?? g.Cdoref0),
        ("bank",      "deg",           (g, r) => r.Params.Bank ?? 0),
        ("elevation", "deg",           (g, r) => r.Params.Elevation ?? 0),
        ("heading",   "deg",           (g, r) => r.Params.Heading ?? 0),
        ("Mach",      "",              (g, r) => r.Params.Mach ?? g.Mach0),
        ("velocity",  "Lunit/Tunit",   (g, r) => r.Params.Velocity ?? 0),
        ("density",   "Munit/Lunit^3", (g, r) => r.Params.Density ?? 1),
        ("grav.acc.", "Lunit/Tunit^2", (g, r) => r.Params.GravAcc ?? 1),
        ("turn_rad.", "Lunit",         (g, r) => r.Params.TurnRad ?? 0),
        ("load_fac.", "",              (g, r) => r.Params.LoadFac ?? 0),
        ("X_cg",      "Lunit",         (g, r) => r.Params.XCg ?? g.Xyzref0[0]),
        ("Y_cg",      "Lunit",         (g, r) => r.Params.YCg ?? g.Xyzref0[1]),
        ("Z_cg",      "Lunit",         (g, r) => r.Params.ZCg ?? g.Xyzref0[2]),
        ("mass",      "Munit",         (g, r) => r.Params.Mass ?? 1),
        ("Ixx",       "Munit-Lunit^2", (g, r) => r.Params.Ixx ?? 1),
        ("Iyy",       "Munit-Lunit^2", (g, r) => r.Params.Iyy ?? 1),
        ("Izz",       "Munit-Lunit^2", (g, r) => r.Params.Izz ?? 1),
        ("Ixy",       "Munit-Lunit^2", (g, r) => r.Params.Ixy ?? 0),
        ("Iyz",       "Munit-Lunit^2", (g, r) => r.Params.Iyz ?? 0),
        ("Izx",       "Munit-Lunit^2", (g, r) => r.Params.Izx ?? 0),
    };

    public static string Write(Geometry geo, IReadOnlyList<RunCase> runCases)
    {
        var varNames = FreeVarNames.Concat(geo.ControlNames).ToList();
        var specs = ParamSpecs();
        var lines = new List<string>();

        foreach (var rc in runCases)
        {
            // FORMAT 1010: leading blank, dashes, "Run case N:  title", trailing blank.
            lines.Add("");
            lines.Add(DASHES);
            lines.Add(" Run case" + FortranI(rc.Index, 3) + ":  " + rc.Title);
            lines.Add("");

            // FORMAT 1050 constraint lines: " VARNAM ->  CONNAM=<G14.6> ".
            for (int iv = 0; iv < varNames.Count; iv++)
            {
                var c = rc.Constraints.FirstOrDefault(cc => cc.FreeVar == iv);
                var kind = c?.Kind ?? (iv < 5
                    ? new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ }[iv]
                    : ConstraintKind.Control);
                string connam = kind == ConstraintKind.Control ? varNames[iv] : ConnamByKind[kind];
                double val = c?.Value ?? 0;
                lines.Add(" " + varNames[iv].PadRight(13) + " ->  " + connam.PadRight(12) + "=" + FortranG(val, 14, 6) + " ");
            }

            lines.Add(""); // WRITE(LU,*) blank

            // FORMAT 1080 parameter lines: " PARNAM=<G14.6> UNIT".
            foreach (var (name, unit, value) in specs)
            {
                lines.Add(" " + name.PadRight(10) + "=" + FortranG(value(geo, rc), 14, 6) + " " + unit);
            }
        }

        return string.Join("\n", lines);
    }
}
