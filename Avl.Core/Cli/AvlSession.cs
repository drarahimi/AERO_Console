// Port of avl.f's top-level menu loop (PROGRAM AVL) and aoper.f's OPER-menu
// loop (SUBROUTINE OPER), as a synchronous line-in/text-out session object
// suitable for driving from a terminal-emulator UI in place of the avl.exe
// child process.
//
// See packages/avl-core/src/cli/avlSession.ts for the full scope notes; the
// same deliberate deviations apply (LOAD's per-surface echo is summarized, and
// the EXEC Newton-iteration trace is omitted).

using Avl.Core.MassFile;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.RunCaseNs;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Cli;

/// <summary>Host-supplied file access for LOAD/MASS/CASE and AFIL airfoil files,
/// plus the write side used when a display command (FT/FN/FS/...) is given a
/// filename to save its output to (as the WinForms plot features do).</summary>
public interface IVirtualFileSystem
{
    /// <summary>Returns file contents, or null if the file doesn't exist.</summary>
    string? ReadFile(string name);

    /// <summary>Writes display-command output to the named file. Default no-op so
    /// read-only hosts (e.g. tests) don't have to implement it.</summary>
    void WriteFile(string name, string contents) { }
}

public sealed class AvlSession
{
    private const string TOP_BANNER = """
 ===================================================
  Athena Vortex Lattice  Program      Version  3.51
  Copyright (C) 2002   Mark Drela, Harold Youngren

  This software comes with ABSOLUTELY NO WARRANTY,
    subject to the GNU General Public License.

  Caveat computor
 ===================================================
""";

    private const string TOP_MENU = """
 ==========================================================
   Quit    Exit program

  .OPER    Compute operating-point run cases
  .MODE    Eigenvalue analysis of run cases
  .TIME    Time-domain calculations

   LOAD f  Read configuration input file
   MASS f  Read mass distribution file
   MSHO    Show mass distribution
   CASE f  Read run case file

   CINI    Clear and initialize run cases
   MSET i  Apply mass file data to stored run case(s)

  .PLOP    Plotting options
   NAME s  Specify new configuration name
""";

    private const string OPER_MENU_BODY = """

  C1  set level or banked  horizontal flight constraints
  C2  set steady pitch rate (looping) flight constraints
  M odify parameters

 "#" select  run case          L ist defined run cases
  +  add new run case          S ave run cases to file
  -  delete  run case          F etch run cases from file
  N ame current run case       W rite forces to file

 eX ecute run case             I nitialize variables

  G eometry plot               T refftz Plane plot

  ST  stability derivatives    FT  total   forces
  SB  body-axis derivatives    FN  surface forces
  RE  reference quantities     FS  strip   forces (FSB bodyaxes)
  DE  design changes           FE  element forces
  O ptions                     FB  body forces
                               HM  hinge moments
  OB offbody flow survey       VM  strip shear,moment
  MRF  machine-readable format CPOM OML surface pressures

""";

    private static readonly string[] VARKEY_SHORT = { "A", "B", "R", "P", "Y" };

    private static readonly Dictionary<string, ConstraintKind> CONKEY_SHORT = new()
    {
        ["A"] = ConstraintKind.Alpha,
        ["B"] = ConstraintKind.Beta,
        ["R"] = ConstraintKind.RotX,
        ["P"] = ConstraintKind.RotY,
        ["Y"] = ConstraintKind.RotZ,
        ["C"] = ConstraintKind.CL,
        ["S"] = ConstraintKind.CY,
        ["RM"] = ConstraintKind.Cl,
        ["PM"] = ConstraintKind.Cm,
        ["YM"] = ConstraintKind.Cn,
    };

    private static readonly Dictionary<ConstraintKind, string> CONNAM_BY_KIND = new()
    {
        [ConstraintKind.Alpha] = "alpha ",
        [ConstraintKind.Beta] = "beta  ",
        [ConstraintKind.RotX] = "pb/2V ",
        [ConstraintKind.RotY] = "qc/2V ",
        [ConstraintKind.RotZ] = "rb/2V ",
        [ConstraintKind.CL] = "CL    ",
        [ConstraintKind.CY] = "CY    ",
        [ConstraintKind.Cl] = "Cl roll mom",
        [ConstraintKind.Cm] = "Cm pitchmom",
        [ConstraintKind.Cn] = "Cn yaw  mom",
    };

