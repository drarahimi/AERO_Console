// Port of avl.f SUBROUTINE RUNGET: parses a .run file into per-run-case
// constraints (directly usable as Trim's TrimConstraint[]) and parameter
// overrides.
//
// Port of packages/avl-core/src/runcase/runCaseFile.ts.

using System.Globalization;
using System.Text.RegularExpressions;
using Avl.Core.Model;
using Avl.Core.Solver;

namespace Avl.Core.RunCaseNs;

public sealed class RunCaseParams
{
    public double? Alpha;
    public double? Beta;
    public double? RotX;
    public double? RotY;
    public double? RotZ;
    public double? Cl;
    public double? Cdo;
    public double? Bank;
    public double? Elevation;
    public double? Heading;
    public double? Mach;
    public double? Velocity;
    public double? Density;
    public double? GravAcc;
    public double? TurnRad;
    public double? LoadFac;
    public double? XCg;
    public double? YCg;
    public double? ZCg;
    public double? Mass;
    public double? Ixx;
    public double? Iyy;
    public double? Izz;
    public double? Ixy;
    public double? Iyz;
    public double? Izx;
}

public sealed class RunCase
{
    public int Index;
    public string Title = "";
    public List<TrimConstraint> Constraints = new();
    public RunCaseParams Params = new();
}

public static class RunCaseFile
{
    private static readonly Dictionary<string, ConstraintKind> ConstraintNameToKind = new()
    {
        ["alpha"] = ConstraintKind.Alpha,
        ["beta"] = ConstraintKind.Beta,
        ["pb/2V"] = ConstraintKind.RotX,
        ["qc/2V"] = ConstraintKind.RotY,
        ["rb/2V"] = ConstraintKind.RotZ,
        ["CL"] = ConstraintKind.CL,
        ["CY"] = ConstraintKind.CY,
        ["Cl roll mom"] = ConstraintKind.Cl,
        ["Cm pitchmom"] = ConstraintKind.Cm,
        ["Cn yaw  mom"] = ConstraintKind.Cn,
    };

    private static readonly string[] FreeVarNames = { "alpha", "beta", "pb/2V", "qc/2V", "rb/2V" };

    private static readonly (string name, Action<RunCaseParams, double> set)[] ParamKeyMap =
    {
        ("alpha", (p, v) => p.Alpha = v),
        ("beta", (p, v) => p.Beta = v),
        ("pb/2V", (p, v) => p.RotX = v),
        ("qc/2V", (p, v) => p.RotY = v),
        ("rb/2V", (p, v) => p.RotZ = v),
        ("CL", (p, v) => p.Cl = v),
        ("CDo", (p, v) => p.Cdo = v),
        ("bank", (p, v) => p.Bank = v),
        ("elevation", (p, v) => p.Elevation = v),
        ("heading", (p, v) => p.Heading = v),
        ("Mach", (p, v) => p.Mach = v),
        ("velocity", (p, v) => p.Velocity = v),
        ("density", (p, v) => p.Density = v),
        ("grav.acc.", (p, v) => p.GravAcc = v),
        ("turn_rad.", (p, v) => p.TurnRad = v),
        ("load_fac.", (p, v) => p.LoadFac = v),
        ("X_cg", (p, v) => p.XCg = v),
        ("Y_cg", (p, v) => p.YCg = v),
        ("Z_cg", (p, v) => p.ZCg = v),
        ("mass", (p, v) => p.Mass = v),
        ("Ixx", (p, v) => p.Ixx = v),
        ("Iyy", (p, v) => p.Iyy = v),
        ("Izz", (p, v) => p.Izz = v),
        ("Ixy", (p, v) => p.Ixy = v),
        ("Iyz", (p, v) => p.Iyz = v),
        ("Izx", (p, v) => p.Izx = v),
    };

    private static bool TryNum(string s, out double v) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    /// <summary>Parses a .run file. controlNames (Geometry.ControlNames) is needed to
    /// resolve control-surface variable/constraint names.</summary>
    public static List<RunCase> ParseRunCaseFile(string contents, Geometry geo)
    {
        var varNames = FreeVarNames.Concat(geo.ControlNames).ToList();
        var conNames = ConstraintNameToKind.Keys.Concat(geo.ControlNames).ToList();

        var cases = new Dictionary<int, RunCase>();
        int ir = 0;

        var lines = Regex.Split(contents, "\r\n|\r|\n");
        foreach (var line in lines)
        {
            int kcol = line.IndexOf(':');
            int karr = line.IndexOf("->", StringComparison.Ordinal);
            int kequ = line.IndexOf('=');

            if (kcol != 0 && kcol > 0)
            {
                string idxStr = line.Substring(Math.Max(0, kcol - 3), kcol - Math.Max(0, kcol - 3)).Trim();
                if (!int.TryParse(idxStr, out int idx) || idx < 1)
                {
                    ir = 0;
                    continue;
                }
                ir = idx;
                string title = line.Substring(kcol + 1).Trim();
                if (!cases.ContainsKey(ir)) cases[ir] = new RunCase { Index = ir, Title = title };
                else cases[ir].Title = title;
                continue;
            }

            if (ir == 0) continue;

            if (karr >= 0 && kequ >= 0)
            {
                string varn = line.Substring(0, karr).Trim();
                string conn = line.Substring(karr + 2, kequ - (karr + 2)).Trim();
                string valueStr = Regex.Split(line.Substring(kequ + 1).Trim(), @"\s+")[0];
                if (!TryNum(valueStr, out double value)) continue;

                int freeVarIdx = varNames.FindIndex(n => n.Contains(varn) || varn.Contains(n));
                int conIdx = conNames.FindIndex(n => n.Contains(conn) || conn.Contains(n));
                if (freeVarIdx < 0 || conIdx < 0) continue;

                string conName = conNames[conIdx];
                ConstraintKind kind = ConstraintNameToKind.TryGetValue(conName, out var k) ? k : ConstraintKind.Control;
                var rc = cases[ir];
                rc.Constraints = rc.Constraints.Where(c => c.FreeVar != freeVarIdx).ToList();
                rc.Constraints.Add(new TrimConstraint { FreeVar = freeVarIdx, Kind = kind, Value = value });
                continue;
            }

            if (karr < 0 && kequ > 0)
            {
                string parn = line.Substring(0, kequ).Trim();
                string valueStr = Regex.Split(line.Substring(kequ + 1).Trim(), @"\s+")[0];
                if (!TryNum(valueStr, out double value)) continue;
                var match = ParamKeyMap.FirstOrDefault(m => m.name.Contains(parn) || parn.Contains(m.name));
                if (match.name == null) continue;
                match.set(cases[ir].Params, value);
            }
        }

        return cases.Values.OrderBy(c => c.Index).ToList();
    }
}
