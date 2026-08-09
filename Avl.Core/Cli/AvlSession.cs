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

/// <summary>OPER-menu plot commands that avl.exe answers with a graphics window. The native
/// session recognizes them and raises <see cref="AvlSession.PlotRequested"/> so the host can
/// draw its own modern plot.</summary>
public enum AvlPlotKind
{
    Geometry, // "G" -- geometry plot
    Trefftz,  // "T" -- Trefftz-plane / spanwise-loading plot
    Modes,    // MODE menu "N" -- eigenvalue root-locus plot
    Loads,    // OPER "VM" (screen output) -- spanwise shear/bending-moment plot
    Fe,       // OPER "FE" (screen output) -- element-forces / chordwise dCp plot
}

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

    // aoper.f's WRITE(*,1052) menu, reproduced with its significant trailing spaces
    // (built via PadRight so a whitespace-trimming formatter can't silently break parity).
    private static readonly string OPER_MENU_BODY = BuildOperMenuBody();

    private static string BuildOperMenuBody()
    {
        var sb = new System.Text.StringBuilder();
        void L(string text, int width) => sb.Append(text.PadRight(width)).Append('\n');
        sb.Append('\n');
        L("  C1  set level or banked  horizontal flight constraints", 56);
        L("  C2  set steady pitch rate (looping) flight constraints", 56);
        L("  M odify parameters", 56);
        sb.Append('\n');
        L(" \"#\" select  run case          L ist defined run cases", 57);
        L("  +  add new run case          S ave run cases to file", 57);
        L("  -  delete  run case          F etch run cases from file", 57);
        L("  N ame current run case       W rite forces to file", 57);
        sb.Append('\n');
        L(" eX ecute run case             I nitialize variables", 57);
        sb.Append('\n');
        L("  G eometry plot               T refftz Plane plot", 57);
        sb.Append('\n');
        L("  ST  stability derivatives    FT  total   forces", 57);
        L("  SB  body-axis derivatives    FN  surface forces", 57);
        L("  RE  reference quantities     FS  strip   forces (FSB bodyaxes)", 64);
        L("  DE  design changes           FE  element forces", 57);
        L("  O ptions                     FB  body forces", 57);
        L("                               HM  hinge moments", 57);
        L("  OB offbody flow survey       VM  strip shear,moment", 57);
        L("  MRF  machine-readable format CPOM OML surface pressures", 56);
        return sb.ToString();
    }

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
    // Eigenvectors aligned with _eigenvalues (each length 12, state order JEU..JEPS),
    // stored so MODE 'N' can print amode.f EIGLST's mode-shape block like avl.exe.
    private List<System.Numerics.Complex[]> _eigenvectors = new();

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

    /// <summary>True when the session is sitting at the .OPER sub-menu prompt. The WinForms
    /// host uses this to recognize the OPER-menu "G" (geometry plot) command and pop its own
    /// interactive geometry window in place of avl.exe's (unported) graphics window.</summary>
    public bool InOperMenu => _mode == Mode.Oper;

    /// <summary>The resolved filename of the .avl the user last successfully LOADed (e.g. the
    /// exact "load x.avl" argument, with the ".avl" retry applied), and its raw text. The
    /// WinForms host uses these to render the OPER-menu "G" geometry plot from precisely what
    /// the console loaded, rather than guessing from the selected project. Null until a LOAD
    /// succeeds.</summary>
    public string? LoadedAvlName { get; private set; }
    public string? LoadedAvlText { get; private set; }

    /// <summary>Raised when the user issues an OPER-menu plot command (G = geometry, T = Trefftz)
    /// that avl.exe would answer with a graphics window. Avl.Core has no graphics, so it just
    /// recognizes the command (returning to the OPER prompt like avl.exe) and lets the host open
    /// its own modern plot in response -- keeping plot handling inside the console app rather than
    /// bolted onto the WinForms input box.</summary>
    public event Action<AvlPlotKind>? PlotRequested;

    /// <summary>Eigenvalues and matching eigenvectors from the most recent MODE "N" computation
    /// (aligned 1:1; vector state order JEU..JEPS). Exposed so the host can draw the root-locus
    /// plot from exactly what the console computed, without re-running the analysis.</summary>
    public IReadOnlyList<System.Numerics.Complex> LastEigenvalues => _eigenvalues;
    public IReadOnlyList<System.Numerics.Complex[]> LastEigenvectors => _eigenvectors;

    /// <summary>The text of the OPER display command that triggered the most recent Loads/Fe
    /// plot request (the same "VM"/"FE" strip-force output printed to the console). The host
    /// parses it to draw the plot -- no re-run.</summary>
    public string? LastPlotData { get; private set; }

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
    public string Start() => $"\n  Copyright (C) 2026 Dr. Afshin Rahimi - Native Port\n{TOP_BANNER}\n\n{TOP_MENU}\n{TopPrompt()}";

    // userio.f ASKC prints every prompt as FORMAT(/A,'   c>  ') -- a leading newline
    // (blank line) then the menu tag. Baking the "\n" in keeps that blank before each prompt.
    private static string TopPrompt() => "\n AVL   c>  ";

    private string OperPrompt() => $"\n .OPER (case {_irun}/{_runCases.Count})   c>  ";

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
                // ainput.f INPUT opens the name as-is; on failure it retries with ".avl"
                // appended (so "LOAD test" resolves to test.avl), echoing the same notices.
                string fname = arg;
                var content = _fs.ReadFile(fname);
                if (content == null)
                {
                    @out += $"\n ** Open error on file: {fname}\n";
                    fname = arg + ".avl";
                    @out += $"    Trying alternative: {fname}\n";
                    content = _fs.ReadFile(fname);
                }
                if (content == null)
                {
                    @out += $"\n ** Open error on file: {fname}\n";
                    @out += " ** File not processed. Current geometry may be corrupted.\n";
                    break;
                }
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
                LoadedAvlName = fname;
                LoadedAvlText = content;
                @out += $"\n Reading file: {fname}  ...\n";
                @out += $"\n Configuration: {Slice(_geo.Title, 60).PadRight(60)}\n";
                @out += BuildGeometryEcho();
                // ainput.f WRITE(*,2018)/(*,2019): Mach then the panel-count summary.
                @out += $"\n Mach ={_geo.Mach0,10:F4}\n\n";
                @out += $"{_geo.Bodies.Count,5} Bodies\n";
                @out += $"{_geo.Surfaces.Count,5} Solid surfaces\n";
                @out += $"{_geo.Strips.Count,5} Strips\n";
                @out += $"{_geo.Vortices.Count,5} Vortices\n";
                @out += $"\n{_geo.ControlNames.Count,5} Control variables\n";
                @out += $"{_geo.DesignNames.Count,5} Design parameters\n";
                @out += "\n Initializing run cases...\n";
                break;
            }
            case "MASS":
            {
                if (_geo == null) break;
                if (arg.Length == 0) { @out += " Enter mass filename: "; break; }
                // avl.exe's MASGET opens the name as-is only; here we mirror LOAD's ".avl"
                // convenience so "MASS x" resolves to x.mass, echoing the same retry notice.
                string fname = arg;
                var content = _fs.ReadFile(fname);
                if (content == null)
                {
                    @out += $"\n ** Open error on file: {fname}\n";
                    fname = arg + ".mass";
                    @out += $"    Trying alternative: {fname}\n";
                    content = _fs.ReadFile(fname);
                }
                if (content == null) { @out += $"\n ** File OPEN error:  {arg}\n"; break; }
                _mass = MassFileParser.ParseMassFile(content);
                @out += "\n Mass distribution read ...\n";
                @out += RenderMassBlock();
                break;
            }
            case "CASE":
            {
                if (_geo == null) break;
                if (arg.Length == 0) { @out += " Enter run case filename: "; break; }
                // avl.exe's RUNGET opens the name as-is only; here we mirror LOAD's ".avl"
                // convenience so "CASE x" resolves to x.run, echoing the same retry notice.
                string fname = arg;
                var content = _fs.ReadFile(fname);
                if (content == null)
                {
                    @out += $"\n ** Open error on file: {fname}\n";
                    fname = arg + ".run";
                    @out += $"    Trying alternative: {fname}\n";
                    content = _fs.ReadFile(fname);
                }
                if (content == null) { @out += $"\n ** File OPEN error:  {arg}\n"; break; }
                _runCases = RunCaseFile.ParseRunCaseFile(content, _geo);
                _irun = 1;
                _lsol = false;
                // avl.f WRITE(*,1025): //' Run cases read  ...' then 100(/1X,I4,': ',A)
                // where A is RTITLE (CHARACTER*40), plus a trailing blank line.
                @out += "\n\n Run cases read  ...\n";
                foreach (var rc in _runCases) @out += $" {rc.Index,4}: {Slice(rc.Title, 40).PadRight(40)}\n";
                @out += "\n";
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

    /// <summary>Reproduces ainput.f/amake.f's per-surface build echo emitted during LOAD:
    /// a blank line + "Building surface: NAME" for each defined surface (with a
    /// "Reading airfoil from file: X" line per AFIL section), and a "  " line +
    /// "Building duplicate image-surface: NAME (YDUP)" for each YDUPLICATE image. Surface
    /// titles are printed in AVL's fixed 40-char field; bodies follow (BTITLE, 40-char).</summary>
    private string BuildGeometryEcho()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in _geo!.Surfaces)
        {
            if (s.IsDuplicateImage)
            {
                sb.Append("  \n");
                sb.Append($"   Building duplicate image-surface: {s.Title.PadRight(40)}\n");
            }
            else
            {
                sb.Append('\n');
                sb.Append($"   Building surface: {s.Title.PadRight(40)}\n");
                foreach (var sec in s.Sections)
                    if (!string.IsNullOrEmpty(sec.AfileName))
                        sb.Append($"     Reading airfoil from file: {sec.AfileName}\n");
            }
        }
        foreach (var b in _geo.Bodies)
        {
            sb.Append('\n');
            sb.Append($"   Building body: {b.Title.PadRight(40)}\n");
        }
        return sb.ToString();
    }

    /// <summary>Reproduces avl.f's MASS command echo: MASSHO (mass, reference/CG points,
    /// inertia tensor) + the separator + APPSHO (apparent mass/inertia from the geometry,
    /// scaled by air density). Ports amass.f MASSHO/APPSHO/APPGET and their unit labels.</summary>
    private string RenderMassBlock()
    {
        var m = _mass!;
        var geo = _geo!;
        string unchM = m.UnitMName, unchL = m.UnitLName;
        string unchI = $"{unchM}-{unchL}^2";
        var sb = new System.Text.StringBuilder();

        // MASSHO: mass (in Munit then base units), reference & CG points, inertia tensor.
        sb.Append('\n');
        if (unchM != "Munit")
            sb.Append($" Mass        = {FortranG(m.Mass / m.UnitM, 12, 4)}  Munit\n");
        sb.Append($" Mass        = {FortranG(m.Mass, 12, 4)}  {unchM}\n");
        sb.Append('\n');
        sb.Append($" Ref. x,y,z  = {G3(geo.Xyzref0[0], geo.Xyzref0[1], geo.Xyzref0[2])}  Lunit\n");
        if (unchL != "Lunit")
            sb.Append($" C.G. x,y,z  = {G3(m.Cg[0] / m.UnitL, m.Cg[1] / m.UnitL, m.Cg[2] / m.UnitL)}  Lunit\n");
        sb.Append($" C.G. x,y,z  = {G3(m.Cg[0], m.Cg[1], m.Cg[2])}  {unchL}\n");
        sb.Append('\n');
        AppendInertiaRows(sb, "Ixx -Ixy -Ixz   | ", "     Iyy -Iyz = | ", "          Izz   | ",
            m.Inertia[0][0], m.Inertia[0][1], m.Inertia[0][2], m.Inertia[1][1], m.Inertia[1][2], m.Inertia[2][2], unchI);

        // APPGET + APPSHO: apparent mass/inertia from geometry (density-scaled).
        var enc = Encalc.Compute(geo);
        var (amass, ainer) = SysMat.AppMass(geo, enc, m.UnitL);
        double rho = m.Rho;
        sb.Append(" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -\n");
        sb.Append(" Apparent mass, inertia\n");
        sb.Append('\n');
        AppendInertiaRows(sb, "mxx  mxy  mxz   | ", "     myy  myz = | ", "          mzz   | ",
            amass[0, 0] * rho, amass[0, 1] * rho, amass[0, 2] * rho, amass[1, 1] * rho, amass[1, 2] * rho, amass[2, 2] * rho, unchM);
        sb.Append('\n');
        AppendInertiaRows(sb, "Ixx -Ixy -Ixz   | ", "     Iyy -Iyz = | ", "          Izz   | ",
            ainer[0, 0] * rho, ainer[0, 1] * rho, ainer[0, 2] * rho, ainer[1, 1] * rho, ainer[1, 2] * rho, ainer[2, 2] * rho, unchI);

        sb.Append('\n');
        sb.Append(" Use MSET to apply these mass,inertias to run cases\n");
        return sb.ToString();
    }

    private static string G3(double a, double b, double c) => FortranG(a, 12, 4) + FortranG(b, 12, 4) + FortranG(c, 12, 4);

    // amass.f MASSHO/APPSHO 3x3 tensor block (formats 1271/1272/1273): the first row ends
    // at '|', the middle row carries the unit label, the last row shows only the diagonal.
    private static void AppendInertiaRows(System.Text.StringBuilder sb, string l1, string l2, string l3,
        double xx, double xy, double xz, double yy, double yz, double zz, string unit)
    {
        sb.Append($" {l1}{FortranG(xx, 12, 4)}{FortranG(xy, 12, 4)}{FortranG(xz, 12, 4)}  |\n");
        sb.Append($" {l2}{new string(' ', 12)}{FortranG(yy, 12, 4)}{FortranG(yz, 12, 4)}  |  {unit}\n");
        sb.Append($" {l3}{new string(' ', 24)}{FortranG(zz, 12, 4)}  |\n");
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
            // aoper.f CONLST WRITE 10200: '  ',A,'  ->  ',A,'=',G12.4,1X,A -- the trailing
            // A is CHSS (CHARACTER*4, "  " padded to 4), the "**" clash marker we never set.
            s += $"  {varkey.PadRight(12)}  ->  {conName.PadRight(12)}={FortranG(value, 12, 4)}" + " " + "    " + "\n";
        }
        s += "  ------------      ------------------------\n";
        return s;
    }

    private static string Slice(string s, int len) => s.Length > len ? s.Substring(0, len) : s;

    private string RenderOperMenu()
    {
        var rc = CurrentRunCase();
        string s = $"\n Operation of run case {_irun}/{_runCases.Count}:  {Slice(rc.Title, 40).PadRight(40)}\n";
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
            return TopPrompt();
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
        if (cmd == "G")
        {
            // Geometry plot -- doesn't need a solution. avl.exe opens a graphics window and returns
            // to the OPER prompt; we recognize the command, signal the host to draw its own modern
            // plot, and return to the prompt.
            PlotRequested?.Invoke(AvlPlotKind.Geometry);
            return OperPrompt();
        }
        if (cmd == "T")
        {
            // Trefftz plot needs a converged solution. Match aplottp.f (PLOTTP): with no solution
            // it prints "*** No flow solution..." and waits back at the OPER prompt -- no plot.
            if (!_lsol)
                return "\n *** No flow solution...\n" + OperPrompt();
            PlotRequested?.Invoke(AvlPlotKind.Trefftz);
            return OperPrompt();
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
        // Screen output. VM/FE are strip-force data commands (not avl.exe graphics), but the host
        // can visualize them: expose the rendered text and signal a Loads/Fe plot. The text is
        // still printed to the console exactly as before, so this is purely additive.
        string disp = RenderDisplay(cmd);
        if (cmd == "VM" || cmd == "FE")
        {
            LastPlotData = disp;
            PlotRequested?.Invoke(cmd == "VM" ? AvlPlotKind.Loads : AvlPlotKind.Fe);
        }
        return disp + RenderOperMenu() + OperPrompt();
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

    private static string ModePrompt() => "\n .MODE   c>  ";

    private static string ModeMenuText() =>
        "\n ==========================================================\n" +
        "  N ew eigenmode calculation\n" +
        "  W rite eigenvalues to file\n";

    private string HandleModeMenu(string line)
    {
        var (cmd, arg) = SplitCommand(line);
        if (cmd.Length == 0) { _mode = Mode.Top; return TopPrompt(); }
        if (cmd == "?") return ModeMenuText() + ModePrompt();
        if (cmd == "N")
        {
            // amode.f (MODE 'N') calls EXEC unconditionally before EIGSOL -- avl.exe never
            // prompts "run flow calculation first", it just (re)solves the trim itself. Mirror
            // that: run the trim solution now if it hasn't been executed for this state. In the
            // GUI's OPER/X -> MODE/N flow _lsol is already set, so this adds no extra output.
            string pre = (!_lsol || _lastResult == null) ? ExecuteCase() : "";

            var (evals, evecs, err) = ComputeEigenvalues();
            if (err != null) return pre + err + ModeMenuText() + ModePrompt();
            _eigenvalues = evals!;
            _eigenvectors = evecs!;
            // avl.exe's MODE 'N' also pops the root-locus (PLEMAP) window. Signal the host to draw
            // its own modern root-locus plot from LastEigenvalues/LastEigenvectors.
            PlotRequested?.Invoke(AvlPlotKind.Modes);
            // amode.f: after EIGSOL, a blank line (WRITE(*,*)) then CALL EIGLST(6,IR).
            return pre + "\n" + EigLst() + ModeMenuText() + ModePrompt();
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
    /// and return its (tolerance-filtered) eigenvalues and matching eigenvectors, or an error
    /// message. Eigenvectors are aligned 1:1 with the eigenvalues (state order JEU..JEPS).</summary>
    private (List<System.Numerics.Complex>? evals, List<System.Numerics.Complex[]>? evecs, string? err) ComputeEigenvalues()
    {
        var rc = CurrentRunCase();
        // Port of amode.f EIGSOL's guard block (lines 844-875): reject the run case up-front
        // when velocity, mass, or any principal moment of inertia is non-positive, echoing the
        // same messages avl.exe prints and skipping the matrix build. This is not just an echo:
        // a zero inertia makes the mass/inertia tensor singular, so building + inverting it would
        // otherwise produce Inf/NaN and hang the eigenvalue balancer indefinitely.
        double vee = rc.Params.Velocity ?? 0;
        double rmass = rc.Params.Mass ?? _mass?.Mass ?? 0;
        double ixx = rc.Params.Ixx ?? _mass?.Inertia[0][0] ?? 0;
        double iyy = rc.Params.Iyy ?? _mass?.Inertia[1][1] ?? 0;
        double izz = rc.Params.Izz ?? _mass?.Inertia[2][2] ?? 0;

        var g = new System.Text.StringBuilder();
        if (vee <= 0.0) g.Append("\n ** Zero Velocity.  Specify with run file or M menu\n");
        if (rmass <= 0.0) g.Append("\n ** Zero Mass.  Specify with mass file or M menu\n");
        if (ixx <= 0.0) g.Append("\n ** Zero Ixx.  Specify with mass file or M menu\n");
        if (iyy <= 0.0) g.Append("\n ** Zero Iyy.  Specify with mass file or M menu\n");
        if (izz <= 0.0) g.Append("\n ** Zero Izz.  Specify with mass file or M menu\n");
        if (_mass == null && g.Length == 0) g.Append("\n ** Zero Mass.  Specify with mass file or M menu\n");
        if (g.Length > 0)
        {
            g.Append("\n Eigenmodes not computed for run case " + _irun + "\n");
            return (null, null, g.ToString());
        }

        // MODE 'N' runs EXEC first (see HandleModeMenu), so a trimmed state normally exists.
        // If it somehow doesn't (e.g. the trim failed), fall back to avl.exe's guidance rather
        // than dereferencing a null state.
        if (_lastCtx == null || _lastResult == null || _mass == null)
            return (null, null, "\n ** Execute flow calculation first (OPER, X)\n");

        var geo = _solveGeo ?? _geo!;
        var d = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, _lastCtx, _lastResult.Alfa, _lastResult.Vinf, _lastResult.Wrot, _lastResult.Delcon);

        // Apparent (added) mass/inertia is only folded in when a real mass distribution was read:
        // avl.f calls APPGET (which fills AMASS/AINER) exclusively after a successful MASGET, and
        // leaves both tensors zero otherwise (empty/absent .mass -> "Internal mass defaults used").
        // e4 has an empty .mass but a .run that supplies mass/inertia, so avl.exe carries zero
        // apparent mass here; including it (ainer ~ 29 >> the run-case Ixx = 1) would swamp the real
        // modes. Gate on the mass FILE's own mass, not the run-case mass, to match that.
        var (amass, ainer) = _mass.Mass > 0.0
            ? SysMat.AppMass(geo, _lastCtx.EncResult, _mass.UnitL)
            : (new double[3, 3], new double[3, 3]);

        // Inertia tensor from the RUN CASE parameters (amode.f EIGSOL reads PARVAL(IPIXX..IPIZX,IR)),
        // falling back to the mass file only where the run case doesn't specify a value. This matters
        // when a .run carries its own mass/inertia but the .mass file is empty/absent (e.g. e4): using
        // the mass file's zeros here would build a singular tensor that avl.exe never sees. The run-case
        // products of inertia are stored negated relative to the tensor (see MSET), so negate them back.
        var inertia = new double[3, 3];
        inertia[0, 0] = ixx; inertia[1, 1] = iyy; inertia[2, 2] = izz;
        double ixyT = rc.Params.Ixy is double pxy ? -pxy : _mass.Inertia[0][1];
        double iyzT = rc.Params.Iyz is double pyz ? -pyz : _mass.Inertia[1][2];
        double izxT = rc.Params.Izx is double pzx ? -pzx : _mass.Inertia[0][2];
        inertia[0, 1] = inertia[1, 0] = ixyT;
        inertia[1, 2] = inertia[2, 1] = iyzT;
        inertia[0, 2] = inertia[2, 0] = izxT;

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

        Avl.Core.Solver.EigenResult sys;
        try
        {
            // EIGSOL calls EISPACK RG with ICALC=1 (eigenvalues AND eigenvectors); EigenSystem is
            // the same RG stand-in and stores conjugate pairs the same way, so the mode shapes match.
            sys = Avl.Core.Solver.Eigen.EigenSystem(SysMat.Build(m), 12);
        }
        catch (Exception)
        {
            // Backstop for any residual singular/non-finite system the zero-inertia guards above
            // don't catch (Eigen throws rather than looping forever on a non-finite matrix).
            return (null, null, "\n ** Eigenvalue calculation failed (singular system)\n" +
                          "\n Eigenmodes not computed for run case " + _irun + "\n");
        }
        double brefd = geo.Bref * _mass.UnitL;
        double etolsq = Math.Pow(1e-5 * vee / brefd, 2); // EIGSOL's ETOL filter
        // Keep eigenvalues (and their eigenvectors) above the ETOL magnitude cut, preserving RG's
        // order exactly as EIGSOL does (it stores WR(J)/WVEC(:,J) in solver order, no sorting).
        var evals = new List<System.Numerics.Complex>();
        var evecs = new List<System.Numerics.Complex[]>();
        for (int j = 0; j < sys.Values.Length; j++)
        {
            var e = sys.Values[j];
            if (e.Real * e.Real + e.Imaginary * e.Imaginary < etolsq) continue;
            evals.Add(e);
            evecs.Add(sys.Vectors[j]);
        }
        return (evals, evecs, null);
    }

    /// <summary>Port of amode.f EIGLST (formats 3100/3200/3300): the run-case header, then for each
    /// mode " mode J:" with its eigenvalue (2G14.6) followed by the four state-triple rows of the
    /// (complex) eigenvector -- u/v/x, w/p/y, q/r/z, the/phi/psi -- exactly as avl.exe prints them.</summary>
    private string EigLst()
    {
        var sb = new System.Text.StringBuilder();
        // 3100 FORMAT(/1X,'Run case',I3,':  ',A) -- RTITLE is CHARACTER*40.
        sb.Append("\n Run case" + FortranI(_irun, 3) + ":  " + Slice(_lastTitle, 40).PadRight(40) + "\n");

        // State index (0-based, AINDEX.INC order used by SysMat): u,w,q,the,v,p,r,phi,x,y,z,psi.
        const int JEU = 0, JEW = 1, JEQ = 2, JETH = 3, JEV = 4, JEP = 5, JER = 6, JEPH = 7, JEX = 8, JEY = 9, JEZ = 10, JEPS = 11;

        for (int k = 0; k < _eigenvalues.Count; k++)
        {
            var e = _eigenvalues[k];
            var v = _eigenvectors.Count > k ? _eigenvectors[k] : new System.Numerics.Complex[12];
            // 3200 FORMAT(/1X,' mode',I2,':', 2G14.6) -- 1X plus the literal's own leading space
            // gives two spaces before "mode".
            sb.Append("\n  mode" + FortranI(k + 1, 2) + ":" + FortranG(e.Real, 14, 6) + FortranG(e.Imaginary, 14, 6) + "\n");
            sb.Append(EigVecRow("u  ", v[JEU], "v  ", v[JEV], "x  ", v[JEX]));
            sb.Append(EigVecRow("w  ", v[JEW], "p  ", v[JEP], "y  ", v[JEY]));
            sb.Append(EigVecRow("q  ", v[JEQ], "r  ", v[JER], "z  ", v[JEZ]));
            sb.Append(EigVecRow("the", v[JETH], "phi", v[JEPH], "psi", v[JEPS]));
        }
        return sb.ToString();
    }

    // amode.f 3300 FORMAT(1X, A,':', 2F11.4, 6X, A,':', 2F11.4, 6X, A,':', 2G12.4): the first two
    // state columns print re/im as F11.4, the third (position/heading) column as G12.4.
    private static string EigVecRow(string l1, System.Numerics.Complex c1, string l2, System.Numerics.Complex c2, string l3, System.Numerics.Complex c3)
        => " " + l1 + ":" + FortranF(c1.Real, 11, 4) + FortranF(c1.Imaginary, 11, 4)
         + "      " + l2 + ":" + FortranF(c2.Real, 11, 4) + FortranF(c2.Imaginary, 11, 4)
         + "      " + l3 + ":" + FortranG(c3.Real, 12, 4) + FortranG(c3.Imaginary, 12, 4) + "\n";

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
            @out += "  Building normalwash AIC matrix...\n";
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

        // aoper.f EXEC's per-iteration Newton-delta trace (INFO>=1).
        @out += BuildTrimTrace(geo, trim.IterDeltas);

        if (!trim.Converged)
        {
            @out += "\n ** Trim did not converge\n";
            return @out;
        }

        @out += RenderDisplay("FT");
        return @out;
    }

    /// <summary>Port of aoper.f EXEC's Newton-delta trace (WRITE 1902 header + 1905 rows):
    /// a blank line, the "iter d(alpha) ... controls" header, then one E11.3 row per
    /// iteration. Empty when the case was already fully constrained (no iterations ran).</summary>
    private static string BuildTrimTrace(Geometry geo, IReadOnlyList<double[]> deltas)
    {
        if (deltas.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        sb.Append('\n');
        sb.Append(" iter d(alpha)   d(beta)    d(pb/2V)   d(qc/2V)   d(rb/2V)   ");
        foreach (var name in geo.ControlNames)
            sb.Append(name.Length >= 11 ? name.Substring(0, 11) : name.PadRight(11));
        sb.Append('\n');
        int iter = 1;
        foreach (var row in deltas)
        {
            sb.Append(FortranI(iter, 4));
            foreach (var v in row) sb.Append(FortranE(v, 11, 3));
            sb.Append('\n');
            iter++;
        }
        return sb.ToString();
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