    private static readonly string[] DISPLAY_COMMANDS = { "FT", "FN", "FS", "FSB", "FE", "FB", "HM", "ST", "SM", "SB", "VM", "CNC" };

    private enum Mode { Top, Oper, OperGetFile, ModeMenu, ModeGetFile }
    private List<System.Numerics.Complex> _eigenvalues = new();

    private readonly IVirtualFileSystem _fs;
    private Geometry? _geo;
    private MassProperties? _mass;
    private List<RunCase> _runCases = new();
    private int _irun = 1;
    private Mode _mode = Mode.Top;
    private string? _pendingDisplay;
    private bool _aicBuilt;
    private bool _lsol;
    private bool _useMrf; // MRF toggle: emit machine-readable ES23.15 output
    private double[] _deldes = Array.Empty<double>(); // design-variable changes (DE command)
    private Geometry? _solveGeo; // geometry actually solved (design-perturbed if DELDES nonzero)
    private CaseResult? _lastResult;
    private SolveContext? _lastCtx;
    private double[]? _lastXyzref;
    private string _lastTitle = "";
    public bool Quit { get; private set; }

    public AvlSession(IVirtualFileSystem fs) => _fs = fs;

    /// <summary>Parsed geometry from the last LOAD, or null if none loaded yet.</summary>
    public Geometry? GetGeometry() => _geo;

    /// <summary>Result of the last solved operating point (OPER's X), or null if none yet.</summary>
    public CaseResult? GetLastResult() => _lastResult;

    private static RunCase DefaultRunCase(Geometry geo, int index)
    {
        var kinds = new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ };
        var constraints = new List<TrimConstraint>();
        for (int i = 0; i < kinds.Length; i++) constraints.Add(new TrimConstraint { FreeVar = i, Kind = kinds[i], Value = 0 });
        for (int n = 0; n < geo.ControlNames.Count; n++) constraints.Add(new TrimConstraint { FreeVar = 5 + n, Kind = ConstraintKind.Control, Value = 0 });
        return new RunCase { Index = index, Title = " -unnamed- ", Constraints = constraints, Params = new RunCaseParams() };
    }

    private static (string cmd, string arg) SplitCommand(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0) return ("", "");
        int sp = 0;
        while (sp < trimmed.Length && !char.IsWhiteSpace(trimmed[sp])) sp++;
        string cmd = trimmed.Substring(0, sp).ToUpperInvariant();
        string arg = sp < trimmed.Length ? trimmed.Substring(sp).Trim() : "";
        return (cmd, arg);
    }

    private static int? MatchVarKey(string cmd, Geometry geo)
    {
        int idx = Array.IndexOf(VARKEY_SHORT, cmd);
        if (idx >= 0) return idx;
        if (cmd.Length >= 2 && cmd[0] == 'D' && int.TryParse(cmd.Substring(1), out int n))
        {
            if (n >= 1 && n <= geo.ControlNames.Count) return 5 + (n - 1);
        }
        return null;
    }

    private static (ConstraintKind kind, int controlIdx)? MatchConKey(string token, Geometry geo)
    {
        if (CONKEY_SHORT.TryGetValue(token, out var k)) return (k, -1);
        if (token.Length >= 2 && token[0] == 'D' && int.TryParse(token.Substring(1), out int n))
        {
            if (n >= 1 && n <= geo.ControlNames.Count) return (ConstraintKind.Control, n - 1);
        }
        return null;
    }

    /// <summary>Startup banner + top menu + initial prompt, matching program launch.</summary>
    public string Start() => $"\n{TOP_BANNER}\n\n{TOP_MENU}\n\n{TopPrompt()}";

    private static string TopPrompt() => " AVL   c>  ";

    private string OperPrompt() => $" .OPER (case {_irun}/{_runCases.Count})   c>  ";

    private RunCase CurrentRunCase() => _runCases[_irun - 1];

    private double[] EffectiveXyzref(RunCase rc)
    {
        var geo = _geo!;
        return new double[]
        {
            rc.Params.XCg ?? geo.Xyzref0[0],
            rc.Params.YCg ?? geo.Xyzref0[1],
            rc.Params.ZCg ?? geo.Xyzref0[2],
        };
    }

    public string Feed(string line)
    {
        if (_mode == Mode.Top) return HandleTop(line);
        if (_mode == Mode.Oper) return HandleOper(line);
        if (_mode == Mode.ModeMenu) return HandleModeMenu(line);
        if (_mode == Mode.ModeGetFile) return HandleModeGetFile(line);
        return HandleGetFile(line);
    }

    private string HandleTop(string line)
    {
        var (cmd, arg) = SplitCommand(line);
        string @out = "";
        switch (cmd)
        {
            case "":
                break;
            case "?":
                @out += $"\n{TOP_MENU}\n";
                break;
            case "QUIT":
            case "Q":
                Quit = true;
                return "";
            case "OPER":
                if (_geo == null)
                {
                    @out += "\n * Configuration not defined\n";
                    break;
                }
                _mode = Mode.Oper;
                return RenderOperMenu() + OperPrompt();
            case "MODE":
                if (_geo == null) { @out += "\n * Configuration not defined\n"; break; }
                _mode = Mode.ModeMenu;
                return ModeMenuText() + ModePrompt();
            case "LOAD":
            {
                if (arg.Length == 0) { @out += " Enter input filename: "; break; }
                var content = _fs.ReadFile(arg);
                if (content == null) { @out += $"\n ** File OPEN error:  {arg}\n"; break; }
                try
                {
                    _geo = AvlInputParser.ParseAvlGeometry(content, new ParseOptions { ResolveAirfoilFile = n => _fs.ReadFile(n) });
                }
                catch
                {
                    @out += " ** File not processed. Current geometry may be corrupted.\n";
                    break;
                }
                _runCases = new List<RunCase> { DefaultRunCase(_geo, 1) };
                _irun = 1;
                _mass = null;
                _aicBuilt = false;
                _lsol = false;
                _deldes = new double[_geo.DesignNames.Count];
                _solveGeo = null;
                @out += $"\n Reading file: {arg}  ...\n\n";
                @out += $" Configuration: {_geo.Title}\n\n";
                @out += $"{_geo.Bodies.Count,5} Bodies\n";
                @out += $"{_geo.Surfaces.Count,5} Solid surfaces\n";
                @out += $"{_geo.Strips.Count,5} Strips\n";
                @out += $"{_geo.Vortices.Count,5} Vortices\n\n";
                @out += $"{_geo.ControlNames.Count,5} Control variables\n";
                @out += $"{_geo.DesignNames.Count,5} Design parameters\n\n";
                @out += " Initializing run cases...\n";
                break;
            }
            case "MASS":
            {
                if (_geo == null) break;
                if (arg.Length == 0) { @out += " Enter mass filename: "; break; }
                var content = _fs.ReadFile(arg);
                if (content == null) { @out += $"\n ** File OPEN error:  {arg}\n"; break; }
                _mass = MassFileParser.ParseMassFile(content);
                @out += "\n Mass distribution read ...\n\n";
                @out += $" Mass        ={FortranG(_mass.Mass, 12, 4)}  {(_mass.UnitM == 1 ? "" : "kg")}\n";
                @out += $" C.G. x,y,z  ={FortranG(_mass.Cg[0], 12, 4)}{FortranG(_mass.Cg[1], 12, 4)}{FortranG(_mass.Cg[2], 12, 4)}\n";
                @out += "\n Use MSET to apply these mass,inertias to run cases\n";
                break;
            }
            case "CASE":
            {
                if (_geo == null) break;
                if (arg.Length == 0) { @out += " Enter run case filename: "; break; }
                var content = _fs.ReadFile(arg);
                if (content == null) { @out += $"\n ** File OPEN error:  {arg}\n"; break; }
                _runCases = RunCaseFile.ParseRunCaseFile(content, _geo);
                _irun = 1;
                _lsol = false;
                @out += "\n\n Run cases read  ...\n";
                foreach (var rc in _runCases) @out += $"{rc.Index,4}: {rc.Title}\n";
                break;
            }
            case "CINI":
                if (_geo != null)
                {
                    _runCases = new List<RunCase> { DefaultRunCase(_geo, 1) };
                    _irun = 1;
                }
                else
                {
                    @out += " No configuration available.\n";
                }
                break;
            case "MSET":
            {
                if (_mass == null) break;
                int idx = arg.Length > 0 && int.TryParse(arg, out int pi) ? pi : 0;
                var targets = idx == 0 ? _runCases : _runCases.Where(rc => rc.Index == idx).ToList();
                foreach (var rc in targets)
                {
                    rc.Params.XCg = _mass.Cg[0];
                    rc.Params.YCg = _mass.Cg[1];
                    rc.Params.ZCg = _mass.Cg[2];
                    rc.Params.Mass = _mass.Mass;
                    rc.Params.Ixx = _mass.Inertia[0][0];
                    rc.Params.Iyy = _mass.Inertia[1][1];
                    rc.Params.Izz = _mass.Inertia[2][2];
                    rc.Params.Ixy = -_mass.Inertia[0][1];
                    rc.Params.Izx = -_mass.Inertia[0][2];
                    rc.Params.Iyz = -_mass.Inertia[1][2];
                }
                _lsol = false;
                break;
            }
            case "NAME":
                if (_geo != null) _geo.Title = arg.Length > 0 ? arg : _geo.Title;
                break;
            default:
                @out += $" {cmd} command not recognized.  Type a \"?\" for list\n";
                break;
        }
        return @out + TopPrompt();
    }

    private string RenderConlst(RunCase rc)
    {
        var geo = _geo!;
        var VARKEYS = new[] { "A lpha", "B eta", "R oll  rate", "P itch rate", "Y aw   rate" };
        string s = "\n  variable          constraint              \n";
        s += "  ------------      ------------------------\n";
        int nvtot = 5 + geo.ControlNames.Count;
        var defaultKinds = new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ };
        for (int iv = 0; iv < nvtot; iv++)
        {
            var c = rc.Constraints.FirstOrDefault(cc => cc.FreeVar == iv);
            double value = c?.Value ?? 0;
            ConstraintKind kind = c?.Kind ?? (iv < 5 ? defaultKinds[iv] : ConstraintKind.Control);
            string varkey = iv < 5 ? VARKEYS[iv] : $"D{iv - 4}  {Slice(geo.ControlNames[iv - 5], 8)}";
            string conName = kind == ConstraintKind.Control ? geo.ControlNames[iv - 5] : CONNAM_BY_KIND[kind];
            s += $"  {varkey.PadRight(12)}  ->  {conName.PadRight(12)}={FortranG(value, 12, 4)}\n";
        }
        s += "  ------------      ------------------------\n";
        return s;
    }

    private static string Slice(string s, int len) => s.Length > len ? s.Substring(0, len) : s;

    private string RenderOperMenu()
    {
        var rc = CurrentRunCase();
        string s = $"\n Operation of run case {_irun}/{_runCases.Count}:  {rc.Title}\n";
        s += " ==========================================================\n";
        s += RenderConlst(rc);
        s += OPER_MENU_BODY;
        return s;
    }

    private string HandleOper(string line)
    {
        var (cmd, arg) = SplitCommand(line);

        if (cmd == "")
        {
            _mode = Mode.Top;
            return "\n" + TopPrompt();
        }
        if (cmd == "?")
        {
            return RenderOperMenu() + OperPrompt();
        }
        if (cmd.Length > 0 && cmd.All(char.IsDigit))
        {
            int idx = int.Parse(cmd);
            _irun = Math.Max(1, Math.Min(_runCases.Count, idx));
            return RenderOperMenu() + OperPrompt();
        }
        if (cmd == "+")
        {
            var rc = CurrentRunCase();
            var copy = new RunCase
            {
                Index = rc.Index + 1,
                Title = rc.Title,
                Constraints = rc.Constraints.Select(c => new TrimConstraint { FreeVar = c.FreeVar, Kind = c.Kind, Value = c.Value }).ToList(),
                Params = Cl2Params(rc.Params),
            };
            _runCases.Insert(_irun, copy);
            for (int i = 0; i < _runCases.Count; i++) _runCases[i].Index = i + 1;
            _irun += 1;
            return "\n Initializing new run case from current one\n" + RenderOperMenu() + OperPrompt();
        }
        if (cmd == "-")
        {
            string @out = "";
            if (_runCases.Count <= 1)
            {
                @out += "\n * Cannot delete one remaining run case\n";
            }
            else
            {
                _runCases.RemoveAt(_irun - 1);
                for (int i = 0; i < _runCases.Count; i++) _runCases[i].Index = i + 1;
                _irun = Math.Max(1, Math.Min(_irun, _runCases.Count));
            }
            return @out + RenderOperMenu() + OperPrompt();
        }
        if (cmd == "N")
        {
            if (arg.Length > 0) CurrentRunCase().Title = arg;
            return RenderOperMenu() + OperPrompt();
        }
        if (cmd == "I")
        {
            return RenderOperMenu() + OperPrompt();
        }
        if (cmd == "X")
        {
            string @out = ExecuteCase();
            return @out + RenderOperMenu() + OperPrompt();
        }
        if (cmd == "S")
        {
            if (arg.Length > 0)
            {
                _fs.WriteFile(arg, RunCaseWriter.Write(_geo!, _runCases));
                return RenderOperMenu() + OperPrompt();
            }
            _mode = Mode.OperGetFile;
            _pendingDisplay = "__SAVE__";
            return "\nEnter run case save filename   s>  ";
        }
        if (cmd == "F")
        {
            if (arg.Length > 0)
            {
                FetchRunCases(arg);
                return RenderOperMenu() + OperPrompt();
            }
            _mode = Mode.OperGetFile;
            _pendingDisplay = "__FETCH__";
            return "\nEnter run case fetch filename   s>  ";
        }
        if (cmd == "MRF")
        {
            _useMrf = true;
            return "\n MRF: all subsequent output in full-precision machine readable format\n" + RenderOperMenu() + OperPrompt();
        }
        if (cmd == "DE")
        {
            var geoD = _geo!;
            if (geoD.DesignNames.Count == 0)
                return "\n * No design parameters are declared\n" + RenderOperMenu() + OperPrompt();
            // Inline "DE k value [k value ...]" pairs; bare "DE" just lists current changes.
            var toks = arg.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < toks.Length; i += 2)
            {
                if (int.TryParse(toks[i], out int k) && k >= 1 && k <= geoD.DesignNames.Count &&
                    double.TryParse(toks[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    _deldes[k - 1] = val;
                    _lsol = false;
                }
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("\n Current design parameter changes:\n\n    k   Parameter      change\n");
            for (int k = 0; k < geoD.DesignNames.Count; k++)
                sb.Append($"{k + 1,4}   {geoD.DesignNames[k].PadRight(12)}{FortranG(_deldes[k], 14, 5)}\n");
            return sb.ToString() + RenderOperMenu() + OperPrompt();
        }
        if (DISPLAY_COMMANDS.Contains(cmd))
        {
            if (!_lsol)
            {
                return "\n * Execute flow calculation first!\n" + RenderOperMenu() + OperPrompt();
            }
            if (arg.Length > 0)
            {
                // filename given inline -- persist the display output to it.
                _fs.WriteFile(arg, RenderDisplay(cmd));
                return RenderOperMenu() + OperPrompt();
            }
            _mode = Mode.OperGetFile;
            _pendingDisplay = cmd;
            return "\nEnter filename, or <return> for screen output   s>  ";
        }

        var geo = _geo;
        if (geo != null)
        {
            var varIdx = MatchVarKey(cmd, geo);
            if (varIdx != null)
            {
                var tokens = arg.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                string? conToken = tokens.Length > 0 ? tokens[0].ToUpperInvariant() : null;
                var con = conToken != null ? MatchConKey(conToken, geo) : null;
                if (con != null)
                {
                    var rc = CurrentRunCase();
                    double value = tokens.Length > 1 && double.TryParse(tokens[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pv)
                        ? pv
                        : (rc.Constraints.FirstOrDefault(c => c.FreeVar == varIdx.Value)?.Value ?? 0);
                    rc.Constraints = rc.Constraints.Where(c => c.FreeVar != varIdx.Value).ToList();
                    rc.Constraints.Add(new TrimConstraint { FreeVar = varIdx.Value, Kind = con.Value.kind, Value = value });
                    _lsol = false;
                }
                else if (conToken != null && double.TryParse(conToken, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var directVal))
                {
                    // "A 5.0" / "D1 10" style: a variable key followed by a bare number
                    // sets that variable's own direct constraint (alpha->alpha=5, etc.).
                    // This is how avl.exe's interactive sub-prompt works and how the
                    // WinForms alpha-sweep drives it (writes "a" then "a <value>").
                    var directKinds = new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ };
                    var kind = varIdx.Value < 5 ? directKinds[varIdx.Value] : ConstraintKind.Control;
                    var rc = CurrentRunCase();
                    rc.Constraints = rc.Constraints.Where(c => c.FreeVar != varIdx.Value).ToList();
                    rc.Constraints.Add(new TrimConstraint { FreeVar = varIdx.Value, Kind = kind, Value = directVal });
                    _lsol = false;
                }
                return RenderOperMenu() + OperPrompt();
            }
        }

        return "\n * Option not recognized\n" + RenderOperMenu() + OperPrompt();
    }

    private static RunCaseParams Cl2Params(RunCaseParams p) => new()
    {
        Alpha = p.Alpha, Beta = p.Beta, RotX = p.RotX, RotY = p.RotY, RotZ = p.RotZ,
        Cl = p.Cl, Cdo = p.Cdo, Bank = p.Bank, Elevation = p.Elevation, Heading = p.Heading,
        Mach = p.Mach, Velocity = p.Velocity, Density = p.Density, GravAcc = p.GravAcc,
        TurnRad = p.TurnRad, LoadFac = p.LoadFac, XCg = p.XCg, YCg = p.YCg, ZCg = p.ZCg,
        Mass = p.Mass, Ixx = p.Ixx, Iyy = p.Iyy, Izz = p.Izz, Ixy = p.Ixy, Iyz = p.Iyz, Izx = p.Izx,
    };

    private string HandleGetFile(string line)
    {
        string filename = line.Trim();
        string cmd = _pendingDisplay!;
        _pendingDisplay = null;
        _mode = Mode.Oper;

        if (cmd == "__SAVE__")
        {
            if (filename.Length > 0) _fs.WriteFile(filename, RunCaseWriter.Write(_geo!, _runCases));
            return RenderOperMenu() + OperPrompt();
        }
        if (cmd == "__FETCH__")
        {
            if (filename.Length > 0) FetchRunCases(filename);
            return RenderOperMenu() + OperPrompt();
        }

        if (filename.Length > 0)
        {
            _fs.WriteFile(filename, RenderDisplay(cmd));
            return RenderOperMenu() + OperPrompt();
        }
        return RenderDisplay(cmd) + RenderOperMenu() + OperPrompt();
    }

    private void FetchRunCases(string filename)
    {
        var content = _fs.ReadFile(filename);
        if (content == null) return;
        var parsed = RunCaseFile.ParseRunCaseFile(content, _geo!);
        _runCases = parsed.Count > 0 ? parsed : new List<RunCase> { DefaultRunCase(_geo!, 1) };
        _irun = 1;
        _lsol = false;
    }

    // ---- MODE (eigenvalue / dynamic-mode) analysis --------------------------------

    private static string ModePrompt() => " .MODE   c>  ";

    private static string ModeMenuText() =>
        "\n ==========================================================\n" +
        "  N ew eigenmode calculation\n" +
        "  W rite eigenvalues to file\n";

    private string HandleModeMenu(string line)
    {
        var (cmd, arg) = SplitCommand(line);
        if (cmd.Length == 0) { _mode = Mode.Top; return "\n" + TopPrompt(); }
        if (cmd == "?") return ModeMenuText() + ModePrompt();
        if (cmd == "N")
        {
            var (evals, err) = ComputeEigenvalues();
            if (err != null) return "\n " + err + "\n" + ModeMenuText() + ModePrompt();
            _eigenvalues = evals!;
            var sb = new System.Text.StringBuilder();
            sb.Append("\n Run case  1:  " + _lastTitle + "\n");
            foreach (var e in _eigenvalues)
                sb.Append("  mode:" + FortranG(e.Real, 14, 6) + FortranG(e.Imaginary, 14, 6) + "\n");
            return sb.ToString() + ModeMenuText() + ModePrompt();
        }
        if (cmd == "W")
        {
            if (arg.Length > 0) { _fs.WriteFile(arg, EigOut()); return ModeMenuText() + ModePrompt(); }
            _mode = Mode.ModeGetFile;
            return "\nEnter eigenvalue save filename   s>  ";
        }
        return "\n * Option not recognized\n" + ModeMenuText() + ModePrompt();
    }

    private string HandleModeGetFile(string line)
    {
        string filename = line.Trim();
        _mode = Mode.ModeMenu;
        if (filename.Length > 0) _fs.WriteFile(filename, EigOut());
        return ModeMenuText() + ModePrompt();
    }

    /// <summary>Build the 12x12 dynamics matrix from the current trimmed state + mass file
    /// and return its (tolerance-filtered) eigenvalues, or an error message.</summary>
    private (List<System.Numerics.Complex>? evals, string? err) ComputeEigenvalues()
    {
        if (_mass == null) return (null, "** Zero Mass.  Load a mass file (MASS) first.");
        if (_lastCtx == null || _lastResult == null || !_lsol) return (null, "** Execute flow calculation first (OPER, X).");
        var rc = CurrentRunCase();
        double vee = rc.Params.Velocity ?? 0;
        if (vee <= 0) return (null, "** Zero Velocity.  Specify with run/mass file or M menu.");

        var geo = _solveGeo ?? _geo!;
        var d = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, _lastCtx, _lastResult.Alfa, _lastResult.Vinf, _lastResult.Wrot, _lastResult.Delcon);
        var (amass, ainer) = SysMat.AppMass(geo, _lastCtx.EncResult, _mass.UnitL);

        var inertia = new double[3, 3];
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) inertia[i, j] = _mass.Inertia[i][j];

        double[][] cfU =
        {
            new[]{ d.Cx.U, d.Cx.V, d.Cx.W, d.Cx.P, d.Cx.Q, d.Cx.R },
            new[]{ d.Cy.U, d.Cy.V, d.Cy.W, d.Cy.P, d.Cy.Q, d.Cy.R },
            new[]{ d.Cz.U, d.Cz.V, d.Cz.W, d.Cz.P, d.Cz.Q, d.Cz.R },
        };
        double[][] cmU =
        {
            new[]{ d.Cl.U, d.Cl.V, d.Cl.W, d.Cl.P, d.Cl.Q, d.Cl.R },
            new[]{ d.Cm.U, d.Cm.V, d.Cm.W, d.Cm.P, d.Cm.Q, d.Cm.R },
            new[]{ d.Cn.U, d.Cn.V, d.Cn.W, d.Cn.P, d.Cn.Q, d.Cn.R },
        };

        var m = new ModeInputs
        {
            Sref = geo.Sref, Cref = geo.Cref, Bref = geo.Bref,
            Vee = vee, Rho = rc.Params.Density ?? _mass.Rho, Gee = rc.Params.GravAcc ?? _mass.G, Unitl = _mass.UnitL,
            Mass = rc.Params.Mass ?? _mass.Mass, Inertia = inertia,
            AlfaRad = _lastResult.Alfa, PhiDeg = rc.Params.Bank ?? 0, TheDeg = rc.Params.Elevation ?? 0, PsiDeg = rc.Params.Heading ?? 0,
            Vinf = _lastResult.Vinf, Wrot = _lastResult.Wrot,
            Cftot = _lastResult.Totals.CfTot, Cmtot = _lastResult.Totals.CmTot, CftotU = cfU, CmtotU = cmU,
            Amass = amass, Ainer = ainer,
        };

        var all = SysMat.Eigenvalues(m);
        double brefd = geo.Bref * _mass.UnitL;
        double etolsq = Math.Pow(1e-5 * vee / brefd, 2); // EIGSOL's ETOL filter
        var kept = all.Where(e => e.Real * e.Real + e.Imaginary * e.Imaginary >= etolsq).ToList();
        return (kept, null);
    }

    /// <summary>EIGOUT format (amode.f): header + one "run_index  real  imag" line per eigenvalue.</summary>
    private string EigOut()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("# " + (_geo?.Title ?? "") + "\n#\n#   Run case     Eigenvalue\n");
        foreach (var e in _eigenvalues)
            sb.Append(" " + FortranI(1, 7) + FortranG(e.Real, 18, 8) + FortranG(e.Imaginary, 18, 8) + "\n");
        return sb.ToString();
    }

    /// <summary>Ensures a run case has exactly one constraint per free variable (5 aero +
    /// one per control), in order -- padding any missing ones with sensible defaults and
    /// dropping any that refer to controls this geometry no longer has.</summary>
    private static void ReconcileConstraints(RunCase rc, Geometry geo)
    {
        int nvtot = 5 + geo.ControlNames.Count;
        var aeroKinds = new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ };
        var result = new List<TrimConstraint>(nvtot);
        for (int iv = 0; iv < nvtot; iv++)
        {
            var existing = rc.Constraints.FirstOrDefault(c => c.FreeVar == iv);
            result.Add(existing ?? new TrimConstraint { FreeVar = iv, Kind = iv < 5 ? aeroKinds[iv] : ConstraintKind.Control, Value = 0 });
        }
        rc.Constraints = result;
    }

    private string ExecuteCase()
    {
        // Apply any design-variable changes (DE) by re-paneling with perturbed incidences.
        var geo = DesignPerturbation.Apply(_geo!, _deldes);
        _solveGeo = geo;
        var rc = CurrentRunCase();
        // Reconcile the run case's constraints with this geometry's control count. A CASE
        // file saved for a different control layout (or an older configuration) can have
        // too few/many control constraints; AVL adapts, so pad missing ones and drop extras.
        ReconcileConstraints(rc, geo);
        var xyzref = EffectiveXyzref(rc);
        var overrides = new SolveContextOverrides { Mach = rc.Params.Mach, Xyzref = xyzref, Cdref = rc.Params.Cdo };

        string @out = "";
        if (!_aicBuilt)
        {
            @out += "   Building normalwash AIC matrix...\n";
            @out += "  Factoring normalwash AIC matrix...\n";
            @out += "  Building source+doublet strength AIC matrix...\n";
            @out += "  Building source+doublet velocity AIC matrix...\n";
            @out += "  Building bound-vortex velocity matrix...\n";
            _aicBuilt = true;
        }

        var trim = Trim.SolveTrimmedCase(geo, rc.Constraints, 30, overrides);
        _lastResult = trim.Result;
        _lastCtx = SolveCase.BuildSolveContext(geo, overrides);
        _lastXyzref = xyzref;
        _lastTitle = rc.Title;
        _lsol = trim.Converged;

        if (!trim.Converged)
        {
            @out += "\n ** Trim did not converge\n";
            return @out;
        }

        @out += "\n" + RenderDisplay("FT");
        return @out;
    }

    private string RenderDisplay(string cmd)
    {
        var geo = _solveGeo ?? _geo!;
        var result = _lastResult!;

        // CNC is always machine-readable; the MRF toggle switches the other display
        // commands to their ES23.15 machine-readable variants.
        if (cmd == "CNC") return OutMrf.MrfCnc(geo, result) + "\n";
        if (_useMrf)
        {
            switch (cmd)
            {
                case "FT": return OutMrf.MrfTot(geo, result, _lastXyzref, _lastTitle) + "\n";
                case "FN": return OutMrf.MrfSurf(geo, result) + "\n";
                case "FS": return OutMrf.MrfStrp(geo, result) + "\n";
                case "FE": return OutMrf.MrfEle(geo, result) + "\n";
                case "HM": return OutMrf.MrfHinge(geo, result.Totals) + "\n";
                case "VM": return OutMrf.MrfVm(geo, result) + "\n";
            }
        }

        switch (cmd)
        {
            case "FT":
                return OutTotal.FormatOutTotal(geo, result, _lastXyzref, _lastTitle) + "\n";
            case "FN":
                return OutSurface.FormatOutSurface(geo, result) + "\n";
            case "FS":
                return OutStrip.FormatOutStrip(geo, result) + "\n";
            case "FSB":
                return OutStrip.FormatOutStripBody(geo, result) + "\n";
            case "FE":
                return OutElement.FormatOutElement(geo, result) + "\n";
            case "HM":
                return OutHinge.FormatOutHinge(geo, result.Totals, result.Delcon) + "\n";
            case "ST":
            {
                var derivs = StabDerivsCalc.ComputeStabDerivs(geo, _lastCtx!, result.Alfa, result.Beta, result.Wrot, result.Delcon);
                return OutStability.FormatStabilityDerivs(geo, result, derivs) + "\n";
            }
            case "SM":
            {
                var derivs = StabDerivsCalc.ComputeStabDerivs(geo, _lastCtx!, result.Alfa, result.Beta, result.Wrot, result.Delcon, MomentAxis.Body);
                return OutStability.FormatStabilityDerivsBody(geo, result, derivs) + "\n";
            }
            case "SB":
            {
                var derivs = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, _lastCtx!, result.Alfa, result.Vinf, result.Wrot, result.Delcon);
                return OutStability.FormatStabilityDerivsGeom(geo, result, derivs) + "\n";
            }
            case "VM":
                return OutVm.FormatOutVm(geo, result) + "\n";
            case "FB":
                return OutBody.FormatOutBody(geo, result, _lastXyzref, _lastTitle) + "\n";
            default:
                return "";
        }
    }
}
